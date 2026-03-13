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
            DataTable dtRaw = new DataTable();

            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                string query = @"
                                SELECT 
                                    U.FULL_NAME,
                                    A.ACTION,
                                    A.TABLE_NAME,
                                    A.TICKET_ID,
                                    A.OLD_VALUE,
                                    A.NEW_VALUE,
                                    A.CREATED_AT
                                FROM AUDIT_LOGS A
                                JOIN USERS U ON A.USER_ID = U.USER_ID
                                WHERE 1=1";

                if (!string.IsNullOrEmpty(ddlUser.SelectedValue))
                    query += " AND U.USER_ID = :UserId";

                if (!string.IsNullOrEmpty(ddlAction.SelectedValue))
                    query += " AND A.ACTION = :Action";

                if (!string.IsNullOrEmpty(txtDateFrom.Text))
                    query += " AND A.CREATED_AT >= :DateFrom";

                if (!string.IsNullOrEmpty(txtDateTo.Text))
                    query += " AND A.CREATED_AT <= :DateTo";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(ddlUser.SelectedValue))
                        cmd.Parameters.Add("UserId", OracleDbType.Int32).Value = Convert.ToInt32(ddlUser.SelectedValue);

                    if (!string.IsNullOrEmpty(ddlAction.SelectedValue))
                        cmd.Parameters.Add("Action", OracleDbType.Varchar2).Value = ddlAction.SelectedValue;

                    if (!string.IsNullOrEmpty(txtDateFrom.Text) && DateTime.TryParse(txtDateFrom.Text, out DateTime dtFrom))
                        cmd.Parameters.Add("DateFrom", OracleDbType.Date).Value = dtFrom;

                    if (!string.IsNullOrEmpty(txtDateTo.Text) && DateTime.TryParse(txtDateTo.Text, out DateTime dtTo))
                        cmd.Parameters.Add("DateTo", OracleDbType.Date).Value = dtTo;

                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    da.Fill(dtRaw);
                }
            }

            // Collect referenced IDs
            var userIds = new HashSet<int>();
            var ticketIds = new HashSet<int>();

            foreach (DataRow row in dtRaw.Rows)
            {
                TryCollectUserIdsFromJson(Convert.ToString(row["OLD_VALUE"]), userIds);
                TryCollectUserIdsFromJson(Convert.ToString(row["NEW_VALUE"]), userIds);

                if (row["TABLE_NAME"]?.ToString() == "TICKETS" && row["TICKET_ID"] != DBNull.Value)
                    ticketIds.Add(Convert.ToInt32(row["TICKET_ID"]));
            }

            var userMap = GetUserMap(userIds);
            var ticketMap = GetTicketMap(ticketIds);

            DataTable dtDisplay = dtRaw.Clone();
            dtDisplay.Columns.Add("ACTION_TEXT", typeof(string));

            foreach (DataRow row in dtRaw.Rows)
            {
                string action = Convert.ToString(row["ACTION"]);
                int? ticketId = row["TICKET_ID"] != DBNull.Value ? Convert.ToInt32(row["TICKET_ID"]) : (int?)null;

                string ticketNumber = ticketId.HasValue && ticketMap.TryGetValue(ticketId.Value, out var num)
                    ? num
                    : $"#{ticketId}";

                string oldJson = Convert.ToString(row["OLD_VALUE"]);
                string newJson = Convert.ToString(row["NEW_VALUE"]);

                JObject oldObj = TryParseJson(oldJson);
                JObject newObj = TryParseJson(newJson);

                if (string.Equals(action, "LOGIN", StringComparison.OrdinalIgnoreCase))
                {
                    AddLogEntry(dtDisplay, row, $"{row["FULL_NAME"]} logged into the system.");
                    continue;
                }

                if (row["TABLE_NAME"]?.ToString() == "TICKETS")
                {
                    bool split = false;

                    // Status change
                    string oldStatus = oldObj?["STATUS"]?.ToString();
                    string newStatus = newObj?["STATUS"]?.ToString();
                    if (oldStatus != newStatus)
                    {
                        AddLogEntry(dtDisplay, row,
                            $"Ticket {ticketNumber}: Status changed from {oldStatus ?? "-"} to {newStatus ?? "-"}");
                        split = true;
                    }

                    // Assignment change
                    int? oldUser = oldObj?["ASSIGNED_TO_USER_ID"]?.Value<int?>();
                    int? newUser = newObj?["ASSIGNED_TO_USER_ID"]?.Value<int?>();

                    if (oldUser != newUser)
                    {
                        AddLogEntry(dtDisplay, row,
                            $"Ticket {ticketNumber}: Assigned to changed from {MapUserName(oldUser, userMap)} to {MapUserName(newUser, userMap)}");
                        split = true;
                    }

                    // Priority change
                    string oldPriority = oldObj?["PRIORITY"]?.ToString();
                    string newPriority = newObj?["PRIORITY"]?.ToString();

                    if (oldPriority != newPriority)
                    {
                        AddLogEntry(dtDisplay, row,
                            $"Ticket {ticketNumber}: Priority changed from {oldPriority ?? "-"} to {newPriority ?? "-"}");
                        split = true;
                    }

                    if (!split)
                        AddLogEntry(dtDisplay, row,
                            $"{row["FULL_NAME"]} performed {action} on Ticket {ticketNumber}");
                }
                else
                {
                    AddLogEntry(dtDisplay, row,
                        $"{row["FULL_NAME"]} performed {action}.");
                }
            }

            DataView dv = dtDisplay.DefaultView;
            dv.Sort = !string.IsNullOrEmpty(sortExpression) ? sortExpression : "CREATED_AT DESC";

            gvAuditLogs.DataSource = dv;
            gvAuditLogs.DataBind();
        }

        // Support Method: Clones original row data and injects the specific activity text
        private void AddLogEntry(DataTable target, DataRow source, string message)
        {
            DataRow newRow = target.NewRow();
            newRow.ItemArray = source.ItemArray;
            newRow["ACTION_TEXT"] = message;
            target.Rows.Add(newRow);
        }

        // Helper: Fetches Ticket Numbers for IDs found in Logs
        private Dictionary<int, string> GetTicketMap(HashSet<int> ids)
        {
            var map = new Dictionary<int, string>();
            if (ids.Count == 0) return map;

            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                conn.Open();
                string sql = $"SELECT TICKET_ID, TICKET_NUMBER FROM BI_OJT.TICKETS WHERE TICKET_ID IN ({string.Join(",", ids)})";
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                using (OracleDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) map[Convert.ToInt32(rdr["TICKET_ID"])] = rdr["TICKET_NUMBER"].ToString();
                }
            }
            return map;
        }

        // Helper: Fetches User Names for IDs found in JSON
        private Dictionary<int, string> GetUserMap(HashSet<int> ids)
        {
            var map = new Dictionary<int, string>();
            if (ids.Count == 0) return map;

            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                conn.Open();
                string sql = $"SELECT USER_ID, FULL_NAME FROM BI_OJT.USERS WHERE USER_ID IN ({string.Join(",", ids)})";
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                using (OracleDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read()) map[Convert.ToInt32(rdr["USER_ID"])] = rdr["FULL_NAME"].ToString();
                }
            }
            return map;
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