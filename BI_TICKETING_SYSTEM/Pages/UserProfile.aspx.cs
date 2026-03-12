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


        protected void btnEditProfile_Click(object sender, EventArgs e)
        {
            LoadProfileIntoModal();
            ScriptManager.RegisterStartupScript(this, GetType(), "showEditModal", "$('#editProfileModal').modal('show');", true);
        }


        private void LoadProfileIntoModal()
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string sql = @"SELECT LAST_NAME, FIRST_NAME, MIDDLE_NAME, GENDER, DOB
                                   FROM BI_OJT.PERSONAL_INFO
                                   WHERE USER_ID = :userId";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtEditLastName.Text = reader["LAST_NAME"] != DBNull.Value ? reader["LAST_NAME"].ToString() : string.Empty;
                                txtEditFirstName.Text = reader["FIRST_NAME"] != DBNull.Value ? reader["FIRST_NAME"].ToString() : string.Empty;
                                txtEditMiddleName.Text = reader["MIDDLE_NAME"] != DBNull.Value ? reader["MIDDLE_NAME"].ToString() : string.Empty;
                                ddlEditGender.SelectedValue = reader["GENDER"] != DBNull.Value ? reader["GENDER"].ToString() : string.Empty;

                                if (reader["DOB"] != DBNull.Value)
                                {
                                    txtEditDOB.Text = Convert.ToDateTime(reader["DOB"]).ToString("MM/dd/yyyy");
                                }
                                else
                                {
                                    txtEditDOB.Text = string.Empty;
                                }
                            }
                            else
                            {
                                txtEditLastName.Text = string.Empty;
                                txtEditFirstName.Text = string.Empty;
                                txtEditMiddleName.Text = string.Empty;
                                ddlEditGender.SelectedIndex = 0;
                                txtEditDOB.Text = string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                pnlError.Visible = true;
                lblError.Text = "Error loading profile: " + ex.Message;
            }
        }


        protected void btnSaveProfile_Click(object sender, EventArgs e)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string checkSql = "SELECT COUNT(1) FROM BI_OJT.PERSONAL_INFO WHERE USER_ID = :userId";
                    using (OracleCommand checkCmd = new OracleCommand(checkSql, conn))
                    {
                        checkCmd.BindByName = true;
                        checkCmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        DateTime? dob = null;
                        if (!string.IsNullOrWhiteSpace(txtEditDOB.Text))
                        {
                            DateTime parsed;
                            if (DateTime.TryParse(txtEditDOB.Text, out parsed))
                            {
                                dob = parsed;
                            }
                            else
                            {
                                throw new Exception("Invalid DOB format. Use MM/dd/yyyy.");
                            }
                        }

                        if (count > 0)
                        {
                            string updateSql = @"UPDATE BI_OJT.PERSONAL_INFO
                                                 SET LAST_NAME = :lastName,
                                                     FIRST_NAME = :firstName,
                                                     MIDDLE_NAME = :middleName,
                                                     GENDER = :gender,
                                                     DOB = :dob
                                                 WHERE USER_ID = :userId";

                            using (OracleCommand updateCmd = new OracleCommand(updateSql, conn))
                            {
                                updateCmd.BindByName = true;
                                updateCmd.Parameters.Add("lastName", OracleDbType.Varchar2).Value = (object)txtEditLastName.Text ?? DBNull.Value;
                                updateCmd.Parameters.Add("firstName", OracleDbType.Varchar2).Value = (object)txtEditFirstName.Text ?? DBNull.Value;
                                updateCmd.Parameters.Add("middleName", OracleDbType.Varchar2).Value = (object)txtEditMiddleName.Text ?? DBNull.Value;
                                updateCmd.Parameters.Add("gender", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(ddlEditGender.SelectedValue) ? (object)DBNull.Value : ddlEditGender.SelectedValue;
                                updateCmd.Parameters.Add("dob", OracleDbType.Date).Value = dob.HasValue ? (object)dob.Value : DBNull.Value;
                                updateCmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;

                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            string insertSql = @"INSERT INTO BI_OJT.PERSONAL_INFO (ID, LAST_NAME, FIRST_NAME, MIDDLE_NAME, GENDER, DOB, CREATED_AT, USER_ID)
                                                 VALUES (SYS_GUID(), :lastName, :firstName, :middleName, :gender, :dob, SYSDATE, :userId)";

                            using (OracleCommand insertCmd = new OracleCommand(insertSql, conn))
                            {
                                insertCmd.BindByName = true;
                                insertCmd.Parameters.Add("lastName", OracleDbType.Varchar2).Value = (object)txtEditLastName.Text ?? DBNull.Value;
                                insertCmd.Parameters.Add("firstName", OracleDbType.Varchar2).Value = (object)txtEditFirstName.Text ?? DBNull.Value;
                                insertCmd.Parameters.Add("middleName", OracleDbType.Varchar2).Value = (object)txtEditMiddleName.Text ?? DBNull.Value;
                                insertCmd.Parameters.Add("gender", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(ddlEditGender.SelectedValue) ? (object)DBNull.Value : ddlEditGender.SelectedValue;
                                insertCmd.Parameters.Add("dob", OracleDbType.Date).Value = dob.HasValue ? (object)dob.Value : DBNull.Value;
                                insertCmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;

                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                LoadUserProfile();
                pnlSuccess.Visible = true;
                lblSuccess.Text = "Profile updated successfully.";

                ScriptManager.RegisterStartupScript(this, GetType(), "hideEditModal", "$('#editProfileModal').modal('hide');", true);
            }
            catch (Exception ex)
            {
                pnlError.Visible = true;
                lblError.Text = "Error saving profile: " + ex.Message;
            }
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