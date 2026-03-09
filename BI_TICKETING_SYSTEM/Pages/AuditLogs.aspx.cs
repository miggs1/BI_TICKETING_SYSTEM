using BI_TICKETING_SYSTEM.Helpers;
using Oracle.ManagedDataAccess.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI.WebControls;
using System.Configuration;


namespace BI_TICKETING_SYSTEM.Pages
{
    public partial class AuditLogs : System.Web.UI.Page
    {
        private string connectionString;

        protected void Page_Init(object sender, EventArgs e)
        {
            connectionString = ConfigurationManager.ConnectionStrings["OracleDbConnection"]?.ConnectionString
                ?? throw new ConfigurationErrorsException("Missing OracleDbConnection in Web.config");
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadUsers();
                LoadAuditLogs();
            }
        }

        private void LoadAuditLogs(string sortExpression = "")
        {
            string query = @"
                            SELECT 
                                U.FULL_NAME,
                                A.ACTION,
                                A.OLD_VALUE,
                                A.NEW_VALUE,
                                A.CREATED_AT
                            FROM AUDIT_LOGS A
                            JOIN USERS U ON A.USER_ID = U.USER_ID
                            WHERE 1=1
                            ";

            if (!string.IsNullOrEmpty(ddlUser.SelectedValue))
                query += " AND U.USER_ID = :UserId";

            if (!string.IsNullOrEmpty(ddlAction.SelectedValue))
                query += " AND A.ACTION = :Action";

            if (!string.IsNullOrEmpty(txtDateFrom.Text))
                query += " AND A.CREATED_AT >= :DateFrom";

            if (!string.IsNullOrEmpty(txtDateTo.Text))
                query += " AND A.CREATED_AT <= :DateTo";

            if (!string.IsNullOrEmpty(sortExpression))
                query += " ORDER BY " + sortExpression;
            else
                query += " ORDER BY A.CREATED_AT DESC";

            DataTable dt = new DataTable();

            using (OracleConnection conn = new OracleConnection(connectionString))
            using (OracleCommand cmd = new OracleCommand(query, conn))
            {
                // Add parameters with appropriate types
                if (!string.IsNullOrEmpty(ddlUser.SelectedValue))
                    cmd.Parameters.Add("UserId", OracleDbType.Int32).Value = Convert.ToInt32(ddlUser.SelectedValue);

                if (!string.IsNullOrEmpty(ddlAction.SelectedValue))
                    cmd.Parameters.Add("Action", OracleDbType.Varchar2).Value = ddlAction.SelectedValue;

                if (!string.IsNullOrEmpty(txtDateFrom.Text))
                {
                    if (DateTime.TryParse(txtDateFrom.Text, out DateTime dtFrom))
                        cmd.Parameters.Add("DateFrom", OracleDbType.Date).Value = dtFrom;
                    else
                        cmd.Parameters.Add("DateFrom", OracleDbType.Varchar2).Value = txtDateFrom.Text;
                }

                if (!string.IsNullOrEmpty(txtDateTo.Text))
                {
                    if (DateTime.TryParse(txtDateTo.Text, out DateTime dtTo))
                        cmd.Parameters.Add("DateTo", OracleDbType.Date).Value = dtTo;
                    else
                        cmd.Parameters.Add("DateTo", OracleDbType.Varchar2).Value = txtDateTo.Text;
                }

                OracleDataAdapter da = new OracleDataAdapter(cmd);
                da.Fill(dt);
            }

            // Prepare user mapping for IDs found in JSON old/new
            var userIds = new HashSet<int>();
            var oldJsons = new List<string>(dt.Rows.Count);
            var newJsons = new List<string>(dt.Rows.Count);
            var ticketIds = new HashSet<int>(); // <- collect referenced ticket ids from ACTION context

            for (int r = 0; r < dt.Rows.Count; r++)
            {
                DataRow row = dt.Rows[r];
                string oldJson = Convert.ToString(row["OLD_VALUE"]);
                string newJson = Convert.ToString(row["NEW_VALUE"]);
                oldJsons.Add(oldJson);
                newJsons.Add(newJson);

                // collect user ids from JSON snapshots (existing behavior)
                TryCollectUserIdsFromJson(oldJson, userIds);
                TryCollectUserIdsFromJson(newJson, userIds);

                // parse ACTION that may have been stored as "ACTION|TABLE|ID"
                string actionRaw = Convert.ToString(row["ACTION"]);
                if (!string.IsNullOrEmpty(actionRaw) && actionRaw.Contains("|"))
                {
                    var parts = actionRaw.Split(new[] { '|' }, StringSplitOptions.None);
                    if (parts.Length >= 3)
                    {
                        // parts[1] is table name, parts[2] is record id
                        if (string.Equals(parts[1], "TICKETS", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(parts[2], out int tid))
                                ticketIds.Add(tid);
                        }
                    }
                }
            }

            var userMap = new Dictionary<int, string>();
            if (userIds.Count > 0)
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string sql = $"SELECT USER_ID, FULL_NAME FROM BI_OJT.USERS WHERE USER_ID IN ({string.Join(",", userIds)})";
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        using (OracleDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                int id = Convert.ToInt32(rdr["USER_ID"]);
                                string name = rdr["FULL_NAME"] == DBNull.Value ? "" : rdr["FULL_NAME"].ToString();
                                userMap[id] = name;
                            }
                        }
                    }
                }
            }

            // Fetch ticket numbers for any referenced ticket ids so we can show them in messages
            var ticketMap = new Dictionary<int, string>();
            if (ticketIds.Count > 0)
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string sql = $"SELECT TICKET_ID, TICKET_NUMBER FROM BI_OJT.TICKETS WHERE TICKET_ID IN ({string.Join(",", ticketIds)})";
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        using (OracleDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                int id = Convert.ToInt32(rdr["TICKET_ID"]);
                                string number = rdr["TICKET_NUMBER"] == DBNull.Value ? $"#{id}" : rdr["TICKET_NUMBER"].ToString();
                                ticketMap[id] = number;
                            }
                        }
                    }
                }
            }

            dt.Columns.Add("ACTION_TEXT");

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow row = dt.Rows[i];
                string actionRaw = Convert.ToString(row["ACTION"]);
                string baseAction = actionRaw;
                int? referencedRecordId = null;

                // If action uses the stored "action|table|id" format, extract base action and record id
                if (!string.IsNullOrEmpty(actionRaw) && actionRaw.Contains("|"))
                {
                    var parts = actionRaw.Split(new[] { '|' }, StringSplitOptions.None);
                    baseAction = parts[0];
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int rid))
                        referencedRecordId = rid;
                }

                string oldVal = oldJsons[i];
                string newVal = newJsons[i];

                // Default messages for non-ticket actions or fallback
                if (string.Equals(baseAction, "LOGIN", StringComparison.OrdinalIgnoreCase))
                {
                    row["ACTION_TEXT"] = $"{row["FULL_NAME"]} logged into the system.";
                    continue;
                }

                // If the action relates to tickets, try to parse JSON and show human-friendly differences
                if (baseAction != null && baseAction.EndsWith("_TICKET", StringComparison.OrdinalIgnoreCase))
                {
                    JObject oldObj = TryParseJson(oldVal);
                    JObject newObj = TryParseJson(newVal);
                    var changes = new List<string>();

                    // Status
                    string oldStatus = oldObj?["STATUS"]?.ToString();
                    string newStatus = newObj?["STATUS"]?.ToString();
                    if (!string.Equals(oldStatus, newStatus, StringComparison.Ordinal))
                    {
                        string ticketPrefix = referencedRecordId.HasValue && ticketMap.TryGetValue(referencedRecordId.Value, out var tnum)
                            ? $"Ticket {tnum}: "
                            : "";
                        changes.Add($"{ticketPrefix}Status changed from {(string.IsNullOrEmpty(oldStatus) ? "-" : oldStatus)} to {(string.IsNullOrEmpty(newStatus) ? "-" : newStatus)}.");
                    }

                    // Assigned to
                    int? oldAssigned = oldObj?["ASSIGNED_TO_USER_ID"]?.Value<int?>();
                    int? newAssigned = newObj?["ASSIGNED_TO_USER_ID"]?.Value<int?>();
                    if (oldAssigned != newAssigned)
                        changes.Add($"Assigned to changed from {MapUserName(oldAssigned, userMap)} to {MapUserName(newAssigned, userMap)}.");

                    // Priority
                    string oldPriority = oldObj?["PRIORITY"]?.ToString();
                    string newPriority = newObj?["PRIORITY"]?.ToString();
                    if (!string.Equals(oldPriority, newPriority, StringComparison.Ordinal))
                        changes.Add($"Priority changed from {(string.IsNullOrEmpty(oldPriority) ? "-" : oldPriority)} to {(string.IsNullOrEmpty(newPriority) ? "-" : newPriority)}.");

                    // Created by (useful for CREATE_TICKET)
                    int? createdBy = newObj?["CREATED_BY_USER_ID"]?.Value<int?>();
                    if (string.Equals(baseAction, "CREATE_TICKET", StringComparison.OrdinalIgnoreCase))
                    {
                        string ticketInfo = referencedRecordId.HasValue && ticketMap.TryGetValue(referencedRecordId.Value, out var tnum) ? $" ({tnum})" : ".";
                        changes.Add($"Ticket created by {MapUserName(createdBy, userMap)}{ticketInfo}");
                    }

                    if (string.Equals(baseAction, "DELETE_TICKET", StringComparison.OrdinalIgnoreCase))
                    {
                        // If deleted, show who deleted it (user is in U.FULL_NAME) and optionally created_by in old
                        changes.Add($"Ticket deleted by {row["FULL_NAME"]}");
                    }

                    row["ACTION_TEXT"] = changes.Count > 0 ? string.Join(" • ", changes) : $"{row["FULL_NAME"]} performed {baseAction}.";
                    continue;
                }

                // Generic fallback
                row["ACTION_TEXT"] = $"{row["FULL_NAME"]} performed {baseAction}.";
            }

            gvAuditLogs.DataSource = dt;
            gvAuditLogs.DataBind();
        }

        private static void TryCollectUserIdsFromJson(string json, HashSet<int> set)
        {
            if (string.IsNullOrEmpty(json)) return;
            json = json.Trim();
            if (!json.StartsWith("{")) return;

            try
            {
                var j = JObject.Parse(json);
                var tokens = new[] { "CREATED_BY_USER_ID", "ASSIGNED_TO_USER_ID" };
                foreach (var t in tokens)
                {
                    var v = j[t]?.Value<int?>();
                    if (v.HasValue) set.Add(v.Value);
                }
            }
            catch
            {
                // ignore malformed JSON
            }
        }

        private static JObject TryParseJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            json = json.Trim();
            if (!json.StartsWith("{")) return null;
            try { return JObject.Parse(json); } catch { return null; }
        }

        private static string MapUserName(int? userId, Dictionary<int, string> map)
        {
            if (!userId.HasValue) return "Unassigned";
            return map != null && map.TryGetValue(userId.Value, out var name) ? name : $"User #{userId.Value}";
        }

        protected void gvAuditLogs_Sorting(object sender, GridViewSortEventArgs e)
        {
            LoadAuditLogs(e.SortExpression);
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            LoadAuditLogs();
        }

        private void LoadUsers()
        {
            string query = "SELECT USER_ID, FULL_NAME FROM USERS";

            using (OracleConnection conn = new OracleConnection(connectionString))
            using (OracleCommand cmd = new OracleCommand(query, conn))
            {
                conn.Open();

                ddlUser.DataSource = cmd.ExecuteReader();
                ddlUser.DataTextField = "FULL_NAME";
                ddlUser.DataValueField = "USER_ID";
                ddlUser.DataBind();
            }

            ddlUser.Items.Insert(0, new ListItem("All Users", ""));
        }

    }
}