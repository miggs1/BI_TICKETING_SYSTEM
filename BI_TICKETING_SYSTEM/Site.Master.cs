using System;
using System.Web;

namespace BI_TICKETING_SYSTEM
{
    public partial class SiteMaster : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
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
                        pnlAdminMenu.Visible = true;
                        pnlSupportMenu.Visible = true;
                        break;
                    case "support":
                        pnlAdminMenu.Visible = false;
                        pnlSupportMenu.Visible = true;
                        break;
                    default: // Regular user
                        pnlAdminMenu.Visible = false;
                        pnlSupportMenu.Visible = false;
                        break;
                }
            }
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            // Clear session
            Session.Clear();
            Session.Abandon();

            // Redirect to login
            Response.Redirect("~/Login.aspx");
        }
    }
}