<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="BI_TICKETING_SYSTEM._Default" %>

<asp:Content ID="HeadExtra" ContentPlaceHolderID="HeadContent" runat="server">
<style>
    .stat-card {
        border-radius: 14px !important;
        border: none !important;
        overflow: hidden;
        box-shadow: 0 6px 20px rgba(0,0,0,0.10) !important;
        transition: transform 0.25s ease, box-shadow 0.25s ease;
        position: relative;
    }
    .stat-card:hover {
        transform: translateY(-4px);
        box-shadow: 0 12px 30px rgba(0,0,0,0.15) !important;
    }
    .stat-card .card-body { padding: 22px 20px !important; position: relative; z-index: 1; }
    .stat-card .stat-icon {
        position: absolute; right: 18px; top: 50%;
        transform: translateY(-50%);
        font-size: 52px; opacity: 0.15; z-index: 0;
    }
    .stat-card .stat-number {
        font-size: 32px; font-weight: 700; color: #fff; line-height: 1; margin-bottom: 4px;
    }
    .stat-card .stat-label {
        font-size: 12px; font-weight: 500; color: rgba(255,255,255,0.85);
        text-transform: uppercase; letter-spacing: 1px;
    }
    .stat-card .stat-footer {
        background: rgba(0,0,0,0.12);
        padding: 8px 20px; font-size: 11px; color: rgba(255,255,255,0.8);
        border-top: 1px solid rgba(255,255,255,0.1);
    }
    .stat-card .stat-footer a { color: rgba(255,255,255,0.9); text-decoration: none; font-weight: 500; }
    .stat-card .stat-footer a:hover { color: #fff; }

    .stat-navy   { background: linear-gradient(135deg, #001f54 0%, #003087 100%); }
    .stat-gold   { background: linear-gradient(135deg, #b8860b 0%, #d4a017 100%); }
    .stat-crimson{ background: linear-gradient(135deg, #8b0000 0%, #ce1126 100%); }
    .stat-teal   { background: linear-gradient(135deg, #006064 0%, #00838f 100%); }

    .welcome-card {
        border-radius: 14px !important; border: none !important; overflow: hidden;
        box-shadow: 0 4px 15px rgba(0,0,0,0.08) !important;
    }
    .welcome-card .card-header {
        background: linear-gradient(135deg, #001f54, #003087) !important;
        border-bottom: 3px solid #d4a017 !important;
        padding: 16px 22px !important; position: relative;
    }
    .welcome-card .card-header::before { display: none !important; }
    .welcome-card .card-header .card-title { color: #fff !important; font-size: 15px !important; }
    .welcome-card .card-header .card-title i { color: #d4a017; }
    .welcome-card .card-body { background: #fff; padding: 22px !important; }
    .welcome-card .welcome-text h5 { color: #001f54; font-weight: 600; font-size: 17px; }
    .welcome-card .welcome-text p { color: #555; font-size: 13px; margin-bottom: 0; }

    .welcome-banner {
        background: linear-gradient(135deg, rgba(0,31,84,0.05), rgba(212,160,23,0.08));
        border: 1px solid rgba(212,160,23,0.2);
        border-left: 4px solid #d4a017;
        border-radius: 8px; padding: 14px 18px;
        display: flex; align-items: center; gap: 14px;
    }
    .welcome-banner .banner-icon { font-size: 36px; color: #d4a017; flex-shrink: 0; }

    .quick-actions-card {
        border-radius: 14px !important; border: none !important;
        box-shadow: 0 4px 15px rgba(0,0,0,0.08) !important; overflow: hidden;
    }
    .quick-actions-card .card-header {
        background: linear-gradient(135deg, #001f54, #003087) !important;
        border-bottom: 3px solid #d4a017 !important; padding: 16px 22px !important;
    }
    .quick-actions-card .card-header::before { display: none !important; }
    .quick-actions-card .card-header .card-title { color: #fff !important; font-size: 15px !important; }
    .quick-actions-card .card-header .card-title i { color: #d4a017; }

    .quick-action-btn {
        display: flex; align-items: center; gap: 12px;
        padding: 13px 16px; border-radius: 10px;
        border: 1.5px solid #e8eaf0; background: #fff;
        color: #001f54; font-weight: 500; font-size: 13px;
        text-decoration: none; transition: all 0.25s ease;
        width: 100%; margin-bottom: 10px;
    }
    .quick-action-btn i {
        width: 36px; height: 36px; border-radius: 8px;
        display: flex; align-items: center; justify-content: center;
        font-size: 16px; flex-shrink: 0;
        background: rgba(0,31,84,0.08); color: #001f54; transition: all 0.25s;
    }
    .quick-action-btn:hover {
        background: linear-gradient(135deg, #001f54, #003087);
        color: #fff; border-color: #001f54;
        text-decoration: none; transform: translateX(4px);
    }
    .quick-action-btn:hover i { background: rgba(212,160,23,0.3); color: #d4a017; }

    /* Real-time refresh indicator */
    #refreshIndicator {
        font-size: 11px; color: #888;
        text-align: right; margin-bottom: 8px;
    }
    #refreshIndicator.refreshing { color: #d4a017; }
</style>
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <!-- REFRESH INDICATOR -->
    <div id="refreshIndicator">
        <i class="fas fa-circle" style="color:#28a745;font-size:8px;"></i>
        Last updated: <span id="lastUpdated">Just now</span>
    </div>

    <!-- STAT CARDS -->
    <div class="row mb-4">
        <div class="col-lg-3 col-md-6 col-sm-6 mb-3">
            <div class="stat-card stat-navy">
                <div class="card-body">
                    <i class="fas fa-ticket-alt stat-icon"></i>
                    <div class="stat-number">
                        <span id="statTotal"><asp:Label ID="lblTotalTickets" runat="server" Text="0" /></span>
                    </div>
                    <div class="stat-label">Total Tickets</div>
                </div>
                <div class="stat-footer">
                    <i class="fas fa-arrow-circle-right mr-1"></i>
                    <a href="~/Pages/Tickets.aspx" runat="server">View All Tickets</a>
                </div>
            </div>
        </div>
        <div class="col-lg-3 col-md-6 col-sm-6 mb-3">
            <div class="stat-card stat-teal">
                <div class="card-body">
                    <i class="fas fa-check-circle stat-icon"></i>
                    <div class="stat-number">
                        <span id="statResolved"><asp:Label ID="lblResolved" runat="server" Text="0" /></span>
                    </div>
                    <div class="stat-label">Resolved</div>
                </div>
                <div class="stat-footer">
                    <i class="fas fa-arrow-circle-right mr-1"></i>
                    <a href="~/Pages/Tickets.aspx" runat="server">View Resolved</a>
                </div>
            </div>
        </div>
        <div class="col-lg-3 col-md-6 col-sm-6 mb-3">
            <div class="stat-card stat-gold">
                <div class="card-body">
                    <i class="fas fa-clock stat-icon"></i>
                    <div class="stat-number">
                        <span id="statPending"><asp:Label ID="lblPending" runat="server" Text="0" /></span>
                    </div>
                    <div class="stat-label">Pending</div>
                </div>
                <div class="stat-footer">
                    <i class="fas fa-arrow-circle-right mr-1"></i>
                    <a href="~/Pages/Tickets.aspx" runat="server">View Pending</a>
                </div>
            </div>
        </div>
        <div class="col-lg-3 col-md-6 col-sm-6 mb-3">
            <div class="stat-card stat-crimson">
                <div class="card-body">
                    <i class="fas fa-exclamation-triangle stat-icon"></i>
                    <div class="stat-number">
                        <span id="statOverdue"><asp:Label ID="lblOverdue" runat="server" Text="0" /></span>
                    </div>
                    <div class="stat-label">Overdue</div>
                </div>
                <div class="stat-footer">
                    <i class="fas fa-arrow-circle-right mr-1"></i>
                    <a href="~/Pages/Tickets.aspx" runat="server">View Overdue</a>
                </div>
            </div>
        </div>
    </div>

    <!-- WELCOME + QUICK ACTIONS -->
    <div class="row">
        <div class="col-lg-8 mb-3">
            <div class="card welcome-card">
                <div class="card-header">
                    <h3 class="card-title">
                        <i class="fas fa-home mr-2"></i>Welcome to the Dashboard
                    </h3>
                </div>
                <div class="card-body">
                    <div class="welcome-banner">
                        <div class="banner-icon">
                            <img src="Images/bi-seal.png" alt="BI Seal"
                                 style="width:52px;height:52px;border-radius:50%;border:2px solid rgba(212,160,23,0.5);object-fit:contain;background:#000;padding:3px;" />
                        </div>
                        <div class="welcome-text">
                            <h5 class="mb-1">Welcome back, <asp:Label ID="lblWelcomeUser" runat="server" Text="Admin" />!</h5>
                            <p>You are logged into the <strong>Bureau of Immigration Ticketing System</strong>. Use the sidebar to manage tickets, users, and system settings.</p>
                        </div>
                    </div>
                    <div class="row mt-4 text-center">
                        <div class="col-4">
                            <div style="padding:14px;background:rgba(0,31,84,0.05);border-radius:10px;">
                                <div style="font-size:22px;font-weight:700;color:#001f54;">
                                    <span id="statOpen"><asp:Label ID="lblOpenCount" runat="server" Text="0" /></span>
                                </div>
                                <div style="font-size:11px;color:#888;text-transform:uppercase;letter-spacing:1px;">Open</div>
                            </div>
                        </div>
                        <div class="col-4">
                            <div style="padding:14px;background:rgba(212,160,23,0.08);border-radius:10px;">
                                <div style="font-size:22px;font-weight:700;color:#b8860b;">
                                    <span id="statInProgress"><asp:Label ID="lblInProgressCount" runat="server" Text="0" /></span>
                                </div>
                                <div style="font-size:11px;color:#888;text-transform:uppercase;letter-spacing:1px;">In Progress</div>
                            </div>
                        </div>
                        <div class="col-4">
                            <div style="padding:14px;background:rgba(0,96,100,0.06);border-radius:10px;">
                                <div style="font-size:22px;font-weight:700;color:#006064;">
                                    <span id="statClosed"><asp:Label ID="lblClosedCount" runat="server" Text="0" /></span>
                                </div>
                                <div style="font-size:11px;color:#888;text-transform:uppercase;letter-spacing:1px;">Closed</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div class="col-lg-4 mb-3">
            <div class="card quick-actions-card">
                <div class="card-header">
                    <h3 class="card-title"><i class="fas fa-bolt mr-2"></i>Quick Actions</h3>
                </div>
                <div class="card-body" style="padding: 16px !important;">
                    <a href="~/Pages/Tickets.aspx" runat="server" class="quick-action-btn">
                        <i class="fas fa-ticket-alt"></i> View All Tickets
                    </a>
                    <a href="~/Pages/Tickets.aspx" runat="server" class="quick-action-btn">
                        <i class="fas fa-plus-circle"></i> Create New Ticket
                    </a>
                    <asp:Panel ID="pnlAdminActions" runat="server" Visible="false">
                        <a href="~/Pages/Users.aspx" runat="server" class="quick-action-btn">
                            <i class="fas fa-users-cog"></i> Manage Users
                        </a>
                        <a href="~/Pages/AuditLogs.aspx" runat="server" class="quick-action-btn">
                            <i class="fas fa-history"></i> View Audit Logs
                        </a>
                    </asp:Panel>
                </div>
            </div>
        </div>
    </div>

</asp:Content>

<asp:Content ID="ScriptsExtra" ContentPlaceHolderID="ScriptsContent" runat="server">
<script>
    // Auto-refresh stats every 30 seconds via AJAX
    function refreshStats() {
        $('#refreshIndicator').addClass('refreshing');
        $('#refreshIndicator').html('<i class="fas fa-sync fa-spin" style="color:#d4a017;font-size:10px;"></i> Refreshing...');

        $.ajax({
            url: 'DashboardHandler.ashx',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                // Animate number update
                animateNumber('statTotal', data.total);
                animateNumber('statResolved', data.resolved);
                animateNumber('statPending', data.pending);
                animateNumber('statOverdue', data.overdue);
                animateNumber('statOpen', data.open);
                animateNumber('statInProgress', data.inProgress);
                animateNumber('statClosed', data.closed);

                var now = new Date();
                var time = now.toLocaleTimeString();
                $('#refreshIndicator').removeClass('refreshing');
                $('#refreshIndicator').html('<i class="fas fa-circle" style="color:#28a745;font-size:8px;"></i> Last updated: ' + time);
            },
            error: function () {
                $('#refreshIndicator').html('<i class="fas fa-circle" style="color:#dc3545;font-size:8px;"></i> Update failed — retrying...');
            }
        });
    }

    function animateNumber(spanId, newValue) {
        var el = $('#' + spanId).find('span').length ? $('#' + spanId).find('span') : $('#' + spanId);
        var current = parseInt(el.text()) || 0;
        if (current !== newValue) {
            el.fadeOut(150, function () {
                el.text(newValue).fadeIn(150);
            });
        }
    }

    // Refresh every 30 seconds
    setInterval(refreshStats, 30000);
</script>
</asp:Content>