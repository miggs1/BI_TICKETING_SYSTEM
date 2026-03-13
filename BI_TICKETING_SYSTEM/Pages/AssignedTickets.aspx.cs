using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using Oracle.ManagedDataAccess.Client;
using BI_TICKETING_SYSTEM.Helpers;
using System.Collections.Generic;

namespace BI_TICKETING_SYSTEM.Pages
{
    public partial class AssignedTickets : Page
    {
        // ===== PAGINATION =====
        private int PageSize = 10;
        private int CurrentPage
        {
            get { return ViewState["CurrentPage"] != null ? (int)ViewState["CurrentPage"] : 1; }
            set { ViewState["CurrentPage"] = value; }
        }

        // ===== SESSION HELPERS =====
        private string CurrentRole => Session["UserRole"]?.ToString() ?? "User";
        private int CurrentUserID => Convert.ToInt32(Session["UserID"] ?? 0);
        private string CurrentUserName => Session["UserName"]?.ToString() ?? "";

        // ===== PAGE LOAD =====
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserName"] == null)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                // User cannot access this page (unchanged)
                if (CurrentRole.ToLower() == "user")
                {
                    Response.Redirect("~/Default.aspx");
                    return;
                }

                // Load tickets (modified: only tickets assigned to current user)
                LoadTickets();
            }
        }

        // ===== LOAD TICKETS =====
        private void LoadTickets()
        {
            string search = txtSearch.Text.Trim();
            string filterStatus = ddlFilterStatus.SelectedValue;
            string filterPriority = ddlFilterPriority.SelectedValue;
            int userId = CurrentUserID;

            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string sql = @"
                        SELECT T.TICKET_ID, T.TICKET_NUMBER, T.TITLE, T.STATUS, T.PRIORITY,
                               T.CREATED_AT,
                               U.FULL_NAME AS CREATED_BY_NAME,
                               A.FULL_NAME AS ASSIGNED_TO_NAME
                        FROM BI_OJT.TICKETS T
                        LEFT JOIN BI_OJT.USERS U ON T.CREATED_BY_USER_ID = U.USER_ID
                        LEFT JOIN BI_OJT.USERS A ON T.ASSIGNED_TO_USER_ID = A.USER_ID
                        WHERE 1=1 ";

                    // Always restrict to tickets assigned to the current user
                    sql += " AND T.ASSIGNED_TO_USER_ID = :assignedTo ";

                    if (!string.IsNullOrEmpty(search))
                        sql += " AND (UPPER(T.TICKET_NUMBER) LIKE UPPER(:search) OR UPPER(T.TITLE) LIKE UPPER(:search)) ";

                    if (!string.IsNullOrEmpty(filterStatus))
                        sql += " AND UPPER(T.STATUS) = UPPER(:filterStatus) ";

                    if (!string.IsNullOrEmpty(filterPriority))
                        sql += " AND UPPER(T.PRIORITY) = UPPER(:filterPriority) ";

                    sql += " ORDER BY T.CREATED_AT DESC ";

                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.Parameters.Add("assignedTo", OracleDbType.Int32).Value = userId;

                    if (!string.IsNullOrEmpty(search))
                        cmd.Parameters.Add("search", OracleDbType.Varchar2).Value = "%" + search + "%";

                    if (!string.IsNullOrEmpty(filterStatus))
                        cmd.Parameters.Add("filterStatus", OracleDbType.Varchar2).Value = filterStatus;

                    if (!string.IsNullOrEmpty(filterPriority))
                        cmd.Parameters.Add("filterPriority", OracleDbType.Varchar2).Value = filterPriority;

                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // Pagination
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

        // ===== REPEATER ITEM COMMAND =====
        protected void rptTickets_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int ticketId = Convert.ToInt32(e.CommandArgument);

            switch (e.CommandName)
            {
                case "ViewTicket":
                    LoadTicketForView(ticketId);
                    break;
                case "ApproveTicket":
                    if (CurrentRole.ToLower() == "admin")
                        ApproveTicket(ticketId);
                    break;
                case "EditTicket":
                    if (CurrentRole.ToLower() == "admin" ||
                        CurrentRole.ToLower() == "support" ||
                        CurrentRole.ToLower() == "user")
                        LoadTicketForEdit(ticketId);
                    break;
                case "DeleteTicket":
                    if (CurrentRole.ToLower() == "admin" || CurrentRole.ToLower() == "user")
                        DeleteTicket(ticketId);
                    break;
            }
        }

        // ===== VIEW TICKET =====
        private void LoadTicketForView(int ticketId)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT T.*, 
                                   U.FULL_NAME AS CREATED_BY_NAME,
                                   A.FULL_NAME AS ASSIGNED_TO_NAME
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
                        lblViewPriority.Text = string.IsNullOrEmpty(row["PRIORITY"].ToString()) ? "Not Set" : row["PRIORITY"].ToString();
                        lblViewCategory.Text = string.IsNullOrEmpty(row["CATEGORY"].ToString()) ? "-" : row["CATEGORY"].ToString();
                        lblViewCreatedBy.Text = row["CREATED_BY_NAME"].ToString();
                        lblViewCreatedDate.Text = Convert.ToDateTime(row["CREATED_AT"]).ToString("MM/dd/yyyy hh:mm tt");
                        lblViewAssignedTo.Text = string.IsNullOrEmpty(row["ASSIGNED_TO_NAME"].ToString()) ? "Unassigned" : row["ASSIGNED_TO_NAME"].ToString();

                        hfViewTicketId.Value = ticketId.ToString();

                        LoadRemarks(ticketId);

                        // Determine whether current user may add remarks:
                        // Admins may always add. Support may add only if assigned to this ticket.
                        bool canAddRemark = false;
                        string role = CurrentRole?.ToLower() ?? "user";
                        if (role == "admin")
                        {
                            canAddRemark = true;
                        }
                        else if (role == "support")
                        {
                            if (row["ASSIGNED_TO_USER_ID"] != DBNull.Value)
                            {
                                int assignedId = Convert.ToInt32(row["ASSIGNED_TO_USER_ID"]);
                                if (assignedId == CurrentUserID) canAddRemark = true;
                            }
                        }

                        // show/hide add-remark UI server-side
                        pnlAddRemark.Visible = canAddRemark;

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

        // ===== LOAD REMARKS =====
        private void LoadRemarks(int ticketId)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"
                        SELECT R.REMARK_ID,
                               R.TICKET_ID,
                               R.REMARK_TEXT,
                               R.CREATED_AT,
                               U.FULL_NAME AS CREATED_BY_NAME
                        FROM BI_OJT.TICKET_REMARKS R
                        LEFT JOIN BI_OJT.USERS U
                        ON R.USER_ID = U.USER_ID
                        WHERE R.TICKET_ID = :ticketId
                        ORDER BY R.CREATED_AT DESC";
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    rptRemarks.DataSource = dt;
                    rptRemarks.DataBind();

                    // Show "No remarks yet." panel when there are no rows
                    pnlNoRemarks.Visible = (dt.Rows.Count == 0);
                    rptRemarks.Visible = (dt.Rows.Count > 0);
                }
            }
            catch (Exception ex)
            {
                ShowError("Error loading remarks: " + ex.Message);
            }
        }

        // ===== ADD REMARK =====
        protected void btnAddRemark_Click(object sender, EventArgs e)
        {
            int ticketId = Convert.ToInt32(hfViewTicketId.Value);
            string remark = txtNewRemark.Text.Trim();

            if (string.IsNullOrEmpty(remark)) return;

            using (OracleConnection conn = new OracleConnection(DatabaseHelper.GetConnectionString()))
            {
                // Use the exact column names from your CREATE TABLE statement
                string sql = @"
            INSERT INTO BI_OJT.TICKET_REMARKS 
            (TICKET_ID, USER_ID, REMARK_TEXT, CREATED_AT, UPDATED_AT) 
            VALUES 
            (:ticketId, :userId, :remarkText, SYSDATE, SYSDATE)";

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(":ticketId", OracleDbType.Int32).Value = ticketId;
                    cmd.Parameters.Add(":userId", OracleDbType.Int32).Value = CurrentUserID;
                    cmd.Parameters.Add(":remarkText", OracleDbType.Clob).Value = remark;

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            txtNewRemark.Text = "";
            LoadTicketForView(ticketId); // Refresh the view
            ShowSuccess("Remark added successfully!");
        }
        
        // ===== APPROVE TICKET =====
        private void ApproveTicket(int ticketId)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        var oldSnap = GetTicketSnapshot(ticketId, conn);

                        string sql = @"UPDATE BI_OJT.TICKETS 
                                       SET STATUS = :status, UPDATED_AT = SYSDATE 
                                       WHERE TICKET_ID = :ticketId";
                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.Transaction = tran;
                            cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = "Open";
                            cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                            int rows = cmd.ExecuteNonQuery();
                            if (rows != 1)
                            {
                                tran.Rollback();
                                ShowError("Ticket not found or not updated.");
                                return;
                            }
                        }

                        var newSnap = GetTicketSnapshot(ticketId, conn);

                        string oldJson = oldSnap == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(oldSnap);
                        string newJson = newSnap == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(newSnap);

                        AuditHelper.Log(CurrentUserID, "APPROVE_TICKET", oldJson, newJson);

                        tran.Commit();

                        ShowSuccess("Ticket approved successfully! Status changed to Open.");
                        LoadTickets();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Error approving ticket: " + ex.Message);
            }
        }

        // ===== EDIT TICKET =====
        private void LoadTicketForEdit(int ticketId)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT * FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId";
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        hfEditTicketId.Value = ticketId.ToString();
                        txtEditTicketNumber.Text = row["TICKET_NUMBER"].ToString();
                        ddlEditStatus.SelectedValue = row["STATUS"].ToString();

                        if (CurrentRole.ToLower() == "admin")
                        {
                            // Admin sees all fields
                            pnlEditTitle.Visible = true;
                            pnlEditDescription.Visible = true;
                            pnlEditPriorityCategory.Visible = true;
                            pnlAssignTo.Visible = true;
                            pnlUserEdit.Visible = false;

                            txtEditTitle.Text = row["TITLE"].ToString();
                            txtEditDescription.Text = row["DESCRIPTION"].ToString();
                            txtEditCategory.Text = row["CATEGORY"].ToString();
                            ddlEditPriority.SelectedValue = row["PRIORITY"].ToString();

                            LoadSupportUsers();
                            if (row["ASSIGNED_TO_USER_ID"] != DBNull.Value)
                            {
                                string assignedId = row["ASSIGNED_TO_USER_ID"].ToString();
                                if (ddlAssignTo.Items.FindByValue(assignedId) != null)
                                    ddlAssignTo.SelectedValue = assignedId;
                            }
                        }
                        else if (CurrentRole.ToLower() == "support")
                        {
                            // Support only sees Status
                            pnlEditTitle.Visible = false;
                            pnlEditDescription.Visible = false;
                            pnlEditPriorityCategory.Visible = false;
                            pnlAssignTo.Visible = false;
                            pnlUserEdit.Visible = false;
                        }
                        else if (CurrentRole.ToLower() == "user")
                        {
                            // User sees Title, Description, Category only
                            pnlEditTitle.Visible = false;
                            pnlEditDescription.Visible = false;
                            pnlEditPriorityCategory.Visible = false;
                            pnlAssignTo.Visible = false;
                            ddlEditStatus.Enabled = false;
                            pnlUserEdit.Visible = true;

                            txtUserEditTitle.Text = row["TITLE"].ToString();
                            txtUserEditDescription.Text = row["DESCRIPTION"].ToString();
                            txtUserEditCategory.Text = row["CATEGORY"].ToString();
                        }

                        hfShowModal.Value = "edit";
                        LoadTickets();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Error loading ticket for edit: " + ex.Message);
            }
        }

        protected void btnSaveEdit_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            try
            {
                int ticketId = Convert.ToInt32(hfEditTicketId.Value);
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    var oldSnap = GetTicketSnapshot(ticketId, conn);

                    string sql;
                    OracleCommand cmd;

                    if (CurrentRole.ToLower() == "support")
                    {
                        sql = @"UPDATE BI_OJT.TICKETS 
                        SET STATUS = :status, UPDATED_AT = SYSDATE
                        WHERE TICKET_ID = :ticketId";

                        cmd = new OracleCommand(sql, conn);
                        cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = ddlEditStatus.SelectedValue;
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                    }
                    else if (CurrentRole.ToLower() == "user")
                    {
                        sql = @"UPDATE BI_OJT.TICKETS 
                        SET TITLE = :title,
                            DESCRIPTION = :description,
                            CATEGORY = :category,
                            UPDATED_AT = SYSDATE
                        WHERE TICKET_ID = :ticketId";

                        cmd = new OracleCommand(sql, conn);
                        cmd.Parameters.Add("title", OracleDbType.Varchar2).Value = txtUserEditTitle.Text.Trim();
                        cmd.Parameters.Add("description", OracleDbType.Clob).Value = txtUserEditDescription.Text.Trim();
                        cmd.Parameters.Add("category", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(txtUserEditCategory.Text.Trim()) ? (object)DBNull.Value : txtUserEditCategory.Text.Trim();
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                    }
                    else
                    {
                        sql = @"UPDATE BI_OJT.TICKETS 
                        SET TITLE = :title,
                            DESCRIPTION = :description,
                            STATUS = :status,
                            PRIORITY = :priority,
                            CATEGORY = :category,
                            ASSIGNED_TO_USER_ID = :assignedTo,
                            UPDATED_AT = SYSDATE
                        WHERE TICKET_ID = :ticketId";

                        cmd = new OracleCommand(sql, conn);
                        cmd.Parameters.Add("title", OracleDbType.Varchar2).Value = txtEditTitle.Text.Trim();
                        cmd.Parameters.Add("description", OracleDbType.Clob).Value = txtEditDescription.Text.Trim();
                        cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = ddlEditStatus.SelectedValue;
                        cmd.Parameters.Add("priority", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(ddlEditPriority.SelectedValue) ? (object)DBNull.Value : ddlEditPriority.SelectedValue;
                        cmd.Parameters.Add("category", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(txtEditCategory.Text.Trim()) ? (object)DBNull.Value : txtEditCategory.Text.Trim();
                        cmd.Parameters.Add("assignedTo", OracleDbType.Int32).Value = string.IsNullOrEmpty(ddlAssignTo.SelectedValue) ? (object)DBNull.Value : Convert.ToInt32(ddlAssignTo.SelectedValue);
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                    }

                    cmd.ExecuteNonQuery();

                    var newSnap = GetTicketSnapshot(ticketId, conn);
                    AuditHelper.LogAction(CurrentUserID, "EDIT_TICKET", "TICKETS", ticketId, oldSnap, newSnap);

                    hfShowModal.Value = "";
                    ShowSuccess("Ticket updated successfully!");
                    LoadTickets();
                }
            }
            catch (Exception ex)
            {
                hfShowModal.Value = "edit";
                ShowError("Error updating ticket: " + ex.Message);
            }
        }

        // ===== DELETE TICKET =====
        private void DeleteTicket(int ticketId)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

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

        // ===== LOAD SUPPORT USERS =====
        private void LoadSupportUsers()
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

                    ddlAssignTo.Items.Clear();
                    ddlAssignTo.Items.Add(new ListItem("-- Unassigned --", ""));

                    foreach (DataRow row in dt.Rows)
                    {
                        ddlAssignTo.Items.Add(new ListItem(
                            row["FULL_NAME"].ToString(),
                            row["USER_ID"].ToString()
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Error loading support users: " + ex.Message);
            }
        }

        // ===== Helper: snapshot of ticket values we track in audit =====
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

        // ===== SEARCH & FILTER =====
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

        // ===== PAGINATION =====
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

        // ===== BADGE HELPERS =====
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

        // ===== SHOW ALERTS =====
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