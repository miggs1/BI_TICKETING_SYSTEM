using BI_TICKETING_SYSTEM.Helpers;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Web;
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
            string fromDate = txtFromDate.Text;
            string toDate = txtToDate.Text;

            try
            {
                using (OracleConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    string sql = @"
                        SELECT T.TICKET_ID, T.TICKET_NUMBER, T.TITLE, T.STATUS, T.PRIORITY,
                               T.CREATED_AT, T.ASSIGNED_TO_USER_ID,
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


                    if (!string.IsNullOrEmpty(fromDate))
                        sql += " AND TRUNC(T.CREATED_AT) >= TO_DATE(:fromDate, 'YYYY-MM-DD') ";

                    if (!string.IsNullOrEmpty(toDate))
                        sql += " AND TRUNC(T.CREATED_AT) <= TO_DATE(:toDate, 'YYYY-MM-DD') ";

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

                    if (!string.IsNullOrEmpty(fromDate))
                        cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate;

                    if (!string.IsNullOrEmpty(toDate))
                        cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate;

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

        // ===== ITEM DATA BOUND =====
        protected void rptTickets_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                DataRowView row = (DataRowView)e.Item.DataItem;

                DropDownList ddlRowPriority = (DropDownList)e.Item.FindControl("ddlRowPriority");
                if (ddlRowPriority != null && ddlRowPriority.Visible)
                {
                    string currentPriority = row["PRIORITY"]?.ToString()?.ToUpper() ?? "";
                    if (!string.IsNullOrEmpty(currentPriority) && ddlRowPriority.Items.FindByValue(currentPriority) != null)
                        ddlRowPriority.SelectedValue = currentPriority;
                    else
                        ddlRowPriority.SelectedValue = "";
                }
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
            }
        }

        // ===== PRIORITY CHANGE =====
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

                    if (CurrentRole.ToLower() == "support")
                    {
                        string checkSql = "SELECT ASSIGNED_TO_USER_ID FROM BI_OJT.TICKETS WHERE TICKET_ID = :ticketId";
                        using (OracleCommand checkCmd = new OracleCommand(checkSql, conn))
                        {
                            checkCmd.BindByName = true;
                            checkCmd.Parameters.Add("ticketId", OracleDbType.Int32).Value = ticketId;
                            object assignedId = checkCmd.ExecuteScalar();

                            if (assignedId == null || assignedId == DBNull.Value || Convert.ToInt32(assignedId) != CurrentUserID)
                            {
                                ShowError("You can only update priority for tickets assigned to you.");
                                LoadTickets();
                                return;
                            }
                        }
                    }

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

        // ===== TICKET SNAPSHOT =====
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
        protected void txtDate_Changed(object sender, EventArgs e)
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
            string fromDate = txtFromDate.Text;
            string toDate = txtToDate.Text;

            using (OracleConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                string sql = @"
                    SELECT T.TICKET_ID, T.TICKET_NUMBER, T.TITLE, T.STATUS, T.PRIORITY,
                           T.CREATED_AT, T.ASSIGNED_TO_USER_ID,
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

                if (!string.IsNullOrEmpty(fromDate))
                    sql += " AND TRUNC(T.CREATED_AT) >= TO_DATE(:fromDate, 'YYYY-MM-DD') ";

                if (!string.IsNullOrEmpty(toDate))
                    sql += " AND TRUNC(T.CREATED_AT) <= TO_DATE(:toDate, 'YYYY-MM-DD') ";

                sql += " ORDER BY T.CREATED_AT DESC ";

                OracleCommand cmd = new OracleCommand(sql, conn);

                if (role == "support")
                    cmd.Parameters.Add("assignedTo", OracleDbType.Int32).Value = userId;

                if (!string.IsNullOrEmpty(search))
                    cmd.Parameters.Add("search", OracleDbType.Varchar2).Value = "%" + search + "%";

                if (!string.IsNullOrEmpty(filterStatus))
                    cmd.Parameters.Add("filterStatus", OracleDbType.Varchar2).Value = filterStatus;

                if (!string.IsNullOrEmpty(fromDate))
                    cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate;

                if (!string.IsNullOrEmpty(toDate))
                    cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate;

                OracleDataAdapter da = new OracleDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return dt;
            }
        }



        // ===== EXPORT TO PDF & EXCEL =====

        // ===== TABLE HELPERS 
        private PdfPCell CreateCell(string text, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.Padding = 5;
            return cell;
        }

        private PdfPCell CreateCellFormatted(string text, BaseColor color)
        {
            Font font = new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, color);
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.Padding = 5;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            return cell;
        }

        private BaseColor GetStatusColor(string status)
        {
            switch (status.ToLower())
            {
                case "resolved": return new BaseColor(0, 128, 0);
                case "pending approval": return new BaseColor(255, 140, 0);
                case "open": return new BaseColor(0, 102, 204);
                case "in progress": return new BaseColor(128, 0, 128);
                case "closed": return BaseColor.GRAY;
                default: return BaseColor.BLACK;
            }
        }

        private BaseColor GetPriorityColor(string priority)
        {
            switch (priority.ToLower())
            {
                case "low": return BaseColor.GRAY;
                case "medium": return new BaseColor(0, 102, 204);
                case "high": return new BaseColor(255, 140, 0);
                case "urgent": return BaseColor.RED;
                default: return BaseColor.BLACK;
            }
        }

        // ===== EXPORT TO PDF
        protected void btnExportPDF_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = GetFilteredTickets();

                if (dt.Rows.Count == 0)
                {
                    ShowError("No data to export.");
                    return;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    Document doc = new Document(iTextSharp.text.PageSize.A4.Rotate(), 20, 20, 20, 20);
                    PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    // ===== HEADER TABLE (SEAL | TEXT | FLAG)
                    PdfPTable headerTable = new PdfPTable(3);
                    headerTable.WidthPercentage = 100;
                    headerTable.SetWidths(new float[] { 1f, 3f, 1f });

                    // ===== LEFT IMAGE (BI SEAL)
                    string sealPath = Server.MapPath("~/Images/bi-seal.png");
                    if (File.Exists(sealPath))
                    {
                        Image seal = Image.GetInstance(sealPath);
                        seal.ScaleToFit(60f, 60f);

                        PdfPCell sealCell = new PdfPCell(seal);
                        sealCell.Border = Rectangle.NO_BORDER;
                        sealCell.HorizontalAlignment = Element.ALIGN_LEFT;
                        sealCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        headerTable.AddCell(sealCell);
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
                    headerText.Add(new Phrase("REPUBLIC OF THE PHILIPPINES\n", subFont));
                    headerText.Add(new Phrase("BUREAU OF IMMIGRATION\n", titleFont));
                    headerText.Add(new Phrase("MANAGEMENT INFORMATION SYSTEMS DIVISION (MISD)\n", subFont));
                    headerText.Add(new Phrase("Ticketing System Report\n\n", titleFont));

                    headerText.Add(new Phrase("Status: " + (string.IsNullOrEmpty(ddlFilterStatus.SelectedValue) ? "All" : ddlFilterStatus.SelectedValue) + "\n", subFont));
                    headerText.Add(new Phrase("Search: " + (string.IsNullOrEmpty(txtSearch.Text) ? "None" : txtSearch.Text) + "\n", subFont));
                    headerText.Add(new Phrase("From: " + (string.IsNullOrEmpty(txtFromDate.Text) ? "N/A" : txtFromDate.Text) + "\n", subFont));
                    headerText.Add(new Phrase("To: " + (string.IsNullOrEmpty(txtToDate.Text) ? "N/A" : txtToDate.Text) + "\n", subFont)); 
                    headerText.Add(new Phrase("Generated: " + DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt"), subFont));

                    PdfPCell textCell = new PdfPCell(headerText);
                    textCell.Border = Rectangle.NO_BORDER;
                    textCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    textCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    headerTable.AddCell(textCell);

                    // ===== RIGHT IMAGE (PH FLAG)
                    string flagPath = Server.MapPath("~/Images/ph-flag.png");
                    if (File.Exists(flagPath))
                    {
                        Image flag = Image.GetInstance(flagPath);
                        flag.ScaleToFit(60f, 60f);

                        PdfPCell flagCell = new PdfPCell(flag);
                        flagCell.Border = Rectangle.NO_BORDER;
                        flagCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        flagCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        headerTable.AddCell(flagCell);
                    }
                    else
                    {
                        headerTable.AddCell(new PdfPCell { Border = Rectangle.NO_BORDER });
                    }

                    doc.Add(headerTable);

                    // ===== LINE SEPARATOR
                    LineSeparator line = new LineSeparator();
                    doc.Add(new Chunk(line));
                    doc.Add(new Paragraph(" "));

                    // ===== TABLE
                    PdfPTable table = new PdfPTable(7);
                    table.WidthPercentage = 100;

                    table.SetWidths(new float[]
                    {
                8f, 18f, 35f, 15f, 15f, 25f, 25f
                    });

                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                    Font cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);

                    string[] headers = {
                "ID",
                "Ticket No.",
                "Title",
                "Status",
                "Priority",
                "Date Created",
                "Assigned To"
            };

                    foreach (string header in headers)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(header, headerFont));
                        cell.BackgroundColor = new BaseColor(230, 230, 230);
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.Padding = 6;
                        table.AddCell(cell);
                    }

                    // ===== DATA
                    foreach (DataRow row in dt.Rows)
                    {
                        table.AddCell(CreateCell(row["TICKET_ID"].ToString(), cellFont));
                        table.AddCell(CreateCell(row["TICKET_NUMBER"].ToString(), cellFont));
                        table.AddCell(CreateCell(row["TITLE"].ToString(), cellFont));

                        table.AddCell(CreateCellFormatted(row["STATUS"].ToString(), GetStatusColor(row["STATUS"].ToString())));
                        table.AddCell(CreateCellFormatted(row["PRIORITY"].ToString(), GetPriorityColor(row["PRIORITY"].ToString())));

                        DateTime date = Convert.ToDateTime(row["CREATED_AT"]);
                        table.AddCell(CreateCell(date.ToString("MMM dd, yyyy hh:mm tt"), cellFont));

                        table.AddCell(CreateCell(
                            string.IsNullOrEmpty(row["ASSIGNED_TO_NAME"].ToString()) ? "Unassigned" : row["ASSIGNED_TO_NAME"].ToString(),
                            cellFont
                        ));
                    }

                    doc.Add(table);
                    doc.Close();

                    // ===== RESPONSE (FIXED DOWNLOAD ISSUE)
                    Response.Clear();
                    Response.ContentType = "application/pdf";
                    Response.AddHeader("content-disposition", "attachment;filename=TicketReport.pdf");
                    Response.Cache.SetCacheability(HttpCacheability.NoCache);

                    Response.BinaryWrite(ms.ToArray());
                    Response.Flush();
                    Response.SuppressContent = true;
                    HttpContext.Current.ApplicationInstance.CompleteRequest();
                }
            }
            catch (Exception ex)
            {
                ShowError("PDF Export Error: " + ex.Message);
            }
        }
        // ===== EXPORT TO EXCEL
        protected void btnExportExcel_Click(object sender, EventArgs e)
        {
            DataTable dt = GetFilteredTickets();

            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", "attachment;filename=TicketReport.xls");
            Response.Charset = "";
            Response.ContentType = "application/vnd.ms-excel";

            using (StringWriter sw = new StringWriter())
            {
                HtmlTextWriter hw = new HtmlTextWriter(sw);

                // ===== FIX IMAGE PATH 
                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                string sealUrl = baseUrl + ResolveUrl("~/Images/bi-seal.png");
                string flagUrl = baseUrl + ResolveUrl("~/Images/ph-flag.png");

                // ===== HEADER TABLE (SEAL | TITLE | FLAG)
                hw.Write(@"
                    <table style='width:100%; margin-bottom:20px; font-family:Arial;'>
                        <tr>
                            <td style='width:20%; text-align:left;'>
                                <img src='" + sealUrl + @"' height='60'/>
                            </td>

                            <td style='width:60%; text-align:center;'>
                                <div style='font-size:14px; font-weight:bold;'>REPUBLIC OF THE PHILIPPINES</div>
                                <div style='font-size:16px; font-weight:bold;'>BUREAU OF IMMIGRATION</div>
                                <div style='font-size:12px;'>MANAGEMENT INFORMATION SYSTEMS DIVISION (MISD)</div>
                                <div style='font-size:14px; font-weight:bold; margin-top:5px;'>Ticketing System Report</div>
                            </td>

                            <td style='width:20%; text-align:right;'>
                                <img src='" + flagUrl + @"' height='60'/>
                            </td>
                        </tr>
                    </table>
                    ");

                // ===== FILTER DETAILS
                hw.Write("<table style='margin-bottom:15px; font-family:Arial;'>");
                hw.Write("<tr><td><b>Status:</b></td><td>" +
                    (string.IsNullOrEmpty(ddlFilterStatus.SelectedValue) ? "All" : ddlFilterStatus.SelectedValue) +
                    "</td></tr>");

                hw.Write("<tr><td><b>Search:</b></td><td>" +
                    (string.IsNullOrEmpty(txtSearch.Text) ? "None" : txtSearch.Text) +
                    "</td></tr>");

                hw.Write("<tr><td><b>From Date:</b></td><td>" +
                    (string.IsNullOrEmpty(txtFromDate.Text) ? "N/A" : txtFromDate.Text) +
                    "</td></tr>");

                hw.Write("<tr><td><b>To Date:</b></td><td>" +
                    (string.IsNullOrEmpty(txtToDate.Text) ? "N/A" : txtToDate.Text) +
                    "</td></tr>");

                hw.Write("<tr><td><b>Generated:</b></td><td>" +
                    DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt") +
                    "</td></tr>");
                hw.Write("</table>");

                hw.Write("<hr/>");

                // ===== TABLE (GRIDVIEW)
                GridView gv = new GridView();
                gv.DataSource = dt;
                gv.DataBind();

                // ===== STYLE TABLE
                gv.HeaderStyle.BackColor = System.Drawing.Color.LightGray;
                gv.HeaderStyle.Font.Bold = true;
                gv.RowStyle.BackColor = System.Drawing.Color.White;
                gv.AlternatingRowStyle.BackColor = System.Drawing.ColorTranslator.FromHtml("#f2f2f2");

                gv.Attributes["style"] = "border-collapse:collapse; width:100%; font-family:Arial;";

                gv.RenderControl(hw);

                // ===== OUTPUT
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