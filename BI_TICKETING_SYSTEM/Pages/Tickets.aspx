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
    .remark-item { background: white; border-left: 3px solid #007bff; border-radius: 6px; padding: 12px; margin-bottom: 10px; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
    .remark-header { font-size: 11px; color: #666; margin-bottom: 8px; }
    .remark-text { font-size: 13px; color: #333; line-height: 1.6; white-space: pre-wrap; }
    .remark-author { font-weight: 600; color: #001f54; }
    .no-remarks { text-align: center; color: #999; font-size: 12px; font-style: italic; padding: 20px; }

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
                        <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control search-bar" placeholder="Search by ticket number or title..." />
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
                                <asp:DropDownList ID="ddlRowPriority" runat="server" CssClass="dropdown-priority" AutoPostBack="true"
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
                                <asp:DropDownList ID="ddlRowStatus" runat="server" CssClass="dropdown-status"
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
                                <asp:PlaceHolder runat="server" Visible='<%# Session["UserRole"] == null || 
                                            (Session["UserRole"].ToString().ToLower() != "admin" && 
                                            Session["UserRole"].ToString().ToLower() != "user") %>'>
                                    <span class="badge <%# GetStatusBadge(Eval("STATUS").ToString()) %>" style="padding:5px 10px; border-radius:20px; font-size:11px;">
                                        <%# Eval("STATUS") %>
                                    </span>
                                </asp:PlaceHolder>
                                <asp:HiddenField ID="hfRowTicketId" runat="server" Value='<%# Eval("TICKET_ID") %>' />
                            </td>
                            <td>
                                <asp:DropDownList ID="ddlRowAssign" runat="server" CssClass="dropdown-assign" AutoPostBack="true"
                                    Visible='<%# Session["UserRole"] != null && (Session["UserRole"].ToString().ToLower() == "admin" || Session["UserRole"].ToString().ToLower() == "user") %>'
                                    OnSelectedIndexChanged="ddlRowAssign_Changed">
                                </asp:DropDownList>
                                <asp:PlaceHolder runat="server" Visible='<%# Session["UserRole"] == null || (Session["UserRole"].ToString().ToLower() != "admin" && Session["UserRole"].ToString().ToLower() != "user") %>'>
                                    <%# string.IsNullOrEmpty(Eval("ASSIGNED_TO_NAME").ToString()) ? "<span style='color:#aaa;'>Unassigned</span>" : Eval("ASSIGNED_TO_NAME").ToString() %>
                                </asp:PlaceHolder>
                            </td>
                            <td><%# Convert.ToDateTime(Eval("CREATED_AT")).ToString("MM/dd/yyyy") %></td>
                            <td><%# Eval("UPDATED_AT") == DBNull.Value ? "-" : Convert.ToDateTime(Eval("UPDATED_AT")).ToString("MM/dd/yyyy") %></td>
                            <td>
                                <asp:LinkButton runat="server" CommandName="ViewTicket"
                                    CommandArgument='<%# Eval("TICKET_ID") %>'
                                    CssClass="btn btn-action btn-view mr-1"
                                    ToolTip="View">
                                    <i class="fas fa-eye"></i>
                                </asp:LinkButton>

                                <asp:LinkButton runat="server" CommandName="EditTicket"
                                    CommandArgument='<%# Eval("TICKET_ID") %>'
                                    CssClass="btn btn-action btn-edit mr-1"
                                    Visible='<%# Session["UserRole"] != null && (Session["UserRole"].ToString().ToLower() == "admin" || Session["UserRole"].ToString().ToLower() == "user") %>'
                                    ToolTip="Edit">
                                    <i class="fas fa-edit"></i>
                                </asp:LinkButton>

                                <asp:LinkButton runat="server" CommandName="DeleteTicket"
                                    CommandArgument='<%# Eval("TICKET_ID") %>'
                                    CssClass="btn btn-action btn-delete"
                                    Visible='<%# Session["UserRole"] != null && Session["UserRole"].ToString().ToLower() == "admin" %>'
                                    ToolTip="Delete"
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
                    <asp:Button ID="btnPrev" runat="server" Text="← Prev" CssClass="btn btn-sm btn-outline-secondary mr-1" OnClick="btnPrev_Click" />
                    <asp:Button ID="btnNext" runat="server" Text="Next →" CssClass="btn btn-sm btn-outline-secondary" OnClick="btnNext_Click" />
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
                            <label class="form-label">Status <span class="required-star">*</span></label>
                            <asp:DropDownList ID="ddlCreateStatus" runat="server" CssClass="form-control filter-select">
                                <asp:ListItem Value="">-- Select Status --</asp:ListItem>
                                <asp:ListItem Value="New">New</asp:ListItem>
                                <asp:ListItem Value="Assigned">Assigned</asp:ListItem>
                                <asp:ListItem Value="In Progress">In Progress</asp:ListItem>
                                <asp:ListItem Value="Resolved">Resolved</asp:ListItem>
                                <asp:ListItem Value="Closed">Closed</asp:ListItem>
                            </asp:DropDownList>
                            <asp:RequiredFieldValidator ID="rfvCreateStatus" runat="server" ControlToValidate="ddlCreateStatus"
                                InitialValue="" ErrorMessage="Status is required." ForeColor="Red" Display="Dynamic"
                                ValidationGroup="CreateTicket" Font-Size="11px" />
                        </div>
                        <div class="col-md-4">
                            <label class="form-label">Assigned To <span class="required-star">*</span></label>
                            <asp:DropDownList ID="ddlCreateAssignedTo" runat="server" CssClass="form-control filter-select">
                            </asp:DropDownList>
                            <asp:RequiredFieldValidator ID="rfvCreateAssignedTo" runat="server" ControlToValidate="ddlCreateAssignedTo"
                                InitialValue="" ErrorMessage="Assigned To is required." ForeColor="Red" Display="Dynamic"
                                ValidationGroup="CreateTicket" Font-Size="11px" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <label class="form-label">Created By</label>
                            <asp:TextBox ID="txtCreatedBy" runat="server" CssClass="form-control" ReadOnly="true" />
                        </div>
                        <div class="col-md-6 mb-3">
                            <label class="form-label">Created Date</label>
                            <asp:TextBox ID="txtCreatedDate" runat="server" CssClass="form-control" ReadOnly="true" />
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
                            <p class="form-control-plaintext" style="white-space:pre-wrap; background:#f8f9fa; border-radius:8px; padding:10px;">
                                <asp:Label ID="lblViewDescription" runat="server" />
                            </p>
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

                    <div class="remarks-section">
                        <div class="remarks-title">
                            <i class="fas fa-comments mr-2"></i>Support Remarks
                        </div>
                        <asp:Repeater ID="rptRemarks" runat="server">
                            <ItemTemplate>
                                <div class="remark-item">
                                    <div class="remark-header">
                                        <span class="remark-author"><%# Eval("FULL_NAME") %></span>
                                        <span class="text-muted ml-2">•</span>
                                        <span class="text-muted ml-2"><%# Convert.ToDateTime(Eval("CREATED_AT")).ToString("MMM dd, yyyy hh:mm tt") %></span>
                                    </div>
                                    <div class="remark-text">
                                        <%# Eval("REMARK_TEXT") %>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                        <asp:Panel ID="pnlNoRemarks" runat="server" Visible="false" CssClass="no-remarks">
                            <i class="fas fa-info-circle mr-2"></i>No remarks have been added to this ticket yet.
                        </asp:Panel>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="modalEditTicket" tabindex="-1" role="dialog" data-backdrop="static">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title"><i class="fas fa-edit mr-2"></i>Edit Ticket</h5>
                    <button type="button" class="close" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <asp:HiddenField ID="hfEditTicketId" runat="server" />
                    <div class="row mb-3">
                        <div class="col-12">
                            <label class="form-label">Ticket Number</label>
                            <asp:TextBox ID="txtEditTicketNumber" runat="server" CssClass="form-control" ReadOnly="true" />
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-12">
                            <label class="form-label">Title <span class="required-star">*</span></label>
                            <asp:TextBox ID="txtEditTitle" runat="server" CssClass="form-control" MaxLength="200" />
                            <asp:RequiredFieldValidator ID="rfvEditTitle" runat="server" ControlToValidate="txtEditTitle"
                                ErrorMessage="Title is required." ForeColor="Red" Display="Dynamic"
                                ValidationGroup="EditTicket" Font-Size="11px" />
                        </div>
                    </div>
                    <div class="row mb-3">
                        <div class="col-12">
                            <label class="form-label">Description <span class="required-star">*</span></label>
                            <asp:TextBox ID="txtEditDescription" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="5" />
                            <asp:RequiredFieldValidator ID="rfvEditDescription" runat="server" ControlToValidate="txtEditDescription"
                                ErrorMessage="Description is required." ForeColor="Red" Display="Dynamic"
                                ValidationGroup="EditTicket" Font-Size="11px" />
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                    <asp:Button ID="btnSaveEdit" runat="server" Text="Save Changes" CssClass="btn btn-create" OnClick="btnSaveEdit_Click" ValidationGroup="EditTicket" />
                </div>
            </div>
        </div>
    </div>

    <asp:HiddenField ID="hfShowModal" runat="server" Value="" />
    <asp:HiddenField ID="hfSwalMessage" runat="server" Value="" />
    <asp:HiddenField ID="hfSwalType" runat="server" Value="" />

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

        var modal = $('#<%= hfShowModal.ClientID %>').val();
        if (modal === 'view') {
            $('#modalViewTicket').modal('show');
        } else if (modal === 'edit') {
            $('#modalEditTicket').modal('show');
        } else if (modal === 'create') {
            $('#modalCreateTicket').modal('show');
        }

    });

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

        if (oldValue === newValue) {
            return false;
        }

        if (newValue === 'New' || newValue === 'Assigned') {
            ddl.value = oldValue;
            Swal.fire({
                icon: 'error',
                title: 'Not Allowed',
                text: '"New" and "Assigned" are set automatically.',
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 2500
            });
            return false;
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
</script>
</asp:Content>
