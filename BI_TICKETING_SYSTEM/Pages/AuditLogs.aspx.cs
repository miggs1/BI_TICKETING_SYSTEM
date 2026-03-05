using BI_TICKETING_SYSTEM.Helpers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;

namespace BI_TICKETING_SYSTEM.Pages
{
    public partial class AuditLogs : System.Web.UI.Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadAuditLogs();
            }
        }

        private void LoadAuditLogs()
        {
            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"
                    SELECT 
                        A.AUDIT_ID,
                        U.FULL_NAME,
                        A.ACTION,
                        A.OLD_VALUE,
                        A.NEW_VALUE,
                        A.CREATED_AT
                    FROM BI_OJT.AUDIT_LOGS A
                    JOIN BI_OJT.USERS U ON A.USER_ID = U.USER_ID
                    ORDER BY A.CREATED_AT DESC";

                OracleDataAdapter da = new OracleDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                dt.Columns.Add("MESSAGE");

                foreach (DataRow row in dt.Rows)
                {
                    string user = row["FULL_NAME"].ToString();
                    string action = row["ACTION"].ToString();
                    string oldVal = row["OLD_VALUE"]?.ToString();
                    string newVal = row["NEW_VALUE"]?.ToString();

                    string message = "";

                    switch (action)
                    {
                        case "LOGIN":
                            message = $"{user} logged in.";
                            break;

                        case "ASSIGN_TICKET":
                            message = $"{user} assigned ticket from {oldVal} to {newVal}.";
                            break;

                        case "STATUS_CHANGE":
                            message = $"{user} changed ticket status from \"{oldVal}\" to \"{newVal}\".";
                            break;

                        default:
                            message = $"{user} performed action: {action}.";
                            break;
                    }

                    row["MESSAGE"] = message;
                }

                rptAuditLogs.DataSource = dt;
                rptAuditLogs.DataBind();
            }
        }
    }
}