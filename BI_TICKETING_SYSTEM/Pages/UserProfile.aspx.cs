using BI_TICKETING_SYSTEM.Helpers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Web.UI;

namespace BI_TICKETING_SYSTEM.Pages
{
    public partial class UserProfile : System.Web.UI.Page
    {

        private int CurrentUserID => Convert.ToInt32(Session["UserID"] ?? 0);

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadUserProfile();
            }
        }


        private void LoadUserProfile()
        {
            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"SELECT FULL_NAME, EMAIL, ROLE
                               FROM BI_OJT.USERS
                               WHERE USER_ID = :userId";

                OracleCommand cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;

                OracleDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    txtFullName.Text = reader["FULL_NAME"].ToString();
                    txtEmail.Text = reader["EMAIL"].ToString();
                    txtRole.Text = reader["ROLE"].ToString();
                }
            }
        }


        protected void btnOpenPassword_Click(object sender, EventArgs e)
        {
            ScriptManager.RegisterStartupScript(this, GetType(),
                "showModal", "$('#passwordModal').modal('show');", true);
        }



        protected void btnSavePassword_Click(object sender, EventArgs e)
        {
            try
            {
                string hashedPassword = PasswordHelper.HashPassword(txtNewPassword.Text);

                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string sql = @"UPDATE BI_OJT.USERS
                                   SET PASSWORD = :password
                                   WHERE USER_ID = :userId";

                    OracleCommand cmd = new OracleCommand(sql, conn);

                    cmd.Parameters.Add("password", OracleDbType.Varchar2).Value = hashedPassword;
                    cmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;

                    cmd.ExecuteNonQuery();
                }

                pnlSuccess.Visible = true;
                lblSuccess.Text = "Password updated successfully.";

            }
            catch (Exception ex)
            {
                pnlError.Visible = true;
                lblError.Text = "Error updating password: " + ex.Message;
            }
        }
    }
}