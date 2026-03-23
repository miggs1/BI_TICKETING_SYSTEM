using BI_TICKETING_SYSTEM.Helpers;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using Image = iTextSharp.text.Image;

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
            int userId = CurrentUserID;
            string role = CurrentRole.ToLower();

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

                    if (role == "support")
                    {
                        sql += " AND T.ASSIGNED_TO_USER_ID = :assignedTo ";
                    }
                    else if (role == "admin")
                    {
                        sql += " AND T.ASSIGNED_TO_USER_ID IS NOT NULL ";
                    }

                    if (!string.IsNullOrEmpty(search))
                        sql += " AND (UPPER(T.TICKET_NUMBER) LIKE UPPER(:search) OR UPPER(T.TITLE) LIKE UPPER(:search)) ";

                    if (!string.IsNullOrEmpty(filterStatus))
                        sql += " AND UPPER(T.STATUS) = UPPER(:filterStatus) ";

                    sql += " ORDER BY T.CREATED_AT DESC ";

                    OracleCommand cmd = new OracleCommand(sql, conn);

                    if (role == "support")
                    {
                        cmd.Parameters.Add("assignedTo", OracleDbType.Int32).Value = userId;
                    }

                    if (!string.IsNullOrEmpty(search))
                        cmd.Parameters.Add("search", OracleDbType.Varchar2).Value = "%" + search + "%";

                    if (!string.IsNullOrEmpty(filterStatus))
                        cmd.Parameters.Add("filterStatus", OracleDbType.Varchar2).Value = filterStatus;

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
                        lblViewCreatedBy.Text = row["CREATED_BY_NAME"].ToString();
                        lblViewCreatedDate.Text = Convert.ToDateTime(row["CREATED_AT"]).ToString("MM/dd/yyyy hh:mm tt");
                        lblViewAssignedTo.Text = string.IsNullOrEmpty(row["ASSIGNED_TO_NAME"].ToString()) ? "Unassigned" : row["ASSIGNED_TO_NAME"].ToString();
                        lblViewPriority.Text = row["PRIORITY"] == DBNull.Value ? "N/A": System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(row["PRIORITY"]?.ToString().ToLower());

                        hfViewTicketId.Value = ticketId.ToString();

                        LoadRemarks(ticketId);

                        // Determine whether current user may add remarks:
                        // Admins may always add. Support may add only if assigned to this ticket.
                        // show/hide add-remark UI server-side
                        bool canAddRemark = false;
                        bool isClosed = string.Equals(row["STATUS"].ToString(), "Closed", StringComparison.OrdinalIgnoreCase);

                        string role = CurrentRole.ToLower();

                        if (!isClosed)
                        {
                            if (role == "admin")
                            {
                                canAddRemark = true;
                            }
                            else if (role == "support")
                            {
                                if (row["ASSIGNED_TO_USER_ID"] != DBNull.Value)
                                {
                                    int assignedId = Convert.ToInt32(row["ASSIGNED_TO_USER_ID"]);
                                    if (assignedId == CurrentUserID)
                                    {
                                        canAddRemark = true;
                                    }
                                }
                            }
                        }

                        pnlAddRemark.Visible = canAddRemark;
                        pnlClosedRemarkNotice.Visible = isClosed;
                        txtNewRemark.Text = "";

                        hfShowModal.Value = "view";
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

            if (string.IsNullOrEmpty(remark))
            {
                ShowError("Please enter a remark.");
                hfShowModal.Value = "view";
                LoadTicketForView(ticketId);
                return;
            }

            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string checkStatusSql = "SELECT STATUS FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId";
                    using (OracleCommand checkCmd = new OracleCommand(checkStatusSql, conn))
                    {
                        checkCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        string status = checkCmd.ExecuteScalar()?.ToString();
                        if (string.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase))
                        {
                            ShowError("Cannot add remark to a closed ticket.");
                            hfShowModal.Value = "view";
                            LoadTicketForView(ticketId);
                            return;
                        }
                    }

                    string sql = @"
                        INSERT INTO BI_OJT.TICKET_REMARKS
                        (TICKET_ID, USER_ID, REMARK_TEXT, CREATED_AT, UPDATED_AT)
                        VALUES
                        (:ticketId, :userId, :remarkText, SYSDATE, SYSDATE)";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;
                        cmd.Parameters.Add("remarkText", OracleDbType.Clob).Value = remark;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = new Dictionary<string, object>
                    {
                        { "REMARK_TEXT",remark },
                        { "TICKET_ID", ticketId }
                    };

                    AuditHelper.LogAction(CurrentUserID, "ADD+REMARK", "TICKET_REMARKS", ticketId, null, newSnap);
                }

                txtNewRemark.Text = "";
                hfShowModal.Value = "view";
                LoadTicketForView(ticketId);
                ShowSuccess("Remark added successfully!");
            }
            catch (Exception ex)
            {
                hfShowModal.Value = "view";
                LoadTicketForView(ticketId);
                ShowError("Error adding remark: " + ex.Message);
            }
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

                        InsertStatusRemark(ticketId, "Open", conn);

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
                            pnlAssignTo.Visible = true;
                            pnlUserEdit.Visible = false;

                            txtEditTitle.Text = row["TITLE"].ToString();
                            txtEditDescription.Text = row["DESCRIPTION"].ToString();
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
                            pnlAssignTo.Visible = false;
                            pnlUserEdit.Visible = false;
                            pnlEditPriorityCategory.Visible = false;
                        }
                        else if (CurrentRole.ToLower() == "user")
                        {
                            // User sees Title, Description, Priority only
                            pnlEditTitle.Visible = false;
                            pnlEditDescription.Visible = false;
                            pnlAssignTo.Visible = false;
                            ddlEditStatus.Enabled = false;
                            pnlUserEdit.Visible = true;
                            pnlEditPriorityCategory.Visible = true;

                            txtUserEditTitle.Text = row["TITLE"].ToString();
                            txtUserEditDescription.Text = row["DESCRIPTION"].ToString();
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
                            UPDATED_AT = SYSDATE
                        WHERE TICKET_ID = :ticketId";

                        cmd = new OracleCommand(sql, conn);
                        cmd.Parameters.Add("title", OracleDbType.Varchar2).Value = txtUserEditTitle.Text.Trim();
                        cmd.Parameters.Add("description", OracleDbType.Clob).Value = txtUserEditDescription.Text.Trim();
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                    }
                    else
                    {
                        sql = @"UPDATE BI_OJT.TICKETS 
                        SET TITLE = :title,
                            DESCRIPTION = :description,
                            STATUS = :status,
                            PRIORITY = :priority,
                            ASSIGNED_TO_USER_ID = :assignedTo,
                            UPDATED_AT = SYSDATE
                        WHERE TICKET_ID = :ticketId";

                        cmd = new OracleCommand(sql, conn);
                        cmd.Parameters.Add("title", OracleDbType.Varchar2).Value = txtEditTitle.Text.Trim();
                        cmd.Parameters.Add("description", OracleDbType.Clob).Value = txtEditDescription.Text.Trim();
                        cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = ddlEditStatus.SelectedValue;
                        cmd.Parameters.Add("priority", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(ddlEditPriority.SelectedValue) ? (object)DBNull.Value : ddlEditPriority.SelectedValue;
                        cmd.Parameters.Add("assignedTo", OracleDbType.Int32).Value = string.IsNullOrEmpty(ddlAssignTo.SelectedValue) ? (object)DBNull.Value : Convert.ToInt32(ddlAssignTo.SelectedValue);
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                    }

                    cmd.ExecuteNonQuery();

                    string oldStatus = oldSnap != null && oldSnap.ContainsKey("STATUS") ? oldSnap["STATUS"]?.ToString() : null;
                    string newEditStatus = null;
                    if (CurrentRole.ToLower() == "support" || CurrentRole.ToLower() == "admin")
                        newEditStatus = ddlEditStatus.SelectedValue;

                    if (newEditStatus != null && !string.Equals(oldStatus, newEditStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        InsertStatusRemark(ticketId, newEditStatus, conn);
                    }

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
                    string oldJson = oldSnap == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(oldSnap);

                    using (var delRemarks = new OracleCommand("DELETE FROM BI_OJT.TICKET_REMARKS WHERE TICKET_ID = :ticketId", conn))
                    {
                        delRemarks.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        delRemarks.ExecuteNonQuery();
                    }

                    using (var delAudit = new OracleCommand("DELETE FROM BI_OJT.AUDIT_LOGS WHERE TICKET_ID = :ticketId", conn))
                    {
                        delAudit.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        delAudit.ExecuteNonQuery();
                    }

                    using (var cmd = new OracleCommand("DELETE FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId", conn))
                    {
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.ExecuteNonQuery();
                    }

                    AuditHelper.Log(CurrentUserID, "DELETE_TICKET", oldJson, null);
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
                    ddlAssignTo.Items.Add(new System.Web.UI.WebControls.ListItem("-- Unassigned --", ""));

                    foreach (DataRow row in dt.Rows)
                    {
                        ddlAssignTo.Items.Add(new System.Web.UI.WebControls.ListItem(
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

        private void InsertStatusRemark(int ticketId, string newStatus, OracleConnection conn)
        {
            string sql = @"INSERT INTO BI_OJT.TICKET_REMARKS 
                (TICKET_ID, USER_ID, REMARK_TEXT, CREATED_AT, UPDATED_AT) 
                VALUES (:ticketId, :userId, :remarkText, SYSDATE, SYSDATE)";
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                cmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;
                cmd.Parameters.Add("remarkText", OracleDbType.Varchar2).Value = "Ticket Status: " + newStatus;
                cmd.ExecuteNonQuery();
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

        // ===== EXPORT =====
        private DataTable GetFilteredTickets()
        {
            string search = txtSearch.Text.Trim();
            string filterStatus = ddlFilterStatus.SelectedValue;
            int userId = CurrentUserID;
            string role = CurrentRole.ToLower();

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

                if (role == "support")
                    sql += " AND T.ASSIGNED_TO_USER_ID = :assignedTo ";
                else if (role == "admin")
                    sql += " AND T.ASSIGNED_TO_USER_ID IS NOT NULL ";

                if (!string.IsNullOrEmpty(search))
                    sql += " AND (UPPER(T.TICKET_NUMBER) LIKE UPPER(:search) OR UPPER(T.TITLE) LIKE UPPER(:search)) ";

                if (!string.IsNullOrEmpty(filterStatus))
                    sql += " AND UPPER(T.STATUS) = UPPER(:filterStatus) ";

                sql += " ORDER BY T.CREATED_AT DESC ";

                OracleCommand cmd = new OracleCommand(sql, conn);

                if (role == "support")
                    cmd.Parameters.Add("assignedTo", OracleDbType.Int32).Value = userId;

                if (!string.IsNullOrEmpty(search))
                    cmd.Parameters.Add("search", OracleDbType.Varchar2).Value = "%" + search + "%";

                if (!string.IsNullOrEmpty(filterStatus))
                    cmd.Parameters.Add("filterStatus", OracleDbType.Varchar2).Value = filterStatus;

                OracleDataAdapter da = new OracleDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }

        // ===== EXPORT TO PDF & EXCEL =====
        protected void btnExportPDF_Click(object sender, EventArgs e)
        {
            DataTable dt = GetFilteredTickets();

            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document(iTextSharp.text.PageSize.A4.Rotate(), 20, 20, 20, 20);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // ===== HEADER TABLE (3 COLUMNS: FLAG | TITLE | SEAL)
                PdfPTable headerTable = new PdfPTable(3);
                headerTable.WidthPercentage = 100;
                headerTable.SetWidths(new float[] { 1f, 3f, 1f });

                // ===== LEFT IMAGE 
                string flagPath = Server.MapPath("~/Images/ph-flag.png");
                if (File.Exists(flagPath))
                {
                    Image flag = Image.GetInstance(flagPath);
                    flag.ScaleToFit(60f, 60f);

                    PdfPCell flagCell = new PdfPCell(flag);
                    flagCell.Border = Rectangle.NO_BORDER;
                    flagCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    headerTable.AddCell(flagCell);
                }
                else
                {
                    headerTable.AddCell(new PdfPCell { Border = Rectangle.NO_BORDER });
                }

                // ===== CENTER TEXT
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                Font subFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                Paragraph headerText = new Paragraph();
                headerText.Alignment = Element.ALIGN_CENTER;
                headerText.Add(new Phrase("BUREAU OF IMMIGRATION\n", titleFont));
                headerText.Add(new Phrase("Ticketing System Report\n", titleFont));
                headerText.Add(new Phrase("\n"));
                headerText.Add(new Phrase("Status: " + (string.IsNullOrEmpty(ddlFilterStatus.SelectedValue) ? "All" : ddlFilterStatus.SelectedValue) + "\n", subFont));
                headerText.Add(new Phrase("Search: " + (string.IsNullOrEmpty(txtSearch.Text) ? "None" : txtSearch.Text) + "\n", subFont));
                headerText.Add(new Phrase("Generated: " + DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt"), subFont));

                PdfPCell textCell = new PdfPCell(headerText);
                textCell.Border = Rectangle.NO_BORDER;
                textCell.HorizontalAlignment = Element.ALIGN_CENTER;
                headerTable.AddCell(textCell);

                // ===== RIGHT IMAGE 
                string sealPath = Server.MapPath("~/Images/bi-seal.png");
                if (File.Exists(sealPath))
                {
                    Image seal = Image.GetInstance(sealPath);
                    seal.ScaleToFit(60f, 60f);

                    PdfPCell sealCell = new PdfPCell(seal);
                    sealCell.Border = Rectangle.NO_BORDER;
                    sealCell.HorizontalAlignment = Element.ALIGN_LEFT;
                    headerTable.AddCell(sealCell);
                }
                else
                {
                    headerTable.AddCell(new PdfPCell { Border = Rectangle.NO_BORDER });
                }

                doc.Add(headerTable);
                doc.Add(new Paragraph(" ")); // spacing

                // ===== TABLE
                PdfPTable table = new PdfPTable(dt.Columns.Count);
                table.WidthPercentage = 100;

                // Header styling
                foreach (DataColumn col in dt.Columns)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(col.ColumnName));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                }

                // Data
                foreach (DataRow row in dt.Rows)
                {
                    foreach (var cell in row.ItemArray)
                    {
                        table.AddCell(cell.ToString());
                    }
                }

                doc.Add(table);
                doc.Close();

                Response.ContentType = "application/pdf";
                Response.AddHeader("content-disposition", "attachment;filename=TicketReport.pdf");
                Response.BinaryWrite(ms.ToArray());
                Response.End();
            }
        }

        protected void btnExportExcel_Click(object sender, EventArgs e)
        {
            DataTable dt = GetFilteredTickets(); 

            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename=Tickets.xls");
            Response.Charset = "";
            Response.ContentType = "application/vnd.ms-excel";

            using (StringWriter sw = new StringWriter())
            {
                HtmlTextWriter hw = new HtmlTextWriter(sw);

                // =========== HEADER
                hw.Write("<h2>TICKET REPORT</h2>");
                hw.Write("<p><b>Status:</b> " + (string.IsNullOrEmpty(ddlFilterStatus.SelectedValue) ? "All" : ddlFilterStatus.SelectedValue) + "</p>");
                hw.Write("<p><b>Search:</b> " + (string.IsNullOrEmpty(txtSearch.Text) ? "None" : txtSearch.Text) + "</p>");
                hw.Write("<p>Generated: " + DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt") + "</p><br/>");

                GridView gv = new GridView();
                gv.DataSource = dt;
                gv.DataBind();
                gv.RenderControl(hw);

                Response.Output.Write(sw.ToString());
                Response.Flush();
                Response.End();
            }
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