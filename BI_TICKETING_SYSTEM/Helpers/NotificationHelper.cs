using System;
using System.Web.UI;
using Oracle.ManagedDataAccess.Client;

namespace BI_TICKETING_SYSTEM.Helpers
{
    public static class NotificationHelper
    {
        public static void SendNotification(int targetUserId, string message, int? ticketId = null)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"INSERT INTO BI_OJT_NOTIFICATIONS (NOTIFICATION_ID, USER_ID, MESSAGE, TICKET_ID, CREATED_AT) VALUES (BI_OJT.NOTIFICATIONS_SEQ.NEXTVAL, :userId, :msg, :ticketId, SYSDATE)";
                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add("userId", OracleDbType.Int32).Value = targetUserId;
                        cmd.Parameters.Add("msg", OracleDbType.Varchar2).Value = message;
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId ?? (object)DBNull.Value;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Notification Error: " + ex.Message);
            }
        }
    }
}