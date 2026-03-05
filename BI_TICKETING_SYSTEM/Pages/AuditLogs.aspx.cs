using BI_TICKETING_SYSTEM.Helpers;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Web.UI.WebControls;
using System.Configuration;


namespace BI_TICKETING_SYSTEM.Pages
{
    public partial class AuditLogs : System.Web.UI.Page
    {
        private string connectionString;

        protected void Page_Init(object sender, EventArgs e)
        {
            connectionString = ConfigurationManager.ConnectionStrings["OracleDbConnection"]?.ConnectionString
                ?? throw new ConfigurationErrorsException("Missing OracleDbConnection in Web.config");
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadUsers();
                LoadAuditLogs();
            }
        }

        private void LoadAuditLogs(string sortExpression = "")
        {
            string query = @"
                            SELECT 
                                U.FULL_NAME,
                                A.ACTION,
                                A.OLD_VALUE,
                                A.NEW_VALUE,
                                A.CREATED_AT
                            FROM AUDIT_LOGS A
                            JOIN USERS U ON A.USER_ID = U.USER_ID
                            WHERE 1=1
                            ";

            if (!string.IsNullOrEmpty(ddlUser.SelectedValue))
                query += " AND U.USER_ID = :UserId";

            if (!string.IsNullOrEmpty(ddlAction.SelectedValue))
                query += " AND A.ACTION = :Action";

            if (!string.IsNullOrEmpty(txtDateFrom.Text))
                query += " AND A.CREATED_AT >= :DateFrom";

            if (!string.IsNullOrEmpty(txtDateTo.Text))
                query += " AND A.CREATED_AT <= :DateTo";

            if (!string.IsNullOrEmpty(sortExpression))
                query += " ORDER BY " + sortExpression;
            else
                query += " ORDER BY A.CREATED_AT DESC";

            DataTable dt = new DataTable();

            using (OracleConnection conn = new OracleConnection(connectionString))
            using (OracleCommand cmd = new OracleCommand(query, conn))
            {
                // Add parameters with appropriate types
                if (!string.IsNullOrEmpty(ddlUser.SelectedValue))
                    cmd.Parameters.Add("UserId", OracleDbType.Varchar2).Value = ddlUser.SelectedValue;

                if (!string.IsNullOrEmpty(ddlAction.SelectedValue))
                    cmd.Parameters.Add("Action", OracleDbType.Varchar2).Value = ddlAction.SelectedValue;

                if (!string.IsNullOrEmpty(txtDateFrom.Text))
                {
                    if (DateTime.TryParse(txtDateFrom.Text, out DateTime dtFrom))
                        cmd.Parameters.Add("DateFrom", OracleDbType.Date).Value = dtFrom;
                    else
                        cmd.Parameters.Add("DateFrom", OracleDbType.Varchar2).Value = txtDateFrom.Text;
                }

                if (!string.IsNullOrEmpty(txtDateTo.Text))
                {
                    if (DateTime.TryParse(txtDateTo.Text, out DateTime dtTo))
                        cmd.Parameters.Add("DateTo", OracleDbType.Date).Value = dtTo;
                    else
                        cmd.Parameters.Add("DateTo", OracleDbType.Varchar2).Value = txtDateTo.Text;
                }

                OracleDataAdapter da = new OracleDataAdapter(cmd);
                da.Fill(dt);
            }

            dt.Columns.Add("ACTION_TEXT");

            foreach (DataRow row in dt.Rows)
            {
                string action = row["ACTION"].ToString();
                string oldVal = row["OLD_VALUE"].ToString();
                string newVal = row["NEW_VALUE"].ToString();
                string user = row["FULL_NAME"].ToString();

                if (action == "LOGIN")
                    row["ACTION_TEXT"] = $"{user} logged into the system.";

                else if (action == "ASSIGN_TICKET")
                    row["ACTION_TEXT"] = $"{user} assigned ticket #{newVal}.";

                else if (action == "STATUS_CHANGE")
                    row["ACTION_TEXT"] = $"Ticket status changed from {oldVal} to {newVal}.";
            }

            gvAuditLogs.DataSource = dt;
            gvAuditLogs.DataBind();
        }

        protected void gvAuditLogs_Sorting(object sender, GridViewSortEventArgs e)
        {
            LoadAuditLogs(e.SortExpression);
        }

        protected void btnFilter_Click(object sender, EventArgs e)
        {
            LoadAuditLogs();
        }

        private void LoadUsers()
        {
            string query = "SELECT USER_ID, FULL_NAME FROM USERS";

            using (OracleConnection conn = new OracleConnection(connectionString))
            using (OracleCommand cmd = new OracleCommand(query, conn))
            {
                conn.Open();

                ddlUser.DataSource = cmd.ExecuteReader();
                ddlUser.DataTextField = "FULL_NAME";
                ddlUser.DataValueField = "USER_ID";
                ddlUser.DataBind();
            }

            ddlUser.Items.Insert(0, new ListItem("All Users", ""));
        }

    }
}