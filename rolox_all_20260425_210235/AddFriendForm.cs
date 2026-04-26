using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    public class AddFriendForm : Form
    {
        private readonly RobloxApiService _api;
        private readonly RobloxUser       _currentUser;
        private TextBox    txtSearch  = null!;
        private Panel      pnlResult  = null!;
        private Label      lblStatus  = null!;

        public AddFriendForm(RobloxUser currentUser, RobloxApiService api)
        {
            _currentUser = currentUser;
            _api         = api;
            InitUI();
        }

        private void InitUI()
        {
            Text            = "Adicionar Amigo — Rolox";
            Size            = new Size(480, 420);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(23, 23, 23);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;

            // ── Header ──────────────────────────────────────────
            var lblTitle = new Label
            {
                Text      = "Adicionar Amigo",
                Font      = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location  = new Point(20, 20),
                AutoSize  = true
            };

            var lblSub = new Label
            {
                Text      = "Busque pelo username do Roblox",
                Font      = new Font("Arial", 9),
                ForeColor = Color.Gray,
                Location  = new Point(20, 50),
                AutoSize  = true
            };

            // ── Search box ──────────────────────────────────────
            var pnlSearch = new Panel
            {
                Location  = new Point(20, 78),
                Size      = new Size(420, 36),
                BackColor = Color.FromArgb(38, 38, 38)
            };

            var lblIcon = new Label
            {
                Text      = "🔍",
                Font      = new Font("Arial", 10),
                ForeColor = Color.Gray,
                Location  = new Point(8, 8),
                AutoSize  = true
            };

            txtSearch = new TextBox
            {
                PlaceholderText = "Username do Roblox...",
                Font            = new Font("Arial", 11),
                BackColor       = Color.FromArgb(38, 38, 38),
                ForeColor       = Color.White,
                BorderStyle     = BorderStyle.None,
                Location        = new Point(30, 8),
                Size            = new Size(340, 22)
            };
            txtSearch.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await SearchAsync();
                }
            };

            var btnSearch = new Button
            {
                Text      = "Buscar",
                Location  = new Point(374, 4),
                Size      = new Size(44, 28),
                Font      = new Font("Arial", 8, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 162, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += async (s, e) => await SearchAsync();

            pnlSearch.Controls.AddRange(new Control[] { lblIcon, txtSearch, btnSearch });

            // ── Status ──────────────────────────────────────────
            lblStatus = new Label
            {
                Text      = "",
                Font      = new Font("Arial", 9),
                ForeColor = Color.Gray,
                Location  = new Point(20, 122),
                Size      = new Size(420, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Result panel ────────────────────────────────────
            pnlResult = new Panel
            {
                Location  = new Point(20, 148),
                Size      = new Size(420, 220),
                BackColor = Color.FromArgb(23, 23, 23)
            };

            Controls.AddRange(new Control[] { lblTitle, lblSub, pnlSearch, lblStatus, pnlResult });
        }

        private async Task SearchAsync()
        {
            string q = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(q)) return;

            lblStatus.Text      = "Buscando...";
            lblStatus.ForeColor = Color.LightBlue;
            pnlResult.Controls.Clear();

            var user = await _api.GetUserByUsernameAsync(q);

            if (user == null)
            {
                lblStatus.Text      = $"Nenhum usuário encontrado para \"{q}\".";
                lblStatus.ForeColor = Color.OrangeRed;
                return;
            }

            lblStatus.Text = "";
            ShowResult(user);
        }

        private void ShowResult(RobloxUser user)
        {
            pnlResult.Controls.Clear();

            // Avatar
            var avatar = new PictureBox
            {
                Size     = new Size(80, 80),
                Location = new Point(0, 10),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            if (!string.IsNullOrEmpty(user.AvatarUrl)) avatar.LoadAsync(user.AvatarUrl);

            var lblDisplay = new Label
            {
                Text      = user.DisplayName,
                Font      = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location  = new Point(96, 10),
                AutoSize  = true
            };

            var lblUsername = new Label
            {
                Text      = "@" + user.Username,
                Font      = new Font("Arial", 10),
                ForeColor = Color.Gray,
                Location  = new Point(96, 38),
                AutoSize  = true
            };

            var lblId = new Label
            {
                Text      = "ID: " + user.Id,
                Font      = new Font("Arial", 8),
                ForeColor = Color.FromArgb(70, 70, 70),
                Location  = new Point(96, 60),
                AutoSize  = true
            };

            // Botão Adicionar Amigo
            var btnAdd = new Button
            {
                Text      = "➕  Enviar Pedido de Amizade",
                Location  = new Point(0, 105),
                Size      = new Size(420, 42),
                Font      = new Font("Arial", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 162, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) =>
            {
                // Abre perfil do Roblox para enviar pedido (API de amizade requer auth)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = $"https://www.roblox.com/users/{user.Id}/profile",
                    UseShellExecute = true
                });
                btnAdd.Text      = "✅ Perfil aberto — envie o pedido no Roblox";
                btnAdd.BackColor = Color.FromArgb(20, 80, 20);
            };

            // Botão Ver Perfil no Rolox
            var btnProfile = new Button
            {
                Text      = "👤  Ver Perfil no Rolox",
                Location  = new Point(0, 155),
                Size      = new Size(420, 38),
                Font      = new Font("Arial", 10),
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnProfile.FlatAppearance.BorderSize = 1;
            btnProfile.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btnProfile.Click += (s, e) =>
            {
                new PlayerProfilePage(user, _currentUser, new P2PServerService()).Show();
            };

            pnlResult.Controls.AddRange(new Control[]
            {
                avatar, lblDisplay, lblUsername, lblId, btnAdd, btnProfile
            });
        }
    }
}
