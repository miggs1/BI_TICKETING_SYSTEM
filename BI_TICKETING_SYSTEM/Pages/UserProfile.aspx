<%@ Page Title="My Profile" Language="C#" MasterPageFile="~/Site.Master"
AutoEventWireup="true" CodeBehind="UserProfile.aspx.cs"
Inherits="BI_TICKETING_SYSTEM.Pages.UserProfile" %>

<asp:Content ID="MainContent" ContentPlaceHolderID="MainContent" runat="server">

<div class="container-fluid">
    <br />
    <h4 class="mb-4 font-weight-bold">My Profile</h4>

    <!-- Success Message -->
    <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="alert alert-success">
        <asp:Label ID="lblSuccess" runat="server"></asp:Label>
    </asp:Panel>

    <!-- Error Message -->
    <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="alert alert-danger">
        <asp:Label ID="lblError" runat="server"></asp:Label>
    </asp:Panel>


    <div class="card shadow-sm">
        <div class="card-body">
                <div class="row">
                    <div class="col-md-4">
                        <label class="form-label">Last Name</label>
                        <asp:TextBox ID="txtLastName" runat="server" CssClass="form-control" ReadOnly="true" />
                    </div>

                    <div class="col-md-4">
                        <label class="form-label">First Name</label>
                        <asp:TextBox ID="txtFirstName" runat="server" CssClass="form-control" ReadOnly="true" />
                    </div>

                    <div class="col-md-4">
                        <label class="form-label">Middle Name</label>
                        <asp:TextBox ID="txtMiddleName" runat="server" CssClass="form-control" ReadOnly="true" />
                    </div>
                </div>

                <div class="row mt-3">
                    <div class="col-md-4">
                        <label class="form-label">Gender</label>
                        <asp:TextBox ID="txtGender" runat="server" CssClass="form-control" ReadOnly="true" />
                    </div>

                    <div class="col-md-4">
                        <label class="form-label">Date Of Birth</label>
                        <asp:TextBox ID="txtDOB" runat="server" CssClass="form-control" ReadOnly="true" />
                    </div>

                    <div class="col-md-4">
                        <label class="form-label">Email</label>
                        <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" ReadOnly="true" />
                    </div>
                </div>

                <div class="row mt-3">
                    <div class="col-md-6">
                        <label class="form-label">Role</label>
                        <asp:TextBox ID="txtRole" runat="server" CssClass="form-control" ReadOnly="true" />
                    </div>
                </div>


                <div class="mt-4">
                    <button type="button" class="btn btn-primary mr-2" data-toggle="modal" data-target="#editProfileModal">
                        Edit Profile</button>
                    <button type="button" class="btn btn-primary" data-toggle="modal" data-target="#passwordModal">
                        Change Password</button>
                </div>
        </div>
    </div>
</div>


<!-- CHANGE PASSWORD MODAL -->

<div class="modal fade" id="passwordModal">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title">Change Password</h5>
            </div>


            <div class="modal-body">
                <div class="form-group">
                    <label>New Password</label>

                    <asp:TextBox
                        ID="txtNewPassword"
                        runat="server"
                        CssClass="form-control"
                        TextMode="Password">
                    </asp:TextBox>
                </div>
            </div>


            <div class="modal-footer">
                <asp:Button
                    ID="btnSavePassword"
                    runat="server"
                    Text="Update Password"
                    CssClass="btn btn-success"
                    OnClick="btnSavePassword_Click"/>
            </div>
        </div>
    </div>
</div>


<!-- EDIT PROFILE MODAL -->
<div class="modal fade" id="editProfileModal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title">Edit Profile</h5>
            </div>
            <div class="modal-body">
                <div class="form-group">
                    <label>Last Name</label>
                    <asp:TextBox ID="txtEditLastName" runat="server" CssClass="form-control" />
                </div>
                <div class="form-group mt-2">
                    <label>First Name</label>
                    <asp:TextBox ID="txtEditFirstName" runat="server" CssClass="form-control" />
                </div>
                <div class="form-group mt-2">
                    <label>Middle Name</label>
                    <asp:TextBox ID="txtEditMiddleName" runat="server" CssClass="form-control" />
                </div>
                <div class="form-group mt-2">
                    <label>Gender</label>
                    <asp:DropDownList ID="ddlEditGender" runat="server" CssClass="form-control">
                        <asp:ListItem Value="">Select...</asp:ListItem>
                        <asp:ListItem Value="Male">Male</asp:ListItem>
                        <asp:ListItem Value="Female">Female</asp:ListItem>
                        <asp:ListItem Value="Other">Other</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="form-group mt-2">
                    <label>Date Of Birth</label>
                    <asp:TextBox ID="txtEditDOB" runat="server" CssClass="form-control" placeholder="MM/dd/yyyy" />
                </div>
            </div>
            <div class="modal-footer">
                <asp:Button ID="btnSaveProfile" runat="server" CssClass="btn btn-success" Text="Save" OnClick="btnSaveProfile_Click" />
            </div>
        </div>
    </div>
</div>

</asp:Content>
