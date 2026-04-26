using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace RoloxLife
{
    public class MainForm : Form
    {
        private readonly string _username;
        private UserInfo? _user;
        private readonly ModerationService _mod = new();

        // Panels
        private Panel pnlHome = null!;
        private Panel pnlChat = null!;
        private Panel pnlProfile = null!;

        // Chat
        private RichTextBox rtbChat = null!;
        private TextBox txtMsg = null!;

        // Home
        private FlowLayoutPanel flpGames = null!;
        private PictureBox picAvatar = null!;
        private Label lblWelcome = null!;

        // Profile
        private PictureBox picProf = null!;
        private Label lblProfName = null!;
        private Label lblProfUser = null!;

        // Nav buttons
        private readonly List<Button> _navBtns = new();

        public MainForm(string username)
        {
            _username = username;
            Text = "Rolox Life";
            Size = new Size(500, 680);
            MinimumSize = new Size(500, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            BuildTopBar();
            BuildHome();
            BuildChat();
            BuildProfile();
            BuildNavBar();

            ShowSection("Home");
            _ = LoadAsync();
        }

        // ── TOP BAR ────────────────────────────────────────────
        private void BuildTopBar()
        {
            var top = new Panel
            {
                Dock = DockStyle.Top, Height = 52,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            picAvatar = new PictureBox
            {
                Size = new Size(36, 36), Location = new Point(10, 8),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.FromArgb(50, 50, 50)
            };

            lblWelcome = new Label
            {
                Text = $"Olá, {_username}",
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(54, 16), AutoSize = true
            };

            var btnSair = new Button
            {
                Text = "Sair", Size = new Size(55, 28),
                Location = new Point(425, 12),
                Font = new Font("Arial", 8),
                BackColor = Color.FromArgb(50, 30, 30),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSair.FlatAppearance.BorderSize = 0;
            btnSair.Click += (s, e) =>
            {
                SessionStore.Clear();
                Hide();
                new LoginForm().Show();
            };

            top.Controls.AddRange(new Control[] { picAvatar, lblWelcome, btnSair });
            Controls.Add(top);
        }

        // ── NAV BAR ────────────────────────────────────────────
        private void BuildNavBar()
        {
            var nav = new Panel
            {
                Dock = DockStyle.Bottom, Height = 52,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            var items = new[] { ("⌂", "Home"), ("💬", "Chat"), ("👤", "Perfil") };
            int w = 500 / items.Length;

            for (int i = 0; i < items.Length; i++)
            {
                var (icon, section) = items[i];
                var btn = new Button
                {
                    Text = $"{icon}\n{section}",
                    Size = new Size(w, 52),
                    Location = new Point(i * w, 0),
                    Font = new Font("Arial", 8),
                    ForeColor = Color.Gray,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = section,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 30, 30);
                string sec = section;
                btn.Click += (s, e) => ShowSection(sec);
                _navBtns.Add(btn);
                nav.Controls.Add(btn);
            }

            Controls.Add(nav);
        }

        private void ShowSection(string name)
        {
            pnlHome.Visible    = name == "Home";
            pnlChat.Visible    = name == "Chat";
            pnlProfile.Visible = name == "Perfil";

            foreach (var b in _navBtns)
            {
                bool active = b.Tag?.ToString() == name;
                b.ForeColor = active ? Color.FromArgb(0, 162, 255) : Color.Gray;
            }
        }

        // ── HOME ───────────────────────────────────────────────
        private void BuildHome()
        {
            pnlHome = new Panel
            {
                Dock = DockStyle.Fill, Visible = false,
                BackColor = Color.FromArgb(23, 23, 23)
            };

            var lblTitle = new Label
            {
                Text = "Jogos Populares",
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top, Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            flpGames = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(23, 23, 23),
                Padding = new Padding(8)
            };

            pnlHome.Controls.Add(flpGames);
            pnlHome.Controls.Add(lblTitle);
            Controls.Add(pnlHome);
        }

        private void PopulateGames(List<GameInfo> games)
        {
            flpGames.Controls.Clear();
            foreach (var g in games)
            {
                long placeId = g.PlaceId;
                var card = new Panel
                {
                    Size = new Size(210, 160),
                    BackColor = Color.FromArgb(32, 32, 32),
                    Margin = new Padding(5),
                    Cursor = Cursors.Hand
                };

                var thumb = new PictureBox
                {
                    Size = new Size(210, 105), Location = new Point(0, 0),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    BackColor = Color.FromArgb(45, 45, 45)
                };
                if (!string.IsNullOrEmpty(g.Thumb)) thumb.LoadAsync(g.Thumb);

                var lblName = new Label
                {
                    Text = g.Name,
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(5, 108), Size = new Size(200, 28),
                    AutoEllipsis = true
                };

                var lblP = new Label
                {
                    Text = $"👥 {g.Players:N0}",
                    Font = new Font("Arial", 7), ForeColor = Color.Gray,
                    Location = new Point(5, 136), AutoSize = true
                };

                EventHandler play = (s, e) =>
                {
                    try
                    {
                        string name = Uri.EscapeDataString(g.Name);
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = $"https://onemapasofficeal.github.io/html-roblox-comunides-games/?name_app={name}&id_map={placeId}",
                            UseShellExecute = true
                        });
                    }
                    catch { }
                };

                card.Click += play; thumb.Click += play; lblName.Click += play;
                card.Controls.AddRange(new Control[] { thumb, lblName, lblP });
                flpGames.Controls.Add(card);
            }
        }

        // ── CHAT ───────────────────────────────────────────────
        private void BuildChat()
        {
            pnlChat = new Panel
            {
                Dock = DockStyle.Fill, Visible = false,
                BackColor = Color.FromArgb(23, 23, 23)
            };

            var lblTitle = new Label
            {
                Text = "Chat Global",
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top, Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            rtbChat = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18),
                ForeColor = Color.White,
                Font = new Font("Arial", 9),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            var inputRow = new Panel
            {
                Dock = DockStyle.Bottom, Height = 44,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            txtMsg = new TextBox
            {
                Location = new Point(8, 8), Size = new Size(370, 28),
                Font = new Font("Arial", 10),
                BackColor = Color.FromArgb(38, 38, 38),
                ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle
            };
            txtMsg.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SendChat(); } };

            var btnSend = new Button
            {
                Text = "▶", Location = new Point(384, 8), Size = new Size(100, 28),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += (s, e) => SendChat();

            inputRow.Controls.AddRange(new Control[] { txtMsg, btnSend });

            pnlChat.Controls.Add(rtbChat);
            pnlChat.Controls.Add(inputRow);
            pnlChat.Controls.Add(lblTitle);
            Controls.Add(pnlChat);

            AppendChat("Sistema", "Bem-vindo ao chat! Seja respeitoso.", Color.FromArgb(0, 162, 255));
        }

        private void SendChat()
        {
            string msg = txtMsg.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;
            txtMsg.Clear();

            if (!_mod.Check(_username, msg, out string reason))
            {
                AppendChat("Sistema", reason, Color.OrangeRed);
                return;
            }

            AppendChat(_username, msg, Color.White);
        }

        private void AppendChat(string user, string msg, Color color)
        {
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionLength = 0;
            rtbChat.SelectionColor = Color.Gray;
            rtbChat.AppendText($"[{DateTime.Now:HH:mm}] ");
            rtbChat.SelectionColor = color;
            rtbChat.AppendText($"{user}: ");
            rtbChat.SelectionColor = Color.LightGray;
            rtbChat.AppendText(msg + "\n");
            rtbChat.ScrollToCaret();
        }

        // ── PERFIL ─────────────────────────────────────────────
        private void BuildProfile()
        {
            pnlProfile = new Panel
            {
                Dock = DockStyle.Fill, Visible = false,
                BackColor = Color.FromArgb(23, 23, 23)
            };

            picProf = new PictureBox
            {
                Size = new Size(90, 90),
                Location = new Point(205, 60),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.FromArgb(50, 50, 50)
            };

            lblProfName = new Label
            {
                Text = "...",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false, Size = new Size(460, 30),
                Location = new Point(20, 165),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblProfUser = new Label
            {
                Text = "",
                Font = new Font("Arial", 9), ForeColor = Color.Gray,
                AutoSize = false, Size = new Size(460, 22),
                Location = new Point(20, 198),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var btnRoblox = new Button
            {
                Text = "Ver no Roblox",
                Location = new Point(150, 240), Size = new Size(200, 36),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnRoblox.FlatAppearance.BorderSize = 0;
            btnRoblox.Click += (s, e) =>
            {
                if (_user == null) return;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"https://www.roblox.com/users/{_user.Id}/profile",
                    UseShellExecute = true
                });
            };

            var lblCredits = new Label
            {
                Text = "Criador Principal: Roblox Official\nPublicador: One Mapas Official",
                Font = new Font("Arial", 7), ForeColor = Color.FromArgb(55, 55, 55),
                AutoSize = false, Size = new Size(460, 32),
                Location = new Point(20, 310),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlProfile.Controls.AddRange(new Control[] { picProf, lblProfName, lblProfUser, btnRoblox, lblCredits });
            Controls.Add(pnlProfile);
        }

        // ── LOAD ───────────────────────────────────────────────
        private async System.Threading.Tasks.Task LoadAsync()
        {
            _user = await ApiService.GetUserAsync(_username);

            if (_user != null)
            {
                void UpdateUI()
                {
                    lblWelcome.Text = $"Olá, {_user.DisplayName}";
                    if (!string.IsNullOrEmpty(_user.AvatarUrl))
                    {
                        picAvatar.LoadAsync(_user.AvatarUrl);
                        picProf.LoadAsync(_user.AvatarUrl);
                    }
                    lblProfName.Text = _user.DisplayName;
                    lblProfUser.Text = $"@{_user.Username}  •  ID: {_user.Id}";
                }

                if (InvokeRequired) Invoke(UpdateUI);
                else UpdateUI();
            }

            var games = await ApiService.GetGamesAsync();
            if (flpGames.InvokeRequired) flpGames.Invoke(() => PopulateGames(games));
            else PopulateGames(games);
        }
    }
}
