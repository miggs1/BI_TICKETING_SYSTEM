using System;
using System.Data;
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



        protected void Page_Load(object sender, EventArgs e)
        {

            if (!IsPostBack)
            {
                LoadUsers();
            }

        }


        private void LoadUsers()
        {

            string search = txtSearch.Text.Trim();

            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"
                SELECT *
                FROM (
                    SELECT a.*, ROWNUM rnum
                    FROM (
                        SELECT USER_ID, FULL_NAME, USERNAME, EMAIL, ROLE, STATUS, CREATED_AT
                        FROM USERS
                        WHERE LOWER(FULL_NAME) LIKE :search
                        ORDER BY CREATED_AT DESC
                    ) a
                    WHERE ROWNUM <= :maxRow
                )
                WHERE rnum > :minRow";

                OracleCommand cmd = new OracleCommand(query, conn);

                int maxRow = CurrentPage * PageSize;
                int minRow = (CurrentPage - 1) * PageSize;

                cmd.Parameters.Add(":search", "%" + search.ToLower() + "%");
                cmd.Parameters.Add(":maxRow", maxRow);
                cmd.Parameters.Add(":minRow", minRow);

                OracleDataAdapter da = new OracleDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);

                gvUsers.DataSource = dt;
                gvUsers.DataBind();

                lblPage.Text = "Page " + CurrentPage;

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
            string role = ddlRole.SelectedValue; // proper-case values from dropdown

            // default password for new users
            string password = PasswordHelper.HashPassword("password123");

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                // pre-check (parameterized)
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
                    // Match the casing expected by UserService ("Active")
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

            if (e.CommandName == "DeleteUser")
            {

                int userId = Convert.ToInt32(e.CommandArgument);

                if (userId == CurrentUserID)
                {
                    ShowError("You cannot delete your own account.");
                    return;
                }

                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string query = "DELETE FROM USERS WHERE USER_ID = :id";

                    OracleCommand cmd = new OracleCommand(query, conn);

                    cmd.Parameters.Add(":id", userId);

                    cmd.ExecuteNonQuery();

                    AuditHelper.Log(CurrentUserID, "DELETE_USER", userId.ToString(), "");

                }

                ShowSuccess("User deleted");

                LoadUsers();

            }

        }



        private void ShowSuccess(string message)
        {

            string script = $"Swal.fire('Success','{message}','success');";

            ClientScript.RegisterStartupScript(this.GetType(), "alert", script, true);

        }


        private void ShowError(string message)
        {

            string script = $"Swal.fire('Error','{message}','error');";

            ClientScript.RegisterStartupScript(this.GetType(), "alert", script, true);

        }

    }
}