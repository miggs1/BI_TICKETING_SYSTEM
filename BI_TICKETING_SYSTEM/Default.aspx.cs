using System;
using System.Web.UI;
using Oracle.ManagedDataAccess.Client;
using BI_TICKETING_SYSTEM.Helpers;

namespace BI_TICKETING_SYSTEM
{
    public partial class _Default : Page
    {
        private string CurrentRole => Session["UserRole"]?.ToString() ?? "User";
        private int CurrentUserID => Convert.ToInt32(Session["UserID"] ?? 0);

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserName"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            string username = Session["UserName"]?.ToString() ?? "User";
            string role = CurrentRole.ToLower();

            // Welcome message
            lblWelcomeUser.Text = username;

            // Role badge
            switch (role)
            {
                case "admin":
                    lblRoleBadge.Text = "<span class='role-badge role-badge-admin'>Admin</span>";
                    lblWelcomeMessage.Text = "You are logged into the <strong>Bureau of Immigration Ticketing System</strong>. Here is the overall system statistics.";
                    pnlAdminActions.Visible = true;
                    pnlCreateTicketAction.Visible = true;
                    lblTotalLabel.Text = "Total Tickets";
                    break;
                case "support":
                    lblRoleBadge.Text = "<span class='role-badge role-badge-support'>Support</span>";
                    lblWelcomeMessage.Text = "Here are the statistics of tickets <strong>assigned to you</strong>.";
                    pnlAdminActions.Visible = false;
                    pnlCreateTicketAction.Visible = false;
                    lblTotalLabel.Text = "My Assigned Tickets";
                    break;
                default: // user
                    lblRoleBadge.Text = "<span class='role-badge role-badge-user'>User</span>";
                    lblWelcomeMessage.Text = "Here are the statistics of <strong>your submitted tickets</strong>.";
                    pnlAdminActions.Visible = false;
                    pnlCreateTicketAction.Visible = true;
                    lblTotalLabel.Text = "My Tickets";
                    break;
            }

            LoadDashboardStats();
        }

        private void LoadDashboardStats()
        {
            string role = CurrentRole.ToLower();
            int userId = CurrentUserID;

            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    if (role == "admin")
                    {
                        // ✅ Admin sees ALL tickets
                        lblTotalTickets.Text = GetScalar(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS").ToString();

                        lblResolved.Text = GetScalar(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'RESOLVED'").ToString();

                        lblPending.Text = GetScalar(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'NEW'").ToString();

                        lblOverdue.Text = GetScalar(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'ASSIGNED'").ToString();

                        lblOpenCount.Text = GetScalar(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'NEW'").ToString();

                        lblInProgressCount.Text = GetScalar(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'IN PROGRESS'").ToString();

                        lblClosedCount.Text = GetScalar(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE UPPER(STATUS) = 'CLOSED'").ToString();
                    }
                    else if (role == "support")
                    {
                        // ✅ Support sees only tickets ASSIGNED TO THEM
                        lblTotalTickets.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE ASSIGNED_TO_USER_ID = :userId",
                            userId).ToString();

                        lblResolved.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE ASSIGNED_TO_USER_ID = :userId AND UPPER(STATUS) = 'RESOLVED'",
                            userId).ToString();

                        lblPending.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE ASSIGNED_TO_USER_ID = :userId AND UPPER(STATUS) = 'NEW'",
                            userId).ToString();

                        lblOverdue.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE ASSIGNED_TO_USER_ID = :userId AND UPPER(STATUS) = 'ASSIGNED'",
                            userId).ToString();

                        lblOpenCount.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE ASSIGNED_TO_USER_ID = :userId AND UPPER(STATUS) = 'NEW'",
                            userId).ToString();

                        lblInProgressCount.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE ASSIGNED_TO_USER_ID = :userId AND UPPER(STATUS) = 'IN PROGRESS'",
                            userId).ToString();

                        lblClosedCount.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE ASSIGNED_TO_USER_ID = :userId AND UPPER(STATUS) = 'CLOSED'",
                            userId).ToString();
                    }
                    else
                    {
                        // ✅ User sees only THEIR OWN tickets
                        lblTotalTickets.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE CREATED_BY_USER_ID = :userId",
                            userId).ToString();

                        lblResolved.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE CREATED_BY_USER_ID = :userId AND UPPER(STATUS) = 'RESOLVED'",
                            userId).ToString();

                        lblPending.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE CREATED_BY_USER_ID = :userId AND UPPER(STATUS) = 'NEW'",
                            userId).ToString();

                        lblOverdue.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE CREATED_BY_USER_ID = :userId AND UPPER(STATUS) = 'ASSIGNED'",
                            userId).ToString();

                        lblOpenCount.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE CREATED_BY_USER_ID = :userId AND UPPER(STATUS) = 'NEW'",
                            userId).ToString();

                        lblInProgressCount.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE CREATED_BY_USER_ID = :userId AND UPPER(STATUS) = 'IN PROGRESS'",
                            userId).ToString();

                        lblClosedCount.Text = GetScalarWithParam(conn,
                            "SELECT COUNT(*) FROM BI_OJT.TICKETS WHERE CREATED_BY_USER_ID = :userId AND UPPER(STATUS) = 'CLOSED'",
                            userId).ToString();
                    }
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

        // ===== Without parameter =====
        private int GetScalar(OracleConnection conn, string sql)
        {
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        // ===== With userId parameter =====
        private int GetScalarWithParam(OracleConnection conn, string sql, int userId)
        {
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("userId", OracleDbType.Int32).Value = userId;
                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }
    }
}