<%@ Page Title="User Management"
Language="C#"
MasterPageFile="~/Site.Master"
AutoEventWireup="true"
CodeBehind="Users.aspx.cs"
Inherits="BI_TICKETING_SYSTEM.Pages.Users" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
    <style>
        .modal-header { background: linear-gradient(135deg, #001f54, #003087); color: white; border-radius: 10px 10px 0 0; }
        .modal-header .btn-close { filter: brightness(0) invert(1); }
        .modal-content { border-radius: 10px; border: none; box-shadow: 0 10px 40px rgba(0,0,0,0.2); }
        .form-label { font-size: 11px; font-weight: 600; color: #001f54; text-transform: uppercase; letter-spacing: 0.8px; }
        .form-control:focus { border-color: #001f54; box-shadow: 0 0 0 3px rgba(0,31,84,0.1); }
        .btn-action { padding: 4px 10px; font-size: 12px; border-radius: 6px; border: none; margin: 0 2px; }
        .btn-edit { background: #ffc107; color: #333; }
        .btn-edit:hover { background: #e0a800; color: #333; }
        .btn-reset-password { background: #17a2b8; color: white; }
        .btn-reset-password:hover { background: #138496; color: white; }
        .btn-delete { background: #dc3545; color: white; }
        .btn-delete:hover { background: #c82333; color: white; }
        .toggle-switch { position: relative; display: inline-block; width: 50px; height: 24px; }
        .toggle-switch input { opacity: 0; width: 0; height: 0; }
        .toggle-slider { position: absolute; cursor: pointer; top: 0; left: 0; right: 0; bottom: 0; background-color: #ccc; transition: .4s; border-radius: 24px; }
        .toggle-slider:before { position: absolute; content: ""; height: 18px; width: 18px; left: 3px; bottom: 3px; background-color: white; transition: .4s; border-radius: 50%; }
        input:checked + .toggle-slider { background-color: #28a745; }
        input:checked + .toggle-slider:before { transform: translateX(26px); }
    </style>
    <script>
        function showAddModal() {
            var modal = new bootstrap.Modal(document.getElementById('addUserModal'));
            modal.show();
        }

        function confirmRoleChange(ddl) {
            var oldValue = ddl.getAttribute('data-oldvalue');
            var newValue = ddl.value;
            if (oldValue === newValue) return false;

            if (newValue === 'Admin') {
                Swal.fire({
                    title: 'Are you sure?',
                    text: 'Are you sure you want to change this user role to Admin?',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#001f54',
                    cancelButtonColor: '#6c757d',
                    confirmButtonText: 'Yes, change it!'
                }).then((result) => {
                    if (result.isConfirmed) {
                        Swal.fire({
                            title: 'Final Confirmation',
                            text: 'Admin access is crucial. Do you REALLY want to assign this user as Admin?',
                            icon: 'warning',
                            showCancelButton: true,
                            confirmButtonColor: '#dc3545',
                            cancelButtonColor: '#6c757d',
                            confirmButtonText: 'Yes, I confirm!'
                        }).then((result2) => {
                            if (result2.isConfirmed) {
                                __doPostBack(ddl.name, '');
                            } else {
                                ddl.value = oldValue;
                            }
                        });
                    } else {
                        ddl.value = oldValue;
                    }
                });
                return false;
            } else {
                Swal.fire({
                    title: 'Confirm Role Change',
                    text: 'Are you sure you want to change this user role to ' + newValue + '?',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonColor: '#001f54',
                    cancelButtonColor: '#6c757d',
                    confirmButtonText: 'Yes, change it!'
                }).then((result) => {
                    if (result.isConfirmed) {
                        __doPostBack(ddl.name, '');
                    } else {
                        ddl.value = oldValue;
                    }
                });
                return false;
            }
        }

        function confirmStatusToggle(checkbox) {
            var userName = checkbox.getAttribute('data-fullname') || 'this user';
            var action = checkbox.checked ? 'activate' : 'deactivate';

            Swal.fire({
                title: 'Confirm Status Change',
                text: 'Do you want to ' + action + ' ' + userName + ' account?',
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#001f54',
                cancelButtonColor: '#6c757d',
                confirmButtonText: 'Yes, ' + action + '!'
            }).then((result) => {
                if (result.isConfirmed) {
                    __doPostBack(checkbox.name, '');
                } else {
                    checkbox.checked = !checkbox.checked;
                }
            });
            return false;
        }

        function confirmDeleteUser(btn, userName) {
            Swal.fire({
                title: 'Delete User',
                text: 'Are you sure you want to delete ' + userName + '? This action cannot be undone.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc3545',
                cancelButtonColor: '#6c757d',
                confirmButtonText: 'Yes, delete!'
            }).then((result) => {
                if (result.isConfirmed) {
                    btn.onclick = null;
                    btn.click();
                }
            });
            return false;
        }

        function confirmDelete(btn) {
            Swal.fire({
                title: 'Are you sure?',
                text: 'This user will be permanently deleted!',
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
                Text="Create User"
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
        OnRowCommand="gvUsers_RowCommand"
        OnRowDataBound="gvUsers_RowDataBound">

        <Columns>

            <asp:BoundField DataField="FULL_NAME" HeaderText="Full Name" />
            <asp:BoundField DataField="USERNAME" HeaderText="Username" />
            <asp:BoundField DataField="EMAIL" HeaderText="Email" />

            <asp:TemplateField HeaderText="Role">
                <ItemTemplate>
                    <asp:DropDownList ID="ddlUserRole" runat="server" CssClass="form-select form-select-sm" AutoPostBack="true" OnSelectedIndexChanged="ddlUserRole_Changed">
                        <asp:ListItem Value="Support">Support</asp:ListItem>
                        <asp:ListItem Value="User">User</asp:ListItem>
                        <asp:ListItem Value="Admin">Admin</asp:ListItem>
                    </asp:DropDownList>
                    <asp:HiddenField ID="hfUserId" runat="server" Value='<%# Eval("USER_ID") %>' />
                </ItemTemplate>
            </asp:TemplateField>

            <asp:TemplateField HeaderText="Status">
                <ItemTemplate>
                    <label class="toggle-switch">
                        <asp:CheckBox ID="chkStatus" runat="server" AutoPostBack="true" OnCheckedChanged="chkStatus_Changed" />
                        <span class="toggle-slider"></span>
                    </label>
                    <asp:HiddenField ID="hfUserName" runat="server" Value='<%# Eval("FULL_NAME") %>' />
                </ItemTemplate>
            </asp:TemplateField>

            <asp:BoundField DataField="CREATED_AT" HeaderText="Created At" />

            <asp:TemplateField HeaderText="Action">
                <ItemTemplate>

                    <asp:LinkButton
                        runat="server"
                        CssClass="btn btn-action btn-edit"
                        CommandName="EditUser"
                        CommandArgument='<%# Eval("USER_ID") %>'
                        ToolTip="Edit User">
                        <i class="fa-solid fa-pen-to-square"></i>
                    </asp:LinkButton>

                    <asp:LinkButton
                        runat="server"
                        CssClass="btn btn-action btn-reset-password"
                        CommandName="ResetPassword"
                        CommandArgument='<%# Eval("USER_ID") %>'
                        ToolTip="Reset Password">
                        <i class="fa-solid fa-key"></i>
                    </asp:LinkButton>

                    <asp:LinkButton
                        runat="server"
                        CssClass="btn btn-action btn-delete"
                        CommandName="DeleteUser"
                        CommandArgument='<%# Eval("USER_ID") %>'
                        ToolTip="Delete User"
                        OnClientClick="return confirmDelete(this);">
                        <i class="fa-solid fa-trash"></i>
                    </asp:LinkButton>

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

<asp:HiddenField ID="hfShowModal" runat="server" Value="" />
<asp:HiddenField ID="hfSwalMessage" runat="server" Value="" />
<asp:HiddenField ID="hfSwalType" runat="server" Value="" />

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

<!-- EDIT USER MODAL -->
<div class="modal fade" id="modalEditUser" tabindex="-1" role="dialog">
    <div class="modal-dialog modal-lg" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title"><i class="fas fa-user-edit mr-2"></i>Edit User</h5>
                <button type="button" class="close" data-dismiss="modal">
                    <span>&times;</span>
                </button>
            </div>
            <div class="modal-body">
                <asp:HiddenField ID="hfEditUserId" runat="server" />
                <asp:HiddenField ID="hfEditOriginalFullName" runat="server" />
                <asp:HiddenField ID="hfEditOriginalUsername" runat="server" />
                <asp:HiddenField ID="hfEditOriginalEmail" runat="server" />
                <asp:HiddenField ID="hfEditUserHasChanges" runat="server" Value="false" />

                <div class="form-group mb-3">
                    <label class="form-label">Full Name</label>
                    <asp:TextBox ID="txtEditFullName" runat="server" CssClass="form-control" />
                </div>

                <div class="form-group mb-3">
                    <label class="form-label">Username</label>
                    <asp:TextBox ID="txtEditUsername" runat="server" CssClass="form-control" />
                </div>

                <div class="form-group mb-3">
                    <label class="form-label">Email</label>
                    <asp:TextBox ID="txtEditEmail" runat="server" CssClass="form-control" TextMode="Email" />
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                <button type="button" id="btnSaveEditUserUI" class="btn btn-primary" disabled onclick="triggerSaveEditUser(); return false;">Save Changes</button>
                <asp:Button ID="btnSaveEditUser" runat="server" Text="Save" OnClick="btnSaveEditUser_Click" style="display:none;" />
            </div>
        </div>
    </div>
</div>

<!-- RESET PASSWORD MODAL -->
<div class="modal fade" id="modalResetPassword" tabindex="-1" role="dialog">
    <div class="modal-dialog" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title"><i class="fas fa-key mr-2"></i>Reset Password</h5>
                <button type="button" class="close" data-dismiss="modal">
                    <span>&times;</span>
                </button>
            </div>
            <div class="modal-body">
                <asp:HiddenField ID="hfResetUserId" runat="server" />
                <asp:HiddenField ID="hfResetPasswordHasChanges" runat="server" Value="false" />

                <div class="form-group mb-3">
                    <label class="form-label">New Password</label>
                    <asp:TextBox ID="txtNewPassword" runat="server" CssClass="form-control" TextMode="Password" placeholder="Enter new password" />
                </div>

                <div class="form-group mb-3">
                    <label class="form-label">Re-type New Password</label>
                    <asp:TextBox ID="txtRetypePassword" runat="server" CssClass="form-control" TextMode="Password" placeholder="Re-enter new password" />
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-dismiss="modal">Cancel</button>
                <button type="button" id="btnSaveResetPasswordUI" class="btn btn-primary" disabled onclick="triggerSaveResetPassword(); return false;">Save Changes</button>
                <asp:Button ID="btnSaveResetPassword" runat="server" Text="Save" OnClick="btnSaveResetPassword_Click" style="display:none;" />
            </div>
        </div>
    </div>
</div>

<script>
    function triggerSaveEditUser() {
        document.getElementById('<%= btnSaveEditUser.ClientID %>').click();
    }

    function triggerSaveResetPassword() {
        document.getElementById('<%= btnSaveResetPassword.ClientID %>').click();
    }

    document.addEventListener('DOMContentLoaded', function () {
        var editFullNameId = '<%= txtEditFullName.ClientID %>';
        var editUsernameId = '<%= txtEditUsername.ClientID %>';
        var editEmailId = '<%= txtEditEmail.ClientID %>';
        var hfEditHasChangesId = '<%= hfEditUserHasChanges.ClientID %>';
        var hfOriginalFullNameId = '<%= hfEditOriginalFullName.ClientID %>';
        var hfOriginalUsernameId = '<%= hfEditOriginalUsername.ClientID %>';
        var hfOriginalEmailId = '<%= hfEditOriginalEmail.ClientID %>';
        var btnSaveEditId = 'btnSaveEditUserUI';

        function checkEditUserChanges() {
            var currentFullName = document.getElementById(editFullNameId).value;
            var currentUsername = document.getElementById(editUsernameId).value;
            var currentEmail = document.getElementById(editEmailId).value;

            var originalFullName = document.getElementById(hfOriginalFullNameId).value;
            var originalUsername = document.getElementById(hfOriginalUsernameId).value;
            var originalEmail = document.getElementById(hfOriginalEmailId).value;

            var hasChanges = (currentFullName !== originalFullName) || 
                           (currentUsername !== originalUsername) || 
                           (currentEmail !== originalEmail);

            document.getElementById(hfEditHasChangesId).value = hasChanges ? 'true' : 'false';
            document.getElementById(btnSaveEditId).disabled = !hasChanges;
        }

        document.getElementById(editFullNameId).addEventListener('input', checkEditUserChanges);
        document.getElementById(editUsernameId).addEventListener('input', checkEditUserChanges);
        document.getElementById(editEmailId).addEventListener('input', checkEditUserChanges);

        var newPasswordId = '<%= txtNewPassword.ClientID %>';
        var retypePasswordId = '<%= txtRetypePassword.ClientID %>';
        var hfResetHasChangesId = '<%= hfResetPasswordHasChanges.ClientID %>';
        var btnSaveResetId = 'btnSaveResetPasswordUI';

        function checkResetPasswordChanges() {
            var newPassword = document.getElementById(newPasswordId).value;
            var retypePassword = document.getElementById(retypePasswordId).value;

            var hasChanges = newPassword.length > 0 && retypePassword.length > 0;

            document.getElementById(hfResetHasChangesId).value = hasChanges ? 'true' : 'false';
            document.getElementById(btnSaveResetId).disabled = !hasChanges;
        }

        document.getElementById(newPasswordId).addEventListener('input', checkResetPasswordChanges);
        document.getElementById(retypePasswordId).addEventListener('input', checkResetPasswordChanges);

        var swalMsg = document.getElementById('<%= hfSwalMessage.ClientID %>').value;
        var swalType = document.getElementById('<%= hfSwalType.ClientID %>').value;

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

        var modal = document.getElementById('<%= hfShowModal.ClientID %>').value;
        if (modal === 'edit') {
            var editModal = new bootstrap.Modal(document.getElementById('modalEditUser'));
            editModal.show();
        } else if (modal === 'reset') {
            var resetModal = new bootstrap.Modal(document.getElementById('modalResetPassword'));
            resetModal.show();
        }
    });
</script>

</asp:Content>