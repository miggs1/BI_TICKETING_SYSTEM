using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using Oracle.ManagedDataAccess.Client;
using BI_TICKETING_SYSTEM.Helpers;
using System.Collections.Generic;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Configuration;
using System.Drawing;

namespace BI_TICKETING_SYSTEM.Pages
{
    public partial class Tickets : Page
    {
        private int PageSize = 10;

        private static readonly HashSet<string> AllowedAttachmentExtensions = 
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { 
                ".jpg",
                ".jpeg", 
                ".png", 
                ".pdf", 
                ".doc", 
                ".docx"
            };

        private const int MaxAttachmentSizeBytes = 10 * 1024 * 1024; //10 MB
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
                                           T.DUE_DATE, T.RESOLVED_AT,
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

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;

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

                        foreach (DataRow row in dt.Rows)
                        {
                            int ticketId = Convert.ToInt32(row["TICKET_ID"]);
                            string status = row["STATUS"]?.ToString() ?? "";
                            DateTime? dueDate = row["DUE_DATE"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(row["DUE_DATE"]);

                            SLAHelper.CheckAndLogSlaBreach(ticketId, dueDate, status);
                        }

                        int totalRecords = dt.Rows.Count;
                        int totalPages = (int)Math.Ceiling((double)totalRecords / PageSize);
                        if (CurrentPage > totalPages && totalPages > 0)
                            CurrentPage = totalPages;

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

                        lblPaginationInfo.Text = totalRecords == 0
                            ? "No records found"
                            : $"Showing {startIndex + 1}–{Math.Min(startIndex + PageSize, totalRecords)} of {totalRecords} tickets";

                        btnPrev.Enabled = CurrentPage > 1;
                        btnNext.Enabled = CurrentPage < totalPages;
                    }
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
                    ddlRowStatus.Attributes["onchange"] = "return confirmStatusChange(this);";
                }

                DropDownList ddlRowPriority = (DropDownList)e.Item.FindControl("ddlRowPriority");
                if (ddlRowPriority != null && ddlRowPriority.Visible)
                {
                    string currentPriority = row["PRIORITY"]?.ToString()?.ToUpper() ?? "";
                    if (!string.IsNullOrEmpty(currentPriority) && ddlRowPriority.Items.FindByValue(currentPriority) != null)
                        ddlRowPriority.SelectedValue = currentPriority;
                    else
                        ddlRowPriority.SelectedValue = "";

                    ddlRowPriority.Attributes["data-oldvalue"] = currentPriority;
                    ddlRowPriority.Attributes["onchange"] = "return confirmPriorityChange(this);";
                }

