using System;
using System.Data;
using System.Web.UI.WebControls;
using Oracle.ManagedDataAccess.Client;
using BI_TICKETING_SYSTEM.Helpers;
using System.Web;

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
        private void LoadNotifications()
        {
            if (Session["UserID"] == null)
            {
                lblNotifCount.Text = "";
                lblNotifCount.Visible = false;
                rptNotifications.DataSource = null;
                rptNotifications.DataBind();
                pnlNoNotifs.Visible = true;
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
                        rptNotifications.DataSource = dt;
                        rptNotifications.DataBind();
                        pnlNoNotifs.Visible = false;
                    }
                    else
                    {
                        rptNotifications.DataSource = null;
                        rptNotifications.DataBind();
                        pnlNoNotifs.Visible = true;
                    }
                }
            }
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