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

                    //CREATED TWO FIELDS

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

                    OracleCommand cmd = new OracleCommand(sql, conn);
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
                    else if (newStatus.Equals("New", StringComparison.OrdinalIgnoreCase))
                    {
                        sql += ", ASSIGNED_TO_USER_ID = NULL";
                    }

                    sql += " WHERE TICKET_ID = :ticketId";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = newStatus;
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.ExecuteNonQuery();
                    }

                    var newSnap = GetTicketSnapshot(ticketId, conn);
                    AuditHelper.LogAction(CurrentUserID, "UPDATE_STATUS", "TICKETS", ticketId, oldSnap, newSnap);

                    InsertStatusRemark(ticketId, newStatus, conn);

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

                    var oldSnap = GetTicketSnapshot(ticketId, conn);

                    string sql = @"UPDATE BI_OJT.TICKETS 
                                   SET PRIORITY = :priority, UPDATED_AT = SYSDATE 
                                   WHERE TICKET_ID = :ticketId";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
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
                DateTime dueDate;

                
                if (!string.IsNullOrWhiteSpace(txtDueDate.Text) &&
                    DateTime.TryParseExact(txtDueDate.Text, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out dueDate))
                {
                }
                else
                {
                    int slaHours = GetSlaHoursByPriority(ddlCreatePriority.SelectedValue);
                    dueDate = CalculateSlaWorkingHours(DateTime.Now, slaHours);
                }

                
                if (dueDate <= DateTime.Now)
                {
                    ShowError("Due date must be in the future.");
                    hfShowModal.Value = "create";
                    return;
                }

                
                string savedFileName = null;
                string relativePath = null;
                string originalFileName = null;

                if (fuAttachment.HasFile)
                {
                    originalFileName = fuAttachment.FileName;
                    string ext = System.IO.Path.GetExtension(originalFileName);
                    savedFileName = Guid.NewGuid().ToString() + ext;
                    relativePath = "~/Uploads/Tickets/" + savedFileName;
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
                     ASSIGNED_TO_USER_ID, CREATED_BY_USER_ID, CREATED_AT, UPDATED_AT, DUE_DATE, ATTACHMENT_PATH)
                    VALUES 
                    (:ticketId, :ticketNumber, :title, :description, :status, :priority,
                     :assignedTo, :createdBy, SYSDATE, SYSDATE, :dueDate, :attachmentPath)";

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
                            cmd.Parameters.Add("attachmentPath", OracleDbType.Varchar2)
                                .Value = (object)relativePath ?? DBNull.Value;

                            cmd.ExecuteNonQuery();

                            
                            if (fuAttachment.HasFile)
                            {
                                string attachSql = @"INSERT INTO BI_OJT.ATTACHMENTS 
                        (ATTACHMENT_ID, TICKET_ID, ORIGINAL_FILE_NAME, SAVED_FILE_NAME, 
                         FILE_PATH, FILE_SIZE, FILE_TYPE, UPLOADED_BY, UPLOADED_AT) 
                        VALUES (BI_OJT.ATTACHMENTS_SEQ.NEXTVAL, :ticketId, :origName, :savedName, 
                                :path, :fileSize, :fileType, :userId, SYSDATE)";

                                OracleCommand attachCmd = new OracleCommand(attachSql, conn);
                                attachCmd.Transaction = txn;
                                attachCmd.BindByName = true;

                                attachCmd.Parameters.Add("ticketId", OracleDbType.Decimal).Value = ticketId;
                                attachCmd.Parameters.Add("origName", OracleDbType.Varchar2).Value = originalFileName;
                                attachCmd.Parameters.Add("savedName", OracleDbType.Varchar2).Value = savedFileName;
                                attachCmd.Parameters.Add("path", OracleDbType.Varchar2).Value = relativePath;
                                attachCmd.Parameters.Add("fileSize", OracleDbType.Int32).Value = fuAttachment.PostedFile.ContentLength; // ✅ FIXED
                                attachCmd.Parameters.Add("fileType", OracleDbType.Varchar2).Value = fuAttachment.PostedFile.ContentType;
                                attachCmd.Parameters.Add("userId", OracleDbType.Int32).Value = CurrentUserID;

                            attachCmd.ExecuteNonQuery();
                        }

                            
                            var newSnap = GetTicketSnapshot((int)ticketId, conn);

                            InsertStatusRemark((int)ticketId, "New", conn);

                            
                            txn.Commit();
                            AuditHelper.LogAction(CurrentUserID, "CREATE_TICKET", "TICKETS", (int)ticketId, null, newSnap);


                            if (fuAttachment.HasFile)
                            {
                                var attachmentSnap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["TICKET_ID"] = (int)ticketId,
                                    ["ORIGINAL_FILE_NAME"] = originalFileName,
                                    ["SAVED_FILE_NAME"] = savedFileName,
                                    ["FILE_PATH"] = relativePath,
                                    ["FILE_SIZE"] = fuAttachment.PostedFile.ContentLength,
                                    ["FILE_TYPE"] = fuAttachment.PostedFile.ContentType,
                                    ["UPLOADED_BY"] = CurrentUserID,
                                };

                                AuditHelper.LogAction(CurrentUserID, "UPLOAD_ATTACHMENT", "ATTACHMENTS", (int)ticketId, null, attachmentSnap);
                            }

                            
                            txtTitle.Text = "";
                            txtDescription.Text = "";
                            txtDueDate.Text = "";
                            ddlCreatePriority.SelectedIndex = 0;

                            
                            //EmailHelper.SendEmail(
                            //    "angjandell24@gmail.com",
                            //    $"New Ticket Submitted: {ticketNumber}",
                            //    $"A new ticket has been submitted by {CurrentUserName}. <br/><b>Title:</b> {txtTitle.Text}"
                            //);

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
                            : Convert.ToDateTime(row["DUE_DATE"]).ToString("MM/dd/yyyy");

                        try
                        {
                            string attachmentPath = row["ATTACHMENT_PATH"].ToString();
                            if (!string.IsNullOrEmpty(attachmentPath))
                            {
                                string resolvedUrl = ResolveUrl(attachmentPath);
                                string fileName = System.IO.Path.GetFileName(attachmentPath);
                                if (fileName.Length > 9 && fileName[8] == '_')
                                    fileName = fileName.Substring(9);
                                string ext = System.IO.Path.GetExtension(attachmentPath).ToLower();
                                bool isImage = ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp" || ext == ".webp";

                                lblAttachFileName.Text = fileName;
                                lblAttachFileType.Text = ext.TrimStart('.').ToUpper();
                                lblAttachUploadedBy.Text = row["CREATED_BY_NAME"].ToString();
                                lblAttachUploadedAt.Text = Convert.ToDateTime(row["CREATED_AT"]).ToString("MM/dd/yyyy hh:mm tt");
                                hlAttachDownload.NavigateUrl = resolvedUrl;
                                hlAttachDownload.Target = "_blank";

                                if (isImage)
                                {
                                    imgAttachFullPreview.ImageUrl = resolvedUrl;
                                    pnlAttachImagePreview.Visible = true;
                                }
                                else
                                {
                                    pnlAttachImagePreview.Visible = false;
                                }

                                pnlHasAttachment.Visible = true;
                                pnlNoAttachmentMsg.Visible = false;
                            }
                            else
                            {
                                pnlHasAttachment.Visible = false;
                                pnlNoAttachmentMsg.Visible = true;
                                pnlAttachImagePreview.Visible = false;
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
            ORDER BY TR.CREATED_AT ASC";

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
            ORDER BY A.CREATED_AT ASC";

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
                                dtFinal.Rows.Add(
                                    dateDisplay,
                                    fullName,
                                    "Assignment Change",
                                    "Assignment updated",
                                    role.ToLower(),
                                    createdAt
                                );
                            }
                        }
                    }
                }

                DataView dv = dtFinal.DefaultView;
                dv.Sort = "SORT_DATE ASC";

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

                    string sql = @"UPDATE BI_OJT.TICKETS 
                                   SET TITLE = :title, DESCRIPTION = :description, UPDATED_AT = SYSDATE";

                    bool hasDueDate = !string.IsNullOrWhiteSpace(txtEditDueDate.Text) && DateTime.TryParse(txtEditDueDate.Text, out _);
                    if (hasDueDate)
                        sql += ", DUE_DATE = :dueDate";



                    sql += " WHERE TICKET_ID = :ticketId";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("title", OracleDbType.Varchar2).Value = txtEditTitle.Text.Trim();
                        cmd.Parameters.Add("description", OracleDbType.Clob).Value = txtEditDescription.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(txtEditDueDate.Text) && DateTime.TryParse(txtEditDueDate.Text, out DateTime parsedDueDate))
                            cmd.Parameters.Add("dueDate", OracleDbType.Date).Value = parsedDueDate;
                        cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                        cmd.ExecuteNonQuery();
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
            string sql = @"SELECT TITLE, DESCRIPTION, STATUS, CREATED_BY_USER_ID, ASSIGNED_TO_USER_ID, PRIORITY
                   FROM BI_OJT.TICKETS
                   WHERE TICKET_ID = :ticketId";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    var snap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    snap["TITLE"] = reader["TITLE"] == DBNull.Value ? null : reader["TITLE"].ToString();
                    snap["DESCRIPTION"] = reader["DESCRIPTION"] == DBNull.Value ? null : reader["DESCRIPTION"].ToString();
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

        //march 27, 2026 - 00:40
        private DateTime? CalculateSlaDueDate(string priority, DateTime startDate)
        {
            switch (priority?.ToLower())
            {
                case "critical": return startDate.AddHours(4);
                case "high": return startDate.AddDays(1);
                case "medium": return startDate.AddDays(3);
                case "low": return startDate.AddDays(7);
                default: return null; //unassigned
            }
        }

        private DateTime CalculateSlaWorkingHours(DateTime startDate, int workingHours)
        {
            DateTime current = startDate;

            // If start is outside working hours, move to next working day start
            if (current.DayOfWeek == DayOfWeek.Saturday)
                current = current.AddDays(2).Date.AddHours(8);
            else if (current.DayOfWeek == DayOfWeek.Sunday)
                current = current.AddDays(1).Date.AddHours(8);
            else if (current.TimeOfDay < TimeSpan.FromHours(8))
                current = current.Date.AddHours(8);
            else if (current.TimeOfDay >= TimeSpan.FromHours(17))
                current = current.AddDays(1).Date.AddHours(8);

            int hoursRemaining = workingHours;

            while (hoursRemaining > 0)
            {
                // Skip weekends
                if (current.DayOfWeek == DayOfWeek.Saturday)
                { current = current.AddDays(2).Date.AddHours(8); continue; }
                if (current.DayOfWeek == DayOfWeek.Sunday)
                { current = current.AddDays(1).Date.AddHours(8); continue; }

                DateTime workEnd = current.Date.AddHours(17);
                double hoursLeftToday = (workEnd - current).TotalHours;

                if (hoursRemaining <= hoursLeftToday)
                {
                    current = current.AddHours(hoursRemaining);
                    hoursRemaining = 0;
                }
                else
                {
                    hoursRemaining -= (int)hoursLeftToday;
                    current = current.AddDays(1).Date.AddHours(8);
                }
            }

            return current;
        }

        private int GetSlaHoursByPriority(string priority)
        {
            switch (priority?.ToLower())
            {
                case "urgent": return 4;
                case "high": return 8;
                case "medium": return 24;
                case "low": return 40;
                default: return 24;
            }
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
