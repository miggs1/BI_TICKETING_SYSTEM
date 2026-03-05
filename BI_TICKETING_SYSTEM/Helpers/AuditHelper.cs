using BI_TICKETING_SYSTEM.Helpers;
using Oracle.ManagedDataAccess.Client;

public static class AuditHelper
{
    public static void Log(int userId, string action, string oldValue, string newValue)
    {
        using (OracleConnection conn = DatabaseHelper.GetConnection())
        {
            conn.Open();

            string sql = @"INSERT INTO BI_OJT.AUDIT_LOGS
                          (USER_ID, ACTION, OLD_VALUE, NEW_VALUE, CREATED_AT)
                          VALUES (:userId, :action, :oldVal, :newVal, SYSDATE)";

            OracleCommand cmd = new OracleCommand(sql, conn);

            cmd.Parameters.Add("userId", userId);
            cmd.Parameters.Add("action", action);
            cmd.Parameters.Add("oldVal", oldValue);
            cmd.Parameters.Add("newVal", newValue);

            cmd.ExecuteNonQuery();
        }
    }
}