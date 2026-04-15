using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;

namespace BI_TICKETING_SYSTEM.Helpers
{
    public static class SLAHelper
    {
        public static DateTime? CalculateSlaDueDate(string priority, DateTime startDate)
        {
            switch ((priority ?? "").Trim().ToLower())
            {
                case "critical":
                case "urgent":
                    return startDate.AddHours(1);

                case "high":
                    return startDate.AddHours(3);

                case "medium":
                    return startDate.AddDays(1);

                case "low":
                    return startDate.AddDays(2);

                default:
                    return null;
            }
        }

        public static void LogSlaCreated(int userId, int ticketId, string priority, DateTime dueDate)
        {
            var newSnap = new Dictionary<string, object>
            {
                { "PRIORITY", priority },
                { "DUE_DATE", dueDate.ToString("MM/dd/yyyy hh:mm tt") }
            };

            AuditHelper.LogAction(userId, "SLA_CREATED", "TICKETS", ticketId, null, newSnap);
        }

        public static void LogSlaUpdated(int userId, int ticketId, string oldPriority, string newPriority, DateTime? oldDueDate, DateTime? newDueDate)
        {
            var oldSnap = new Dictionary<string, object>
            {
                { "PRIORITY", oldPriority },
                { "DUE_DATE", oldDueDate.HasValue ? oldDueDate.Value.ToString("MM/dd/yyyy hh:mm tt") : null }
            };

            var newSnap = new Dictionary<string, object>
            {
                { "PRIORITY", newPriority },
                { "DUE_DATE", newDueDate.HasValue ? newDueDate.Value.ToString("MM/dd/yyyy hh:mm tt") : null }
            };

            AuditHelper.LogAction(userId, "SLA_UPDATED", "TICKETS", ticketId, oldSnap, newSnap);
        }

        public static void LogSlaDueDateUpdated(int userId, int ticketId, DateTime? oldDueDate, DateTime newDueDate)
        {
            var oldSnap = new Dictionary<string, object>
            {
                { "DUE_DATE", oldDueDate.HasValue ? oldDueDate.Value.ToString("MM/dd/yyyy hh:mm tt") : null }
            };

            var newSnap = new Dictionary<string, object>
            {
                { "DUE_DATE", newDueDate.ToString("MM/dd/yyyy hh:mm tt") }
            };

            AuditHelper.LogAction(userId, "SLA_UPDATED", "TICKETS", ticketId, oldSnap, newSnap);
        }

        public static void LogSlaBreached(int ticketId, DateTime dueDate)
        {
            var oldSnap = new Dictionary<string, object>
            {
                { "DUE_DATE", dueDate.ToString("MM/dd/yyyy hh:mm tt") },
                { "SLA_STATE", "Within SLA" }
            };

            var newSnap = new Dictionary<string, object>
            {
                { "DUE_DATE", dueDate.ToString("MM/dd/yyyy hh:mm tt") },
                { "SLA_STATE", "Breached" }
            };

            AuditHelper.LogAction(0, "SLA_BREACHED", "TICKETS", ticketId, oldSnap, newSnap);
        }

        public static void LogSlaMet(int userId, int ticketId, DateTime dueDate, string status)
        {
            var oldSnap = new Dictionary<string, object>
            {
                { "DUE_DATE", dueDate.ToString("MM/dd/yyyy hh:mm tt") },
                { "SLA_RESULT", null }
            };

            var newSnap = new Dictionary<string, object>
            {
                { "DUE_DATE", dueDate.ToString("MM/dd/yyyy hh:mm tt") },
                { "SLA_RESULT", "Met" },
                { "STATUS", status }
            };

            AuditHelper.LogAction(userId, "SLA_MET", "TICKETS", ticketId, oldSnap, newSnap);
        }

        public static void LogSlaMissed(int userId, int ticketId, DateTime dueDate, string status)
        {
            var oldSnap = new Dictionary<string, object>
            {
                { "DUE_DATE", dueDate.ToString("MM/dd/yyyy hh:mm tt") },
                { "SLA_RESULT", null }
            };

            var newSnap = new Dictionary<string, object>
            {
                { "DUE_DATE", dueDate.ToString("MM/dd/yyyy hh:mm tt") },
                { "SLA_RESULT", "Missed" },
                { "STATUS", status }
            };

            AuditHelper.LogAction(userId, "SLA_MISSED", "TICKETS", ticketId, oldSnap, newSnap);
        }

        public static bool HasSlaEventAlreadyLogged(int ticketId, string action)
        {
            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT COUNT(*)
                    FROM BI_OJT.AUDIT_LOGS
                    WHERE TICKET_ID = :TICKET_ID
                      AND ACTION = :ACTION";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.Parameters.Add(":TICKET_ID", OracleDbType.Int32).Value = ticketId;
                    cmd.Parameters.Add(":ACTION", OracleDbType.Varchar2).Value = action;

                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        public static void CheckAndLogSlaBreach(int ticketId, DateTime? dueDate, string status)
        {
            if (!dueDate.HasValue)
                return;

            string normalizedStatus = (status ?? "").Trim().ToLower();
            bool isClosed = normalizedStatus == "resolved" || normalizedStatus == "closed";

            if (DateTime.Now > dueDate.Value && !isClosed)
            {
                if (!HasSlaEventAlreadyLogged(ticketId, "SLA_BREACHED"))
                {
                    LogSlaBreached(ticketId, dueDate.Value);
                }
            }
        }

        public static void CheckAndLogSlaCompletion(int userId, int ticketId, DateTime? dueDate, string newStatus)
        {
            if (!dueDate.HasValue)
                return;

            string normalizedStatus = (newStatus ?? "").Trim().ToLower();

            if (normalizedStatus == "resolved" || normalizedStatus == "closed")
            {
                if (DateTime.Now <= dueDate.Value)
                    LogSlaMet(userId, ticketId, dueDate.Value, newStatus);
                else
                    LogSlaMissed(userId, ticketId, dueDate.Value, newStatus);
            }
        }
    }
}