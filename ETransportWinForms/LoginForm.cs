using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ETransportWinForms
{
    public class LoginForm : Form
    {
        TextBox txtApi, txtEmail, txtPassword;
        Button btnLogin;
        Label lblStatus;
        public static ApiClient Api;
        public static Dictionary<string, object> CurrentUser;

        public LoginForm()
        {
            Text = "E-Transport Booking & Management System";
            Width = 980; Height = 620; StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(245, 247, 251);

            var left = new Panel { Dock = DockStyle.Left, Width = 410, BackColor = Color.FromArgb(22, 36, 71) };
            Controls.Add(left);
            var brand = new Label { Text = "E-TRANSPORT\nBOOKING &\nMANAGEMENT", ForeColor = Color.White, Font = new Font("Segoe UI", 28, FontStyle.Bold), AutoSize = false, Left = 38, Top = 120, Width = 330, Height = 210 };
            left.Controls.Add(brand);
            left.Controls.Add(new Label { Text = "Windows Forms client connected to your PHP API", ForeColor = Color.FromArgb(198, 208, 235), Left = 42, Top = 340, Width = 310, Height = 60 });

            var card = new Panel { Left = 500, Top = 70, Width = 380, Height = 440, BackColor = Color.White };
            Controls.Add(card);
            card.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle, Color.FromArgb(225,225,235), ButtonBorderStyle.Solid);
            card.Controls.Add(new Label { Text = "Sign in", Font = new Font("Segoe UI", 22, FontStyle.Bold), Left = 32, Top = 25, Width = 250 });
            card.Controls.Add(MakeLabel("API URL", 32, 95));
            txtApi = MakeText("http://localhost/e_transport_system/api/index.php?r=", 32, 122, false); card.Controls.Add(txtApi);
            card.Controls.Add(MakeLabel("Email", 32, 170));
            txtEmail = MakeText("admin@etransport.local", 32, 197, false); card.Controls.Add(txtEmail);
            card.Controls.Add(MakeLabel("Password", 32, 245));
            txtPassword = MakeText("password", 32, 272, true); card.Controls.Add(txtPassword);
            btnLogin = new Button { Text = "LOGIN", Left = 32, Top = 335, Width = 315, Height = 45, BackColor = Color.FromArgb(34, 92, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLogin.Click += async (s, e) => await Login();
            card.Controls.Add(btnLogin);
            lblStatus = new Label { Left = 32, Top = 390, Width = 315, Height = 35, ForeColor = Color.Firebrick };
            card.Controls.Add(lblStatus);
        }
        Label MakeLabel(string t, int x, int y) => new Label { Text = t, Left = x, Top = y, Width = 300, Height = 22, ForeColor = Color.FromArgb(70,70,85) };
        TextBox MakeText(string t, int x, int y, bool pass) => new TextBox { Text = t, Left = x, Top = y, Width = 315, Height = 30, UseSystemPasswordChar = pass };

        async Task Login()
        {
            try
            {
                lblStatus.Text = "Connecting..."; btnLogin.Enabled = false;
                Api = new ApiClient(txtApi.Text.Trim());
                var result = await Api.SendAsync("login", "POST", new Dictionary<string, object> { { "email", txtEmail.Text.Trim() }, { "password", txtPassword.Text.Trim() } });
                if (result.ContainsKey("user"))
                {
                    CurrentUser = result["user"] as Dictionary<string, object>;
                    Hide();
                    new MainForm().ShowDialog();
                    Close();
                }
                else lblStatus.Text = result.ContainsKey("error") ? Convert.ToString(result["error"]) : "Invalid login.";
            }
            catch (Exception ex) { lblStatus.Text = "API error: " + ex.Message; }
            finally { btnLogin.Enabled = true; }
        }
    }
}
