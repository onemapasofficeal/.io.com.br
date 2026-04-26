using System;
using System.Drawing;
using System.Windows.Forms;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    public class PlayerProfilePage : Form
    {
        private readonly RobloxUser _profileUser;
        private readonly RobloxUser _currentUser;
        private readonly P2PServerService _p2p;

        public PlayerProfilePage(RobloxUser profileUser, RobloxUser currentUser, P2PServerService p2p)
        {
            _profileUser = profileUser;
            _currentUser = currentUser;
            _p2p = p2p;
            InitUI();
        }

        private void InitUI()
        {
            Text = _profileUser.DisplayName + " - Rolox";
            Size = new Size(520, 480);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;

            var avatar = new PictureBox
            {
                Size = new Size(100, 100),
                Location = new Point(30, 30),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            if (!string.IsNullOrEmpty(_profileUser.AvatarUrl))
                avatar.LoadAsync(_profileUser.AvatarUrl);

            var lblDisplay = new Label
            {
                Text = string.IsNullOrWhiteSpace(_profileUser.DisplayName) ? _profileUser.Username : _profileUser.DisplayName,
                Font = new Font("Arial", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(145, 30),
                AutoSize = true
            };

            var lblUsername = new Label
            {
                Text = "@" + _profileUser.Username,
                Font = new Font("Arial", 11),
                ForeColor = Color.Gray,
                Location = new Point(145, 68),
                AutoSize = true
            };

            var lblId = new Label
            {
                Text = "ID: " + _profileUser.Id,
                Font = new Font("Arial", 9),
                ForeColor = Color.FromArgb(80, 80, 80),
                Location = new Point(145, 92),
                AutoSize = true
            };

            var lblDesc = new Label
            {
                Text = string.IsNullOrEmpty(_profileUser.Description) ? "Sem descrição." : _profileUser.Description,
                Font = new Font("Arial", 10),
                ForeColor = Color.LightGray,
                Location = new Point(30, 150),
                Size = new Size(450, 60),
                AutoEllipsis = true
            };

            // Linha separadora
            var sep = new Panel
            {
                Location = new Point(30, 225),
                Size = new Size(450, 1),
                BackColor = Color.FromArgb(50, 50, 50)
            };

            var lblServerTitle = new Label
            {
                Text = "Servidor Ativo",
                Font = new Font("Arial", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, 240),
                AutoSize = true
            };

            // Verifica se este jogador tem servidor ativo
            var server = GamePage.ActiveServers.Find(s => s.HostUsername == _profileUser.Username);

            if (server != null)
            {
                var serverCard = new Panel
                {
                    Location = new Point(30, 275),
                    Size = new Size(450, 110),
                    BackColor = Color.FromArgb(35, 35, 35)
                };

                var lblGame = new Label
                {
                    Text = server.GameName,
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(10, 10),
                    Size = new Size(430, 25),
                    AutoEllipsis = true
                };

                var lblPlayers = new Label
                {
                    Text = server.PlayerCount + " / " + server.MaxPlayers + " jogadores",
                    Font = new Font("Arial", 9),
                    ForeColor = Color.Gray,
                    Location = new Point(10, 38),
                    AutoSize = true
                };

                var lblIp = new Label
                {
                    Text = "Host: " + server.HostIp + ":" + server.Port,
                    Font = new Font("Arial", 9),
                    ForeColor = Color.FromArgb(80, 80, 80),
                    Location = new Point(10, 58),
                    AutoSize = true
                };

                var btnJoin = new Button
                {
                    Text = "▶  Jogar / Join",
                    Location = new Point(10, 72),
                    Size = new Size(430, 30),
                    Font = new Font("Arial", 11, FontStyle.Bold),
                    BackColor = Color.FromArgb(0, 162, 255),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnJoin.FlatAppearance.BorderSize = 0;
                btnJoin.Click += async (s, e) =>
                {
                    btnJoin.Enabled = false;
                    btnJoin.Text = "Conectando...";
                    bool ok = await _p2p.JoinServerAsync(server.HostIp, server.Port, _currentUser.Username);
                    if (ok)
                    {
                        btnJoin.Text = "✅ Conectado!";
                        btnJoin.BackColor = Color.LimeGreen;
                        server.PlayerCount++;
                    }
                    else
                    {
                        btnJoin.Text = "❌ Falhou - tente novamente";
                        btnJoin.BackColor = Color.OrangeRed;
                        btnJoin.Enabled = true;
                    }
                };

                serverCard.Controls.AddRange(new Control[] { lblGame, lblPlayers, lblIp, btnJoin });
                Controls.Add(serverCard);
            }
            else
            {
                var lblNoServer = new Label
                {
                    Text = _profileUser.DisplayName + " não está hospedando nenhum servidor no momento.",
                    Font = new Font("Arial", 10),
                    ForeColor = Color.Gray,
                    Location = new Point(30, 275),
                    Size = new Size(450, 40),
                    AutoEllipsis = true
                };
                Controls.Add(lblNoServer);
            }

            Controls.AddRange(new Control[] { avatar, lblDisplay, lblUsername, lblId, lblDesc, sep, lblServerTitle });
        }
    }
}
