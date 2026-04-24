using System;
using System.Data;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using Oracle.ManagedDataAccess.Client;
using BI_TICKETING_SYSTEM.Helpers;

namespace BI_TICKETING_SYSTEM.Pages
{
    public partial class Users : System.Web.UI.Page
    {

        private int PageSize = 10;

        private int CurrentPage
        {
            get { return ViewState["CurrentPage"] != null ? (int)ViewState["CurrentPage"] : 1; }
            set { ViewState["CurrentPage"] = value; }
        }

        private int CurrentUserID
        {
            get { return Session["UserID"] != null ? Convert.ToInt32(Session["UserID"]) : 0; }
        }

        private string CurrentUserName
        {
            get { return Session["UserName"]?.ToString() ?? ""; }
        }

        private string CurrentRole
        {
            get { return Session["UserRole"]?.ToString() ?? ""; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {

            if (!IsPostBack)
            {
                LoadUsers();
            }

        }


        private void LoadUsers()
        {

            string search = txtSearch.Text.Trim().ToLower();

            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string updateStatusSql = @"UPDATE USERS 
                                          SET STATUS = 'Active' 
                                          WHERE STATUS IS NULL 
                                             OR TRIM(STATUS) = '' 
                                             OR UPPER(TRIM(STATUS)) NOT IN ('ACTIVE', 'INACTIVE')";
                using (OracleCommand updateCmd = new OracleCommand(updateStatusSql, conn))
                {
                    updateCmd.BindByName = true;
                    updateCmd.ExecuteNonQuery();
                }

                string query = @"
                SELECT *
                FROM (
                    SELECT a.*, ROWNUM rnum
                    FROM (
                        SELECT USER_ID, FULL_NAME, USERNAME, EMAIL, ROLE, STATUS, CREATED_AT
                        FROM USERS
                        WHERE LOWER(FULL_NAME) LIKE :search
                           OR LOWER(USERNAME) LIKE :search
                           OR LOWER(ROLE) LIKE :search
                           OR LOWER(EMAIL) LIKE :search
                        ORDER BY CREATED_AT DESC
                    ) a
                    WHERE ROWNUM <= :maxRow
                )
                WHERE rnum > :minRow";

                OracleCommand cmd = new OracleCommand(query, conn);
                cmd.BindByName = true;

                int maxRow = CurrentPage * PageSize;
                int minRow = (CurrentPage - 1) * PageSize;

                cmd.Parameters.Add("search", OracleDbType.Varchar2).Value = "%" + search + "%";
                cmd.Parameters.Add("maxRow", OracleDbType.Int32).Value = maxRow;
                cmd.Parameters.Add("minRow", OracleDbType.Int32).Value = minRow;

                OracleDataAdapter da = new OracleDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);

                gvUsers.DataSource = dt;
                gvUsers.DataBind();

                lblPage.Text = "Page " + CurrentPage;

            }

        }


        protected void gvUsers_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView row = (DataRowView)e.Row.DataItem;

                DropDownList ddlUserRole = (DropDownList)e.Row.FindControl("ddlUserRole");
                if (ddlUserRole != null)
                {
                    string currentRole = row["ROLE"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(currentRole) && ddlUserRole.Items.FindByValue(currentRole) != null)
                        ddlUserRole.SelectedValue = currentRole;

                    ddlUserRole.Attributes["data-oldvalue"] = currentRole;
                    ddlUserRole.Attributes["onchange"] = "return confirmRoleChange(this);";
                }

