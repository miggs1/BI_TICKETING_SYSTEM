<%@ Page Title="Audit Logs" Language="C#" MasterPageFile="~/Site.Master"
AutoEventWireup="true" CodeBehind="AuditLogs.aspx.cs"
Inherits="BI_TICKETING_SYSTEM.Pages.AuditLogs" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

<div class="container-fluid">
    <br />
    <h4 class="mb-3 font-weight-bold">Audit Logs</h4>

<div class="card shadow mb-4">

    <div class="card-body">

        <!-- FILTERS -->
        <div class="row mb-3">

            <div class="col-md-3">
                <asp:DropDownList ID="ddlUser" runat="server" CssClass="form-control">
                    <asp:ListItem Value="">All Users</asp:ListItem>
                </asp:DropDownList>
            </div>

            <div class="col-md-3">
                <asp:DropDownList ID="ddlAction" runat="server" CssClass="form-control">
                    <asp:ListItem Value="">All Actions</asp:ListItem>
                    <asp:ListItem Value="LOGIN">Login</asp:ListItem>
                    <asp:ListItem Value="ASSIGN_TICKET">Ticket Assigned</asp:ListItem>
                    <asp:ListItem Value="STATUS_CHANGE">Ticket Status Change</asp:ListItem>
                </asp:DropDownList>
            </div>

            <div class="col-md-2">
                <asp:TextBox ID="txtDateFrom" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
            </div>

            <div class="col-md-2">
                <asp:TextBox ID="txtDateTo" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
            </div>

            <div class="col-md-2">
                <asp:Button ID="btnFilter" runat="server" Text="Filter"
                    CssClass="btn btn-primary btn-block"
                    OnClick="btnFilter_Click" />
            </div>

        </div>

        <!-- AUDIT TABLE -->
        <asp:GridView ID="gvAuditLogs"
            runat="server"
            AutoGenerateColumns="False"
            CssClass="table table-bordered"
            AllowSorting="True"
            OnSorting="gvAuditLogs_Sorting">

            <Columns>

                <asp:BoundField DataField="FULL_NAME" HeaderText="User" SortExpression="FULL_NAME" />

                <asp:BoundField DataField="ACTION_TEXT" HeaderText="Activity" />

                <asp:BoundField DataField="CREATED_AT"
                    HeaderText="Date"
                    DataFormatString="{0:MMM dd, yyyy HH:mm}"
                    SortExpression="CREATED_AT" />

            </Columns>

        </asp:GridView>

    </div>
</div></div>

</asp:Content>