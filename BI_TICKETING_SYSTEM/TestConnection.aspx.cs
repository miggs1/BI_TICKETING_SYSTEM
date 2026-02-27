using System;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace BI_TICKETING_SYSTEM
{
    public partial class TestConnection : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnTestConnection_Click(object sender, EventArgs e)
        {
            try
            {
                string connString = ConfigurationManager.ConnectionStrings["OracleDbConnection"].ConnectionString;
                using (OracleConnection conn = new OracleConnection(connString))
                {
                    conn.Open();
                    lblResult.Text = "<h3 style='color:green;'>✅ Database Connected Successfully!</h3>";
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                lblResult.Text = "<h3 style='color:red;'>❌ Connection Failed: " + ex.Message + "</h3>";
            }
        }
    }
}