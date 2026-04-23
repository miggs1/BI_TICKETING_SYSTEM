<%@ Page Title="Tickets" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Tickets.aspx.cs" Inherits="BI_TICKETING_SYSTEM.Pages.Tickets" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
<style>
    .ticket-card { border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.08); }
    .search-bar { border-radius: 8px 0 0 8px !important; border-right: none; }
    .btn-search { border-radius: 0 8px 8px 0 !important; background: #001f54; color: white; border: none; padding: 0 16px; }
    .btn-search:hover { background: #003087; }
    .filter-select { border-radius: 8px !important; font-size: 13px; }
    .btn-create { background: linear-gradient(135deg, #001f54, #003087); color: white; border: none; border-radius: 8px; padding: 8px 20px; font-size: 13px; font-weight: 600; }
    .btn-create:hover { background: linear-gradient(135deg, #003087, #0041a8); color: white; }
    .badge-new { background: #fd7e14; color: white; }
    .badge-assigned { background: #ffc107; color: #333; }
    .badge-in-progress { background: #007bff; color: white; }
    .badge-resolved { background: #28a745; color: white; }
    .badge-closed { background: #000000; color: white; }
    .badge-low { background: #007bff; color: white; }
    .badge-medium { background: #ffc107; color: #333; }
    .badge-high { background: #fd7e14; color: white; }
    .badge-urgent { background: #dc3545; color: white; }
    .badge-not-set { background: #dee2e6; color: #555; }
    .table th { background: #001f54; color: white; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; }
    .table td { font-size: 13px; vertical-align: middle; }
    .table tbody tr:hover { background: #f0f4ff; }
    .btn-action { padding: 4px 10px; font-size: 12px; border-radius: 6px; border: none; }
    .btn-view { background: #17a2b8; color: white; }
    .btn-view:hover { background: #138496; color: white; }
    .btn-edit { background: #ffc107; color: #333; }
    .btn-edit:hover { background: #e0a800; color: #333; }
    .btn-delete { background: #dc3545; color: white; }
    .btn-delete:hover { background: #c82333; color: white; }
    .dropdown-status, .dropdown-assign, .dropdown-priority { font-size: 12px; padding: 4px 8px; border-radius: 6px; border: 1px solid #ddd; }
    .dropdown-status:focus, .dropdown-assign:focus, .dropdown-priority:focus { border-color: #001f54; box-shadow: 0 0 0 2px rgba(0,31,84,0.1); }
    .modal-header { background: linear-gradient(135deg, #001f54, #003087); color: white; border-radius: 10px 10px 0 0; }
    .modal-header .close { color: white; opacity: 1; }
    .modal-content { border-radius: 10px; border: none; box-shadow: 0 10px 40px rgba(0,0,0,0.2); }
    .form-label { font-size: 11px; font-weight: 600; color: #001f54; text-transform: uppercase; letter-spacing: 0.8px; }
    .form-control:focus { border-color: #001f54; box-shadow: 0 0 0 3px rgba(0,31,84,0.1); }
    .required-star { color: #dc3545; }
    .ticket-number-display { background: #f0f4ff; border: 2px dashed #001f54; border-radius: 8px; padding: 10px 15px; font-weight: 700; color: #001f54; font-size: 15px; text-align: center; letter-spacing: 1px; }
    .pagination-info { font-size: 12px; color: #888; }
    .empty-state { text-align: center; padding: 60px 20px; color: #aaa; }
    .empty-state i { font-size: 60px; margin-bottom: 15px; color: #ddd; }
    .alert-success-custom { background: #d4edda; border: 1px solid #c3e6cb; border-left: 4px solid #28a745; border-radius: 8px; color: #155724; padding: 10px 15px; font-size: 13px; }
    .alert-danger-custom { background: #f8d7da; border: 1px solid #f5c6cb; border-left: 4px solid #dc3545; border-radius: 8px; color: #721c24; padding: 10px 15px; font-size: 13px; }
    .remarks-section { background: #f8f9fa; border-radius: 8px; padding: 15px; margin-top: 20px; }
    .remarks-title { font-size: 13px; font-weight: 600; color: #001f54; text-transform: uppercase; letter-spacing: 0.8px; margin-bottom: 15px; border-bottom: 2px solid #001f54; padding-bottom: 8px; }
    .no-remarks { text-align: center; color: #999; font-size: 12px; font-style: italic; padding: 20px; }
    .audit-trail-table { margin-bottom: 0; }
    .audit-trail-table th { background: #001f54; color: white; font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; }
    .audit-trail-table td { font-size: 12px; vertical-align: middle; padding: 8px 10px; }
    .audit-row-user { border-left: 3px solid #dc3545; background: rgba(220, 53, 69, 0.05); }
    .audit-row-admin { border-left: 3px solid #007bff; background: rgba(0, 123, 255, 0.05); }
    .audit-row-support { border-left: 3px solid #ffc107; background: rgba(40, 167, 69, 0.05); }
    .audit-badge-admin { background: #007bff; color: white; font-size: 10px; padding: 3px 8px; border-radius: 10px; display: inline-block; }
    .audit-badge-user { background: #dc3545; color: white; font-size: 10px; padding: 3px 8px; border-radius: 10px; display: inline-block; }
    .audit-badge-support { background: #ffc107; color: #333; font-size: 10px; padding: 3px 8px; border-radius: 10px; display: inline-block; }
    .audit-badge-default { background: #6c757d; color: white; font-size: 10px; padding: 3px 8px; border-radius: 10px; display: inline-block; }
    .btn-disabled-look { opacity: 0.5; cursor: not-allowed; }
    .description-display { white-space: pre-wrap; text-align: left; margin: 0; padding: 10px; background: #f8f9fa; border-radius: 8px; }
</style>
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

    <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="alert-success-custom mb-3">
        <i class="fas fa-check-circle mr-2"></i>
        <asp:Label ID="lblSuccess" runat="server" />
    </asp:Panel>
    <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert-danger-custom mb-3">
        <i class="fas fa-exclamation-circle mr-2"></i>
        <asp:Label ID="lblError" runat="server" />
    </asp:Panel>

    <div class="card ticket-card">
        <div class="card-header d-flex justify-content-between align-items-center" style="background:white; border-bottom: 2px solid #f0f4ff; padding: 15px 20px;">
            <h5 class="m-0" style="color:#001f54; font-weight:700;"><i class="fas fa-ticket-alt mr-2"></i>Ticket Management</h5>
            <asp:Panel ID="pnlCreateBtn" runat="server">
                <button type="button" class="btn btn-create" data-toggle="modal" data-target="#modalCreateTicket">
                    <i class="fas fa-plus mr-1"></i> Create Ticket
                </button>
            </asp:Panel>
        </div>

        <div class="card-body">
            <div class="row mb-3">
                <div class="col-md-6">
                    <div class="input-group">
                        <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control search-bar" placeholder="Search by ticket number or title..." AutoPostBack="true" OnTextChanged="txtSearch_TextChanged" />
                        <div class="input-group-append">
                            <asp:Button ID="btnSearch" runat="server" CssClass="btn-search" Text="Search" OnClick="btnSearch_Click" />
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <asp:DropDownList ID="ddlFilterStatus" runat="server" CssClass="form-control filter-select" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_Changed">
                        <asp:ListItem Value="">-- All Status --</asp:ListItem>
                        <asp:ListItem Value="New">New</asp:ListItem>
                        <asp:ListItem Value="Assigned">Assigned</asp:ListItem>
                        <asp:ListItem Value="In Progress">In Progress</asp:ListItem>
                        <asp:ListItem Value="Resolved">Resolved</asp:ListItem>
                        <asp:ListItem Value="Closed">Closed</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="col-md-3">
                    <asp:DropDownList ID="ddlFilterPriority" runat="server" CssClass="form-control filter-select" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_Changed">
                        <asp:ListItem Value="">-- All Priority --</asp:ListItem>
                        <asp:ListItem Value="Low">Low</asp:ListItem>
                        <asp:ListItem Value="Medium">Medium</asp:ListItem>
                        <asp:ListItem Value="High">High</asp:ListItem>
                        <asp:ListItem Value="Urgent">Urgent</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>

            <div class="table-responsive">
                <asp:Repeater ID="rptTickets" runat="server" OnItemCommand="rptTickets_ItemCommand" OnItemDataBound="rptTickets_ItemDataBound">
                    <HeaderTemplate>
                        <table class="table table-bordered table-hover">
                            <thead>
                                <tr>
                                    <th>Ticket No.</th>
                                    <th>Title</th>
                                    <th>Created By</th>
                                    <th>Priority</th>
                                    <th>Status</th>
                                    <th>Assigned To</th>
                                    <th>Created Date</th>
                                    <th>Last Updated</th>
                                    <th>Due Date</th>
                                    <th>Aging</th>
                                    <th style="width:160px;">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr>
                            <td><strong><%# Eval("TICKET_NUMBER") %></strong></td>
                            <td><%# Eval("TITLE") %></td>
                            <td><%# Eval("CREATED_BY_NAME") %></td>
                            <td>
                                <asp:DropDownList ID="ddlRowPriority" runat="server" AutoPostBack="true" CssClass="dropdown-priority"
                                    Visible='<%# Session["UserRole"] != null && (Session["UserRole"].ToString().ToLower() == "admin" || Session["UserRole"].ToString().ToLower() == "user") %>'
                                    OnSelectedIndexChanged="ddlRowPriority_Changed">
                                    <asp:ListItem Value="">NOT SET</asp:ListItem>
                                    <asp:ListItem Value="LOW">Low</asp:ListItem>
                                    <asp:ListItem Value="MEDIUM">Medium</asp:ListItem>
                                    <asp:ListItem Value="HIGH">High</asp:ListItem>
                                    <asp:ListItem Value="URGENT">Urgent</asp:ListItem>
                                </asp:DropDownList>
                                <asp:PlaceHolder runat="server" Visible='<%# Session["UserRole"] == null || (Session["UserRole"].ToString().ToLower() != "admin" && Session["UserRole"].ToString().ToLower() != "user") %>'>
                                    <span class="badge <%# GetPriorityBadge(Eval("PRIORITY").ToString()) %>" style="padding:5px 10px; border-radius:20px; font-size:11px;">
                                        <%# string.IsNullOrEmpty(Eval("PRIORITY").ToString()) ? "Not Set" : Eval("PRIORITY").ToString() %>
                                    </span>
                                </asp:PlaceHolder>
                            </td>
                            <td>
                                <asp:DropDownList ID="ddlRowStatus" runat="server" AutoPostBack="true" CssClass="dropdown-status"
                                    Visible='<%# Session["UserRole"] != null &&
                                        (Session["UserRole"].ToString().ToLower() == "admin" ||
                                        Session["UserRole"].ToString().ToLower() == "user") %>'
                                    OnSelectedIndexChanged="ddlRowStatus_Changed">
                                    <asp:ListItem Value="New">New</asp:ListItem>
                                    <asp:ListItem Value="Assigned">Assigned</asp:ListItem>
                                    <asp:ListItem Value="In Progress">In Progress</asp:ListItem>
                                    <asp:ListItem Value="Resolved">Resolved</asp:ListItem>
                                    <asp:ListItem Value="Closed">Closed</asp:ListItem>
                                </asp:DropDownList>
                                <asp:PlaceHolder runat="server" 
                                    Visible='<%# Session["UserRole"] == null ||
                                            (Session["UserRole"].ToString().ToLower() != "admin" && 
                                            Session["UserRole"].ToString().ToLower() != "user") %>'>
                                    <span class="badge <%# GetStatusBadge(Eval("STATUS").ToString()) %>" style="padding:5px 10px; border-radius:20px; font-size:11px;">
                                        <%# Eval("STATUS") %>
                                    </span>
                                </asp:PlaceHolder>
                                <asp:HiddenField ID="hfRowTicketId" runat="server" Value='<%# Eval("TICKET_ID") %>' />
                            </td>
                            <td>
                                <asp:DropDownList ID="ddlRowAssign" runat="server" AutoPostBack="true" CssClass="dropdown-assign"
                                    Visible='<%# Session["UserRole"] != null && Session["UserRole"].ToString().ToLower() == "admin" %>'
                                    OnSelectedIndexChanged="ddlRowAssign_Changed">
                                </asp:DropDownList>

                                <asp:PlaceHolder runat="server" 
                                    Visible='<%# Session["UserRole"] == null || Session["UserRole"].ToString().ToLower() != "admin" %>'>
                                    <%# string.IsNullOrEmpty(Eval("ASSIGNED_TO_NAME").ToString()) ? "<span style='color:#aaa;'>Unassigned</span>" : Eval("ASSIGNED_TO_NAME").ToString() %>
                                </asp:PlaceHolder>
                            </td>
                            <td><%# Convert.ToDateTime(Eval("CREATED_AT")).ToString("MM/dd/yyyy") %></td>
                            <td><%# Eval("UPDATED_AT") == DBNull.Value ? "-" : Convert.ToDateTime(Eval("UPDATED_AT")).ToString("MM/dd/yyyy") %></td>
                            <td>
                                <span class="<%# GetSlaCssClass(Eval("DUE_DATE"), Eval("STATUS")) %>">
                                    <%# Eval("DUE_DATE") != DBNull.Value ? Convert.ToDateTime(Eval("DUE_DATE")).ToString("MM/dd/yyyy") : "Not Set" %>
                                </span>
                            </td>
                            <td>
                                <strong><%# GetAging(Eval("CREATED_AT"), Eval("RESOLVED_AT"), Eval("STATUS")) %></strong>
                            </td>

                            <td>
                                <asp:LinkButton runat="server"
                                    CommandName="ViewTicket"
                                    CommandArgument='<%# Eval("TICKET_ID") %>'
                                    CssClass="btn btn-action btn-view mr-1"
                                    ToolTip="View"
                                    CausesValidation="false"
                                    UseSubmitBehavior="false">
                                    <i class="fas fa-eye"></i>
                                </asp:LinkButton>

                                <asp:LinkButton runat="server"
                                    CommandName="EditTicket"
                                    CommandArgument='<%# Eval("TICKET_ID") %>'
                                    CssClass="btn btn-action btn-edit mr-1"
                                    ToolTip="Edit"
                                    CausesValidation="false"
                                    UseSubmitBehavior="false"
                                    Visible='<%# Session["UserRole"] != null && (Session["UserRole"].ToString().ToLower() == "admin" || Session["UserRole"].ToString().ToLower() == "user") %>'>
                                    <i class="fas fa-edit"></i>
                                </asp:LinkButton>

                                <asp:LinkButton runat="server" CommandName="DeleteTicket"
                                    CommandArgument='<%# Eval("TICKET_ID") %>'
                                    CssClass="btn btn-action btn-delete"
                                    Visible='<%# Session["UserRole"] != null && Session["UserRole"].ToString().ToLower() == "admin" %>'
                                    ToolTip="Delete"
                                    CausesValidation="false"
                                    OnClientClick="return confirmDelete(this);">
                                    <i class="fas fa-trash"></i>
                                </asp:LinkButton>
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                            </tbody>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </div>

            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="empty-state">
                <i class="fas fa-ticket-alt"></i>
                <p style="font-size:16px; font-weight:600; color:#555;">No tickets found</p>
                <p style="font-size:13px;">Try adjusting your search or filters</p>
            </asp:Panel>

            <div class="d-flex justify-content-between align-items-center mt-3">
                <asp:Label ID="lblPaginationInfo" runat="server" CssClass="pagination-info" />
                <div>
                    <asp:Button ID="btnPrev" runat="server" Text="Prev" CssClass="btn btn-sm btn-outline-secondary mr-1" OnClick="btnPrev_Click" CausesValidation="false" UseSubmitBehavior="false" />
                    <asp:Button ID="btnNext" runat="server" Text="Next" CssClass="btn btn-sm btn-outline-secondary" OnClick="btnNext_Click" CausesValidation="false" UseSubmitBehavior="false" />
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="modalCreateTicket" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title"><i class="fas fa-plus-circle mr-2"></i>Create New Ticket</h5>
                    <button type="button" class="close" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <div class="row mb-3">
                        <div class="col-12">
                            <label class="form-label">Ticket Number</label>
                            <div class="ticket-number-display">
                                <asp:Label ID="lblNewTicketNumber" runat="server" Text="Auto-generated on submit" />
                            </div>
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-12">
                            <label class="form-label">Title <span class="required-star">*</span></label>
                            <asp:TextBox ID="txtTitle" runat="server" CssClass="form-control" placeholder="Enter ticket title" MaxLength="200" />
                            <asp:RequiredFieldValidator ID="rfvTitle" runat="server" ControlToValidate="txtTitle"
                                ErrorMessage="Title is required." ForeColor="Red" Display="Dynamic"
                                ValidationGroup="CreateTicket" Font-Size="11px" />
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-12">
                            <label class="form-label">Description <span class="required-star">*</span></label>
                            <asp:TextBox ID="txtDescription" runat="server" CssClass="form-control" placeholder="Describe your issue in detail..." TextMode="MultiLine" Rows="5" />
                            <asp:RequiredFieldValidator ID="rfvDescription" runat="server" ControlToValidate="txtDescription"
                                ErrorMessage="Description is required." ForeColor="Red" Display="Dynamic"
                                ValidationGroup="CreateTicket" Font-Size="11px" />
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-12">
                            <label class="form-label">Attach a File (Optional)</label>
                            <asp:FileUpload ID="fuAttachment" runat="server" CssClass="form-control" accept=".jpg,.jpeg,.png,.pdf,.doc,.docx" />
                            <small class="text-muted">Allowed file types: jpg, jpeg, png, pdf, doc, docx</small>
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-md-4">
                            <label class="form-label">Priority <span class="required-star">*</span></label>
                           <asp:DropDownList ID="ddlCreatePriority" runat="server" CssClass="form-control filter-select">
                                <asp:ListItem Value="">-- Select Priority --</asp:ListItem>
                                <asp:ListItem Value="Low">Low</asp:ListItem>
                                <asp:ListItem Value="Medium">Medium</asp:ListItem>
                                <asp:ListItem Value="High">High</asp:ListItem>
                                <asp:ListItem Value="Urgent">Urgent</asp:ListItem>
                            </asp:DropDownList>
                            <asp:RequiredFieldValidator ID="rfvCreatePriority" runat="server" ControlToValidate="ddlCreatePriority"
                                InitialValue="" ErrorMessage="Priority is required." ForeColor="Red" Display="Dynamic"
                                ValidationGroup="CreateTicket" Font-Size="11px" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">Created By</label>
                            <asp:TextBox ID="txtCreatedBy" runat="server" CssClass="form-control" ReadOnly="true" />
                        </div>
                            <div class="col-md-4">
                            <label>Due Date:</label>
                                <asp:TextBox 
                                    ID="txtDueDate" 
                                    runat="server" 
                                    CssClass="form-control" 
                                    TextMode="Date">
                                </asp:TextBox>
                                <asp:RequiredFieldValidator 
                                    ID="rfvDueDate" 
                                    runat="server" 
                                    ControlToValidate="txtDueDate"
                                    ErrorMessage="Due Date is required."
                                    ForeColor="Red"
                                    Display="Dynamic"
                                    ValidationGroup="CreateTicket">
                                </asp:RequiredFieldValidator>
                            </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                    <asp:Button ID="btnCreateTicket" runat="server" Text="Submit Ticket" CssClass="btn btn-create" OnClick="btnCreateTicket_Click" ValidationGroup="CreateTicket" />
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="modalViewTicket" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title"><i class="fas fa-eye mr-2"></i>Ticket Details</h5>
                    <button type="button" class="close" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <label class="form-label">Ticket Number</label>
                            <p class="form-control-plaintext font-weight-bold" style="color:#001f54;">
                                <asp:Label ID="lblViewTicketNumber" runat="server" />
                            </p>
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">Status</label>
                            <p class="form-control-plaintext">
                                <asp:Label ID="lblViewStatus" runat="server" />
                            </p>
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-12">
                            <label class="form-label">Title</label>
                            <p class="form-control-plaintext">
                                <asp:Label ID="lblViewTitle" runat="server" />
                            </p>
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-12">
                            <label class="form-label">Description</label>
                            <div class="form-control-plaintext" style="white-space: pre-wrap; text-align: left; margin: 0; padding: 0;">
                                <asp:Label ID="lblViewDescription" runat="server" />
                            </div>
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-12">
                            <label class="form-label">Attachment</label><br />
                            <asp:Panel ID="pnlHasAttachment" runat="server" Visible="false">
                                <button type="button" class="btn btn-sm btn-outline-primary" onclick="openAttachmentPreview()">
                                    <i class="fas fa-paperclip mr-1"></i> View Attached File
                                </button>
                            </asp:Panel>
                            <asp:Panel ID="pnlNoAttachmentMsg" runat="server" Visible="false">
                                <span class="text-muted" style="font-size:13px;">There is no attached file</span>
                            </asp:Panel>
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <label class="form-label">Created By</label>
                            <p class="form-control-plaintext">
                                <asp:Label ID="lblViewCreatedBy" runat="server" />
                            </p>
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">Created Date</label>
                            <p class="form-control-plaintext">
                                <asp:Label ID="lblViewCreatedDate" runat="server" />
                            </p>
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <label class="form-label">Assigned To</label>
                            <p class="form-control-plaintext">
                                <asp:Label ID="lblViewAssignedTo" runat="server" />
                            </p>
                        </div>
                        <div class="col-md-6">
                            <label class="form-label">Assigned To Role</label>
                            <p class="form-control-plaintext">
                                <asp:Label ID="lblViewAssignedToRole" runat="server" />
                            </p>
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <label class="form-label">Due Date</label>
                            <p class="form-control-plaintext">
                                <asp:Label ID="lblViewDueDate" runat="server" />
                            </p>
                        </div>
                    </div>

                    <div class="remarks-section">
                        <div class="remarks-title">
                            <i class="fas fa-comments mr-2"></i>Audit Trail
                        </div>
                        <div class="table-responsive">
                            <table class="table table-bordered audit-trail-table">
                                <thead>
                                    <tr>
                                        <th style="width:155px;">Date</th>
                                        <th>Changed By</th>
                                        <th style="width:110px;">Type</th>
                                        <th>Details</th>
                                    </tr>
                                </thead>
                                <tbody id="auditTrailBody">
                                    <asp:Repeater ID="rptRemarks" runat="server">
                                        <ItemTemplate>
                                            <tr class='audit-trail-row <%# GetAuditRowClass(Eval("USER_ROLE").ToString()) %>'>
                                                <td style="white-space:nowrap;"><%# Eval("DATE_DISPLAY") %></td>
                                                <td><%# Eval("CHANGED_BY") %></td>
                                                <td><span class='<%# 
                                                        Eval("USER_ROLE").ToString() == "admin" ? "audit-badge-admin" :
                                                        Eval("USER_ROLE").ToString() == "user" ? "audit-badge-user" :
                                                        Eval("USER_ROLE").ToString() == "support" ? "audit-badge-support" :
                                                        "audit-badge-default"
                                                    %>'>
                                                    <%# Eval("ENTRY_TYPE") %>
                                                    </span>
                                                </td>
                                                <td style="white-space:pre-wrap;"><%# Eval("DETAILS") %></td>
                                            </tr>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                        <asp:Panel ID="pnlNoRemarks" runat="server" Visible="false" CssClass="no-remarks">
                            <i class="fas fa-info-circle mr-2"></i>No remarks have been added to this ticket yet.
                        </asp:Panel>

                        <asp:Panel ID="pnlAddRemark" runat="server" Visible="false" CssClass="mt-3">
                            <label class="form-label">Add Remark</label>
                            <asp:TextBox ID="txtNewRemark" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" 
                                placeholder="Enter your remark here..." />
                            <asp:Button ID="btnAddRemark" runat="server" Text="Add Remark"
                                CssClass="btn btn-create mt-2"
                                OnClick="btnAddRemark_Click"
                                CausesValidation="false" />
                        </asp:Panel>

                        <asp:Panel ID="pnlClosedRemarkNotice" runat="server" Visible="false" CssClass="mt-3">
                            <div class="alert alert-secondary mb-0">
                                Remarks are disabled for closed tickets.
                            </div>
                        </asp:Panel>

                        <div class="d-flex justify-content-between align-items-center mt-2" id="auditPaginationTickets">
                            <span class="pagination-info" id="auditPageInfoTickets"></span>
                            <div>
                                <button type="button" class="btn btn-sm btn-outline-secondary mr-1" id="btnAuditPrevTickets" onclick="auditPrev()">Prev</button>
                                <button type="button" class="btn btn-sm btn-outline-secondary" id="btnAuditNextTickets" onclick="auditNext()">Next</button>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="modalAttachedPreview" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title"><i class="fas fa-paperclip mr-2"></i>Attached Preview</h5>
                    <button type="button" class="close" onclick="closeAttachmentPreview()"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <div class="table-responsive">
                        <table class="table table-bordered">
                            <thead>
                                <tr>
                                    <th>File Name</th>
                                    <th>File Type</th>
                                    <th>Uploaded By</th>
                                    <th>Uploaded At</th>
                                    <th style="width:100px;">Action</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td><asp:Label ID="lblAttachFileName" runat="server" /></td>
                                    <td><asp:Label ID="lblAttachFileType" runat="server" /></td>
                                    <td><asp:Label ID="lblAttachUploadedBy" runat="server" /></td>
                                    <td><asp:Label ID="lblAttachUploadedAt" runat="server" /></td>
                                    <td>
                                        <asp:LinkButton ID="btnAttachDownload" runat="server" CssClass="btn btn-sm btn-outline-primary" CausesValidation="false" OnClick="btnAttachDownload_Click">
                                            <i class="fas fa-download mr-1"></i>Download
                                        </asp:LinkButton>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                    <asp:Panel ID="pnlAttachImagePreview" runat="server" Visible="false">
                        <label class="form-label mt-2">Image Preview</label>
                        <div style="text-align:center; padding:10px; background:#f8f9fa; border-radius:8px;">
                            <asp:Image ID="imgAttachFullPreview" runat="server"
                                style="max-width:100%; max-height:400px; border-radius:8px; border:1px solid #dee2e6; cursor:pointer;"
                                onclick="window.open(this.src,'_blank')" title="Click to open full size" />
                        </div>
                    </asp:Panel>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" onclick="closeAttachmentPreview()"><i class="fas fa-arrow-left mr-1"></i>Back</button>
                </div>
            </div>
        </div>
    </div>

<div class="modal fade" id="modalEditTicket" tabindex="-1" role="dialog" data-backdrop="static">
    <div class="modal-dialog modal-lg" role="document">
        <div class="modal-content">

            <div class="modal-header">
                <h5 class="modal-title"><i class="fas fa-edit mr-2"></i>Edit Ticket</h5>
                <button type="button" class="close" data-dismiss="modal">
                    <span>&times;</span>
                </button>
            </div>

            <div class="modal-body">
                <asp:HiddenField ID="hfEditTicketId" runat="server" />

                <div class="form-group">
                    <label>Ticket Number</label>
                    <asp:TextBox ID="txtEditTicketNumber" runat="server" CssClass="form-control" ReadOnly="true" />
                </div>

                <div class="form-group">
                    <label>Title</label>
                    <asp:TextBox ID="txtEditTitle" runat="server" CssClass="form-control" />
                </div>

                <div class="form-group">
                    <label>Description</label>
                    <asp:TextBox ID="txtEditDescription" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="5" />
                </div>

                <div class="form-group">
                    <label>Due Date</label>
                    <asp:TextBox ID="txtEditDueDate" runat="server" CssClass="form-control" TextMode="Date" />
                </div>

                <div class="form-group">
                    <label>Current Attachment</label>
                    <asp:Label ID="lblEditAttachmentStatus" runat="server" CssClass="form-control-plaintext" Text="No attachment uploaded" />
                </div>

                <div class="form-group">
                    <label>Upload / Replace Attachment</label>
                    <asp:FileUpload ID="fuEditAttachment" runat="server" CssClass="form-control"
                        accept=".jpg,.jpeg,.png,.pdf,.doc,.docx" />
                </div>
            </div>

            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                <button id="btnSaveEditUi" type="button" class="btn btn-primary btn-disabled-look" disabled onclick="triggerEditSave();">Save Changes</button>
                <asp:Button ID="btnSaveEditServer" runat="server" Text="Save Changes"
                    Style="display:none;"
                    OnClick="btnSaveEdit_Click"
                    CausesValidation="false" />
            </div>

        </div>
    </div>
</div>

    <asp:HiddenField ID="hfShowModal" runat="server" Value="" />
    <asp:HiddenField ID="hfSwalMessage" runat="server" Value="" />
    <asp:HiddenField ID="hfSwalType" runat="server" Value="" />

    <asp:HiddenField ID="hfAttachFilePath" runat="server" Value="" />
    <asp:HiddenField ID="hfAttachOriginalName" runat="server" Value="" />
    <asp:HiddenField ID="hfAttachFileType" runat="server" Value="" />

    <asp:HiddenField ID="hfViewTicketId" runat="server" />

    <asp:HiddenField ID="hfEditOriginalTitle" runat="server" />
    <asp:HiddenField ID="hfEditOriginalDescription" runat="server" />
    <asp:HiddenField ID="hfEditOriginalDueDate" runat="server" />
    <asp:HiddenField ID="hfEditHasChanges" runat="server" Value="false" />


</asp:Content>

<asp:Content ID="ScriptsContent" ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
    $(document).ready(function () {

        var swalMsg = $('#<%= hfSwalMessage.ClientID %>').val();
        var swalType = $('#<%= hfSwalType.ClientID %>').val();

        if (swalMsg !== '') {
            Swal.fire({
                position: "top-end",
                icon: swalType,
                title: swalMsg,
                showConfirmButton: false,
                timer: 2500,
                toast: true
            });
        }
        $('#<%= ddlCreatePriority.ClientID %>').on('change', function ()
        {
            computeSLADueDate($(this).val(), '<%= txtDueDate.ClientID %>');
        });
        var modal = $('#<%= hfShowModal.ClientID %>').val();
        if (modal === 'view') {
            $('#modalViewTicket').modal('show');
        } else if (modal === 'edit') {
            $('#modalEditTicket').modal('show');
            setTimeout(function () { setupEditChangeWatcher(); }, 200);
        } else if (modal === 'create') {
            $('#modalCreateTicket').modal('show');
        }

        $('#modalEditTicket').on('shown.bs.modal', function () {
            setupEditChangeWatcher();
        });

        $('#modalViewTicket').on('shown.bs.modal', function () {
            auditCurrentPage = 1;
            auditPaginate();
        });

        if (modal === 'view') {
            auditCurrentPage = 1;
            auditPaginate();
        }

        if (modal === 'attachment') {
            $('#modalAttachedPreview').modal('show');
        }

    });

    function openAttachmentPreview() {
        $('#modalViewTicket').modal('hide');
        setTimeout(function () { $('#modalAttachedPreview').modal('show'); }, 300);
    }

    function closeAttachmentPreview() {
        $('#modalAttachedPreview').modal('hide');
        setTimeout(function () { $('#modalViewTicket').modal('show'); }, 300);
    }

    function triggerEditSave() {
        var hasChanges = document.getElementById('<%= hfEditHasChanges.ClientID %>').value;
        if (hasChanges === 'true') {
            document.getElementById('<%= btnSaveEditServer.ClientID %>').click();
        }
    }

    function setupEditChangeWatcher() {
        var title = document.getElementById('<%= txtEditTitle.ClientID %>');
        var description = document.getElementById('<%= txtEditDescription.ClientID %>');
        var dueDate = document.getElementById('<%= txtEditDueDate.ClientID %>');
        var attachment = document.getElementById('<%= fuEditAttachment.ClientID %>');
        var saveBtn = document.getElementById('btnSaveEditUi');
        var hasChangesField = document.getElementById('<%= hfEditHasChanges.ClientID %>');

        var originalTitle = document.getElementById('<%= hfEditOriginalTitle.ClientID %>').value || '';
        var originalDescription = document.getElementById('<%= hfEditOriginalDescription.ClientID %>').value || '';
        var originalDueDate = document.getElementById('<%= hfEditOriginalDueDate.ClientID %>').value || '';

        if (!title || !description || !dueDate || !attachment || !saveBtn || !hasChangesField) return;

        function checkForChanges() {
            var titleChanged = title.value !== originalTitle;
            var descriptionChanged = description.value !== originalDescription;
            var dueDateChanged = dueDate.value !== originalDueDate;
            var attachmentChanged = attachment.value !== '';

            var hasChanges = titleChanged || descriptionChanged || dueDateChanged || attachmentChanged;

            if (hasChanges) {
                hasChangesField.value = 'true';
                saveBtn.disabled = false;
                saveBtn.classList.remove('btn-disabled-look');
            } else {
                hasChangesField.value = 'false';
                saveBtn.disabled = true;
                saveBtn.classList.add('btn-disabled-look');
            }
        }

        title.oninput = checkForChanges;
        description.oninput = checkForChanges;
        dueDate.onchange = checkForChanges;
        attachment.onchange = checkForChanges;

        checkForChanges();
    }
    var auditCurrentPage = 1;
    var auditPageSize = 5;

    function auditPaginate() {
        var rows = $('#auditTrailBody tr.audit-trail-row');
        var total = rows.length;
        var totalPages = Math.ceil(total / auditPageSize);
        if (auditCurrentPage > totalPages) auditCurrentPage = totalPages;
        if (auditCurrentPage < 1) auditCurrentPage = 1;
        rows.hide();
        var start = (auditCurrentPage - 1) * auditPageSize;
        var end = start + auditPageSize;
        rows.slice(start, end).show();
        var pageInfo = document.getElementById('auditPageInfoTickets');
        var pagination = document.getElementById('auditPaginationTickets');
        if (total > 0 && pagination) {
            pagination.style.display = '';
            if (pageInfo) pageInfo.textContent = 'Showing ' + (start + 1) + '\u2013' + Math.min(end, total) + ' of ' + total + ' entries';
        } else if (pagination) {
            pagination.style.display = 'none';
        }
        var btnPrev = document.getElementById('btnAuditPrevTickets');
        var btnNext = document.getElementById('btnAuditNextTickets');
        if (btnPrev) btnPrev.disabled = (auditCurrentPage <= 1);
        if (btnNext) btnNext.disabled = (auditCurrentPage >= totalPages);
    }

    function auditPrev() {
        if (auditCurrentPage > 1) { auditCurrentPage--; auditPaginate(); }
    }

    function auditNext() {
        var rows = $('#auditTrailBody tr.audit-trail-row');
        var totalPages = Math.ceil(rows.length / auditPageSize);
        if (auditCurrentPage < totalPages) { auditCurrentPage++; auditPaginate(); }
    }

    function confirmDelete(btn) {
        Swal.fire({
            title: 'Are you sure?',
            text: 'This ticket will be permanently deleted!',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#dc3545',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, delete it!',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                btn.removeAttribute('onclick');
                btn.click();
            }
        });
        return false;
    }

    function confirmStatusChange(ddl) {
        var oldValue = ddl.getAttribute('data-oldvalue');
        var newValue = ddl.value;

        if (oldValue === newValue) return false;

        if (newValue === 'Assigned') {
            ddl.value = oldValue;
            Swal.fire({
                icon: 'error',
                title: 'Not Allowed',
                text: '"Assigned" status is set automatically when a support staff is selected.',
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 2500
            });
            return false;
        }

        if (newValue !== 'New') {
            var row = ddl.closest('tr');
            var assignDdl = row.querySelector('select[id*="ddlRowAssign"]');
            if (assignDdl && !assignDdl.value) {
                ddl.value = oldValue;
                Swal.fire({
                    icon: 'error',
                    title: 'Assignment Required',
                    text: 'Please assign a support staff before changing the status.',
                    toast: true,
                    position: 'top-end',
                    showConfirmButton: false,
                    timer: 2500
                });
                return false;
            }
        }

        ddl.value = oldValue;

        Swal.fire({
            title: 'Are you sure?',
            text: 'Do you want to change the ticket status to "' + newValue + '"?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#001f54',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, update it',
            cancelButtonText: 'Cancel'
        }).then(function (result) {
            if (result.isConfirmed) {
                ddl.value = newValue;
                ddl.setAttribute('data-oldvalue', newValue);
                __doPostBack(ddl.name, '');
            }
        });

        return false;
    }

    function confirmPriorityChange(ddl) {
        var oldValue = ddl.getAttribute('data-oldvalue');
        var newValue = ddl.value;

        if (oldValue === newValue) return false;

        var label = '';
        for (var i = 0; i < ddl.options.length; i++) {
            if (ddl.options[i].value === newValue) { label = ddl.options[i].text; break; }
        }

        ddl.value = oldValue;

        Swal.fire({
            title: 'Are you sure?',
            text: 'Do you want to change the ticket priority to "' + label + '"?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#001f54',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, update it',
            cancelButtonText: 'Cancel'
        }).then(function (result) {
            if (result.isConfirmed) {
                ddl.value = newValue;
                ddl.setAttribute('data-oldvalue', newValue);
                __doPostBack(ddl.name, '');
            }
        });

        return false;
    }

    function confirmAssignChange(ddl) {
        var oldValue = ddl.getAttribute('data-oldvalue');
        var newValue = ddl.value;

        if (oldValue === newValue) return false;

        var label = '';
        for (var i = 0; i < ddl.options.length; i++) {
            if (ddl.options[i].value === newValue) { label = ddl.options[i].text; break; }
        }

        var msg = newValue ? 'Do you want to assign this ticket to "' + label + '"?' : 'Do you want to unassign this ticket?';

        ddl.value = oldValue;

        Swal.fire({
            title: 'Are you sure?',
            text: msg,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#001f54',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, update it',
            cancelButtonText: 'Cancel'
        }).then(function (result) {
            if (result.isConfirmed) {
                ddl.value = newValue;
                ddl.setAttribute('data-oldvalue', newValue);
                __doPostBack(ddl.name, '');
            }
        });

        return false;
    }

    function computeSLADueDate(priority, dueDateFieldId) {
        var hoursMap = { 'Urgent': 4, 'High': 8, 'Medium': 24, 'Low': 40 };
        var hours = hoursMap[priority];
        if (!hours) return;

        var now = new Date();
        var result = addWorkingHours(now, hours);

        // Format as yyyy-MM-dd for TextMode="Date"
        var yyyy = result.getFullYear();
        var mm = String(result.getMonth() + 1).padStart(2, '0');
        var dd = String(result.getDate()).padStart(2, '0');
        document.getElementById(dueDateFieldId).value = yyyy + '-' + mm + '-' + dd;
    }

    function addWorkingHours(start, hoursToAdd) {
        var current = new Date(start);

        // Snap to start of next working period if outside hours
        current = snapToWorkingTime(current);

        while (hoursToAdd > 0) {
            var workEnd = new Date(current);
            workEnd.setHours(17, 0, 0, 0);

            var hoursLeftToday = (workEnd - current) / 3600000;

            if (hoursToAdd <= hoursLeftToday) {
                current = new Date(current.getTime() + hoursToAdd * 3600000);
                hoursToAdd = 0;
            } else {
                hoursToAdd -= hoursLeftToday;
                current.setDate(current.getDate() + 1);
                current.setHours(8, 0, 0, 0);
                current = snapToWorkingTime(current);
            }
        }
        return current;
    }

    function snapToWorkingTime(dt) {
        var day = dt.getDay(); // 0=Sun, 6=Sat
        if (day === 6) { dt.setDate(dt.getDate() + 2); dt.setHours(8, 0, 0, 0); }
        else if (day === 0) { dt.setDate(dt.getDate() + 1); dt.setHours(8, 0, 0, 0); }
        else if (dt.getHours() < 8) { dt.setHours(8, 0, 0, 0); }
        else if (dt.getHours() >= 17) { dt.setDate(dt.getDate() + 1); dt.setHours(8, 0, 0, 0); dt = snapToWorkingTime(dt); }
        return dt;
    }
</script>
</asp:Content>
