using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Oracle.ManagedDataAccess.Client;
using BI_TICKETING_SYSTEM.Helpers;

namespace BI_TICKETING_SYSTEM
{
    public class DashboardHandler : IHttpHandler, System.Web.SessionState.IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            try
            {
                string mode = (context.Request["mode"] ?? string.Empty).ToLower();
                string role = (context.Session["UserRole"] ?? string.Empty).ToString().ToLower();

                if (mode == "monthly")
                {
                    if (role != "admin")
                    {
                        context.Response.StatusCode = 403;
                        context.Response.Write("{\"error\":\"Unauthorized\"}");
                        return;
                    }

                    WriteMonthlyChart(context);
                    return;
                }

                WriteDashboardStats(context);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DashboardHandler Error: " + ex.Message);
                context.Response.Write(
                    "{\"total\":0,\"resolved\":0,\"overdue\":0,\"dueToday\":0,\"open\":0,\"inProgress\":0,\"closed\":0}");
            }
        }

        private void WriteDashboardStats(HttpContext context)
        {
            int total = 0, resolved = 0, overdue = 0, dueToday = 0,
                open = 0, inProgress = 0, closed = 0;

            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                total = GetScalar(conn, "SELECT COUNT(*) FROM BI_OJT.TICKETS");

                resolved = GetScalar(conn,
                    "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'RESOLVED'");

                overdue = GetScalar(conn,
                    @"SELECT COUNT(*)
                      FROM BI_OJT.TICKETS
                      WHERE DUE_DATE IS NOT NULL
                        AND TRUNC(DUE_DATE) < TRUNC(SYSDATE)
                        AND UPPER(STATUS) NOT IN ('RESOLVED', 'CLOSED')");

                dueToday = GetScalar(conn,
                    @"SELECT COUNT(*)
                      FROM BI_OJT.TICKETS
                      WHERE DUE_DATE IS NOT NULL
                        AND TRUNC(DUE_DATE) = TRUNC(SYSDATE)
                        AND UPPER(STATUS) NOT IN ('RESOLVED', 'CLOSED')");

                open = GetScalar(conn,
                    "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'NEW'");

                inProgress = GetScalar(conn,
                    "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'IN PROGRESS'");

                closed = GetScalar(conn,
                    "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'CLOSED'");
            }

            context.Response.Write(string.Format(
                "{{\"total\":{0},\"resolved\":{1},\"overdue\":{2},\"dueToday\":{3},\"open\":{4},\"inProgress\":{5},\"closed\":{6}}}",
                total, resolved, overdue, dueToday, open, inProgress, closed));
        }

        private void WriteMonthlyChart(HttpContext context)
        {
            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"
                    SELECT 
                        TO_CHAR(TRUNC(CREATED_AT, 'MM'), 'Mon YYYY') AS MONTH_LABEL,
                        COUNT(*) AS TICKET_COUNT
                    FROM BI_OJT.TICKETS
                    WHERE CREATED_AT >= ADD_MONTHS(TRUNC(SYSDATE, 'MM'), -11)
                    GROUP BY TRUNC(CREATED_AT, 'MM')
                    ORDER BY TRUNC(CREATED_AT, 'MM')";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    List<string> labels = new List<string>();
                    List<int> values = new List<int>();

                    while (reader.Read())
                    {
                        labels.Add(reader["MONTH_LABEL"].ToString());
                        values.Add(Convert.ToInt32(reader["TICKET_COUNT"]));
                    }

                    string json = "{\"labels\":[" +
                                  string.Join(",", labels.Select(x => "\"" + x + "\"")) +
                                  "],\"values\":[" +
                                  string.Join(",", values) +
                                  "]}";

                    context.Response.Write(json);
                }
            }
        }

        private int GetScalar(OracleConnection conn, string sql)
        {
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        public bool IsReusable => false;
    }
}