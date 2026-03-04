using BI_TICKETING_SYSTEM;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Configuration;
using System.Data;
using System.Web.ModelBinding;

namespace BI_TICKETING_SYSTEM
{
    public partial class Tickets : System.Web.UI.Page
    {
        private string connString;

        protected void Page_Init(object sender, EventArgs e)
        {
            var cs = ConfigurationManager.ConnectionStrings["OracleDbConnection"];
            if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                throw new ConfigurationErrorsException("Missing connection string 'OracleDbConnection' in web.config.");
            connString = cs.ConnectionString;
        }


        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadTickets();
            }
        }

        private void LoadTickets()
        {
            using (OracleConnection conn = new OracleConnection(connString))
            {
                conn.Open();

                string role = (Session["Role"] ?? string.Empty).ToString();
                OracleCommand cmd = conn.CreateCommand();

                if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    cmd.CommandText = "SELECT * FROM TICKETS ORDER BY CREATED_AT DESC";
                }
                else
                {
                    if (Session["USER_ID"] == null)
                    {
                        gvTickets.DataSource = null;
                        gvTickets.DataBind();
                        return;
                    }

                    int userId;
                    try
                    {
                        userId = Convert.ToInt32(Session["USER_ID"]);
                    }
                    catch
                    {
                        gvTickets.DataSource = null;
                        gvTickets.DataBind();
                        return;
                    }

                    cmd.CommandText = @"SELECT * FROM TICKETS
                                        WHERE ASSIGNED_TO_USERID = :userId
                                        ORDER BY CREATED_AT DESC";
                    cmd.Parameters.Add(":userId", OracleDbType.Int32).Value = userId;
                }

                using (OracleDataAdapter da = new OracleDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvTickets.DataSource = dt;
                    gvTickets.DataBind();
                }
            }
        }

        protected void gvTickets_RowUpdating(object sender, DevExpress.Web.Data.ASPxDataUpdatingEventArgs e)
        {
            using (OracleConnection conn = new OracleConnection(connString))
            {
                conn.Open();

                string sql = @"UPDATE TICKETS 
                           SET TITLE=:title,
                               PRIORITY=:priority,
                               STATUS=:status,
                               UPDATED_AT=SYSDATE
                           WHERE TICKET_ID=:id";

                OracleCommand cmd = new OracleCommand(sql, conn);

                cmd.Parameters.Add(":title", e.NewValues["TITLE"]);
                cmd.Parameters.Add(":priority", e.NewValues["PRIORITY"]);
                cmd.Parameters.Add(":status", e.NewValues["STATUS"]);
                cmd.Parameters.Add(":id", e.Keys["TICKET_ID"]);

                cmd.ExecuteNonQuery();
            }

            e.Cancel = true;
            gvTickets.CancelEdit();
            LoadTickets();
        }

        protected void gvTickets_RowDeleting(object sender, DevExpress.Web.Data.ASPxDataDeletingEventArgs e)
        {
            using (OracleConnection conn = new OracleConnection(connString))
            {
                conn.Open();

                string sql = "DELETE FROM TICKETS WHERE TICKET_ID=:id";
                OracleCommand cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add(":id", e.Keys["TICKET_ID"]);
                cmd.ExecuteNonQuery();
            }

            e.Cancel = true;
            LoadTickets();
        }

        protected void gvTickets_RowInserting(object sender, DevExpress.Web.Data.ASPxDataInsertingEventArgs e)
        {
            using (OracleConnection conn = new OracleConnection(connString))
            {
                conn.Open();

                string sql = @"INSERT INTO TICKETS
                          (TICKET_ID, TITLE, PRIORITY, STATUS, CREATED_AT)
                          VALUES (TICKET_SEQ.NEXTVAL, :title, :priority, 'New', SYSDATE)";

                OracleCommand cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add(":title", e.NewValues["TITLE"]);
                cmd.Parameters.Add(":priority", e.NewValues["PRIORITY"]);

                cmd.ExecuteNonQuery();
            }

            e.Cancel = true;
            gvTickets.CancelEdit();
            LoadTickets();
        }
    }
}