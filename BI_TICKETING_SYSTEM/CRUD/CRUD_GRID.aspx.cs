using System;
using System.Data;
using System.Configuration;
// For Oracle:
// using Oracle.ManagedDataAccess.Client;
// For SQL Server:
// using System.Data.SqlClient;

namespace BI_TICKETING_SYSTEM.CRUD
{
    public partial class crud_grid : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindGrid();
            }
        }

        private void BindGrid()
        {
            string connString = ConfigurationManager
                .ConnectionStrings["OracleDBConnection"].ConnectionString;

            // Example using Oracle:
            // using (OracleConnection conn = new OracleConnection(connString))
            // {
            //     conn.Open();
            //     string query = "SELECT * FROM your_table";
            //     OracleDataAdapter da = new OracleDataAdapter(query, conn);
            //     DataTable dt = new DataTable();
            //     da.Fill(dt);
            //     GridView1.DataSource = dt;
            //     GridView1.DataBind();
            // }
        }

        protected void GridView1_PageIndexChanging(object sender,
            System.Web.UI.WebControls.GridViewPageEventArgs e)
        {
            GridView1.PageIndex = e.NewPageIndex;
            BindGrid();
        }
    }
}