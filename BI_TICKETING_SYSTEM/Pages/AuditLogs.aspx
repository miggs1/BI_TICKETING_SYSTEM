<%@ Page Title="Audit Logs" Language="C#" MasterPageFile="~/Site.Master"
AutoEventWireup="true" CodeBehind="AuditLogs.aspx.cs"
Inherits="BI_TICKETING_SYSTEM.Pages.AuditLogs" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

<div class="container-fluid">

    <h4 class="mb-3 font-weight-bold">Audit Logs</h4>

    <div class="card shadow-sm">
        <div class="card-body p-0">
        <table class="table table-hover mb-0">
            <thead>
            <tr>
                <th width="180">DATE</th>
                <th>ACTIVITY</th>
            </tr>
            </thead>

            <tbody>
                <asp:Repeater ID="rptAuditLogs" runat="server">

                <ItemTemplate>
                    <tr>
                        <td>
                            <%# Eval("CREATED_AT", "{0:MMM dd, yyyy hh:mm tt}") %>
                        </td>
                        <td>
                            <%# Eval("MESSAGE") %>
                        </td>
                    </tr>
                </ItemTemplate>
                </asp:Repeater>
            </tbody>
        </table>
        </div>
    </div>

</div>

</asp:Content>