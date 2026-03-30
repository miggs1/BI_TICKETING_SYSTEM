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
                string attachmentPath = SaveAttachment((FileUpload)Master.FindControl("MainContent").FindControl("fuCreateAttachment"));
                DateTime? dueDate = CalculateSlaDueDate("meidum", DateTime.Now);
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string year = DateTime.Now.Year.ToString();
                    OracleCommand seqCmd = new OracleCommand("SELECT BI_OJT.TICKETS_NUM_SEQ.NEXTVAL FROM DUAL", conn);
                    seqCmd.BindByName = true;
                    decimal nextNum = Convert.ToDecimal(seqCmd.ExecuteScalar());
                    string ticketNumber = $"TKT-{year}-{((int)nextNum).ToString("D4")}";

                    OracleCommand idCmd = new OracleCommand("SELECT BI_OJT.TICKETS_SEQ.NEXTVAL FROM DUAL", conn);
                    idCmd.BindByName = true;
                    decimal ticketId = Convert.ToDecimal(idCmd.ExecuteScalar());

                    string selectedPriority = ddlCreatePriority.SelectedValue;

                    string sql = @"INSERT INTO BI_OJT.TICKETS 
                        (TICKET_ID, TICKET_NUMBER, TITLE, DESCRIPTION, STATUS, PRIORITY,
                         ASSIGNED_TO_USER_ID, CREATED_BY_USER_ID, CREATED_AT, UPDATED_AT)
                        VALUES 
                        (:ticketId, :ticketNumber, :title, :description, :status, :priority,
                         :assignedTo, :createdBy, SYSDATE, SYSDATE)";

                    OracleCommand cmd = new OracleCommand(sql, conn);
                    cmd.BindByName = true;
                    cmd.Parameters.Add("ticketId", OracleDbType.Decimal).Value = ticketId;
                    cmd.Parameters.Add("ticketNumber", OracleDbType.Varchar2).Value = ticketNumber;
                    cmd.Parameters.Add("title", OracleDbType.Varchar2).Value = txtTitle.Text.Trim();
                    cmd.Parameters.Add("description", OracleDbType.Clob).Value = txtDescription.Text.Trim();
                    cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = "New";
                    cmd.Parameters.Add("priority", OracleDbType.Varchar2).Value = selectedPriority;
                    cmd.Parameters.Add("assignedTo", OracleDbType.Int32).Value = DBNull.Value;
                    cmd.Parameters.Add("createdBy", OracleDbType.Int32).Value = CurrentUserID;
                    cmd.ExecuteNonQuery();

                    var newSnap = GetTicketSnapshot((int)ticketId, conn);
                    AuditHelper.LogAction(CurrentUserID, "CREATE_TICKET", "TICKETS", (int)ticketId, null, newSnap);

                    InsertStatusRemark((int)ticketId, "New", conn);

                    txtTitle.Text = "";
                    txtDescription.Text = "";
                    ddlCreatePriority.SelectedIndex = 0;

                    EmailHelper.SendEmail("angjandell24@gmail.com", $"New Ticket Submitted: {ticketNumber}", $"A new ticket has been submitted by {CurrentUserName}. <br/><b>Title:</b> {txtTitle.Text}");

                    hfShowModal.Value = "";
                    ShowSuccess($"Ticket {ticketNumber} submitted successfully!");
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

                        //new attachment view logic
                        string attachmentPath = row["ATTACHMENT_PATH"].ToString();
                        if (!string.IsNullOrEmpty(attachmentPath))
                        {
                            hlViewAttachment.NavigateUrl = ResolveUrl(attachmentPath);
                            hlViewAttachment.Visible = true;
                            lblNoAttachment.Visible = false;
                        }
                        else
                        {
                            hlViewAttachment.Visible = false;
                            lblNoAttachment.Visible = true;
                        }

                        LoadTicketRemarks(ticketId, conn);

                        hfShowModal.Value = "view";
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
                cmd.BindByName = true;
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
            catch (Exception)
            {
                pnlNoRemarks.Visible = true;
                rptRemarks.DataSource = null;
                rptRemarks.DataBind();
            }
        }

        private void LoadTicketForEdit(int ticketId)
        {
            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT TICKET_NUMBER, TITLE, DESCRIPTION FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId";
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
                                   SET TITLE = :title, DESCRIPTION = :description, UPDATED_AT = SYSDATE
                                   WHERE TICKET_ID = :ticketId";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("title", OracleDbType.Varchar2).Value = txtEditTitle.Text.Trim();
                        cmd.Parameters.Add("description", OracleDbType.Clob).Value = txtEditDescription.Text.Trim();
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

                    string checkSql = @"SELECT CREATED_BY_USER_ID, U.ROLE AS CREATED_BY_ROLE
                                        FROM BI_OJT.TICKETS T
                                        LEFT JOIN BI_OJT.USERS U ON T.CREATED_BY_USER_ID = U.USER_ID
                                        WHERE TICKET_ID = :ticketId";

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

                    DataRow ticketRow = checkDt.Rows[0];
                    int createdBy = Convert.ToInt32(ticketRow["CREATED_BY_USER_ID"]);
                    string createdByRole = ticketRow["CREATED_BY_ROLE"].ToString().ToLower();

                    if (CurrentRole.ToLower() != "admin")
                    {
                        ShowError("Only Admins can delete tickets.");
                        return;
                    }

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

        private Dictionary<string, object> GetTicketSnapshot(int ticketId, OracleConnection conn)
        {
            string sql = @"SELECT STATUS, CREATED_BY_USER_ID, ASSIGNED_TO_USER_ID, PRIORITY
                           FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
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
        private string SaveAttachment(FileUpload fileUploadControl)
        {
            if (fileUploadControl.HasFile)
            {
                string fileName = Guid.NewGuid().ToString().Substring(0, 8) + "_" + fileUploadControl.FileName;
                string serverPath = Server.MapPath("~/Uploads/Tickets/");
                System.IO.Directory.CreateDirectory(serverPath);
                fileUploadControl.SaveAs(serverPath + fileName);
                return "~/Uploads/Tickets/" + fileName;
            }
            return null;
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