                DropDownList ddlRowAssign = (DropDownList)e.Item.FindControl("ddlRowAssign");
                if (ddlRowAssign != null && ddlRowAssign.Visible)
                {
                    LoadSupportUsersIntoDropDown(ddlRowAssign);
                    string assignedValue = "";
                    if (row["ASSIGNED_TO_USER_ID"] != DBNull.Value)
                    {
                        assignedValue = row["ASSIGNED_TO_USER_ID"].ToString();
                        if (ddlRowAssign.Items.FindByValue(assignedValue) != null)
                            ddlRowAssign.SelectedValue = assignedValue;
                    }

                    ddlRowAssign.Attributes["data-oldvalue"] = assignedValue;
                    ddlRowAssign.Attributes["onchange"] = "return confirmAssignChange(this);";
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
                    cmd.BindByName = true;
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

            if (newStatus.Equals("Assigned", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("'Assigned' status is set automatically when a support staff is selected.");
                LoadTickets();
                return;
            }

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
                            checkCmd.BindByName = true;
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

                    if (!newStatus.Equals("New", StringComparison.OrdinalIgnoreCase))
                    {
                        string checkAssignSql = "SELECT ASSIGNED_TO_USER_ID FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId";
                        using (OracleCommand checkCmd = new OracleCommand(checkAssignSql, conn))
                        {
                            checkCmd.BindByName = true;
                            checkCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                            object assignedId = checkCmd.ExecuteScalar();

                            if (assignedId == null || assignedId == DBNull.Value)
                            {
                                ShowError("Please assign a support staff before changing the status.");
                                LoadTickets();
                                return;
                            }
                        }
                    }

                    var oldSnap = GetTicketSnapshot(ticketId, conn);

                    DateTime? dueDate = null;
                    string infoSql = @"SELECT DUE_DATE
                               FROM BI_OJT.TICKETS
                               WHERE TICKET_ID = :ticketId";

                    using (OracleCommand infoCmd = new OracleCommand(infoSql, conn))
                    {
                        infoCmd.BindByName = true;
                        infoCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                        using (OracleDataReader reader = infoCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                dueDate = reader["DUE_DATE"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(reader["DUE_DATE"]);
                            }
                        }
                    }

                    string sql = @"UPDATE BI_OJT.TICKETS
                           SET STATUS = :status, UPDATED_AT = SYSDATE";

                    if (newStatus.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
                        sql += ", RESOLVED_AT = SYSDATE";
                    else if (newStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                        sql += ", CLOSED_AT = SYSDATE";
                    else if (newStatus.Equals("New", StringComparison.OrdinalIgnoreCase))
                        sql += ", ASSIGNED_TO_USER_ID = NULL";

                    sql += " WHERE TICKET_ID = :ticketId";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = newStatus;
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = GetTicketSnapshot(ticketId, conn);
                    AuditHelper.LogAction(CurrentUserID, "UPDATE_STATUS", "TICKETS", ticketId, oldSnap, newSnap);

                    InsertStatusRemark(ticketId, newStatus, conn);

                    SLAHelper.CheckAndLogSlaCompletion(
                        CurrentUserID,
                        ticketId,
                        dueDate,
                        newStatus
                    );

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

                    bool isAssigning = !string.IsNullOrEmpty(assignedTo);
                    string sql;
                    string newStatus;

                    if (isAssigning)
                    {
                        sql = @"UPDATE BI_OJT.TICKETS 
                                SET ASSIGNED_TO_USER_ID = :assignedTo, STATUS = 'Assigned', UPDATED_AT = SYSDATE 
                                WHERE TICKET_ID = :ticketId";
                        newStatus = "Assigned";
                    }
                    else
                    {
                        sql = @"UPDATE BI_OJT.TICKETS 
                                SET ASSIGNED_TO_USER_ID = NULL, STATUS = 'New', UPDATED_AT = SYSDATE 
                                WHERE TICKET_ID = :ticketId";
                        newStatus = "New";
                    }

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        if (isAssigning)
                            cmd.Parameters.Add("assignedTo", OracleDbType.Int32).Value = Convert.ToInt32(assignedTo);
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = GetTicketSnapshot(ticketId, conn);
                    AuditHelper.LogAction(CurrentUserID, "UPDATE_ASSIGNMENT", "TICKETS", ticketId, oldSnap, newSnap);

                    if (isAssigning)
                    {
                        string assignedName = ddl.SelectedItem.Text;
                        InsertAssignmentRemark(ticketId, assignedName, conn);
                    }
                    else
                    {
                        InsertStatusRemark(ticketId, newStatus, conn);
                    }

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

                    string oldPriority = "";
                    DateTime? oldDueDate = null;
                    DateTime createdAt = DateTime.Now;

                    string infoSql = @"SELECT PRIORITY, DUE_DATE, CREATED_AT
                               FROM BI_OJT.TICKETS
                               WHERE TICKET_ID = :ticketId";

                    using (OracleCommand infoCmd = new OracleCommand(infoSql, conn))
                    {
                        infoCmd.BindByName = true;
                        infoCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                        using (OracleDataReader reader = infoCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                oldPriority = reader["PRIORITY"]?.ToString() ?? "";
                                oldDueDate = reader["DUE_DATE"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(reader["DUE_DATE"]);
                                createdAt = reader["CREATED_AT"] == DBNull.Value
                                    ? DateTime.Now
                                    : Convert.ToDateTime(reader["CREATED_AT"]);
                            }
                        }
                    }

                    DateTime? newDueDate = SLAHelper.CalculateSlaDueDate(newPriority, createdAt);

                    var oldSnap = GetTicketSnapshot(ticketId, conn);

                    string updateSql = @"UPDATE BI_OJT.TICKETS
                                 SET PRIORITY = :priority,
                                     DUE_DATE = :dueDate,
                                     UPDATED_AT = SYSDATE
                                 WHERE TICKET_ID = :ticketId";

                    using (OracleCommand cmd = new OracleCommand(updateSql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("priority", OracleDbType.Varchar2).Value =
                            string.IsNullOrEmpty(newPriority) ? (object)DBNull.Value : newPriority;
                        cmd.Parameters.Add("dueDate", OracleDbType.Date).Value =
                            newDueDate.HasValue ? (object)newDueDate.Value : DBNull.Value;
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = GetTicketSnapshot(ticketId, conn);
                    AuditHelper.LogAction(CurrentUserID, "UPDATE_PRIORITY", "TICKETS", ticketId, oldSnap, newSnap);

                    SLAHelper.LogSlaUpdated(
                        CurrentUserID,
                        ticketId,
                        oldPriority,
                        newPriority,
                        oldDueDate,
                        newDueDate
                    );

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
                string priority = ddlCreatePriority.SelectedValue.Trim();
                DateTime createdDate = DateTime.Now;
                DateTime? slaDueDate = SLAHelper.CalculateSlaDueDate(priority, createdDate);

                if (!slaDueDate.HasValue)
                {
                    ShowError("Invalid priority selected.");
                    hfShowModal.Value = "create";
                    return;
                }

                DateTime dueDate = slaDueDate.Value;

                if (dueDate <= createdDate)
                {
                    ShowError("Due date must be later than the ticket creation time.");
                    hfShowModal.Value = "create";
                    return;
                }

                string savedFileName = null;
                string relativePath = null;
                string originalFileName = null;

                if (fuAttachment.HasFile)
                {
                    if (!IsAllowedAttachmentType(fuAttachment))
                    {
                        ShowError("Invalid file type. Allowed file types are: jpg, jpeg, png, pdf, docs, docx.");
                        hfShowModal.Value = "create";
                        return;
                    }

                    if (!IsAllowedAttachmentSize(fuAttachment))
                    {
                        ShowError("File size must not exceed 10 MB.");
                        hfShowModal.Value = "create";
                        return;
                    }

                    var fileInfo = SaveAttachmentDetails(fuAttachment);
                    originalFileName = fileInfo.origName;
                    savedFileName = fileInfo.savedName;
                    relativePath = fileInfo.fullPath;
                }

                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    using (OracleTransaction txn = conn.BeginTransaction())
                    {
                        try
                        {
                            string year = DateTime.Now.Year.ToString();

                            OracleCommand seqCmd = new OracleCommand(
                                "SELECT BI_OJT.TICKETS_NUM_SEQ.NEXTVAL FROM DUAL", conn);
                            seqCmd.Transaction = txn;
                            seqCmd.BindByName = true;
                            decimal nextNum = Convert.ToDecimal(seqCmd.ExecuteScalar());

                            string ticketNumber = $"TKT-{year}-{((int)nextNum).ToString("D4")}";

                            OracleCommand idCmd = new OracleCommand(
                                "SELECT BI_OJT.TICKETS_SEQ.NEXTVAL FROM DUAL", conn);
                            idCmd.Transaction = txn;
                            idCmd.BindByName = true;
                            decimal ticketId = Convert.ToDecimal(idCmd.ExecuteScalar());

                            string sql = @"INSERT INTO BI_OJT.TICKETS
                        (TICKET_ID, TICKET_NUMBER, TITLE, DESCRIPTION, STATUS, PRIORITY,
                         ASSIGNED_TO_USER_ID, CREATED_BY_USER_ID, CREATED_AT, UPDATED_AT, DUE_DATE)
                        VALUES
                        (:ticketId, :ticketNumber, :title, :description, :status, :priority,
                         :assignedTo, :createdBy, SYSDATE, SYSDATE, :dueDate)";

                            OracleCommand cmd = new OracleCommand(sql, conn);
                            cmd.Transaction = txn;
                            cmd.BindByName = true;

                            cmd.Parameters.Add("ticketId", OracleDbType.Decimal).Value = ticketId;
                            cmd.Parameters.Add("ticketNumber", OracleDbType.Varchar2).Value = ticketNumber;
                            cmd.Parameters.Add("title", OracleDbType.Varchar2).Value = txtTitle.Text.Trim();
                            cmd.Parameters.Add("description", OracleDbType.Clob).Value = txtDescription.Text.Trim();
                            cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = "New";
                            cmd.Parameters.Add("priority", OracleDbType.Varchar2).Value = ddlCreatePriority.SelectedValue;
                            cmd.Parameters.Add("assignedTo", OracleDbType.Int32).Value = DBNull.Value;
                            cmd.Parameters.Add("createdBy", OracleDbType.Int32).Value = CurrentUserID;
                            cmd.Parameters.Add("dueDate", OracleDbType.Date).Value = dueDate;

                            cmd.ExecuteNonQuery();

                            if (fuAttachment.HasFile)
                            {
                                string attachSql = @"INSERT INTO BI_OJT.ATTACHMENTS
                            (TICKET_ID, ORIGINAL_FILE_NAME, SAVED_FILE_NAME,
                             FILE_PATH, FILE_SIZE, FILE_TYPE, UPLOADED_BY, UPLOADED_AT)
                            VALUES (:ticketId, :origName, :savedName,
                                    :path, :fileSize, :fileType, :userId, SYSDATE)";

                                OracleCommand attachCmd = new OracleCommand(attachSql, conn);
                                attachCmd.Transaction = txn;
                                attachCmd.BindByName = true;

                                attachCmd.Parameters.Add("ticketId", OracleDbType.Decimal).Value = ticketId;
                                attachCmd.Parameters.Add("origName", OracleDbType.Varchar2).Value = originalFileName;
                                attachCmd.Parameters.Add("savedName", OracleDbType.Varchar2).Value = savedFileName;
                                attachCmd.Parameters.Add("path", OracleDbType.Varchar2).Value = relativePath;
                                attachCmd.Parameters.Add("fileSize", OracleDbType.Int32).Value = fuAttachment.PostedFile.ContentLength;
                                attachCmd.Parameters.Add("fileType", OracleDbType.Varchar2).Value = System.IO.Path.GetExtension(fuAttachment.FileName).ToLower();
                                attachCmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;

                                attachCmd.ExecuteNonQuery();
                            }

                            var newSnap = GetTicketSnapshot((int)ticketId, conn);

                            InsertStatusRemark((int)ticketId, "New", conn);

                            txn.Commit();

                            AuditHelper.LogAction(CurrentUserID, "CREATE_TICKET", "TICKETS", (int)ticketId, null, newSnap);

                            SLAHelper.LogSlaCreated(
                                CurrentUserID,
                                (int)ticketId,
                                priority,
                                dueDate
                            );

                            if (fuAttachment.HasFile)
                            {
                                var attachmentSnap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["TICKET_ID"] = (int)ticketId,
                                    ["ORIGINAL_FILE_NAME"] = originalFileName,
                                    ["SAVED_FILE_NAME"] = savedFileName,
                                    ["FILE_PATH"] = relativePath,
                                    ["FILE_SIZE"] = fuAttachment.PostedFile.ContentLength,
                                    ["FILE_TYPE"] = System.IO.Path.GetExtension(fuAttachment.FileName).ToLower(),
                                    ["UPLOADED_BY"] = CurrentUserID,
                                };

                                AuditHelper.LogAction(CurrentUserID, "UPLOAD_ATTACHMENT", "ATTACHMENTS", (int)ticketId, null, attachmentSnap);
                            }

                            txtTitle.Text = "";
                            txtDescription.Text = "";
                            txtDueDate.Text = "";
                            ddlCreatePriority.SelectedIndex = 0;

                            ShowSuccess($"Ticket {ticketNumber} submitted successfully!");
                            Response.Redirect(Request.RawUrl);
                        }
                        catch (Exception)
                        {
                            txn.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                hfShowModal.Value = "create";
                ShowError("FULL ERROR: " + ex.ToString());
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
                case "EditTicket":
                    LoadTicketForEdit(ticketId);
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
                    cmd.BindByName = true;
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
                        // Due Date
                        lblViewDueDate.Text = row["DUE_DATE"] == DBNull.Value
                            ? "Not Set"
                            : Convert.ToDateTime(row["DUE_DATE"]).ToString("MM/dd/yyyy hh:mm tt");

                        try
                        {
                            string attachSql = @"
                                SELECT ORIGINAL_FILE_NAME, SAVED_FILE_NAME, FILE_PATH, FILE_TYPE, FILE_SIZE, UPLOADED_BY, UPLOADED_AT
                                FROM BI_OJT.ATTACHMENTS
                                WHERE TICKET_ID = :ticketId
                                ORDER BY UPLOADED_AT DESC";

                            using (OracleCommand attachCmd = new OracleCommand(attachSql, conn))
                            {
                                attachCmd.BindByName = true;
                                attachCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                                OracleDataAdapter attachDa = new OracleDataAdapter(attachCmd);
                                DataTable dtAttach = new DataTable();
                                attachDa.Fill(dtAttach);

                                if (dtAttach.Rows.Count > 0)
                                {
                                    pnlHasAttachment.Visible = true;
                                    pnlNoAttachmentMsg.Visible = false;

                                    DataRow attachRow = dtAttach.Rows[0];

                                    string attachmentPath = attachRow["FILE_PATH"]?.ToString();
                                    string fileName = attachRow["ORIGINAL_FILE_NAME"]?.ToString();
                                    string fileType = attachRow["FILE_TYPE"]?.ToString();
                                    string resolvedUrl = ResolveUrl(attachmentPath);

                                    string lowerPath = (attachmentPath ?? "").ToLower();
                                    string lowerType = (fileType ?? "").ToLower();

                                    bool isImage =
                                        lowerPath.EndsWith(".jpg") || lowerPath.EndsWith(".jpeg") ||
                                        lowerPath.EndsWith(".png") || lowerPath.EndsWith(".gif") ||
                                        lowerPath.EndsWith(".bmp") || lowerPath.EndsWith(".webp") ||
                                        lowerType.Contains("image");

                                    lblAttachFileName.Text = string.IsNullOrWhiteSpace(fileName)
                                        ? System.IO.Path.GetFileName(attachmentPath)
                                        : fileName;

                                    lblAttachFileType.Text = string.IsNullOrWhiteSpace(fileType)
                                        ? System.IO.Path.GetExtension(attachmentPath)?.TrimStart('.').ToUpper()
                                        : fileType;

                                    lblAttachUploadedBy.Text = row["CREATED_BY_NAME"].ToString();

                                    lblAttachUploadedAt.Text = attachRow["UPLOADED_AT"] == DBNull.Value
                                        ? "-"
                                        : Convert.ToDateTime(attachRow["UPLOADED_AT"]).ToString("MM/dd/yyyy hh:mm tt");

                                    hfAttachFilePath.Value = attachmentPath ?? "";
                                    hfAttachOriginalName.Value = fileName ?? "";
                                    hfAttachFileType.Value = fileType ?? "";

                                    if (isImage)
                                    {
                                        imgAttachFullPreview.ImageUrl = resolvedUrl;
                                        pnlAttachImagePreview.Visible = true;
                                    }
                                    else
                                    {
                                        pnlAttachImagePreview.Visible = false;
                                    }
                                }
                                else
                                {
                                    pnlHasAttachment.Visible = false;
                                    pnlNoAttachmentMsg.Visible = true;
                                    pnlAttachImagePreview.Visible = false;
                                }
                            }
                        }
                        catch
                        {
                            pnlHasAttachment.Visible = false;
                            pnlNoAttachmentMsg.Visible = true;
                            pnlAttachImagePreview.Visible = false;
                        }

                        hfShowModal.Value = "view";

                        LoadTicketRemarks(ticketId, conn);

                        hfViewTicketId.Value = ticketId.ToString();

                        bool isClosed = string.Equals(row["STATUS"].ToString(), "Closed", StringComparison.OrdinalIgnoreCase);
                        bool canAddRemark = false;

                        string role = CurrentRole.ToLower();

                        if (!isClosed)
                        {
                            if (role == "user")
                            {
                                if (row["CREATED_BY_USER_ID"] != DBNull.Value)
                                {
                                    int createdById = Convert.ToInt32(row["CREATED_BY_USER_ID"]);
                                    if (createdById == CurrentUserID)
                                    {
                                        canAddRemark = true;
                                    }
                                }
                            }
                        }

                        pnlAddRemark.Visible = canAddRemark;
                        pnlClosedRemarkNotice.Visible = isClosed;
                        txtNewRemark.Text = "";

                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Error loading ticket details: " + ex.Message);
            }
        }

        protected void btnAttachDownload_Click(object sender, EventArgs e)
        {
            string relativePath = hfAttachFilePath.Value;
            string originalName = hfAttachOriginalName.Value;
            string fileType = hfAttachFileType.Value;

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                ShowError("No attachment file path found.");
                return;
            }

            try
            {
                string physicalPath = Server.MapPath(relativePath);

                if (!System.IO.File.Exists(physicalPath))
                {
                    ShowError("The attachment file was not found on the server.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(originalName))
                    originalName = System.IO.Path.GetFileName(physicalPath);

                if (string.IsNullOrWhiteSpace(fileType))
                    fileType = "application/octet-stream";

                Response.Clear();
                Response.ContentType = fileType;
                Response.AddHeader("Content-Disposition", "attachment; filename=\"" + originalName + "\"");
                Response.TransmitFile(physicalPath);
                Response.Flush();
                Response.End();
            }
            catch (System.Threading.ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
                ShowError("Error downloading attachment: " + ex.Message);
            }
        }

        protected void btnAddRemark_Click(object sender, EventArgs e)
        {
            int ticketId = 0;
            if (string.IsNullOrWhiteSpace(hfViewTicketId.Value) || !int.TryParse(hfViewTicketId.Value, out ticketId) || ticketId <= 0)
            {
                ShowError("Invalid ticket.");
                return;
            }

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

                    string checkSql = "SELECT STATUS, CREATED_BY_USER_ID FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId";
                    using (OracleCommand checkCmd = new OracleCommand(checkSql, conn))
                    {
                        checkCmd.BindByName = true;
                        checkCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                        using (var reader = checkCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ShowError("Ticket not found.");
                                return;
                            }

                            string status = reader["STATUS"]?.ToString();
                            if (string.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase))
                            {
                                ShowError("Cannot add remark to a closed ticket.");
                                hfShowModal.Value = "view";
                                LoadTicketForView(ticketId);
                                return;
                            }

                            if (CurrentRole.ToLower() == "user")
                            {
                                int createdById = reader["CREATED_BY_USER_ID"] != DBNull.Value
                                    ? Convert.ToInt32(reader["CREATED_BY_USER_ID"])
                                    : 0;
                                if (createdById != CurrentUserID)
                                {
                                    ShowError("You can only add remarks to your own tickets.");
                                    hfShowModal.Value = "view";
                                    LoadTicketForView(ticketId);
                                    return;
                                }
                            }
                        }
                    }

                    string sql = @"INSERT INTO BI_OJT.TICKET_REMARKS
                        (TICKET_ID, USER_ID, REMARK_TEXT, CREATED_AT, UPDATED_AT)
                        VALUES
                        (:ticketId, :userId, :remarkText, SYSDATE, SYSDATE)";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;
                        cmd.Parameters.Add("remarkText", OracleDbType.Clob).Value = remark;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = new Dictionary<string, object>
                    {
                        { "REMARK_TEXT", remark },
                        { "TICKET_ID", ticketId }
                    };
                    AuditHelper.LogAction(CurrentUserID, "ADD_REMARK", "TICKET_REMARKS", ticketId, null, newSnap);
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

        private void LoadTicketRemarks(int ticketId, OracleConnection conn)
        {
            try
            {
                DataTable dtFinal = new DataTable();
                dtFinal.Columns.Add("DATE_DISPLAY", typeof(string));
                dtFinal.Columns.Add("CHANGED_BY", typeof(string));
                dtFinal.Columns.Add("ENTRY_TYPE", typeof(string));
                dtFinal.Columns.Add("DETAILS", typeof(string));
                dtFinal.Columns.Add("USER_ROLE", typeof(string));
                dtFinal.Columns.Add("SORT_DATE", typeof(DateTime));


                string remarksSql = @"
            SELECT TR.REMARK_TEXT, TR.CREATED_AT, U.FULL_NAME, U.ROLE
            FROM BI_OJT.TICKET_REMARKS TR
            LEFT JOIN BI_OJT.USERS U ON TR.USER_ID = U.USER_ID
            WHERE TR.TICKET_ID = :ticketId
            ORDER BY TR.CREATED_AT DESC";

                using (OracleCommand cmdRemarks = new OracleCommand(remarksSql, conn))
                {
                    cmdRemarks.BindByName = true;
                    cmdRemarks.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                    OracleDataAdapter daRemarks = new OracleDataAdapter(cmdRemarks);
                    DataTable dtRawRemarks = new DataTable();
                    daRemarks.Fill(dtRawRemarks);

                    foreach (DataRow rawRow in dtRawRemarks.Rows)
                    {
                        string remarkText = rawRow["REMARK_TEXT"].ToString();
                        string fullName = rawRow["FULL_NAME"] != DBNull.Value ? rawRow["FULL_NAME"].ToString() : "";
                        string role = rawRow["ROLE"] != DBNull.Value ? rawRow["ROLE"].ToString() : "";
                        DateTime createdAt = Convert.ToDateTime(rawRow["CREATED_AT"]);
                        string dateDisplay = createdAt.ToString("MM/dd/yyyy h:mm tt");

                        string entryType;
                        string details;

                        if (remarkText == "Ticket Status: New")
                        {
                            entryType = "Status Change";
                            details = "Created a new ticket";
                        }
                        else if (remarkText.StartsWith("Ticket Status: Assigned to "))
                        {
                            entryType = "Status Change";
                            string assignedName = remarkText.Substring("Ticket Status: Assigned to ".Length);
                            details = fullName + " assigned ticket to " + assignedName;
                        }
                        else if (remarkText.StartsWith("Ticket Status: "))
                        {
                            entryType = "Status Change";
                            details = remarkText;
                        }
                        else
                        {
                            entryType = "Remarks";
                            details = remarkText;
                        }

                        dtFinal.Rows.Add(dateDisplay, fullName, entryType, details, role.ToLower(), createdAt);
                    }
                }


                string auditSql = @"
            SELECT U.FULL_NAME, U.ROLE, A.ACTION, A.OLD_VALUE, A.NEW_VALUE, A.CREATED_AT
            FROM BI_OJT.AUDIT_LOGS A
            LEFT JOIN BI_OJT.USERS U ON A.USER_ID = U.USER_ID
            WHERE A.TABLE_NAME = 'TICKETS'
              AND A.TICKET_ID = :ticketId
            ORDER BY A.CREATED_AT DESC";

                using (OracleCommand cmdAudit = new OracleCommand(auditSql, conn))
                {
                    cmdAudit.BindByName = true;
                    cmdAudit.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                    OracleDataAdapter daAudit = new OracleDataAdapter(cmdAudit);
                    DataTable dtAudit = new DataTable();
                    daAudit.Fill(dtAudit);

                    foreach (DataRow row in dtAudit.Rows)
                    {
                        string fullName = row["FULL_NAME"] != DBNull.Value ? row["FULL_NAME"].ToString() : "";
                        string role = row["ROLE"] != DBNull.Value ? row["ROLE"].ToString() : "";
                        string action = row["ACTION"] != DBNull.Value ? row["ACTION"].ToString() : "";
                        DateTime createdAt = Convert.ToDateTime(row["CREATED_AT"]);
                        string dateDisplay = createdAt.ToString("MM/dd/yyyy h:mm tt");

                        var oldObj = TryParseJsonForTicketAudit(row["OLD_VALUE"]?.ToString());
                        var newObj = TryParseJsonForTicketAudit(row["NEW_VALUE"]?.ToString());

                        if (action == "EDIT_TICKET")
                        {
                            string oldTitle = oldObj?["TITLE"]?.ToString();
                            string newTitle = newObj?["TITLE"]?.ToString();

                            string oldDesc = oldObj?["DESCRIPTION"]?.ToString();
                            string newDesc = newObj?["DESCRIPTION"]?.ToString();

                            if (oldTitle != newTitle)
                            {
                                dtFinal.Rows.Add(dateDisplay, fullName, "Edit", "Title changed", role.ToLower(), createdAt);
                            }

                            if (oldDesc != newDesc)
                            {
                                dtFinal.Rows.Add(dateDisplay, fullName, "Edit", "Description changed", role.ToLower(), createdAt);
                            }
                        }

                        if (action == "UPDATE_PRIORITY")
                        {
                            string oldPri = oldObj?["PRIORITY"]?.ToString();
                            string newPri = newObj?["PRIORITY"]?.ToString();

                            if (oldPri != newPri)
                            {
                                dtFinal.Rows.Add(
                                    dateDisplay,
                                    fullName,
                                    "Priority Change",
                                    $"Priority changed from {oldPri ?? "-"} to {newPri ?? "-"}",
                                    role.ToLower(),
                                    createdAt
                                );
                            }
                        }

                        if (action == "UPDATE_ASSIGNMENT")
                        {
                            string oldAssigned = oldObj?["ASSIGNED_TO_USER_ID"]?.ToString();
                            string newAssigned = newObj?["ASSIGNED_TO_USER_ID"]?.ToString();

                            if (oldAssigned != newAssigned)
                            {
                                bool hadPreviousAssignment = !string.IsNullOrWhiteSpace(oldAssigned) && oldAssigned != "0";
                                bool hasNewAssignment = !string.IsNullOrWhiteSpace(newAssigned) && newAssigned != "0";

                                if (hadPreviousAssignment && hasNewAssignment)
                                {
                                    string assignedName = GetAssignedUserNameById(newAssigned, conn);

                                    dtFinal.Rows.Add(
                                        dateDisplay,
                                        fullName,
                                        "Assignment Change",
                                        $"{fullName} changed the assigned ticket to {assignedName}",
                                        role.ToLower(),
                                        createdAt
                                );
                                }
                            }
                        }
                    }

                    RemoveRedundantAssignmentStatusRows(dtFinal);

                    DataView dv = dtFinal.DefaultView;
                    dv.Sort = "SORT_DATE DESC";

                    DataTable dtBind = dv.ToTable();
                    dtBind.Columns.Remove("SORT_DATE");

                    if (dtBind.Rows.Count > 0)
                    {
                        rptRemarks.DataSource = dtBind;
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
            }
            catch (Exception ex)
            {
                ShowError("Error loading audit trail: " + ex.Message);
                pnlNoRemarks.Visible = true;
                rptRemarks.DataSource = null;
                rptRemarks.DataBind();
            }
        }

        private Newtonsoft.Json.Linq.JObject TryParseJsonForTicketAudit(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            json = json.Trim();
            if (!json.StartsWith("{"))
                return null;

            try
            {
                return Newtonsoft.Json.Linq.JObject.Parse(json);
            }
            catch
            {
                return null;
            }
        }
        private DataTable BuildAuditTrailTable(DataTable dtRaw)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("DATE_DISPLAY", typeof(string));
            dt.Columns.Add("CHANGED_BY", typeof(string));
            dt.Columns.Add("ENTRY_TYPE", typeof(string));
            dt.Columns.Add("DETAILS", typeof(string));
            dt.Columns.Add("USER_ROLE", typeof(string));

            foreach (DataRow rawRow in dtRaw.Rows)
            {
                string remarkText = rawRow["REMARK_TEXT"].ToString();
                string fullName = rawRow["FULL_NAME"] != DBNull.Value ? rawRow["FULL_NAME"].ToString() : "";
                string role = rawRow["ROLE"] != DBNull.Value ? rawRow["ROLE"].ToString() : "";
                DateTime createdAt = Convert.ToDateTime(rawRow["CREATED_AT"]);
                string dateDisplay = createdAt.ToString("MM/dd/yyyy h:mm") + createdAt.ToString("tt").ToLower();

                string entryType;
                string details;

                if (remarkText == "Ticket Status: New")
                {
                    entryType = "Status Change";
                    details = "Created a new ticket";
                }
                else if (remarkText.StartsWith("Ticket Status: Assigned to "))
                {
                    entryType = "Status Change";
                    string assignedName = remarkText.Substring("Ticket Status: Assigned to ".Length);
                    details = fullName + " assigned ticket to " + assignedName;
                }
                else if (remarkText.StartsWith("Ticket Status: "))
                {
                    entryType = "Status Change";
                    details = remarkText;
                }
                else
                {
                    entryType = "Remarks";
                    details = remarkText;
                }

                dt.Rows.Add(dateDisplay, fullName, entryType, details, role.ToLower());
            }

            return dt;
        }

        protected string GetAuditRowClass(string role)
        {
            switch (role?.ToLower())
            {
                case "user": return "audit-row-user";
                case "admin": return "audit-row-admin";
                case "support": return "audit-row-support";
                default: return "";
            }
        }

        private void LoadTicketForEdit(int ticketId)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT TICKET_NUMBER, TITLE, DESCRIPTION, DUE_DATE FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId"; 
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        hfEditTicketId.Value = ticketId.ToString();
                        txtEditTicketNumber.Text = row["TICKET_NUMBER"].ToString();
                        txtEditTitle.Text = row["TITLE"].ToString();
                        txtEditDescription.Text = row["DESCRIPTION"].ToString();
                        txtEditDueDate.Text = row["DUE_DATE"] != DBNull.Value
                            ? Convert.ToDateTime(row["DUE_DATE"]).ToString("yyyy-MM-dd")
                            : string.Empty;
                        string attachmentSql = @"
                            SELECT ORIGINAL_FILE_NAME
                            FROM BI_OJT.ATTACHMENTS
                            WHERE TICKET_ID = :ticketId
                            ORDER BY UPLOADED_AT DESC";

                        using (OracleCommand attachCmd = new OracleCommand(attachmentSql, conn))
                        {
                            attachCmd.BindByName = true;
                            attachCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                            object attachmentResult = attachCmd.ExecuteScalar();
                            if (attachmentResult != null && attachmentResult != DBNull.Value)
                                lblEditAttachmentStatus.Text = attachmentResult.ToString();
                            else
                                lblEditAttachmentStatus.Text = "No attachment uploaded";
                        }
                            hfShowModal.Value = "edit";
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

                    if (CurrentRole.ToLower() == "user")
                    {
                        string checkSql = "SELECT CREATED_BY_USER_ID FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId";
                        using (OracleCommand checkCmd = new OracleCommand(checkSql, conn))
                        {
                            checkCmd.BindByName = true;
                            checkCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                            object ownerId = checkCmd.ExecuteScalar();

                            if (ownerId == null || Convert.ToInt32(ownerId) != CurrentUserID)
                            {
                                ShowError("You can only edit your own tickets.");
                                return;
                            }
                        }
                    }

                    var oldSnap = GetTicketSnapshot(ticketId, conn);

                    DateTime? oldDueDate = null;
                    string ticketInfoSql = @"SELECT DUE_DATE
                                     FROM BI_OJT.TICKETS
                                     WHERE TICKET_ID = :ticketId";

                    using (OracleCommand infoCmd = new OracleCommand(ticketInfoSql, conn))
                    {
                        infoCmd.BindByName = true;
                        infoCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                        using (OracleDataReader reader = infoCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                oldDueDate = reader["DUE_DATE"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(reader["DUE_DATE"]);
                            }
                        }
                    }

                    string sql = @"UPDATE BI_OJT.TICKETS
                           SET TITLE = :title, DESCRIPTION = :description, UPDATED_AT = SYSDATE";

                    DateTime parsedEditDueDate = DateTime.MinValue;
                    bool hasDueDate = !string.IsNullOrWhiteSpace(txtEditDueDate.Text)
                                      && DateTime.TryParse(txtEditDueDate.Text, out parsedEditDueDate);

                    if (hasDueDate)
                        sql += ", DUE_DATE = :dueDate";

                    sql += " WHERE TICKET_ID = :ticketId";

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("title", OracleDbType.Varchar2).Value = txtEditTitle.Text.Trim();
                        cmd.Parameters.Add("description", OracleDbType.Clob).Value = txtEditDescription.Text.Trim();

                        if (hasDueDate)
                            cmd.Parameters.Add("dueDate", OracleDbType.Date).Value = parsedEditDueDate;

                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.ExecuteNonQuery();
                    }
                    if (fuEditAttachment.HasFile)
                    {
                        if (!IsAllowedAttachmentType(fuEditAttachment))
                        {
                            hfShowModal.Value = "edit";
                            ShowError("Invalid file type. Allowed file types are: jpg, jpeg, png, pdf, doc, docx.");
                            return;
                        }
                        string checkAttachmentSql = @"
                            SELECT ATTACHMENT_ID, FILE_PATH, SAVED_FILE_NAME
                            FROM BI_OJT.ATTACHMENTS
                            WHERE TICKET_ID = :ticketId
                            ORDER BY UPLOADED_AT DESC";

                        int existingAttachmentId = 0;
                        string oldFilePath = null;
                        string oldSavedFileName = null;

                        using (OracleCommand checkCmd = new OracleCommand(checkAttachmentSql, conn))
                        {
                            checkCmd.BindByName = true;
                            checkCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;


                            using (OracleDataReader reader = checkCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    existingAttachmentId = Convert.ToInt32(reader["ATTACHMENT_ID"]);
                                    oldFilePath = reader["FILE_PATH"] == DBNull.Value ? null : reader["FILE_PATH"].ToString();
                                    oldSavedFileName = reader["SAVED_FILE_NAME"] == DBNull.Value ? null : reader["SAVED_FILE_NAME"].ToString();
                                }
                            }
                        }
                        var fileInfo = SaveAttachmentDetails(fuEditAttachment);
                        string newOriginalFileName = fileInfo.origName;
                        string newSavedFileName = fileInfo.savedName;
                        string newRelativePath = fileInfo.fullPath;
                        string newFileType = System.IO.Path.GetExtension(fuEditAttachment.FileName).ToLower();
                        int newFileSize = fuEditAttachment.PostedFile.ContentLength;

                        if (existingAttachmentId > 0)
                        {
                            string updateAttachmentSql = @"
                                UPDATE BI_OJT.ATTACHMENTS
                                SET ORIGINAL_FILE_NAME = :origName,
                                    SAVED_FILE_NAME = :savedName,
                                    FILE_PATH = :filePath,
                                    FILE_SIZE = :fileSize,
                                    FILE_TYPE = :fileType,
                                    UPLOADED_BY = :uploadedBy,
                                    UPLOADED_AT = SYSDATE
                               WHERE ATTACHMENT_ID = :attachmentId";

                            using (OracleCommand updateAttachCmd = new OracleCommand(updateAttachmentSql, conn))
                            {
                                updateAttachCmd.BindByName = true;
                                updateAttachCmd.Parameters.Add("origName", OracleDbType.Varchar2).Value = newOriginalFileName;
                                updateAttachCmd.Parameters.Add("savedName", OracleDbType.Varchar2).Value = newSavedFileName;
                                updateAttachCmd.Parameters.Add("filePath", OracleDbType.Varchar2).Value = newRelativePath;
                                updateAttachCmd.Parameters.Add("fileSize", OracleDbType.Int32).Value = newFileSize;
                                updateAttachCmd.Parameters.Add("fileType", OracleDbType.Varchar2).Value = newFileType;
                                updateAttachCmd.Parameters.Add("uploadedBy", OracleDbType.Int32).Value = CurrentUserID;
                                updateAttachCmd.Parameters.Add("attachmentId", OracleDbType.Int32).Value = existingAttachmentId;
                                updateAttachCmd.ExecuteNonQuery();
                            }
                            if (!string.IsNullOrWhiteSpace(oldFilePath))
                            {
                                string oldPhysicalPath = Server.MapPath(oldFilePath);
                                if (System.IO.File.Exists(oldPhysicalPath))
                                    System.IO.File.Delete(oldPhysicalPath);
                            }
                        }
                        else
                        {
                            string insertAttachmentSql = @"
                                INSERT INTO BI_OJT.ATTACHMENTS
                                (TICKET_ID, ORIGINAL_FILE_NAME, SAVED_FILE_NAME, FILE_PATH, FILE_SIZE, FILE_TYPE, UPLOADED_BY, UPLOADED_AT)
                                VALUES
                                (:ticketId, :origName, :savedName, :filePath, :fileSize, :fileType, :uploadedBy, SYSDATE)";

                            using (OracleCommand insertAttachCmd = new OracleCommand(insertAttachmentSql, conn))
                            {
                                insertAttachCmd.BindByName = true;
                                insertAttachCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                                insertAttachCmd.Parameters.Add("origName", OracleDbType.Varchar2).Value = newOriginalFileName;
                                insertAttachCmd.Parameters.Add("savedName", OracleDbType.Varchar2).Value = newSavedFileName;
                                insertAttachCmd.Parameters.Add("filePath", OracleDbType.Varchar2).Value = newRelativePath;
                                insertAttachCmd.Parameters.Add("fileSize", OracleDbType.Int32).Value = newFileSize;
                                insertAttachCmd.Parameters.Add("fileType", OracleDbType.Varchar2).Value = newFileType;
                                insertAttachCmd.Parameters.Add("uploadedBy", OracleDbType.Int32).Value = CurrentUserID;
                                insertAttachCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    var newSnap = GetTicketSnapshot(ticketId, conn);
                    AuditHelper.LogAction(CurrentUserID, "EDIT_TICKET", "TICKETS", ticketId, oldSnap, newSnap);

                    if (hasDueDate)
                    {
                        bool dueDateChanged = !oldDueDate.HasValue || oldDueDate.Value != parsedEditDueDate;

                        if (dueDateChanged)
                        {
                            SLAHelper.LogSlaDueDateUpdated(
                                CurrentUserID,
                                ticketId,
                                oldDueDate,
                                parsedEditDueDate
                            );
                        }
                    }

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
        private void DeleteTicket(int ticketId)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string checkSql = @"SELECT T.TICKET_NUMBER, T.CREATED_BY_USER_ID
                                        FROM BI_OJT.TICKETS T
                                        WHERE T.TICKET_ID = :ticketId";

                    OracleCommand checkCmd = new OracleCommand(checkSql, conn);
                    checkCmd.BindByName = true;
                    checkCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                    OracleDataAdapter checkDa = new OracleDataAdapter(checkCmd);
                    DataTable checkDt = new DataTable();
                    checkDa.Fill(checkDt);

                    if (checkDt.Rows.Count == 0)
                    {
                        ShowError("Ticket not found.");
                        return;
                    }

                    if (CurrentRole.ToLower() != "admin")
                    {
                        ShowError("Only Admins can delete tickets.");
                        return;
                    }

                    DataRow ticketRow = checkDt.Rows[0];
                    string ticketNumber = ticketRow["TICKET_NUMBER"].ToString();

                    var oldSnap = GetTicketSnapshot(ticketId, conn);
                    if (oldSnap != null)
                        oldSnap["TICKET_NUMBER"] = ticketNumber;

                    using (OracleTransaction txn = conn.BeginTransaction())
                    {
                        try
                        {
                            using (var delRemarks = new OracleCommand("DELETE FROM BI_OJT.TICKET_REMARKS WHERE TICKET_ID = :ticketId", conn))
                            {
                                delRemarks.Transaction = txn;
                                delRemarks.BindByName = true;
                                delRemarks.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                                delRemarks.ExecuteNonQuery();
                            }

                            using (var delAttachments = new OracleCommand("DELETE FROM BI_OJT.ATTACHMENTS WHERE TICKET_ID = :ticketId", conn))
                            {
                                delAttachments.Transaction = txn;
                                delAttachments.BindByName = true;
                                delAttachments.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                                delAttachments.ExecuteNonQuery();
                            }

                            using (var delNotifications = new OracleCommand("DELETE FROM BI_OJT.NOTIFICATIONS WHERE TICKET_ID = :ticketId", conn))
                            {
                                delNotifications.Transaction = txn;
                                delNotifications.BindByName = true;
                                delNotifications.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                                delNotifications.ExecuteNonQuery();
                            }

                            using (var nullAudit = new OracleCommand("UPDATE BI_OJT.AUDIT_LOGS SET TICKET_ID = NULL WHERE TICKET_ID = :ticketId", conn))
                            {
                                nullAudit.Transaction = txn;
                                nullAudit.BindByName = true;
                                nullAudit.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                                nullAudit.ExecuteNonQuery();
                            }

                            using (var cmd = new OracleCommand("DELETE FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId", conn))
                            {
                                cmd.Transaction = txn;
                                cmd.BindByName = true;
                                cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                                cmd.ExecuteNonQuery();
                            }

                            txn.Commit();
                        }
                        catch
                        {
                            txn.Rollback();
                            throw;
                        }
                    }

                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    string oldJson = oldSnap != null ? serializer.Serialize(oldSnap) : null;
                    var newSnapDict = new Dictionary<string, object>
                    {
                        { "TICKET_NUMBER", ticketNumber },
                        { "MESSAGE", CurrentUserName + " deleted ticket " + ticketNumber }
                    };
                    string newJson = serializer.Serialize(newSnapDict);

                    string auditSql = @"INSERT INTO BI_OJT.AUDIT_LOGS 
                        (USER_ID, ACTION, TABLE_NAME, TICKET_ID, OLD_VALUE, NEW_VALUE, CREATED_AT) 
                        VALUES (:userId, :action, :tableName, NULL, :oldVal, :newVal, SYSDATE)";

                    using (var auditCmd = new OracleCommand(auditSql, conn))
                    {
                        auditCmd.BindByName = true;
                        auditCmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;
                        auditCmd.Parameters.Add("action", OracleDbType.Varchar2).Value = "DELETE_TICKET";
                        auditCmd.Parameters.Add("tableName", OracleDbType.Varchar2).Value = "TICKETS";
                        auditCmd.Parameters.Add("oldVal", OracleDbType.Clob).Value = (object)oldJson ?? DBNull.Value;
                        auditCmd.Parameters.Add("newVal", OracleDbType.Clob).Value = (object)newJson ?? DBNull.Value;
                        auditCmd.ExecuteNonQuery();
                    }

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
            string sql = @"SELECT TITLE, DESCRIPTION, STATUS, CREATED_BY_USER_ID, ASSIGNED_TO_USER_ID, PRIORITY, DUE_DATE
                   FROM BI_OJT.TICKETS
                   WHERE TICKET_ID = :ticketId";

            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    var snap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    snap["TITLE"] = reader["TITLE"] == DBNull.Value ? null : reader["TITLE"].ToString();
                    snap["DESCRIPTION"] = reader["DESCRIPTION"] == DBNull.Value ? null : reader["DESCRIPTION"].ToString();
                    snap["STATUS"] = reader["STATUS"] == DBNull.Value ? null : reader["STATUS"].ToString();
                    snap["CREATED_BY_USER_ID"] = reader["CREATED_BY_USER_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["CREATED_BY_USER_ID"]);
                    snap["ASSIGNED_TO_USER_ID"] = reader["ASSIGNED_TO_USER_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ASSIGNED_TO_USER_ID"]);
                    snap["PRIORITY"] = reader["PRIORITY"] == DBNull.Value ? null : reader["PRIORITY"].ToString();
                    snap["DUE_DATE"] = reader["DUE_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["DUE_DATE"]).ToString("MM/dd/yyyy hh:mm tt");

                    return snap;
                }
            }
        }
        private string GetAssignedUserNameById(string userId, OracleConnection conn)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return "Unassigned";

            string sql = "SELECT FULL_NAME FROM BI_OJT.USERS WHERE USER_ID = :userId";
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("userId", OracleDbType.Int32).Value = Convert.ToInt32(userId);

                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? "Unassigned" : result.ToString();
            }
        }

        private void RemoveRedundantAssignmentStatusRows(DataTable dtFinal)
        {
            List<DataRow> rowsToRemove = new List<DataRow>();

            foreach (DataRow row in dtFinal.Rows)
            {
                string entryType = row["ENTRY_TYPE"]?.ToString() ?? "";
                string changedBy = row["CHANGED_BY"]?.ToString() ?? "";
                string details = row["DETAILS"]?.ToString() ?? "";
                DateTime sortDate = Convert.ToDateTime(row["SORT_DATE"]);

                if (entryType == "Status Change" && details.Contains(" assigned ticket to "))
                {
                    bool hasMatchingAssignmentChange = false;

                    foreach (DataRow otherRow in dtFinal.Rows)
                    {
                        if (otherRow == row) continue;

                        string otherEntryType = otherRow["ENTRY_TYPE"]?.ToString() ?? "";
                        string otherChangedBy = otherRow["CHANGED_BY"]?.ToString() ?? "";
                        string otherDetails = otherRow["DETAILS"]?.ToString() ?? "";
                        DateTime otherSortDate = Convert.ToDateTime(otherRow["SORT_DATE"]);

                        if (otherEntryType == "Assignment Change" &&
                            otherChangedBy == changedBy &&
                            Math.Abs((otherSortDate - sortDate).TotalSeconds) < 5 &&
                            otherDetails.Contains(" changed the assigned ticket to "))
                        {
                            hasMatchingAssignmentChange = true;
                            break;
                        }
                    }

                    if (hasMatchingAssignmentChange)
                    {
                        rowsToRemove.Add(row);
                    }
                }
            }

            foreach (DataRow r in rowsToRemove)
                dtFinal.Rows.Remove(r);

            dtFinal.AcceptChanges();
        }
        private void InsertStatusRemark(int ticketId, string newStatus, OracleConnection conn)
        {
            string sql = @"INSERT INTO BI_OJT.TICKET_REMARKS 
                (TICKET_ID, USER_ID, REMARK_TEXT, CREATED_AT, UPDATED_AT) 
                VALUES (:ticketId, :userId, :remarkText, SYSDATE, SYSDATE)";
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                cmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;
                cmd.Parameters.Add("remarkText", OracleDbType.Varchar2).Value = "Ticket Status: " + newStatus;
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertAssignmentRemark(int ticketId, string assignedToName, OracleConnection conn)
        {
            string sql = @"INSERT INTO BI_OJT.TICKET_REMARKS 
                (TICKET_ID, USER_ID, REMARK_TEXT, CREATED_AT, UPDATED_AT) 
                VALUES (:ticketId, :userId, :remarkText, SYSDATE, SYSDATE)";
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                cmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;
                cmd.Parameters.Add("remarkText", OracleDbType.Varchar2).Value = "Ticket Status: Assigned to " + assignedToName;
                cmd.ExecuteNonQuery();
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

        //how many days a ticket has been opened calculator
        protected string GetAging(object createdAt, object resolvedAt, object status)
        {
            if (createdAt == DBNull.Value) return "0 Days";

            DateTime start = Convert.ToDateTime(createdAt);
            DateTime end = DateTime.Now;

            string stat = status?.ToString();
            if ((stat == "Resolved" || stat == "Closed") && resolvedAt != DBNull.Value)
            {
                end = Convert.ToDateTime(resolvedAt); //to stop the calculation
            }
            int days = (int)Math.Floor((end - start).TotalDays);
            return days == 0 ? "Today" : $"{days} Days";
        }

        //to highlight overdue tickets red
        protected string GetSlaCssClass(object dueDate, object status)
        {
            if (dueDate == DBNull.Value || status?.ToString() == "Resolved" || status?.ToString() == "Closed")
                return "";
            if (Convert.ToDateTime(dueDate) < DateTime.Now)
                return "text-danger font-weight-bold";

            return "";
        }
        private (string origName, string savedName, string fullPath) SaveAttachmentDetails(FileUpload fu)
        {
            if (fu.HasFile)
            {
                string originalFileName = fu.FileName;
                string extension = System.IO.Path.GetExtension(originalFileName);
                // Requirement: GUID + original extension
                string savedFileName = Guid.NewGuid().ToString() + extension;
                string folderPath = Server.MapPath("~/Uploads/Tickets/");

                if (!System.IO.Directory.Exists(folderPath))
                    System.IO.Directory.CreateDirectory(folderPath);

                string fullPath = folderPath + savedFileName;
                fu.SaveAs(fullPath);

                return (originalFileName, savedFileName, "~/Uploads/Tickets/" + savedFileName);
            }
            return (null, null, null);
        }

        private bool IsAllowedAttachmentType(FileUpload fu)
        {
            if (fu == null || !fu.HasFile)
                return false;
            
            string extension = System.IO.Path.GetExtension(fu.FileName);
            return AllowedAttachmentExtensions.Contains(extension);

        }

        private bool IsAllowedAttachmentSize(FileUpload fu)
        {
            if (fu == null || !fu.HasFile)
                return false;

            return fu.PostedFile.ContentLength <= MaxAttachmentSizeBytes;
        }


        public string GetStatusBadge(string status)
        {
            switch (status?.ToLower())
            {
                case "new": return "badge-new";
                case "assigned": return "badge-assigned";
                case "in progress": return "badge-in-progress";
                case "resolved": return "badge-resolved";
                case "closed": return "badge-closed";
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
