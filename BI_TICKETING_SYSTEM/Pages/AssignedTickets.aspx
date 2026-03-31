<%@ Page Title="Assigned Tickets" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AssignedTickets.aspx.cs" Inherits="BI_TICKETING_SYSTEM.Pages.AssignedTickets" %>

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
    .audit-row-support { border-left: 3px solid #28a745; background: rgba(40, 167, 69, 0.05); }
    .audit-badge-status { background: #17a2b8; color: white; font-size: 10px; padding: 3px 8px; border-radius: 10px; display: inline-block; }
    .audit-badge-remark { background: #28a745; color: white; font-size: 10px; padding: 3px 8px; border-radius: 10px; display: inline-block; }
    .dropdown-priority { font-size: 12px; padding: 4px 8px; border-radius: 6px; border: 1px solid #ddd; }
    .dropdown-priority:focus { border-color: #001f54; box-shadow: 0 0 0 2px rgba(0,31,84,0.1); }
    .export-dropdown { position: relative; display: inline-block; align-self: flex-start; }
    .export-toggle { display: flex; align-items: center; justify-content: space-between; gap: 8px; background: #fff; border: 1px solid #ced4da; border-radius: 8px; padding: 0 14px; font-size: 13px; font-weight: 600; color: #001f54; cursor: pointer; height: 38px; min-width: 130px; }
    .export-toggle:hover { border-color: #001f54; }
    .export-toggle.open { border-radius: 8px 8px 0 0; border-bottom-color: transparent; }
    .export-menu { display: none; position: absolute; top: 100%; left: 0; min-width: 100%; background: #fff; border: 1px solid #ced4da; border-top: none; border-radius: 0 0 8px 8px; box-shadow: 0 4px 8px rgba(0,0,0,0.08); z-index: 1050; overflow: hidden; }
    .export-menu.show { display: block; }
    .export-item { display: flex; align-items: center; width: 100%; border: none; background: transparent; text-align: left; padding: 10px 14px; font-size: 13px; color: #001f54; cursor: pointer; white-space: nowrap; }
    .export-item:hover { background: #f0f4ff; }
    .export-item i { width: 18px; margin-right: 8px; }
    .filter-row { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
    .filter-row .filter-status { width: 150px; flex-shrink: 0; }
    .filter-row .filter-date { width: 130px; flex-shrink: 0; }
    .filter-row .filter-search { flex: 1; min-width: 180px; }
    .filter-row .filter-export { flex-shrink: 0; margin-left: auto; }
</style>
</asp:Content>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

    <!-- Alert Messages -->
    <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="alert-success-custom mb-3">
        <i class="fas fa-check-circle mr-2"></i>
        <asp:Label ID="lblSuccess" runat="server" />
    </asp:Panel>
    <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert-danger-custom mb-3">
        <i class="fas fa-exclamation-circle mr-2"></i>
        <asp:Label ID="lblError" runat="server" />
    </asp:Panel>

    <!-- Main Card -->
    <div class="card ticket-card">
        <div class="card-header d-flex justify-content-between align-items-center" style="background:white; border-bottom: 2px solid #f0f4ff; padding: 15px 20px;">
            <h5 class="m-0" style="color:#001f54; font-weight:700;"><i class="fas fa-ticket-alt mr-2"></i>Assigned Tickets</h5>
            <!-- Create button removed — page now only shows tickets assigned to current user -->
        </div>

        <div class="card-body">
            <div class="filter-row mb-3">
                <div class="filter-status">
                    <asp:DropDownList ID="ddlFilterStatus" runat="server" CssClass="form-control filter-select" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_Changed">
                        <asp:ListItem Value="">Ticket Status</asp:ListItem>
                        <asp:ListItem Value="">All Status</asp:ListItem>
                        <asp:ListItem Value="New">New</asp:ListItem>
                        <asp:ListItem Value="Assigned">Assigned</asp:ListItem>
                        <asp:ListItem Value="In Progress">In Progress</asp:ListItem>
                        <asp:ListItem Value="Resolved">Resolved</asp:ListItem>
                        <asp:ListItem Value="Closed">Closed</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="filter-date">
                    <asp:TextBox ID="txtFromDate" runat="server" TextMode="Date" CssClass="form-control filter-select" placeholder="From" />
                </div>
                <div class="filter-date">
                    <asp:TextBox ID="txtToDate" runat="server" TextMode="Date" CssClass="form-control filter-select" placeholder="To" />
                </div>
                <div class="filter-search">
                    <div class="input-group">
                        <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control search-bar" placeholder="Search by ticket number or title..." />
                        <div class="input-group-append">
                            <asp:Button ID="btnSearch" runat="server" CssClass="btn-search" Text="Search" OnClick="btnSearch_Click" />
                        </div>
                    </div>
                </div>
                <div class="filter-export">
                    <div class="export-dropdown">
                        <button type="button" class="export-toggle" onclick="toggleExportMenu(event)">
                            <span><i class="fas fa-download mr-1"></i>Export</span>
                            <i class="fas fa-chevron-down"></i>
                        </button>
                        <div id="exportMenu" class="export-menu">
                            <button type="button" class="export-item" onclick="triggerExport('<%= btnExportPDF.ClientID %>')">
                                <i class="fas fa-file-pdf"></i>Save as PDF
                            </button>
                            <button type="button" class="export-item" onclick="triggerExport('<%= btnExportExcel.ClientID %>')">
                                <i class="fas fa-file-excel"></i>Save as Excel
                            </button>
                        </div>
                    </div>
                    <asp:Button ID="btnExportPDF" runat="server" Text="Export to PDF"
                        OnClick="btnExportPDF_Click" Style="display:none;" />
                    <asp:Button ID="btnExportExcel" runat="server" Text="Export to Excel"
                        OnClick="btnExportExcel_Click" Style="display:none;" />
                </div>
            </div>

            <!-- Tickets Table -->
            <div class="table-responsive">
                <asp:Repeater ID="rptTickets" runat="server" OnItemDataBound="rptTickets_ItemDataBound" OnItemCommand="rptTickets_ItemCommand">
                    <HeaderTemplate>
                        <table class="table table-bordered table-hover">
                            <thead>
                                <tr>
                                    <th>Ticket No.</th>
                                    <th>Title</th>
                                    <th>Status</th>
                                    <th>Priority</th>
                                    <th>Created By</th>
                                    <th>Assigned To</th>
                                    <th>Date</th>
                                    <th style="width:120px;">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr>
                            <td><strong><%# Eval("TICKET_NUMBER") %></strong></td>
                            <td><%# Eval("TITLE") %></td>
                            <td>
                                <span class="badge <%# GetStatusBadge(Eval("STATUS").ToString()) %>" style="padding:5px 10px; border-radius:20px; font-size:11px;">
                                    <%# Eval("STATUS") %>
                                </span>
                            </td>
                            <td>
                                <span class="badge <%# GetPriorityBadge(Eval("PRIORITY").ToString()) %>" style="padding:5px 10px; border-radius:20px; font-size:11px;">
                                    <%# string.IsNullOrEmpty(Eval("PRIORITY").ToString()) ? "Not Set" : Eval("PRIORITY").ToString() %>
                                </span>
                                <asp:HiddenField ID="hfRowTicketId" runat="server" Value='<%# Eval("TICKET_ID") %>' />
                            </td>
                            <td><%# Eval("CREATED_BY_NAME") %></td>
                            <td><%# string.IsNullOrEmpty(Eval("ASSIGNED_TO_NAME").ToString()) ? "<span style='color:#aaa;'>Unassigned</span>" : Eval("ASSIGNED_TO_NAME").ToString() %></td>
                            <td><%# Convert.ToDateTime(Eval("CREATED_AT")).ToString("MM/dd/yyyy") %></td>
                            <td>
                                <asp:LinkButton runat="server" CommandName="ViewTicket"
                                    CommandArgument='<%# Eval("TICKET_ID") %>'
                                    CssClass="btn btn-action btn-view mr-1"
                                    ToolTip="View">
                                    <i class="fas fa-eye"></i>
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

            <!-- Empty State -->
            <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="empty-state">
                <i class="fas fa-ticket-alt"></i>
                <p style="font-size:16px; font-weight:600; color:#555;">No tickets found</p>
                <p style="font-size:13px;">Try adjusting your search or filters</p>
            </asp:Panel>

            <!-- Pagination -->
            <div class="d-flex justify-content-between align-items-center mt-3">
                <asp:Label ID="lblPaginationInfo" runat="server" CssClass="pagination-info" />
                <div>
                    <asp:Button ID="btnPrev" runat="server" Text="← Prev" CssClass="btn btn-sm btn-outline-secondary mr-1" OnClick="btnPrev_Click" />
                    <asp:Button ID="btnNext" runat="server" Text="Next →" CssClass="btn btn-sm btn-outline-secondary" OnClick="btnNext_Click" />
                </div>
            </div>
        </div>
    </div>

    <!-- ===== VIEW TICKET MODAL  ===== -->
    <div class="modal fade" id="modalViewTicket" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title"><i class="fas fa-eye mr-2"></i>Ticket Details</h5>
                    <button type="button" class="close" data-dismiss="modal"><span>&times;</span></button>
                </div>
                <div class="modal-body">
                    <asp:HiddenField ID="hfViewTicketId" runat="server" />

                     <%-- Ticket Number and Status --%>
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
                     <%-- Description --%>
                    <div class="row mb-3">
                        <div class="col-12">
                            <label class="form-label">Description</label>
                            <p class="form-control-plaintext" style="white-space:pre-wrap; background:#f8f9fa; border-radius:8px; padding:10px;">
                                <asp:Label ID="lblViewDescription" runat="server" />
                            </p>
                        </div>
                    </div>

                     <%-- Details --%>
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
                            <label class="form-label">Priority</label>
                            <p class="form-control-plaintext">
                                <asp:Label ID="lblViewPriority" runat="server" />
                            </p>
                        </div>
                    </div>

                    <!-- Remarks section -->
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
                                <tbody id="auditTrailBodyAssigned">
                                    <asp:Repeater ID="rptRemarks" runat="server">
                                        <ItemTemplate>
                                            <tr class='audit-trail-row <%# GetAuditRowClass(Eval("USER_ROLE").ToString()) %>'>
                                                <td style="white-space:nowrap;"><%# Eval("DATE_DISPLAY") %></td>
                                                <td><%# Eval("CHANGED_BY") %></td>
                                                <td><span class='<%# Eval("ENTRY_TYPE").ToString() == "Status Change" ? "audit-badge-status" : "audit-badge-remark" %>'><%# Eval("ENTRY_TYPE") %></span></td>
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
                        <div class="d-flex justify-content-between align-items-center mt-2" id="auditPaginationAssigned">
                            <span class="pagination-info" id="auditPageInfoAssigned"></span>
                            <div>
                                <button type="button" class="btn btn-sm btn-outline-secondary mr-1" id="btnAuditPrevAssigned" onclick="auditPrevAssigned()">Prev</button>
                                <button type="button" class="btn btn-sm btn-outline-secondary" id="btnAuditNextAssigned" onclick="auditNextAssigned()">Next</button>
                            </div>
                        </div>
                        </div>

                    <!-- Add remark form (only visible to assigned support user and admin) -->
                    <asp:Panel ID="pnlAddRemark" runat="server" Visible="false">
                        <div class="row mt-3">
                            <div class="col-12">
                                <label class="form-label">Add Remark</label>
                                <asp:TextBox ID="txtNewRemark" runat="server" CssClass="form-control" TextMode="MultiLine" Rows="3" />
                                <asp:Button ID="btnAddRemark" runat="server" CssClass="btn btn-create mt-2" Text="Add Remark" OnClick="btnAddRemark_Click" />
                            </div>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlClosedRemarkNotice" runat="server" Visible="false" CssClass="alert alert-secondary mt-3">
                        Remarks are disabled for closed tickets.
                    </asp:Panel>

                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

    <!-- ===== HIDDEN FIELDS ===== -->
    <asp:HiddenField ID="hfShowModal" runat="server" Value="" />
    <asp:HiddenField ID="hfSwalMessage" runat="server" Value="" />
    <asp:HiddenField ID="hfSwalType" runat="server" Value="" />

</asp:Content>

<asp:Content ID="ScriptsContent" ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
    function toggleExportMenu(event) {
        event.stopPropagation();
        var menu = document.getElementById('exportMenu');
        var toggle = event.currentTarget;
        menu.classList.toggle('show');
        toggle.classList.toggle('open', menu.classList.contains('show'));
    }

    function triggerExport(buttonId) {
        var menu = document.getElementById('exportMenu');
        menu.classList.remove('show');
        var toggle = menu.parentElement.querySelector('.export-toggle');
        if (toggle) toggle.classList.remove('open');
        document.getElementById(buttonId).click();
    }

    document.addEventListener('click', function () {
        var menu = document.getElementById('exportMenu');
        if (menu) {
            menu.classList.remove('show');
            var toggle = menu.parentElement.querySelector('.export-toggle');
            if (toggle) toggle.classList.remove('open');
        }
    });

    $(document).ready(function () {

        // ===== SWEETALERT2 NOTIFICATION =====
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
        }

        $('#modalViewTicket').on('shown.bs.modal', function () {
            auditCurrentPage = 1;
            auditPaginateAssigned();
        });

        if (modal === 'view') {
            auditCurrentPage = 1;
            auditPaginateAssigned();
        }

    });

    var auditCurrentPage = 1;
    var auditPageSize = 5;

    function auditPaginateAssigned() {
        var rows = $('#auditTrailBodyAssigned tr.audit-trail-row');
        var total = rows.length;
        var totalPages = Math.ceil(total / auditPageSize);
        if (auditCurrentPage > totalPages) auditCurrentPage = totalPages;
        if (auditCurrentPage < 1) auditCurrentPage = 1;
        rows.hide();
        var start = (auditCurrentPage - 1) * auditPageSize;
        var end = start + auditPageSize;
        rows.slice(start, end).show();
        var pageInfo = document.getElementById('auditPageInfoAssigned');
        var pagination = document.getElementById('auditPaginationAssigned');
        if (total > 0 && pagination) {
            pagination.style.display = '';
            if (pageInfo) pageInfo.textContent = 'Showing ' + (start + 1) + '\u2013' + Math.min(end, total) + ' of ' + total + ' entries';
        } else if (pagination) {
            pagination.style.display = 'none';
        }
        var btnPrev = document.getElementById('btnAuditPrevAssigned');
        var btnNext = document.getElementById('btnAuditNextAssigned');
        if (btnPrev) btnPrev.disabled = (auditCurrentPage <= 1);
        if (btnNext) btnNext.disabled = (auditCurrentPage >= totalPages);
    }

    function auditPrevAssigned() {
        if (auditCurrentPage > 1) { auditCurrentPage--; auditPaginateAssigned(); }
    }

    function auditNextAssigned() {
        var rows = $('#auditTrailBodyAssigned tr.audit-trail-row');
        var totalPages = Math.ceil(rows.length / auditPageSize);
        if (auditCurrentPage < totalPages) { auditCurrentPage++; auditPaginateAssigned(); }
    }
    </script>
</asp:Content>
