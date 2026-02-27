using System.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace BI_TICKETING_SYSTEM.Helpers
{
    public static class DatabaseHelper
    {
        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["OracleDbConnection"].ConnectionString;
        }

        public static OracleConnection GetConnection()
        {
            return new OracleConnection(GetConnectionString());
        }
    }
}