<%@ Page Title="CRUD Grid" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="crud_grid.aspx.cs"
    Inherits="BI_TICKETING_SYSTEM.CRUD.crud_grid" %>

<asp:Content ID="Content1" ContentPlaceHolderID="PageTitle" runat="server">
    CRUD Grid
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <div class="card-header">
            <h3 class="card-title">Data Management</h3>
        </div>
        <div class="card-body">
            <asp:GridView ID="GridView1" runat="server" CssClass="table table-bordered table-striped"
                AutoGenerateColumns="true" AllowPaging="true" PageSize="10"
                OnPageIndexChanging="GridView1_PageIndexChanging">
            </asp:GridView>
        </div>
    </div>
</asp:Content>