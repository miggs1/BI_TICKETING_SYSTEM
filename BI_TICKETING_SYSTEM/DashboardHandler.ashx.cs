using System;
using System.Web;
using Oracle.ManagedDataAccess.Client;
using BI_TICKETING_SYSTEM.Helpers;

namespace BI_TICKETING_SYSTEM
{
    public class DashboardHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DashboardHandler Error: " + ex.Message);
                context.Response.Write(
                    "{\"total\":0,\"resolved\":0,\"overdue\":0,\"dueToday\":0,\"open\":0,\"inProgress\":0,\"closed\":0}");
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