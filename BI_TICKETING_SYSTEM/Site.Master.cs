using BI_TICKETING_SYSTEM.Helpers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Web.UI.WebControls;
using System.Linq;

namespace BI_TICKETING_SYSTEM
{
    public partial class SiteMaster : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.MinValue);

            // Check if user is logged in
            if (Session["UserName"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                // Set username in navbar and sidebar
                string username = Session["UserName"].ToString();
                string role = Session["UserRole"]?.ToString() ?? "User";

                lblUsername.Text = username;
                lblSidebarUser.Text = username;
                lblSidebarRole.Text = role;

                // Role-based menu visibility
                switch (role.ToLower())
                {
                    case "admin":
                        pnlTicketsMenu.Visible = true;
                        pnlAdminMenu.Visible = true;
                        pnlSupportMenu.Visible = true;
                        break;
                    case "support":
                        pnlTicketsMenu.Visible = false;
                        pnlAdminMenu.Visible = false;
                        pnlSupportMenu.Visible = true;
                        break;
                    default:
                        pnlTicketsMenu.Visible = true;
                        pnlAdminMenu.Visible = false;
                        pnlSupportMenu.Visible = false;
                        break;
                }
                CheckDueDateNotifications();
                LoadNotifications();
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            // Clear session
            Session.Clear();
            Session.Abandon();

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.MinValue);

            Response.Redirect("~/Login.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
        private void CheckDueDateNotifications()
        {
            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"
            SELECT 
                TICKET_ID,
                TICKET_NUMBER,
                CREATED_BY_USER_ID,
                ASSIGNED_TO_USER_ID,
                DUE_DATE,
                STATUS,
                NVL(DUE_SOON_NOTIFIED, 0) AS DUE_SOON_NOTIFIED,
                NVL(OVERDUE_NOTIFIED, 0) AS OVERDUE_NOTIFIED
            FROM BI_OJT.TICKETS
            WHERE DUE_DATE IS NOT NULL
              AND LOWER(STATUS) NOT IN ('resolved', 'closed')";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;

                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            int ticketId = Convert.ToInt32(dr["TICKET_ID"]);
                            string ticketNumber = dr["TICKET_NUMBER"].ToString();

                            int createdByUserId = dr["CREATED_BY_USER_ID"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(dr["CREATED_BY_USER_ID"]);

                            int assignedToUserId = dr["ASSIGNED_TO_USER_ID"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(dr["ASSIGNED_TO_USER_ID"]);

                            DateTime dueDate = Convert.ToDateTime(dr["DUE_DATE"]);
                            int dueSoonNotified = Convert.ToInt32(dr["DUE_SOON_NOTIFIED"]);
                            int overdueNotified = Convert.ToInt32(dr["OVERDUE_NOTIFIED"]);

                            TimeSpan remaining = dueDate - DateTime.Now;

                            List<int> recipients = new List<int>();

                            if (createdByUserId > 0)
                                recipients.Add(createdByUserId);

                            if (assignedToUserId > 0)
                                recipients.Add(assignedToUserId);

                            recipients.AddRange(GetActiveUserIdsByRole("admin", conn));

                            recipients = recipients
                                .Where(id => id > 0)
                                .Distinct()
                                .ToList();

                            if (remaining.TotalSeconds < 0 && overdueNotified == 0)
                            {
                                foreach (int userId in recipients)
                                {
                                    NotificationHelper.SendNotification(
                                        userId,
                                        "Ticket Overdue",
                                        $"Ticket {ticketNumber} is overdue. Due date was {dueDate:MMM dd, yyyy hh:mm tt}.",
                                        "~/Pages/Tickets.aspx",
                                        ticketId
                                    );
                                }

                                MarkTicketNotificationFlag(conn, ticketId, "OVERDUE_NOTIFIED");
                            }
                            else if (remaining.TotalHours <= 24 && remaining.TotalSeconds > 0 && dueSoonNotified == 0)
                            {
                                foreach (int userId in recipients)
                                {
                                    NotificationHelper.SendNotification(
                                        userId,
                                        "Due Date Approaching",
                                        $"Ticket {ticketNumber} is due on {dueDate:MMM dd, yyyy hh:mm tt}.",
                                        "~/Pages/Tickets.aspx",
                                        ticketId
                                    );
                                }

                                MarkTicketNotificationFlag(conn, ticketId, "DUE_SOON_NOTIFIED");
                            }
                        }
                    }
                }
            }
        }

        private List<int> GetActiveUserIdsByRole(string role, OracleConnection conn)
        {
            List<int> ids = new List<int>();

            string sql = @"
        SELECT USER_ID
        FROM BI_OJT.USERS
        WHERE LOWER(ROLE) = :role
          AND LOWER(STATUS) = 'active'";

            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("role", OracleDbType.Varchar2).Value = role.ToLower();

                using (OracleDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        ids.Add(Convert.ToInt32(dr["USER_ID"]));
                    }
                }
            }

            return ids;
        }

        private void MarkTicketNotificationFlag(OracleConnection conn, int ticketId, string flagColumn)
        {
            if (flagColumn != "DUE_SOON_NOTIFIED" && flagColumn != "OVERDUE_NOTIFIED")
                return;

            string sql = $@"
        UPDATE BI_OJT.TICKETS
        SET {flagColumn} = 1
        WHERE TICKET_ID = :ticketId";

            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                cmd.ExecuteNonQuery();
            }
        }
        private void LoadNotifications()
        {
            btnClearNotifications.Enabled = false;
            btnClearNotifications.Style["color"] = "#6c757d";

            if (Session["UserID"] == null)
            {
                lblNotifCount.Text = "";
                lblNotifCount.Visible = false;
                rptNotifications.DataSource = null;
                rptNotifications.DataBind();
                pnlNoNotifs.Visible = true;

                btnClearNotifications.Enabled = false;
                btnClearNotifications.Style["color"] = "#6c757d";

                return;
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string countSql = "SELECT COUNT(*) FROM BI_OJT.NOTIFICATIONS WHERE USER_ID = :userId AND IS_READ = 0";
                using (OracleCommand countCmd = new OracleCommand(countSql, conn))
                {
                    countCmd.BindByName = true;
                    countCmd.Parameters.Add("userId", OracleDbType.Int32).Value = userId;
                    int unreadCount = Convert.ToInt32(countCmd.ExecuteScalar());

                    lblNotifCount.Text = unreadCount > 0 ? unreadCount.ToString() : "";
                    lblNotifCount.Visible = unreadCount > 0;
                }

                string sql = @"
                    SELECT * FROM (
                        SELECT NOTIFICATION_ID, TITLE, MESSAGE, TICKET_ID, IS_READ, CREATED_AT, LINK_PAGE
                        FROM BI_OJT.NOTIFICATIONS
                        WHERE USER_ID = :userId
                        ORDER BY CREATED_AT DESC
                    )
                    WHERE ROWNUM <= 10";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("userId", OracleDbType.Int32).Value = userId;

                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        btnClearNotifications.Enabled = true;
                        btnClearNotifications.Style["color"] = "#007bff";
                        rptNotifications.DataSource = dt;
                        rptNotifications.DataBind();
                        pnlNoNotifs.Visible = false;
                    }
                    else
                    {
                        btnClearNotifications.Enabled = false;
                        btnClearNotifications.Style["color"] = "#6c757d";
                        rptNotifications.DataSource = null;
                        rptNotifications.DataBind();
                        pnlNoNotifs.Visible = true;
                    }
                }
            }
        }

        protected void btnClearNotifications_Click(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
                return;

            int userId = Convert.ToInt32(Session["UserID"]);

            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"
            DELETE FROM BI_OJT.NOTIFICATIONS
            WHERE USER_ID = :userId";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("userId", OracleDbType.Int32).Value = userId;
                    cmd.ExecuteNonQuery();
                }
            }

            LoadNotifications();
        }
        protected void rptNotifications_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "MarkRead")
            {
                int notifId = Convert.ToInt32(e.CommandArgument);
                string linkPage = "~/Pages/Tickets.aspx";

                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string getSql = "SELECT LINK_PAGE FROM BI_OJT.NOTIFICATIONS WHERE NOTIFICATION_ID = :notifId";
                    using (OracleCommand getCmd = new OracleCommand(getSql, conn))
                    {
                        getCmd.BindByName = true;
                        getCmd.Parameters.Add("notifId", OracleDbType.Int32).Value = notifId;

                        object result = getCmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value && !string.IsNullOrWhiteSpace(result.ToString()))
                        {
                            linkPage = result.ToString();
                        }
                    }

                    string updateSql = "UPDATE BI_OJT.NOTIFICATIONS SET IS_READ = 1 WHERE NOTIFICATION_ID = :notifId";
                    using (OracleCommand updateCmd = new OracleCommand(updateSql, conn))
                    {
                        updateCmd.BindByName = true;
                        updateCmd.Parameters.Add("notifId", OracleDbType.Int32).Value = notifId;
                        updateCmd.ExecuteNonQuery();
                    }
                }

                LoadNotifications();
                Response.Redirect(linkPage);
            }
        }
    }
}