using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    public class GamePage : Form
    {
        private readonly RobloxGame _game;
        private readonly RobloxUser _currentUser;
        private readonly P2PServerService _p2p;
        private FlowLayoutPanel pnlServers = null!;

        // Lista estática compartilhada de servidores ativos no Rolox
        public static List<RoloxServer> ActiveServers { get; } = new();

        public GamePage(RobloxGame game, RobloxUser currentUser, P2PServerService p2p)
        {
            _game = game;
            _currentUser = currentUser;
            _p2p = p2p;
            InitUI();
            LoadServers();
        }

        private void InitUI()
        {
            Text = _game.Name;
            Size = new Size(860, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);

            // Header com thumbnail
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 180,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            var thumb = new PictureBox
            {
                Size = new Size(280, 160),
                Location = new Point(20, 10),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            if (!string.IsNullOrEmpty(_game.ThumbnailUrl))
                thumb.LoadAsync(_game.ThumbnailUrl);

            var lblName = new Label
            {
                Text = _game.Name,
                Font = new Font("Arial", 18, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(315, 15),
                Size = new Size(500, 40),
                AutoEllipsis = true
            };

            var lblCreator = new Label
            {
                Text = "Criador: " + _game.Creator + "  |  Publicador: " + _game.Publisher,
                Font = new Font("Arial", 9),
                ForeColor = Color.Gray,
                Location = new Point(315, 58),
                AutoSize = true
            };

            var lblPlayers = new Label
            {
                Text = _game.PlayerCount + " jogando agora",
                Font = new Font("Arial", 10),
                ForeColor = Color.LimeGreen,
                Location = new Point(315, 80),
                AutoSize = true
            };

            var btnPlay = new Button
            {
                Text = "▶  Jogar",
                Location = new Point(315, 115),
                Size = new Size(160, 42),
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 162, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnPlay.FlatAppearance.BorderSize = 0;
            long placeId = _game.PlaceId;
            string gameName = Uri.EscapeDataString(_game.Name);
            btnPlay.Click += (s, e) =>
            {
                try
                {
                    long ts     = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    string user = Uri.EscapeDataString(_currentUser.Username);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName        = $"https://onemapasofficeal.github.io/html-roblox-comunides-games/?name_app={gameName}&id_map={placeId}&name_username={user}&data_and_horario={ts}",
                        UseShellExecute = true
                    });
                }
                catch { }
            };

            var btnHost = new Button
            {
                Text = "🖥  Hospedar Servidor",
                Location = new Point(490, 115),
                Size = new Size(200, 42),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 80, 30),
                ForeColor = Color.LimeGreen,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnHost.FlatAppearance.BorderSize = 0;
            btnHost.Click += async (s, e) =>
            {
                // Registra servidor na lista global
                var existing = ActiveServers.Find(sv => sv.HostUsername == _currentUser.Username && sv.GameName == _game.Name);
                if (existing != null) ActiveServers.Remove(existing);

                await _p2p.StartHostAsync(_currentUser.Username);
                var server = new RoloxServer
                {
                    HostUsername = _currentUser.Username,
                    HostDisplayName = _currentUser.DisplayName,
                    HostAvatarUrl = _currentUser.AvatarUrl,
                    GameName = _game.Name,
                    PlaceId = _game.PlaceId,
                    Port = _p2p.Port,
                    HostIp = GetLocalIp(),
                    MaxPlayers = 20
                };
                ActiveServers.Add(server);
                btnHost.Text = "✅ Servidor Online";
                btnHost.BackColor = Color.FromArgb(0, 100, 0);
                LoadServers();
            };

            header.Controls.AddRange(new Control[] { thumb, lblName, lblCreator, lblPlayers, btnPlay, btnHost });

            // Seção de servidores
            var lblServers = new Label
            {
                Text = "Servidores Privados Rolox  (Grátis)",
                Font = new Font("Arial", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            };

            var btnRefresh = new Button
            {
                Text = "↻ Atualizar",
                Size = new Size(100, 28),
                Location = new Point(720, 190),
                Font = new Font("Arial", 9),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadServers();

            pnlServers = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(23, 23, 23),
                Padding = new Padding(10),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            Controls.Add(pnlServers);
            Controls.Add(lblServers);
            Controls.Add(btnRefresh);
            Controls.Add(header);
        }

        private void LoadServers()
        {
            pnlServers.Controls.Clear();
            var servers = ActiveServers.FindAll(s => s.PlaceId == _game.PlaceId);

            if (servers.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "Nenhum servidor Rolox ativo para este jogo.\nSeja o primeiro a hospedar!",
                    Font = new Font("Arial", 11),
                    ForeColor = Color.Gray,
                    AutoSize = true,
                    Margin = new Padding(20)
                };
                pnlServers.Controls.Add(lblEmpty);
                return;
            }

            foreach (var sv in servers)
            {
                var card = new Panel
                {
                    Size = new Size(240, 160),
                    BackColor = Color.FromArgb(35, 35, 35),
                    Margin = new Padding(8),
                    Cursor = Cursors.Default
                };

                var avatar = new PictureBox
                {
                    Size = new Size(60, 60),
                    Location = new Point(10, 10),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    BackColor = Color.FromArgb(50, 50, 50)
                };
                if (!string.IsNullOrEmpty(sv.HostAvatarUrl))
                    avatar.LoadAsync(sv.HostAvatarUrl);

                var lblHost = new Label
                {
                    Text = sv.HostDisplayName,
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(80, 10),
                    Size = new Size(150, 22),
                    AutoEllipsis = true
                };

                var lblAt = new Label
                {
                    Text = "@" + sv.HostUsername,
                    Font = new Font("Arial", 8),
                    ForeColor = Color.Gray,
                    Location = new Point(80, 32),
                    AutoSize = true
                };

                var lblCount = new Label
                {
                    Text = sv.PlayerCount + " de " + sv.MaxPlayers + " jogadores",
                    Font = new Font("Arial", 9),
                    ForeColor = Color.LightGray,
                    Location = new Point(10, 80),
                    AutoSize = true
                };

                // Barra de progresso de jogadores
                var bar = new ProgressBar
                {
                    Location = new Point(10, 100),
                    Size = new Size(220, 8),
                    Minimum = 0,
                    Maximum = sv.MaxPlayers,
                    Value = Math.Min(sv.PlayerCount, sv.MaxPlayers),
                    Style = ProgressBarStyle.Continuous
                };

                var btnJoin = new Button
                {
                    Text = "Entrar",
                    Location = new Point(10, 118),
                    Size = new Size(220, 32),
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    BackColor = Color.FromArgb(0, 162, 255),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = sv
                };
                btnJoin.FlatAppearance.BorderSize = 0;
                btnJoin.Click += async (s, e) =>
                {
                    btnJoin.Enabled = false;
                    btnJoin.Text = "Conectando...";
                    bool ok = await _p2p.JoinServerAsync(sv.HostIp, sv.Port, _currentUser.Username);
                    if (ok)
                    {
                        btnJoin.Text = "Conectado!";
                        btnJoin.BackColor = Color.LimeGreen;
                        sv.PlayerCount++;
                    }
                    else
                    {
                        btnJoin.Text = "Falhou";
                        btnJoin.BackColor = Color.OrangeRed;
                        btnJoin.Enabled = true;
                    }
                };

                card.Controls.AddRange(new Control[] { avatar, lblHost, lblAt, lblCount, bar, btnJoin });
                pnlServers.Controls.Add(card);
            }
        }

        private string GetLocalIp()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return ip.ToString();
            }
            catch { }
            return "127.0.0.1";
        }
    }

    public class RoloxServer
    {
        public string HostUsername { get; set; } = "";
        public string HostDisplayName { get; set; } = "";
        public string HostAvatarUrl { get; set; } = "";
        public string GameName { get; set; } = "";
        public long PlaceId { get; set; }
        public string HostIp { get; set; } = "";
        public int Port { get; set; }
        public int PlayerCount { get; set; } = 0;
        public int MaxPlayers { get; set; } = 20;
    }
}
