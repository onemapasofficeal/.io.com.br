using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    /// <summary>
    /// Versão simplificada do Rolox para crianças menores de 9 anos.
    /// Interface mais colorida, menos opções, chat com moderação extra.
    /// </summary>
    public class MainFormKid : Form
    {
        private readonly RobloxUser _user;
        private readonly RobloxApiService _api;
        private readonly ChatModerationService _mod = new();
        private readonly P2PServerService _p2p = new();

        private FlowLayoutPanel flpGames = null!;
        private RichTextBox rtbChat = null!;
        private TextBox txtMsg = null!;
        private Panel pnlHome = null!;
        private Panel pnlChat = null!;

        public MainFormKid(RobloxUser user, RobloxApiService api)
        {
            _user = user; _api = api;
            Text          = "Rolox Kid";
            Size          = new Size(900, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = Color.FromArgb(15, 15, 40); // azul escuro infantil
            MinimumSize   = new Size(700, 500);
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            FormClosed += (s, e) => SessionService.Save(_user.Username);
            RoloxAppContext.Instance?.SetMainForm(this);
            Build();
            _ = LoadGamesAsync();
        }

        private void Build()
        {
            // Top bar colorida
            var top = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(255, 140, 0) };
            var lblLogo = new Label { Text = "🎮 ROLOX KID", Font = new Font("Arial", 16, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 12), AutoSize = true };
            var lblUser = new Label { Text = $"Olá, {_user.DisplayName}! 👋", Font = new Font("Arial", 10), ForeColor = Color.White, Location = new Point(300, 18), AutoSize = true };
            var btnSair = new Button { Text = "Sair", Location = new Point(800, 12), Size = new Size(70, 32), Font = new Font("Arial", 9, FontStyle.Bold), BackColor = Color.FromArgb(200, 80, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSair.FlatAppearance.BorderSize = 0;
            btnSair.Click += (s, e) => { SessionService.Clear(); Hide(); RoloxAppContext.Instance.ShowLogin(); };
            top.Controls.AddRange(new Control[] { lblLogo, lblUser, btnSair });

            // Nav bottom
            var nav = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(20, 20, 60) };
            var btnHome = MakeNavBtn("🏠 Jogos", 0);
            var btnChat = MakeNavBtn("💬 Chat", 1);
            btnHome.Click += (s, e) => ShowSection("home");
            btnChat.Click += (s, e) => ShowSection("chat");
            nav.Controls.AddRange(new Control[] { btnHome, btnChat });

            // Home — jogos grandes e coloridos
            pnlHome = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 15, 40) };
            var lblTitle = new Label { Text = "Escolha um jogo! 🎮", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.FromArgb(255, 200, 0), Dock = DockStyle.Top, Height = 36, TextAlign = ContentAlignment.MiddleCenter };
            flpGames = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(15, 15, 40), Padding = new Padding(12) };
            pnlHome.Controls.Add(flpGames);
            pnlHome.Controls.Add(lblTitle);

            // Chat com moderação extra
            pnlChat = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 15, 40), Visible = false };
            var lblChatTitle = new Label { Text = "Chat 💬", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.FromArgb(255, 200, 0), Dock = DockStyle.Top, Height = 36, TextAlign = ContentAlignment.MiddleCenter };
            var lblChatInfo = new Label { Text = "⚠️ Seja gentil! Palavras ruins são bloqueadas automaticamente.", Font = new Font("Arial", 9), ForeColor = Color.Orange, Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleCenter };
            rtbChat = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 20, 50), ForeColor = Color.White, Font = new Font("Arial", 10), ReadOnly = true, BorderStyle = BorderStyle.None };
            var inputRow = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.FromArgb(20, 20, 60) };
            txtMsg = new TextBox { Location = new Point(8, 8), Size = new Size(720, 32), Font = new Font("Arial", 11), BackColor = Color.FromArgb(38, 38, 60), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            txtMsg.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SendChat(); } };
            var btnSend = new Button { Text = "▶", Location = new Point(736, 8), Size = new Size(80, 32), Font = new Font("Arial", 12, FontStyle.Bold), BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += (s, e) => SendChat();
            inputRow.Controls.AddRange(new Control[] { txtMsg, btnSend });
            pnlChat.Controls.Add(rtbChat);
            pnlChat.Controls.Add(inputRow);
            pnlChat.Controls.Add(lblChatInfo);
            pnlChat.Controls.Add(lblChatTitle);

            AppendChat("Sistema", "Bem-vindo ao Rolox Kid! 🎉 Divirta-se!", Color.FromArgb(255, 200, 0));

            Controls.Add(pnlChat);
            Controls.Add(pnlHome);
            Controls.Add(nav);
            Controls.Add(top);
        }

        private Button MakeNavBtn(string text, int index)
        {
            var btn = new Button
            {
                Text = text, Location = new Point(index * 200, 8), Size = new Size(180, 44),
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void ShowSection(string name)
        {
            pnlHome.Visible = name == "home";
            pnlChat.Visible = name == "chat";
        }

        private async Task LoadGamesAsync()
        {
            var games = await _api.GetPopularGamesAsync();
            if (flpGames.InvokeRequired) flpGames.Invoke(() => PopulateGames(games));
            else PopulateGames(games);
        }

        private void PopulateGames(List<RobloxGame> games)
        {
            flpGames.Controls.Clear();
            foreach (var g in games)
            {
                var game = g;
                var card = new Panel { Size = new Size(220, 180), BackColor = Color.FromArgb(30, 30, 70), Margin = new Padding(8), Cursor = Cursors.Hand };
                var thumb = new PictureBox { Size = new Size(220, 120), Location = new Point(0, 0), SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.FromArgb(45, 45, 80) };
                if (!string.IsNullOrEmpty(g.ThumbnailUrl)) thumb.LoadAsync(g.ThumbnailUrl);
                var lblName = new Label { Text = g.Name, Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = Color.White, Location = new Point(5, 124), Size = new Size(210, 28), AutoEllipsis = true };
                var btnPlay = new Button { Text = "▶ Jogar!", Location = new Point(5, 152), Size = new Size(210, 24), Font = new Font("Arial", 8, FontStyle.Bold), BackColor = Color.FromArgb(255, 140, 0), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
                btnPlay.FlatAppearance.BorderSize = 0;
                long pid = g.PlaceId; string gname = Uri.EscapeDataString(g.Name);
                btnPlay.Click += (s, e) =>
                {
                    long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = $"https://onemapasofficeal.github.io/html-roblox-comunides-games/?name_app={gname}&id_map={pid}&name_username={Uri.EscapeDataString(_user.Username)}&data_and_horario={ts}",
                        UseShellExecute = true
                    });
                };
                card.Controls.AddRange(new Control[] { thumb, lblName, btnPlay });
                flpGames.Controls.Add(card);
            }
        }

        private void SendChat()
        {
            string msg = txtMsg.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;
            txtMsg.Clear();
            if (!_mod.CheckMessage(_user.Username, msg, out string reason))
            {
                AppendChat("⚠️ Sistema", reason, Color.OrangeRed);
                return;
            }
            _p2p.SendMessage(msg);
            AppendChat(_user.Username, msg, Color.FromArgb(255, 200, 0));
        }

        private void AppendChat(string user, string msg, Color color)
        {
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = color;
            rtbChat.AppendText($"[{DateTime.Now:HH:mm}] {user}: {msg}\n");
            rtbChat.ScrollToCaret();
        }
    }
}
