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
                    (USER_ID, ACTION, TABLE_NAME, OLD_VALUE, NEW_VALUE, CREATED_AT)
                    VALUES (:userId, :action, :tableName, :oldVal, :newVal, SYSDATE))";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add(":userId", OracleDbType.Int32).Value = userId;
                cmd.Parameters.Add(":action", OracleDbType.Varchar2).Value = (action ?? string.Empty);
                cmd.Parameters.Add(":oldVal", OracleDbType.Varchar2).Value = (oldValue ?? string.Empty);
                cmd.Parameters.Add(":newVal", OracleDbType.Varchar2).Value = (newValue ?? string.Empty);
                cmd.Parameters.Add(":tableName", OracleDbType.Varchar2).Value = "USERS";
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void LogAction(int userId, string action, string tableName, int recordId, Dictionary<string, object> oldSnap, Dictionary<string, object> newSnap)
    {
        if (userId <= 0) return;

        var serializer = new JavaScriptSerializer();

        // Serialize to JSON strings
        string oldJson = oldSnap == null ? null : serializer.Serialize(oldSnap);
        string newJson = newSnap == null ? null : serializer.Serialize(newSnap);

        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();

            // Use the specific columns for Table and ID
            string sql = @"INSERT INTO BI_OJT.AUDIT_LOGS 
                      (USER_ID, ACTION, TABLE_NAME, TICKET_ID, OLD_VALUE, NEW_VALUE, CREATED_AT) 
                      VALUES (:userId, :action, :tableName, :recordId, :oldVal, :newVal, SYSDATE)";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add(":userId", OracleDbType.Int32).Value = userId;
                cmd.Parameters.Add(":action", OracleDbType.Varchar2).Value = action;
                cmd.Parameters.Add(":tableName", OracleDbType.Varchar2).Value = tableName ?? (object)DBNull.Value;
                cmd.Parameters.Add(":recordId", OracleDbType.Int32).Value = recordId;

                cmd.Parameters.Add(":oldVal", OracleDbType.Clob).Value = (object)oldJson ?? DBNull.Value;
                cmd.Parameters.Add(":newVal", OracleDbType.Clob).Value = (object)newJson ?? DBNull.Value;

                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void LogUserAction(int adminUserId, string action, Dictionary<string, object> oldSnap, Dictionary<string, object> newSnap)
    {
        if (adminUserId <= 0) return;

        var serializer = new JavaScriptSerializer();

        string oldJson = oldSnap == null ? null : serializer.Serialize(oldSnap);
        string newJson = newSnap == null ? null : serializer.Serialize(newSnap);

        using (var conn = DatabaseHelper.GetConnection())
        {
            conn.Open();

            string sql = @"INSERT INTO BI_OJT.AUDIT_LOGS
                      (USER_ID, ACTION, TABLE_NAME, OLD_VALUE, NEW_VALUE, CREATED_AT)
                      VALUES
                      (:userId, :action, :tableName, :oldVal, :newVal, SYSDATE)";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add(":userId", OracleDbType.Int32).Value = adminUserId;
                cmd.Parameters.Add(":action", OracleDbType.Varchar2).Value = action;
                cmd.Parameters.Add(":tableName", OracleDbType.Varchar2).Value = "USERS";
                cmd.Parameters.Add(":oldVal", OracleDbType.Clob).Value = (object)oldJson ?? DBNull.Value;
                cmd.Parameters.Add(":newVal", OracleDbType.Clob).Value = (object)newJson ?? DBNull.Value;

                cmd.ExecuteNonQuery();
            }
        }
    }
}