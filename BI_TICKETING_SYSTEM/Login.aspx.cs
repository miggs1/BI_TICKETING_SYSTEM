using BI_TICKETING_SYSTEM.Helpers;
using System;
using System.Data;
using System.Web;

namespace BI_TICKETING_SYSTEM
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Prevent browser from caching this page
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (Session["UserName"] != null)
            {
                Response.Redirect("~/Default.aspx");
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            try
            {
                DataRow user = UserService.ValidateUser(username, password);

                if (user != null)
                {
                    int userId = Convert.ToInt32(user["USER_ID"]);
                    string fullName = user["FULL_NAME"].ToString();
                    string email = user["EMAIL"].ToString();
                    string role = user["ROLE"].ToString();

                    Session["UserID"] = userId;
                    Session["UserName"] = fullName;
                    Session["Email"] = email;
                    Session["UserRole"] = role;

                    UserService.LogAction(userId, "LOGIN", "USERS", userId);

                    switch (role.ToLower())
                    {
                        case "admin":
                            Response.Redirect("~/Default.aspx");
                            break;
                        case "support":
                            Response.Redirect("~/Default.aspx");
                            break;
                        default:
                            Response.Redirect("~/Default.aspx");
                            break;
                    }
                }
                else
                {
                    ShowError("Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                ShowError("Error: " + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            pnlError.Visible = true;
        }
    }
}