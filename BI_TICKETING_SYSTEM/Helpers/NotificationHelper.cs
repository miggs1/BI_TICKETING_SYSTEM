using System;
using Oracle.ManagedDataAccess.Client;

namespace BI_TICKETING_SYSTEM.Helpers
{
    public static class NotificationHelper
    {
        public static void SendNotification(int targetUserId, string title, string message, string linkPage, int? ticketId = null)
        {
            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"
                    INSERT INTO BI_OJT.NOTIFICATIONS
                    (
                        USER_ID,
                        TITLE,
                        MESSAGE,
                        TICKET_ID,
                        IS_READ,
                        CREATED_AT,
                        LINK_PAGE
                    )
                    VALUES
                    (
                        :userId,
                        :title,
                        :message,
                        :ticketId,
                        0,
                        SYSDATE,
                        :linkPage
                    )";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("userId", OracleDbType.Int32).Value = targetUserId;
                    cmd.Parameters.Add("title", OracleDbType.Varchar2).Value = title;
                    cmd.Parameters.Add("message", OracleDbType.Clob).Value = message;
                    cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value =
                        ticketId.HasValue ? (object)ticketId.Value : DBNull.Value;
                    cmd.Parameters.Add("linkPage", OracleDbType.Varchar2).Value = linkPage;

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}