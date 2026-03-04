<%@ Register Assembly="DevExpress.Web.v25.2"
    Namespace="DevExpress.Web"
    TagPrefix="dx" %>

<%@ Page Title="Tickets" Language="C#" 
    MasterPageFile="~/Site.Master" 
    AutoEventWireup="true" 
    CodeBehind="Tickets.aspx.cs" 
    Inherits="BI_TICKETING_SYSTEM.Tickets" %>

<asp:Content ID="Content1" 
    ContentPlaceHolderID="MainContent" 
    runat="server">

    <dx:ASPxGridView ID="gvTickets" 
        runat="server"
        AutoGenerateColumns="False"
        KeyFieldName="TICKET_ID"
        OnRowUpdating="gvTickets_RowUpdating"
        OnRowInserting="gvTickets_RowInserting"
        OnRowDeleting="gvTickets_RowDeleting">

        <Columns>
            <dx:GridViewDataTextColumn FieldName="TICKET_NUMBER" Caption="Ticket #" />
            <dx:GridViewDataTextColumn FieldName="TITLE" Caption="Title" />
            <dx:GridViewDataTextColumn FieldName="PRIORITY" Caption="Priority" />
            <dx:GridViewDataTextColumn FieldName="STATUS" Caption="Status" />
            <dx:GridViewDataDateColumn FieldName="CREATED_AT" Caption="Created" />

            <dx:GridViewCommandColumn ShowEditButton="true" ShowDeleteButton="true" />
        </Columns>

    </dx:ASPxGridView>

</asp:Content>