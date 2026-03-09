using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Oracle.ManagedDataAccess.Client;
using BI_TICKETING_SYSTEM.Helpers;

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