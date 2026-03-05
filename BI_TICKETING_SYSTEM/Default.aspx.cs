using System;
using System.Web.UI;
using Oracle.ManagedDataAccess.Client;
using BI_TICKETING_SYSTEM.Helpers;

namespace BI_TICKETING_SYSTEM
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserName"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            string username = Session["UserName"]?.ToString() ?? "User";
            string role = Session["UserRole"]?.ToString() ?? "User";
            lblWelcomeUser.Text = username;

            if (role.ToLower() == "admin")
                pnlAdminActions.Visible = true;

            LoadDashboardStats();
        }

        private void LoadDashboardStats()
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    lblTotalTickets.Text = GetScalar(conn,
                        "SELECT COUNT(*) FROM BI_OJT.TICKETS").ToString();

                    lblResolved.Text = GetScalar(conn,
                        "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'RESOLVED'").ToString();

                    lblPending.Text = GetScalar(conn,
                        "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'PENDING'").ToString();

                    lblOverdue.Text = GetScalar(conn,
                        @"SELECT COUNT(*) FROM BI_OJT.TICKETS 
                          WHERE UPPER(STATUS) NOT IN ('RESOLVED','CLOSED') 
                          AND CREATED_AT < SYSDATE - 7").ToString();

                    lblOpenCount.Text = GetScalar(conn,
                        "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'OPEN'").ToString();

                    lblInProgressCount.Text = GetScalar(conn,
                        "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'IN PROGRESS'").ToString();

                    lblClosedCount.Text = GetScalar(conn,
                        "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'CLOSED'").ToString();
                }
            }
            catch (Exception ex)
            {
                lblTotalTickets.Text = "0";
                lblResolved.Text = "0";
                lblPending.Text = "0";
                lblOverdue.Text = "0";
                lblOpenCount.Text = "0";
                lblInProgressCount.Text = "0";
                lblClosedCount.Text = "0";

                System.Diagnostics.Debug.WriteLine("Dashboard DB Error: " + ex.Message);
            }
        }

        private int GetScalar(OracleConnection conn, string sql)
        {
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                return result == null || result == System.DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }
    }
}