                CheckBox chkStatus = (CheckBox)e.Row.FindControl("chkStatus");
                if (chkStatus != null)
                {
                    string status = row["STATUS"]?.ToString()?.Trim() ?? "";
                    chkStatus.Checked = !status.Equals("Inactive", StringComparison.OrdinalIgnoreCase);

                    int userId = Convert.ToInt32(row["USER_ID"]);
                    string fullName = row["FULL_NAME"]?.ToString() ?? "this user";

                    chkStatus.InputAttributes["data-userid"] = userId.ToString();
                    chkStatus.InputAttributes["data-fullname"] = fullName;
                    chkStatus.InputAttributes["onclick"] = "return confirmStatusToggle(this);";
                }
            }
        }


        protected void txtSearch_TextChanged(object sender, EventArgs e)
        {
            CurrentPage = 1;
            LoadUsers();
        }


        protected void btnPrev_Click(object sender, EventArgs e)
        {

            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadUsers();
            }

        }


        protected void btnNext_Click(object sender, EventArgs e)
        {
            CurrentPage++;
            LoadUsers();
        }


        protected void btnCreateUser_Click(object sender, EventArgs e)
        {

            string name = txtFullName.Text.Trim();
            string uname = txtUName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string role = ddlRole.SelectedValue;

            string password = PasswordHelper.HashPassword("password123");

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                using (var check = new OracleCommand("SELECT COUNT(1) FROM USERS WHERE USERNAME = :u OR EMAIL = :e", conn))
                {
                    check.BindByName = true;
                    check.Parameters.Add(":u", uname);
                    check.Parameters.Add(":e", email);
                    bool exists = Convert.ToInt32(check.ExecuteScalar()) > 0;
                    if (exists) { ShowError("Username or email already exists."); return; }
                }

                string insert = @"
                        INSERT INTO USERS (USER_ID, FULL_NAME, USERNAME, EMAIL, PASSWORD, ROLE, STATUS, CREATED_AT)
                        VALUES (USERS_SEQ.NEXTVAL, :name, :uname, :email, :pass, :role, :status, SYSDATE)";

                using (var cmd = new OracleCommand(insert, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(":name", name);
                    cmd.Parameters.Add(":uname", uname);
                    cmd.Parameters.Add(":email", email);
                    cmd.Parameters.Add(":pass", password);
                    cmd.Parameters.Add(":role", role);
                    cmd.Parameters.Add(":status", "Active");
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (OracleException ex) when (ex.Number == 1)
                    {
                        ShowError("Username or email already exists.");
                        return;
                    }
                }
            }

            ShowSuccess("User created successfully");

            LoadUsers();

        }



        protected void gvUsers_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            int userId = Convert.ToInt32(e.CommandArgument);

            if (e.CommandName == "EditUser")
            {
                LoadUserForEdit(userId);
                return;
            }

            if (e.CommandName == "ResetPassword")
            {
                LoadUserForResetPassword(userId);
                return;
            }

            if (e.CommandName == "DeleteUser")
            {
                if (userId == CurrentUserID)
                {
                    ShowError("You cannot delete your own account.");
                    return;
                }

                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    var oldSnap = GetUserSnapshot(userId, conn);

                    string query = "DELETE FROM USERS WHERE USER_ID = :id";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add(":id", userId);
                        cmd.ExecuteNonQuery();
                    }

                    LogUserAudit("DELETE_USER", oldSnap, null);
                }

                ShowSuccess("User deleted");
                LoadUsers();
                return;
            }
        }


        private void LoadUserForEdit(int userId)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT USER_ID, FULL_NAME, USERNAME, EMAIL FROM USERS WHERE USER_ID = :userId";
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.BindByName = true;
                    cmd.Parameters.Add("userId", OracleDbType.Int32).Value = userId;

                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        hfEditUserId.Value = userId.ToString();
                        txtEditFullName.Text = row["FULL_NAME"].ToString();
                        txtEditUsername.Text = row["USERNAME"].ToString();
                        txtEditEmail.Text = row["EMAIL"].ToString();

                        hfEditOriginalFullName.Value = txtEditFullName.Text;
                        hfEditOriginalUsername.Value = txtEditUsername.Text;
                        hfEditOriginalEmail.Value = txtEditEmail.Text;
                        hfEditUserHasChanges.Value = "false";

                        hfShowModal.Value = "edit";
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Error loading user: " + ex.Message);
            }
        }


        private void LoadUserForResetPassword(int userId)
        {
            try
            {
                hfResetUserId.Value = userId.ToString();
                txtNewPassword.Text = "";
                txtRetypePassword.Text = "";
                hfResetPasswordHasChanges.Value = "false";
                hfShowModal.Value = "reset";
            }
            catch (Exception ex)
            {
                ShowError("Error loading reset password modal: " + ex.Message);
            }
        }


        protected void btnSaveEditUser_Click(object sender, EventArgs e)
        {
            if (hfEditUserHasChanges.Value != "true")
            {
                hfShowModal.Value = "edit";
                ShowError("No changes detected.");
                return;
            }

            try
            {
                int userId = Convert.ToInt32(hfEditUserId.Value);
                string fullName = txtEditFullName.Text.Trim();
                string username = txtEditUsername.Text.Trim();
                string email = txtEditEmail.Text.Trim();

                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
                {
                    hfShowModal.Value = "edit";
                    ShowError("All fields are required.");
                    return;
                }

                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    using (var check = new OracleCommand("SELECT COUNT(1) FROM USERS WHERE (USERNAME = :u OR EMAIL = :e) AND USER_ID != :id", conn))
                    {
                        check.BindByName = true;
                        check.Parameters.Add(":u", username);
                        check.Parameters.Add(":e", email);
                        check.Parameters.Add(":id", userId);
                        bool exists = Convert.ToInt32(check.ExecuteScalar()) > 0;
                        if (exists)
                        {
                            hfShowModal.Value = "edit";
                            ShowError("Username or email already exists.");
                            return;
                        }
                    }

                    var oldSnap = GetUserSnapshot(userId, conn);

                    string sql = @"UPDATE USERS
                           SET FULL_NAME = :fullName, USERNAME = :username, EMAIL = :email, UPDATED_AT = SYSDATE
                           WHERE USER_ID = :userId";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("fullName", OracleDbType.Varchar2).Value = fullName;
                        cmd.Parameters.Add("username", OracleDbType.Varchar2).Value = username;
                        cmd.Parameters.Add("email", OracleDbType.Varchar2).Value = email;
                        cmd.Parameters.Add("userId", OracleDbType.Int32).Value = userId;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = GetUserSnapshot(userId, conn);
                    LogUserAudit("EDIT_USER", oldSnap, newSnap);

                    hfShowModal.Value = "";
                    hfEditUserHasChanges.Value = "false";
                    ShowSuccess("User updated successfully!");
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                hfShowModal.Value = "edit";
                ShowError("Error updating user: " + ex.Message);
            }
        }


        protected void btnSaveResetPassword_Click(object sender, EventArgs e)
        {
            if (hfResetPasswordHasChanges.Value != "true")
            {
                hfShowModal.Value = "reset";
                ShowError("Please enter a new password.");
                return;
            }

            try
            {
                int userId = Convert.ToInt32(hfResetUserId.Value);
                string newPassword = txtNewPassword.Text.Trim();
                string retypePassword = txtRetypePassword.Text.Trim();

                if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(retypePassword))
                {
                    hfShowModal.Value = "reset";
                    ShowError("Both password fields are required.");
                    return;
                }

                if (newPassword != retypePassword)
                {
                    hfShowModal.Value = "reset";
                    ShowError("Passwords do not match.");
                    return;
                }

                string hashedPassword = PasswordHelper.HashPassword(newPassword);

                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        UPDATE USERS
                        SET PASSWORD = :password,
                        UPDATED_AT = SYSDATE
                        WHERE USER_ID = :id";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add(":password", hashedPassword);
                        cmd.Parameters.Add(":id", userId);
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = new Dictionary<string, object>
                    {
                        { "USER_ID", userId },
                        { "PASSWORD_CHANGED", true }
                    };
                    LogUserAudit("RESET_PASSWORD", null, newSnap);

                    hfShowModal.Value = "";
                    hfResetPasswordHasChanges.Value = "false";
                    ShowSuccess("Password reset successfully!");
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                hfShowModal.Value = "reset";
                ShowError("Error resetting password: " + ex.Message);
            }
        }


        protected void ddlUserRole_Changed(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            GridViewRow row = (GridViewRow)ddl.NamingContainer;
            int rowIndex = row.RowIndex;

            HiddenField hfUserId = (HiddenField)row.FindControl("hfUserId");
            int userId = Convert.ToInt32(hfUserId.Value);
            string newRole = ddl.SelectedValue;

            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    var oldSnap = GetUserSnapshot(userId, conn);

                    string sql = @"UPDATE USERS
                           SET ROLE = :role, UPDATED_AT = SYSDATE
                           WHERE USER_ID = :userId";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("role", OracleDbType.Varchar2).Value = newRole;
                        cmd.Parameters.Add("userId", OracleDbType.Int32).Value = userId;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = GetUserSnapshot(userId, conn);
                    LogUserAudit("UPDATE_ROLE", oldSnap, newSnap);

                    ShowSuccess("Role updated successfully!");
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                ShowError("Error updating role: " + ex.Message);
                LoadUsers();
            }
        }


        protected void chkStatus_Changed(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            GridViewRow row = (GridViewRow)chk.NamingContainer;
            int rowIndex = row.RowIndex;

            HiddenField hfUserId = (HiddenField)row.FindControl("hfUserId");
            int userId = Convert.ToInt32(hfUserId.Value);
            bool isActive = chk.Checked;

            if (userId == CurrentUserID && !isActive)
            {
                ShowError("You cannot deactivate your own account.");
                LoadUsers();
                return;
            }

            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    var oldSnap = GetUserSnapshot(userId, conn);

                    string newStatus = isActive ? "Active" : "Inactive";

                    string sql = @"UPDATE USERS
                           SET STATUS = :status, UPDATED_AT = SYSDATE
                           WHERE USER_ID = :userId";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = newStatus;
                        cmd.Parameters.Add("userId", OracleDbType.Int32).Value = userId;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = GetUserSnapshot(userId, conn);
                    LogUserAudit("UPDATE_STATUS", oldSnap, newSnap);

                    ShowSuccess($"User status updated to {newStatus}!");
                    LoadUsers();
                }
            }
            catch (Exception ex)
            {
                ShowError("Error updating status: " + ex.Message);
                LoadUsers();
            }
        }


        private Dictionary<string, object> GetUserSnapshot(int userId, OracleConnection conn)
        {
            string sql = @"SELECT USER_ID, FULL_NAME, USERNAME, EMAIL, ROLE, STATUS
                   FROM USERS
                   WHERE USER_ID = :userId";

            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("userId", OracleDbType.Int32).Value = userId;

                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    var snap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    snap["USER_ID"] = reader["USER_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["USER_ID"]);
                    snap["FULL_NAME"] = reader["FULL_NAME"] == DBNull.Value ? null : reader["FULL_NAME"].ToString();
                    snap["USERNAME"] = reader["USERNAME"] == DBNull.Value ? null : reader["USERNAME"].ToString();
                    snap["EMAIL"] = reader["EMAIL"] == DBNull.Value ? null : reader["EMAIL"].ToString();
                    snap["ROLE"] = reader["ROLE"] == DBNull.Value ? null : reader["ROLE"].ToString();
                    snap["STATUS"] = reader["STATUS"] == DBNull.Value ? null : reader["STATUS"].ToString();

                    return snap;
                }
            }
        }


        private void LogUserAudit(string action, Dictionary<string, object> oldSnap, Dictionary<string, object> newSnap)
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string oldJson = oldSnap == null ? null : serializer.Serialize(oldSnap);
            string newJson = newSnap == null ? null : serializer.Serialize(newSnap);
            AuditHelper.Log(CurrentUserID, action, oldJson, newJson);
        }


        private void ShowSuccess(string message)
        {
            hfSwalMessage.Value = message;
            hfSwalType.Value = "success";
        }


        private void ShowError(string message)
        {
            hfSwalMessage.Value = message;
            hfSwalType.Value = "error";
        }

    }
}
