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

                // NOTE: This query expects a linking column PERSONAL_INFO.USER_ID to exist.
                // If your schema does not have PERSONAL_INFO.USER_ID, see the SQL provided
                // in the project root / instructions to add the linking column before using
                // the personal info fields on this page.

                string sql = @"SELECT U.EMAIL, U.ROLE,
                                      P.LAST_NAME, P.FIRST_NAME, P.MIDDLE_NAME, P.GENDER, P.DOB
                               FROM BI_OJT.USERS U
                               LEFT JOIN BI_OJT.PERSONAL_INFO P
                                 ON P.USER_ID = U.USER_ID
                               WHERE U.USER_ID = :userId";

                OracleCommand cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;

                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        txtEmail.Text = reader["EMAIL"]?.ToString() ?? string.Empty;
                        txtRole.Text = reader["ROLE"]?.ToString() ?? string.Empty;

                        txtLastName.Text = reader["LAST_NAME"] != DBNull.Value ? reader["LAST_NAME"].ToString() : string.Empty;
                        txtFirstName.Text = reader["FIRST_NAME"] != DBNull.Value ? reader["FIRST_NAME"].ToString() : string.Empty;
                        txtMiddleName.Text = reader["MIDDLE_NAME"] != DBNull.Value ? reader["MIDDLE_NAME"].ToString() : string.Empty;
                        txtGender.Text = reader["GENDER"] != DBNull.Value ? reader["GENDER"].ToString() : string.Empty;

                        if (reader["DOB"] != DBNull.Value)
                        {
                            DateTime dob;
                            if (DateTime.TryParse(reader["DOB"].ToString(), out dob))
                            {
                                txtDOB.Text = dob.ToString("MM/dd/yyyy");
                            }
                        }
                    }
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