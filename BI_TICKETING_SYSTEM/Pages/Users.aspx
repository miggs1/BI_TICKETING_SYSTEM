<%@ Page Title="User Management"
Language="C#"
MasterPageFile="~/Site.Master"
AutoEventWireup="true"
CodeBehind="Users.aspx.cs"
Inherits="BI_TICKETING_SYSTEM.Pages.Users" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <script>
        function showAddModal() {
            var modal = new bootstrap.Modal(document.getElementById('addUserModal'));
            modal.show();
        }
    </script>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>



<div class="container mt-4">

    <h2 class="mb-4">User Management</h2>

    <!-- SEARCH -->
    <div class="row mb-3">
        <div class="col-md-4">
            <asp:TextBox
                ID="txtSearch"
                runat="server"
                CssClass="form-control"
                placeholder="Search users..."
                AutoPostBack="true"
                OnTextChanged="txtSearch_TextChanged" />
        </div>
        <div class="col-md-2">
            <asp:Button
                ID="btnAddUser"
                runat="server"
                CssClass="btn btn-primary"
                Text="Add User"
                OnClientClick="showAddModal(); return false;" />
        </div>
    </div>

    <script type="text/javascript">
        (function () {
            var searchTimer = null;
            var searchDelay = 450; // milliseconds
            var inputId = '<%= txtSearch.ClientID %>';
            var uniqueId = '<%= txtSearch.UniqueID %>';

            function triggerSearch() {
                if (typeof __doPostBack === 'function') {
                    __doPostBack(uniqueId, '');
                }
            }

            function onInput() {
                if (searchTimer) clearTimeout(searchTimer);
                searchTimer = setTimeout(triggerSearch, searchDelay);
            }

            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', function () {
                    var el = document.getElementById(inputId);
                    if (el) el.addEventListener('input', onInput);
                });
            } else {
                var el = document.getElementById(inputId);
                if (el) el.addEventListener('input', onInput);
            }
        })();
    </script>


    <!-- USER TABLE -->
    <asp:GridView
        ID="gvUsers"
        runat="server"
        CssClass="table table-bordered table-striped"
        AutoGenerateColumns="false"
        OnRowCommand="gvUsers_RowCommand">

        <Columns>

            <asp:BoundField DataField="FULL_NAME" HeaderText="Full Name" />
            <asp:BoundField DataField="USERNAME" HeaderText="Username" />
            <asp:BoundField DataField="EMAIL" HeaderText="Email" />
            <asp:BoundField DataField="ROLE" HeaderText="Role" />
            <asp:BoundField DataField="STATUS" HeaderText="Status" />
            <asp:BoundField DataField="CREATED_AT" HeaderText="Created At" />

            <asp:TemplateField HeaderText="Action">
                <ItemTemplate>

                    <asp:Button
                        runat="server"
                        Text="Delete"
                        CssClass="btn btn-danger btn-sm"
                        CommandName="DeleteUser"
                        CommandArgument='<%# Eval("USER_ID") %>' />

                </ItemTemplate>
            </asp:TemplateField>

        </Columns>

    </asp:GridView>


    <!-- PAGINATION -->

    <div class="mt-3">

        <asp:Button
            ID="btnPrev"
            runat="server"
            Text="Previous"
            CssClass="btn btn-secondary"
            OnClick="btnPrev_Click" />

        <asp:Label
            ID="lblPage"
            runat="server"
            CssClass="mx-3" />

        <asp:Button
            ID="btnNext"
            runat="server"
            Text="Next"
            CssClass="btn btn-secondary"
            OnClick="btnNext_Click" />

    </div>

</div>


<!-- ADD USER MODAL -->

<div class="modal fade" id="addUserModal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">

            <div class="modal-header">
                <h5 class="modal-title">Create User</h5>
            </div>

            <div class="modal-body">

                <div class="mb-3">
                    <label>Full Name</label>
                    <asp:TextBox ID="txtFullName" runat="server" CssClass="form-control" />
                </div>

                <div class="mb-3">
                    <label>Email</label>
                    <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" />
                </div>

                <div class="mb-3">
                    <label>Username</label>
                    <asp:TextBox ID="txtUName" runat="server" CssClass="form-control" />
                </div>

                <div class="mb-3">
                    <label>Role</label>
                <asp:DropDownList ID="ddlRole" runat="server" CssClass="form-control">
                        <asp:ListItem Value="Admin">Admin</asp:ListItem>
                        <asp:ListItem Value="Support">Support</asp:ListItem>
                        <asp:ListItem Value="User">User</asp:ListItem>
                    </asp:DropDownList>
                </div>

            </div>

            <div class="modal-footer">
                <asp:Button
                    ID="btnCreateUser"
                    runat="server"
                    CssClass="btn btn-success"
                    Text="Create"
                    OnClick="btnCreateUser_Click" />
            </div>

        </div>
    </div>
</div>



</asp:Content>