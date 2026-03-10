using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Oracle.ManagedDataAccess.Client;
using BI_TICKETING_SYSTEM.Helpers;

public static class AuditHelper
{
    public static void Log(int userId, string action, string oldValue, string newValue)
    {
        // Guard: do not insert audit rows without a valid user id
        if (userId <= 0) return;

        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();

            string sql = @"INSERT INTO BI_OJT.AUDIT_LOGS
                          (USER_ID, ACTION, OLD_VALUE, NEW_VALUE, CREATED_AT)
                          VALUES (:userId, :action, :oldVal, :newVal, SYSDATE)";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add(":userId", OracleDbType.Int32).Value = userId;
                cmd.Parameters.Add(":action", OracleDbType.Varchar2).Value = (action ?? string.Empty);
                cmd.Parameters.Add(":oldVal", OracleDbType.Varchar2).Value = (oldValue ?? string.Empty);
                cmd.Parameters.Add(":newVal", OracleDbType.Varchar2).Value = (newValue ?? string.Empty);

                cmd.ExecuteNonQuery();
            }
        }
    }

    // New overload used by Tickets.aspx.cs which expects LogAction(...)
    public static void LogAction(int userId, string action, string tableName, int recordId, Dictionary<string, object> oldSnap, Dictionary<string, object> newSnap)
    {
        var serializer = new JavaScriptSerializer();

        string oldJson = oldSnap == null ? null : serializer.Serialize(oldSnap);
        string newJson = newSnap == null ? null : serializer.Serialize(newSnap);

        // Preserve table and record context by appending to action (no DB schema changes required)
        string actionWithContext = $"{action}|{tableName}|{recordId}";

        Log(userId, actionWithContext, oldJson, newJson);
    }
}