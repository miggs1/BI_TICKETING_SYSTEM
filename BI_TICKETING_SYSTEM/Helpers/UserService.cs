using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace BI_TICKETING_SYSTEM.Helpers
{
    public class UserService
    {
        public static DataRow ValidateUser(string username, string password)
        {
            string hashedPassword = PasswordHelper.HashPassword(password);

            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"SELECT USER_ID, USERNAME, FULL_NAME, EMAIL, ROLE 
                                 FROM USERS 
                                 WHERE USERNAME = :username 
                                 AND PASSWORD = :password_hash 
                                 AND STATUS = 'Active'";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.Parameters.Add(":username", OracleDbType.Varchar2).Value = username;
                    cmd.Parameters.Add(":password_hash", OracleDbType.Varchar2).Value = hashedPassword;

                    using (OracleDataAdapter adapter = new OracleDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            return dt.Rows[0];
                        }
                    }
                }
            }

            return null;
        }

        public static void LogAction(int userId, string action, string tableName, int? recordId)
        {
            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"INSERT INTO AUDIT_LOGS (USER_ID, ACTION, TABLE_NAME, CREATED_AT) 
                         VALUES (:user_id, :action, :table_name, SYSDATE)";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.Parameters.Add(":user_id", OracleDbType.Int32).Value = userId;
                    cmd.Parameters.Add(":action", OracleDbType.Varchar2).Value = action;
                    cmd.Parameters.Add(":table_name", OracleDbType.Varchar2).Value = tableName;

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}