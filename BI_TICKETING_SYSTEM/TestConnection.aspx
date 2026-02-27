<%@ Page Title="Test Connection" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="TestConnection.aspx.cs"
    Inherits="BI_TICKETING_SYSTEM.TestConnection" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Database Connection Test</h2>
    <asp:Button ID="btnTestConnection" runat="server"
        Text="🔌 Test Database Connection"
        OnClick="btnTestConnection_Click"
        CssClass="btn btn-primary" />
    <br /><br />
    <asp:Label ID="lblResult" runat="server" Text=""></asp:Label>
</asp:Content>