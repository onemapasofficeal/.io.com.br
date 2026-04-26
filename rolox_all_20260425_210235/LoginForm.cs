using System;
using System.Drawing;
using System.Windows.Forms;

namespace RoloxLife
{
    public class LoginForm : Form
    {
        private TextBox txtUser = null!;
        private Label lblStatus = null!;
        private Button btnLogin = null!;

        public LoginForm()
        {
            Text = "Rolox Life";
            Size = new Size(400, 340);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Build();
        }

        private void Build()
        {
            var lblLogo = new Label
            {
                Text = "ROLOX LIFE",
                Font = new Font("Arial", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 162, 255),
                AutoSize = false, Size = new Size(360, 50),
                Location = new Point(20, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblSub = new Label
            {
                Text = "Criador: Roblox Official  |  Publicador: One Mapas Official",
                Font = new Font("Arial", 7), ForeColor = Color.Gray,
                AutoSize = false, Size = new Size(360, 18),
                Location = new Point(20, 82), TextAlign = ContentAlignment.MiddleCenter
            };

            var lblUser = new Label
            {
                Text = "Nome de usuário Roblox:",
                Font = new Font("Arial", 9), ForeColor = Color.White,
                Location = new Point(30, 115), AutoSize = true
            };

            txtUser = new TextBox
            {
                Location = new Point(30, 135), Size = new Size(340, 28),
                Font = new Font("Arial", 11),
                BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtUser.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnLogin.PerformClick(); };

            lblStatus = new Label
            {
                Text = "", Font = new Font("Arial", 8), ForeColor = Color.OrangeRed,
                AutoSize = false, Size = new Size(340, 20),
                Location = new Point(30, 168), TextAlign = ContentAlignment.MiddleCenter
            };

            btnLogin = new Button
            {
                Text = "ENTRAR", Location = new Point(30, 192),
                Size = new Size(340, 40),
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += async (s, e) =>
            {
                string u = txtUser.Text.Trim();
                if (string.IsNullOrEmpty(u)) return;
                btnLogin.Enabled = false;
                lblStatus.ForeColor = Color.LightBlue;
                lblStatus.Text = "Buscando...";

                var user = await ApiService.GetUserAsync(u);
                if (user == null)
                {
                    lblStatus.ForeColor = Color.OrangeRed;
                    lblStatus.Text = "Usuário não encontrado.";
                    btnLogin.Enabled = true;
                    return;
                }

                SessionStore.Save(user.Username);
                Hide();
                new MainForm(user.Username).Show();
            };

            Controls.AddRange(new Control[] { lblLogo, lblSub, lblUser, txtUser, lblStatus, btnLogin });
        }
    }
}
