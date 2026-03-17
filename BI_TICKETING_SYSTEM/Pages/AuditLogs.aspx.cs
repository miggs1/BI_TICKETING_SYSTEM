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
                // 1. Ensure all columns are selected
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
                    if (!string.IsNullOrEmpty(txtDateFrom.Text) && DateTime.TryParse(txtDateFrom.Text, out DateTime dFrom))
                        cmd.Parameters.Add("DateFrom", OracleDbType.Date).Value = dFrom;
                    if (!string.IsNullOrEmpty(txtDateTo.Text) && DateTime.TryParse(txtDateTo.Text, out DateTime dTo))
                        cmd.Parameters.Add("DateTo", OracleDbType.Date).Value = dTo;

                    new OracleDataAdapter(cmd).Fill(dtRaw);
                }
            }

            // 2. Build Maps
            var ticketIds = new HashSet<int>();
            var userIdsFromLog = new HashSet<int>();
            foreach (DataRow row in dtRaw.Rows)
            {
                if (row["TICKET_ID"] != DBNull.Value) ticketIds.Add(Convert.ToInt32(row["TICKET_ID"]));
                TryCollectUserIdsFromJson(Convert.ToString(row["OLD_VALUE"]), userIdsFromLog);
                TryCollectUserIdsFromJson(Convert.ToString(row["NEW_VALUE"]), userIdsFromLog);
            }

            var userMap = GetUserMap(userIdsFromLog);
            var ticketMap = GetTicketMap(ticketIds);

            // 3. Create Display Table
            DataTable dtDisplay = dtRaw.Clone();
            dtDisplay.Columns.Add("ACTION_TEXT", typeof(string));

            foreach (DataRow row in dtRaw.Rows)
            {
                string action = Convert.ToString(row["ACTION"]);
                string tableName = Convert.ToString(row["TABLE_NAME"]);
                string oldJson = Convert.ToString(row["OLD_VALUE"]);
                string newJson = Convert.ToString(row["NEW_VALUE"]);
                int? ticketId = row["TICKET_ID"] != DBNull.Value ? (int?)Convert.ToInt32(row["TICKET_ID"]) : null;

                if (tableName == "TICKETS")
                {
                    JObject oldObj = TryParseJson(oldJson);
                    JObject newObj = TryParseJson(newJson);
                    string ticketNumber = (ticketId.HasValue && ticketMap.TryGetValue(ticketId.Value, out var t)) ? t : $"#{ticketId}";
                    bool splitOccurred = false;

                    if (action.Contains("CREATE"))
                    {
                        AddLogEntry(dtDisplay, row, $"Ticket {ticketNumber} was created by {row["FULL_NAME"]}");
                    }
                    else
                    {

                        // STATUS CHANGE
                        string oldStatus = oldObj?["STATUS"]?.ToString();
                        string newStatus = newObj?["STATUS"]?.ToString();
                        if (oldStatus != newStatus)
                        {
                            AddLogEntry(dtDisplay, row, $"Ticket {ticketNumber}: Status changed from {oldStatus ?? "-"} to {newStatus ?? "-"}");
                            splitOccurred = true;
                        }

                        // ASSIGNMENT CHANGE
                        int? oldU = oldObj?["ASSIGNED_TO_USER_ID"]?.Value<int?>();
                        int? newU = newObj?["ASSIGNED_TO_USER_ID"]?.Value<int?>();
                        if (oldU != newU)
                        {
                            AddLogEntry(dtDisplay, row, $"Ticket {ticketNumber}: Assigned to changed from {MapUserName(oldU, userMap)} to {MapUserName(newU, userMap)}");
                            splitOccurred = true;
                        }

                        // REMARK ADDED
                        if (action == "ADD_REMARK")
                        {
                            string remarkSnippet = newObj?["REMARK_TEXT"]?.ToString();
                            if (remarkSnippet?.Length > 50) remarkSnippet = remarkSnippet.Substring(0, 47) + "...";
                            AddLogEntry(dtDisplay, row, $"Added Remark to Ticket {ticketNumber}: \"{remarkSnippet}\"");
                        }

                        // FALLBACK (Handles CREATE_TICKET, DELETE, or any other ticket action)
                        if (!splitOccurred)
                        {
                            string cleanAction = action.Replace("_", " ");
                            AddLogEntry(dtDisplay, row, $"{cleanAction}: Ticket {ticketNumber}");
                        }
                    }
                }
                else
                {
                    // Non-ticket actions (Login, etc.)
                    AddLogEntry(dtDisplay, row, $"{row["FULL_NAME"]} performed {action.Replace("_", " ")}");
                }
            }

            // 4. APPLY SORTING TO THE FINAL DISPLAY TABLE
            DataView dv = dtDisplay.DefaultView;
            if (!string.IsNullOrEmpty(sortExpression))
            {
                dv.Sort = sortExpression;
            }
            else
            {
                dv.Sort = $"{SortColumn} {SortDirection}";
            }

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
            if (SortColumn == e.SortExpression)
            {
                SortDirection = SortDirection == "ASC" ? "DESC" : "ASC";
            }
            else
            {
                SortColumn = e.SortExpression;
                SortDirection = "ASC";
            }

            LoadAuditLogs($"{SortColumn} {SortDirection}");
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            LoadAuditLogs();
        }

        private string SortDirection
        {
            get { return ViewState["SortDirection"] as string ?? "DESC"; }
            set { ViewState["SortDirection"] = value; }
        }

        private string SortColumn
        {
            get { return ViewState["SortColumn"] as string ?? "CREATED_AT"; }
            set { ViewState["SortColumn"] = value; }
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