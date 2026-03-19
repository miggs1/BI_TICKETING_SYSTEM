using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using Oracle.ManagedDataAccess.Client;
using BI_TICKETING_SYSTEM.Helpers;
using System.Collections.Generic;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace BI_TICKETING_SYSTEM.Pages
{
    public partial class Tickets : Page
    {
        private int PageSize = 10;
        private int CurrentPage
        {
            get { return ViewState["CurrentPage"] != null ? (int)ViewState["CurrentPage"] : 1; }
            set { ViewState["CurrentPage"] = value; }
        }

        private string CurrentRole => Session["UserRole"]?.ToString() ?? "User";
        private int CurrentUserID => Convert.ToInt32(Session["UserID"] ?? 0);
        private string CurrentUserName => Session["UserName"]?.ToString() ?? "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserName"] == null || Session["UserID"] == null || Session["UserRole"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                if (CurrentRole.ToLower() == "support")
                {
                    Response.Redirect("~/Default.aspx");
                    return;
                }

                pnlCreateBtn.Visible = (CurrentRole.ToLower() != "support");

                txtCreatedBy.Text = CurrentUserName;
                txtCreatedDate.Text = DateTime.Now.ToString("MM/dd/yyyy");

                LoadTickets();
            }
        }

        private void LoadTickets()
        {
            string search = txtSearch.Text.Trim();
            string filterStatus = ddlFilterStatus.SelectedValue;
            string filterPriority = ddlFilterPriority.SelectedValue;
            string role = CurrentRole.ToLower();
            int userId = CurrentUserID;

            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string sql = @"
                        SELECT T.TICKET_ID, T.TICKET_NUMBER, T.TITLE, T.STATUS, T.PRIORITY,
                               T.CREATED_AT, T.UPDATED_AT, T.CREATED_BY_USER_ID, T.ASSIGNED_TO_USER_ID,
                               U.FULL_NAME AS CREATED_BY_NAME, U.ROLE AS CREATED_BY_ROLE,
                               A.FULL_NAME AS ASSIGNED_TO_NAME
                        FROM BI_OJT.TICKETS T
                        LEFT JOIN BI_OJT.USERS U ON T.CREATED_BY_USER_ID = U.USER_ID
                        LEFT JOIN BI_OJT.USERS A ON T.ASSIGNED_TO_USER_ID = A.USER_ID
                        WHERE 1=1 ";

                    if (role == "user")
                        sql += " AND T.CREATED_BY_USER_ID = :userId ";

                    if (!string.IsNullOrEmpty(search))
                        sql += " AND (UPPER(T.TICKET_NUMBER) LIKE UPPER(:search) OR UPPER(T.TITLE) LIKE UPPER(:search)) ";

                    if (!string.IsNullOrEmpty(filterStatus))
                        sql += " AND UPPER(T.STATUS) = UPPER(:filterStatus) ";

                    if (!string.IsNullOrEmpty(filterPriority))
                        sql += " AND UPPER(T.PRIORITY) = UPPER(:filterPriority) ";

                    sql += " ORDER BY T.CREATED_AT DESC ";

                    OracleCommand cmd = new OracleCommand(sql, conn);

                    if (role == "user")
                        cmd.Parameters.Add("userId", OracleDbType.Int32).Value = userId;

                    if (!string.IsNullOrEmpty(search))
                        cmd.Parameters.Add("search", OracleDbType.Varchar2).Value = "%" + search + "%";

                    if (!string.IsNullOrEmpty(filterStatus))
                        cmd.Parameters.Add("filterStatus", OracleDbType.Varchar2).Value = filterStatus;

                    if (!string.IsNullOrEmpty(filterPriority))
                        cmd.Parameters.Add("filterPriority", OracleDbType.Varchar2).Value = filterPriority;

                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    int totalRecords = dt.Rows.Count;
                    int totalPages = (int)Math.Ceiling((double)totalRecords / PageSize);
                    if (CurrentPage > totalPages && totalPages > 0) CurrentPage = totalPages;

                    int startIndex = (CurrentPage - 1) * PageSize;
                    DataTable pagedDt = dt.Clone();
                    for (int i = startIndex; i < Math.Min(startIndex + PageSize, totalRecords); i++)
                        pagedDt.ImportRow(dt.Rows[i]);

                    if (pagedDt.Rows.Count == 0)
                    {
                        pnlEmpty.Visible = true;
                        rptTickets.Visible = false;
                    }
                    else
                    {
                        pnlEmpty.Visible = false;
                        rptTickets.Visible = true;
                        rptTickets.DataSource = pagedDt;
                        rptTickets.DataBind();
                    }

                    lblPaginationInfo.Text = totalRecords == 0 ? "No records found" :
                        $"Showing {startIndex + 1}–{Math.Min(startIndex + PageSize, totalRecords)} of {totalRecords} tickets";

                    btnPrev.Enabled = CurrentPage > 1;
                    btnNext.Enabled = CurrentPage < totalPages;
                }
            }
            catch (Exception ex)
            {
                ShowError("Error loading tickets: " + ex.Message);
            }
        }

        protected void rptTickets_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                DataRowView row = (DataRowView)e.Item.DataItem;

                DropDownList ddlRowStatus = (DropDownList)e.Item.FindControl("ddlRowStatus");
                if (ddlRowStatus != null && ddlRowStatus.Visible)
                {
                    string currentStatus = row["STATUS"].ToString();
                    if (ddlRowStatus.Items.FindByValue(currentStatus) != null)
                        ddlRowStatus.SelectedValue = currentStatus;

                    ddlRowStatus.Attributes["data-oldvalue"] = currentStatus;

                    string role = CurrentRole.ToLower();

                    if (role == "admin" || role == "user")
                    {
                        ddlRowStatus.Attributes["onchange"] = "return confirmStatusChange(this);";
                    }
                    

                }

                DropDownList ddlRowPriority = (DropDownList)e.Item.FindControl("ddlRowPriority");
                if (ddlRowPriority != null && ddlRowPriority.Visible)
                {
                    string currentPriority = row["PRIORITY"]?.ToString()?.ToUpper() ?? "";
                    if (!string.IsNullOrEmpty(currentPriority) && ddlRowPriority.Items.FindByValue(currentPriority) != null)
                        ddlRowPriority.SelectedValue = currentPriority;
                    else
                        ddlRowPriority.SelectedValue = "";
                }

                DropDownList ddlRowAssign = (DropDownList)e.Item.FindControl("ddlRowAssign");
                if (ddlRowAssign != null && ddlRowAssign.Visible)
                {
                    LoadSupportUsersIntoDropDown(ddlRowAssign);
                    if (row["ASSIGNED_TO_USER_ID"] != DBNull.Value)
                    {
                        string assignedId = row["ASSIGNED_TO_USER_ID"].ToString();
                        if (ddlRowAssign.Items.FindByValue(assignedId) != null)
                            ddlRowAssign.SelectedValue = assignedId;
                    }
                }
            }
        }

        private void LoadSupportUsersIntoDropDown(DropDownList ddl)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT USER_ID, FULL_NAME 
                                   FROM BI_OJT.USERS 
                                   WHERE UPPER(ROLE) = 'SUPPORT' 
                                   AND UPPER(STATUS) = 'ACTIVE'
                                   ORDER BY FULL_NAME";

                    OracleCommand cmd = new OracleCommand(sql, conn);
                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    ddl.Items.Clear();
                    ddl.Items.Add(new System.Web.UI.WebControls.ListItem("-- Unassigned --", ""));

                    foreach (DataRow row in dt.Rows)
                    {
                        ddl.Items.Add(new System.Web.UI.WebControls.ListItem(
                            row["FULL_NAME"].ToString(),
                            row["USER_ID"].ToString()
                        ));
                    }
                }
            }
            catch { }
        }

        protected void ddlRowStatus_Changed(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            RepeaterItem item = (RepeaterItem)ddl.NamingContainer;
            HiddenField hf = (HiddenField)item.FindControl("hfRowTicketId");

            int ticketId = Convert.ToInt32(hf.Value);
            string newStatus = ddl.SelectedValue;

            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    if (CurrentRole.ToLower() == "user")
                    {
                        string checkSql = "SELECT CREATED_BY_USER_ID FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId";
                        using (OracleCommand checkCmd = new OracleCommand(checkSql, conn))
                        {
                            checkCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                            object ownerId = checkCmd.ExecuteScalar();

                            if (ownerId == null || Convert.ToInt32(ownerId) != CurrentUserID)
                            {
                                ShowError("You can only update your own tickets.");
                                LoadTickets();
                                return;
                            }
                        }
                    }

                    var oldSnap = GetTicketSnapshot(ticketId, conn);

                    string sql = @"UPDATE BI_OJT.TICKETS 
                                   SET STATUS = :status, UPDATED_AT = SYSDATE";

                    if (newStatus.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
                    {
                        sql += ", RESOLVED_AT = SYSDATE";
                    }
                    else if (newStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                    {
                        sql += ", CLOSED_AT = SYSDATE";
                    }

                    sql += " WHERE TICKET_ID = :ticketId";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = newStatus;
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = GetTicketSnapshot(ticketId, conn);
                    AuditHelper.LogAction(CurrentUserID, "UPDATE_STATUS", "TICKETS", ticketId, oldSnap, newSnap);

                    ShowSuccess("Status updated successfully!");
                    LoadTickets();
                }
            }
            catch (Exception ex)
            {
                ShowError("Error updating status: " + ex.Message);
                LoadTickets();
            }
        }

        protected void ddlRowAssign_Changed(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            RepeaterItem item = (RepeaterItem)ddl.NamingContainer;
            HiddenField hf = (HiddenField)item.FindControl("hfRowTicketId");

            int ticketId = Convert.ToInt32(hf.Value);
            string assignedTo = ddl.SelectedValue;

            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    var oldSnap = GetTicketSnapshot(ticketId, conn);

                    string sql = @"UPDATE BI_OJT.TICKETS 
                                   SET ASSIGNED_TO_USER_ID = :assignedTo, UPDATED_AT = SYSDATE 
                                   WHERE TICKET_ID = :ticketId";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add("assignedTo", OracleDbType.Int32).Value =
                            string.IsNullOrEmpty(assignedTo) ? (object)DBNull.Value : Convert.ToInt32(assignedTo);
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = GetTicketSnapshot(ticketId, conn);
                    AuditHelper.LogAction(CurrentUserID, "UPDATE_ASSIGNMENT", "TICKETS", ticketId, oldSnap, newSnap);

                    ShowSuccess("Assignment updated successfully!");
                    LoadTickets();
                }
            }
            catch (Exception ex)
            {
                ShowError("Error updating assignment: " + ex.Message);
                LoadTickets();
            }
        }

        protected void ddlRowPriority_Changed(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            RepeaterItem item = (RepeaterItem)ddl.NamingContainer;
            HiddenField hf = (HiddenField)item.FindControl("hfRowTicketId");

            int ticketId = Convert.ToInt32(hf.Value);
            string newPriority = ddl.SelectedValue;

            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    var oldSnap = GetTicketSnapshot(ticketId, conn);

                    string sql = @"UPDATE BI_OJT.TICKETS 
                                   SET PRIORITY = :priority, UPDATED_AT = SYSDATE 
                                   WHERE TICKET_ID = :ticketId";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add("priority", OracleDbType.Varchar2).Value =
                            string.IsNullOrEmpty(newPriority) ? (object)DBNull.Value : newPriority;
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = GetTicketSnapshot(ticketId, conn);
                    AuditHelper.LogAction(CurrentUserID, "UPDATE_PRIORITY", "TICKETS", ticketId, oldSnap, newSnap);

                    ShowSuccess("Priority updated successfully!");
                    LoadTickets();
                }
            }
            catch (Exception ex)
            {
                ShowError("Error updating priority: " + ex.Message);
                LoadTickets();
            }
        }

        protected void btnCreateTicket_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string year = DateTime.Now.Year.ToString();
                    OracleCommand seqCmd = new OracleCommand("SELECT BI_OJT.TICKETS_NUM_SEQ.NEXTVAL FROM DUAL", conn);
                    decimal nextNum = Convert.ToDecimal(seqCmd.ExecuteScalar());
                    string ticketNumber = $"TKT-{year}-{((int)nextNum).ToString("D4")}";

                    OracleCommand idCmd = new OracleCommand("SELECT BI_OJT.TICKETS_SEQ.NEXTVAL FROM DUAL", conn);
                    decimal ticketId = Convert.ToDecimal(idCmd.ExecuteScalar());

                    string sql = @"INSERT INTO BI_OJT.TICKETS 
                        (TICKET_ID, TICKET_NUMBER, TITLE, DESCRIPTION, STATUS, 
                         CREATED_BY_USER_ID, CREATED_AT, UPDATED_AT)
                        VALUES 
                        (:ticketId, :ticketNumber, :title, :description, 'Pending Approval',
                         :createdBy, SYSDATE, SYSDATE)";

                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.Parameters.Add("ticketId", OracleDbType.Decimal).Value = ticketId;
                    cmd.Parameters.Add("ticketNumber", OracleDbType.Varchar2).Value = ticketNumber;
                    cmd.Parameters.Add("title", OracleDbType.Varchar2).Value = txtTitle.Text.Trim();
                    cmd.Parameters.Add("description", OracleDbType.Clob).Value = txtDescription.Text.Trim();
                    cmd.Parameters.Add("createdBy", OracleDbType.Int32).Value = CurrentUserID;
                    cmd.ExecuteNonQuery();

                    var newSnap = GetTicketSnapshot((int)ticketId, conn);
                    AuditHelper.LogAction(CurrentUserID, "CREATE_TICKET", "TICKETS", (int)ticketId, null, newSnap);

                    txtTitle.Text = "";
                    txtDescription.Text = "";

                    hfShowModal.Value = "";
                    ShowSuccess($"Ticket {ticketNumber} submitted successfully! Status: Pending Approval.");
                    LoadTickets();
                }
            }
            catch (Exception ex)
            {
                hfShowModal.Value = "create";
                ShowError("Error creating ticket: " + ex.Message);
            }
        }

        protected void rptTickets_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int ticketId = Convert.ToInt32(e.CommandArgument);

            switch (e.CommandName)
            {
                case "ViewTicket":
                    LoadTicketForView(ticketId);
                    break;
                case "DeleteTicket":
                    DeleteTicket(ticketId);
                    break;
            }
        }

        private void LoadTicketForView(int ticketId)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT T.*, 
                                   U.FULL_NAME AS CREATED_BY_NAME,
                                   A.FULL_NAME AS ASSIGNED_TO_NAME,
                                   A.ROLE AS ASSIGNED_TO_ROLE
                                   FROM BI_OJT.TICKETS T
                                   LEFT JOIN BI_OJT.USERS U ON T.CREATED_BY_USER_ID = U.USER_ID
                                   LEFT JOIN BI_OJT.USERS A ON T.ASSIGNED_TO_USER_ID = A.USER_ID
                                   WHERE T.TICKET_ID = :ticketId";

                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        lblViewTicketNumber.Text = row["TICKET_NUMBER"].ToString();
                        lblViewTitle.Text = row["TITLE"].ToString();
                        lblViewDescription.Text = row["DESCRIPTION"].ToString();
                        lblViewStatus.Text = row["STATUS"].ToString();
                        lblViewCreatedBy.Text = row["CREATED_BY_NAME"].ToString();
                        lblViewCreatedDate.Text = Convert.ToDateTime(row["CREATED_AT"]).ToString("MM/dd/yyyy hh:mm tt");
                        lblViewAssignedTo.Text = string.IsNullOrEmpty(row["ASSIGNED_TO_NAME"].ToString()) ? "Unassigned" : row["ASSIGNED_TO_NAME"].ToString();
                        lblViewAssignedToRole.Text = string.IsNullOrEmpty(row["ASSIGNED_TO_ROLE"].ToString()) ? "-" : row["ASSIGNED_TO_ROLE"].ToString();

                        LoadTicketRemarks(ticketId, conn);

                        hfShowModal.Value = "view";
                        LoadTickets();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Error loading ticket details: " + ex.Message);
            }
        }

        private void LoadTicketRemarks(int ticketId, OracleConnection conn)
        {
            try
            {
                string sql = @"SELECT TR.REMARK_TEXT, TR.CREATED_AT, U.FULL_NAME
                               FROM BI_OJT.TICKET_REMARKS TR
                               LEFT JOIN BI_OJT.USERS U ON TR.USER_ID = U.USER_ID
                               WHERE TR.TICKET_ID = :ticketId
                               ORDER BY TR.CREATED_AT ASC";

                OracleCommand cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                OracleDataAdapter da = new OracleDataAdapter(cmd);
                DataTable dtRemarks = new DataTable();
                da.Fill(dtRemarks);

                if (dtRemarks.Rows.Count > 0)
                {
                    rptRemarks.DataSource = dtRemarks;
                    rptRemarks.DataBind();
                    pnlNoRemarks.Visible = false;
                }
                else
                {
                    rptRemarks.DataSource = null;
                    rptRemarks.DataBind();
                    pnlNoRemarks.Visible = true;
                }
            }
            catch (Exception ex)
            {
                pnlNoRemarks.Visible = true;
                rptRemarks.DataSource = null;
                rptRemarks.DataBind();
            }
        }

        private void DeleteTicket(int ticketId)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string checkSql = @"SELECT CREATED_BY_USER_ID, U.ROLE AS CREATED_BY_ROLE
                                        FROM BI_OJT.TICKETS T
                                        LEFT JOIN BI_OJT.USERS U ON T.CREATED_BY_USER_ID = U.USER_ID
                                        WHERE TICKET_ID = :ticketId";

                    OracleCommand checkCmd = new OracleCommand(checkSql, conn);
                    checkCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                    OracleDataAdapter checkDa = new OracleDataAdapter(checkCmd);
                    DataTable checkDt = new DataTable();
                    checkDa.Fill(checkDt);

                    if (checkDt.Rows.Count == 0)
                    {
                        ShowError("Ticket not found.");
                        return;
                    }

                    DataRow ticketRow = checkDt.Rows[0];
                    int createdBy = Convert.ToInt32(ticketRow["CREATED_BY_USER_ID"]);
                    string createdByRole = ticketRow["CREATED_BY_ROLE"].ToString().ToLower();

                    if (CurrentRole.ToLower() != "admin")
                    {
                        ShowError("Only Admins can delete tickets.");
                        return;
                    }

                    var oldSnap = GetTicketSnapshot(ticketId, conn);

                    string sql = "DELETE FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId";
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                    cmd.ExecuteNonQuery();

                    AuditHelper.LogAction(CurrentUserID, "DELETE_TICKET", "TICKETS", ticketId, oldSnap, null);
                    ShowSuccess("Ticket deleted successfully.");
                    LoadTickets();
                }
            }
            catch (Exception ex)
            {
                ShowError("Error deleting ticket: " + ex.Message);
            }
        }

        private Dictionary<string, object> GetTicketSnapshot(int ticketId, OracleConnection conn)
        {
            string sql = @"SELECT STATUS, CREATED_BY_USER_ID, ASSIGNED_TO_USER_ID, PRIORITY
                           FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    var snap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    snap["STATUS"] = reader["STATUS"] == DBNull.Value ? null : reader["STATUS"].ToString();
                    snap["CREATED_BY_USER_ID"] = reader["CREATED_BY_USER_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["CREATED_BY_USER_ID"]);
                    snap["ASSIGNED_TO_USER_ID"] = reader["ASSIGNED_TO_USER_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ASSIGNED_TO_USER_ID"]);
                    snap["PRIORITY"] = reader["PRIORITY"] == DBNull.Value ? null : reader["PRIORITY"].ToString();
                    return snap;
                }
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            CurrentPage = 1;
            LoadTickets();
        }

        protected void ddlFilter_Changed(object sender, EventArgs e)
        {
            CurrentPage = 1;
            LoadTickets();
        }

        protected void btnPrev_Click(object sender, EventArgs e)
        {
            if (CurrentPage > 1) CurrentPage--;
            LoadTickets();
        }

        protected void btnNext_Click(object sender, EventArgs e)
        {
            CurrentPage++;
            LoadTickets();
        }

        public string GetStatusBadge(string status)
        {
            switch (status?.ToLower())
            {
                case "pending approval": return "badge-pending-approval";
                case "open": return "badge-open";
                case "in progress": return "badge-in-progress";
                case "resolved": return "badge-resolved";
                case "closed": return "badge-closed";
                case "overdue": return "badge-overdue";
                default: return "badge-secondary";
            }
        }

        public string GetPriorityBadge(string priority)
        {
            switch (priority?.ToLower())
            {
                case "low": return "badge-low";
                case "medium": return "badge-medium";
                case "high": return "badge-high";
                case "urgent": return "badge-urgent";
                default: return "badge-not-set";
            }
        }

        private void ShowSuccess(string msg)
        {
            hfSwalMessage.Value = msg;
            hfSwalType.Value = "success";
            pnlSuccess.Visible = false;
            pnlError.Visible = false;
        }

        private void ShowError(string msg)
        {
            hfSwalMessage.Value = msg;
            hfSwalType.Value = "error";
            pnlSuccess.Visible = false;
            pnlError.Visible = false;
        }
    }
}
