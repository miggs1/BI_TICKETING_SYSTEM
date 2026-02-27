<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="BI_TICKETING_SYSTEM.Login" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Login - BI Ticketing System</title>

    <!-- Google Fonts -->
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700&display=swap" />
    <!-- Font Awesome -->
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css" />

    <style>
               * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
            font-family: 'Poppins', sans-serif;
        }

        html, body {
            width: 100%;
            min-height: 100vh;
            overflow-x: hidden;
        }

        body {
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 50px 15px 40px 15px;
        }

        /* ===== BACKGROUND IMAGE ===== */
        .bg-image {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: url('<%= ResolveUrl("~/Images/bg-buliding.png") %>') center center / cover no-repeat;
            z-index: -2;
        }

        .bg-overlay {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: linear-gradient(135deg, rgba(0, 31, 84, 0.88) 0%, rgba(10, 20, 60, 0.85) 50%, rgba(139, 0, 0, 0.7) 100%);
            z-index: -1;
        }

        /* ===== TOP GOVERNMENT BANNER ===== */
        .gov-banner {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            background: linear-gradient(90deg, #001f54, #003087);
            padding: 8px 20px;
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 100;
            border-bottom: 3px solid #d4a017;
        }

        .gov-banner span {
            color: #fff;
            font-size: 11px;
            letter-spacing: 1px;
            text-transform: uppercase;
            opacity: 0.9;
        }

        .gov-banner .flag-colors {
            display: flex;
            gap: 3px;
            margin-right: 12px;
        }

        .gov-banner .flag-colors div {
            width: 20px;
            height: 3px;
            border-radius: 2px;
        }

        .flag-blue { background: #0038a8; }
        .flag-red { background: #ce1126; }
        .flag-yellow { background: #fcd116; }

        /* ===== LOGIN WRAPPER ===== */
        .login-wrapper {
            display: flex;
            flex-direction: column;
            align-items: center;
            width: 100%;
            max-width: 420px;
            animation: fadeInUp 0.7s ease-out;
        }

        @keyframes fadeInUp {
            from { opacity: 0; transform: translateY(40px); }
            to { opacity: 1; transform: translateY(0); }
        }

        /* ===== LOGO SECTION ===== */
        .logo-section {
            text-align: center;
            margin-bottom: 20px;
        }

        .logo-section img {
            width: 80px;
            height: 80px;
            border-radius: 50%;
            border: 3px solid rgba(212, 160, 23, 0.6);
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.4);
            background: #000;
            object-fit: contain;
            padding: 5px;
        }

        .logo-section h1 {
            color: #fff;
            font-size: 20px;
            font-weight: 700;
            margin-top: 12px;
            text-shadow: 0 2px 10px rgba(0,0,0,0.5);
        }

        .logo-section .org-name {
            color: #d4a017;
            font-size: 10px;
            font-weight: 500;
            letter-spacing: 3px;
            text-transform: uppercase;
            margin-top: 4px;
        }

        /* ===== LOGIN CARD ===== */
        .login-card {
            width: 100%;
            background: rgba(255, 255, 255, 0.97);
            border-radius: 16px;
            padding: 30px 32px;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.4);
            position: relative;
            overflow: hidden;
        }

        .login-card::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 4px;
            background: linear-gradient(90deg, #001f54, #d4a017, #ce1126);
        }

        .login-card h2 {
            text-align: center;
            color: #001f54;
            font-size: 20px;
            font-weight: 600;
            margin-bottom: 4px;
        }

        .login-card .subtitle {
            text-align: center;
            color: #888;
            font-size: 12px;
            margin-bottom: 24px;
        }

        /* ===== FORM FIELDS ===== */
        .form-group {
            margin-bottom: 18px;
        }

        .form-group label {
            display: block;
            font-size: 11px;
            font-weight: 600;
            color: #001f54;
            text-transform: uppercase;
            letter-spacing: 0.8px;
            margin-bottom: 6px;
        }

        .input-field {
            position: relative;
        }

        .input-field i {
            position: absolute;
            left: 14px;
            top: 50%;
            transform: translateY(-50%);
            color: #aaa;
            font-size: 14px;
            transition: color 0.3s;
        }

        .input-field input {
            width: 100%;
            padding: 12px 15px 12px 44px;
            border: 2px solid #e0e0e0;
            border-radius: 10px;
            font-size: 14px;
            font-family: 'Poppins', sans-serif;
            outline: none;
            background: #f8f9fa;
            transition: all 0.3s ease;
        }

        .input-field input:focus {
            border-color: #001f54;
            background: #fff;
            box-shadow: 0 0 0 4px rgba(0, 31, 84, 0.08);
        }

        .input-field:focus-within i {
            color: #001f54;
        }

        /* ===== LOGIN BUTTON ===== */
        .btn-login {
            width: 100%;
            padding: 13px;
            background: linear-gradient(135deg, #001f54, #003087);
            border: none;
            border-radius: 10px;
            color: white;
            font-size: 14px;
            font-weight: 600;
            font-family: 'Poppins', sans-serif;
            cursor: pointer;
            transition: all 0.3s ease;
            text-transform: uppercase;
            letter-spacing: 1.5px;
            margin-top: 5px;
            position: relative;
            overflow: hidden;
        }

        .btn-login:hover {
            background: linear-gradient(135deg, #002a6e, #0041a8);
            box-shadow: 0 8px 25px rgba(0, 48, 135, 0.4);
            transform: translateY(-2px);
        }

        .btn-login:active {
            transform: translateY(0);
        }

        .btn-login::after {
            content: '';
            position: absolute;
            bottom: 0;
            left: 0;
            width: 100%;
            height: 3px;
            background: #d4a017;
        }

        /* ===== ERROR MESSAGE ===== */
        .error-box {
            background: #fff5f5;
            border: 1px solid #feb2b2;
            border-left: 4px solid #ce1126;
            border-radius: 8px;
            padding: 10px 14px;
            margin-bottom: 15px;
            color: #9b1c1c;
            font-size: 13px;
            display: flex;
            align-items: center;
        }

        .error-box i {
            margin-right: 8px;
            color: #ce1126;
        }

        /* ===== FOOTER ===== */
        .login-footer {
            text-align: center;
            margin-top: 20px;
            padding-top: 15px;
            border-top: 1px solid #eee;
            color: #999;
            font-size: 11px;
        }

        .login-footer .secure-text {
            color: #28a745;
            font-size: 10px;
            margin-bottom: 4px;
        }

        /* ===== BOTTOM CREDITS ===== */
        .page-footer {
            position: fixed;
            bottom: 0;
            left: 0;
            width: 100%;
            text-align: center;
            padding: 8px;
            color: rgba(255,255,255,0.5);
            font-size: 10px;
            z-index: 50;
        }

        /* ===== RESPONSIVE ===== */
        @media (max-width: 480px) {
            .login-card {
                padding: 25px 20px;
            }
            .logo-section img {
                width: 110px !important;
                height: 110px !important;
            }
            .logo-section h1 {
                font-size: 17px;
            }
        }
    </style>
</head>
<body>
    <!-- Background -->
    <div class="bg-image" style="background-image: url('Images/bg-building.png'); background-position: center center; background-size: cover; background-repeat: no-repeat;"></div>
    <div class="bg-overlay"></div>

    <!-- Government Banner -->
    <div class="gov-banner">
        <div class="flag-colors">
            <div class="flag-blue"></div>
            <div class="flag-red"></div>
            <div class="flag-yellow"></div>
        </div>
        <span>Republic of the Philippines &bull; Bureau of Immigration &bull; Department of Justice</span>
    </div>

    <form id="form1" runat="server">
        <div class="login-wrapper">

            <!-- Logo Section -->
            <div class="logo-section">
                <img src="Images/bi-seal.png" alt="Organization Logo" style="width: 120px !important; height: 120px !important;" />
                <h1>BI Ticketing System</h1>
                <div class="org-name">Bureau of Immigration</div>
            </div>

            <!-- Login Card -->
            <div class="login-card">
                <h2>Sign In</h2>
                <p class="subtitle">Enter your credentials to access the system</p>

                <!-- Error Message -->
                <asp:Panel ID="pnlError" runat="server" Visible="false" CssClass="error-box">
                    <i class="fas fa-exclamation-circle"></i>
                    <asp:Label ID="lblError" runat="server" />
                </asp:Panel>

                <!-- Username -->
                <div class="form-group">
                    <label>Username</label>
                    <div class="input-field">
                        <i class="fas fa-user"></i>
                        <asp:TextBox ID="txtUsername" runat="server" placeholder="Enter your username" />
                    </div>
                </div>

                <!-- Password -->
                <div class="form-group">
                    <label>Password</label>
                    <div class="input-field">
                        <i class="fas fa-lock"></i>
                        <asp:TextBox ID="txtPassword" runat="server" placeholder="Enter your password" TextMode="Password" />
                    </div>
                </div>

                <!-- Login Button -->
                <asp:Button ID="btnLogin" runat="server" Text="Sign In"
                    CssClass="btn-login" OnClick="btnLogin_Click" />

                <!-- Footer -->
                <div class="login-footer">
                    <div class="secure-text">
                        <i class="fas fa-shield-alt"></i> Secured Connection
                    </div>
                    &copy; 2026 BI Ticketing System. All rights reserved.
                </div>
            </div>

        </div>
    </form>

    <!-- Page Footer -->
    <div class="page-footer">
        Bureau of Immigration &bull; Department of Justice &bull; Republic of the Philippines
    </div>
</body>
</html>