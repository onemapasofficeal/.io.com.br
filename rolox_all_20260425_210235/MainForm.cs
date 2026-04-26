using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    public class MainForm : Form
    {
        private readonly RobloxUser _user;
        private readonly RobloxApiService _api;
        private readonly ChatModerationService _mod = new();
        private readonly P2PServerService _p2p = new();
        private RoloxApp.Services.RoloxMode _mode = RoloxApp.Services.RoloxMode.Select;

        private Panel pnlContent = null!;
        private Label lblPageTitle = null!;
        private Panel pnlHome = null!;
        private Panel pnlProfile = null!;
        private Panel pnlServer = null!;
        private Panel pnlChat = null!;
        private Panel pnlDestaques = null!;
        private Panel pnlTurma = null!;
        private Panel pnlMais = null!;
        private Panel pnlConfig = null!;
        private FlowLayoutPanel flpFriends = null!;
        private FlowLayoutPanel flpRecommended = null!;
        private FlowLayoutPanel flpContinue = null!;
        private RichTextBox rtbChat = null!;
        private TextBox txtChatInput = null!;
        private ListBox lstPlayers = null!;
        private Label lblServerStatus = null!;

        // Sidebar buttons para highlight ativo
        private readonly List<Button> _sidebarBtns = new();

        public MainForm(RobloxUser user, RobloxApiService api)
        {
            _user = user;
            _api  = api;

            // Define o modo baseado na idade salva
            int age = SessionService.LoadAge();
            _mode = AgeService.GetMode(user.Username, age >= 0 ? age : null);

            InitUI();
            _ = LoadHomeAsync();
            RoloxAppContext.Instance?.SetMainForm(this);
        }

        private void InitUI()
        {
            Text = AgeService.GetModeName(_mode);
            Size = new Size(1200, 750);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            FormClosed += (s, e) => SessionService.Save(_user.Username);

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rolox_janela.ico");
            if (File.Exists(icoPath)) Icon = new Icon(icoPath);

            // ORDEM IMPORTA no WinForms Dock:
            // Fill deve ser adicionado DEPOIS de Left/Right/Top/Bottom
            BuildTopBar();   // adiciona wrap (Fill) — deve vir primeiro no código
            BuildSidebar();  // adiciona sidebar (Left) — deve vir depois para ficar na frente
            BuildContentPanels();

            Load += (s, e) => ActivateSection("Home");
        }

        // ── SIDEBAR ────────────────────────────────────────────
        private void BuildSidebar()
        {
            var sb = new Panel
            {
                Dock = DockStyle.Left,
                Width = 52,
                BackColor = Color.FromArgb(13, 13, 13)
            };

            // Logo
            var logo = new Label
            {
                Text = "R",
                Font = new Font("Arial", 18, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 162, 255),
                Size = new Size(32, 32),
                Location = new Point(10, 12),
                TextAlign = ContentAlignment.MiddleCenter
            };
            sb.Controls.Add(logo);

            // Itens — usando texto simples, não emoji para evitar bug
            var items = new (string icon, string label, string section)[]
            {
                ("⌂", "Início",     "Home"),
                ("▦", "Destaques",  "Destaques"),
                ("☺", "Avatar",     "Avatar"),
                ("⚇", "Turma",      "Turma"),
                ("…", "Mais",       "Mais"),
            };

            int y = 56;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var btn = new Button
                {
                    Text = item.icon,
                    Font = new Font("Arial", 14),
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(52, 44),
                    Location = new Point(0, y),
                    Cursor = Cursors.Hand,
                    Tag = item.section,
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseVisualStyleBackColor = false
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 35, 35);

                var lbl = new Label
                {
                    Text = item.label,
                    Font = new Font("Arial", 6),
                    ForeColor = Color.Gray,
                    Size = new Size(52, 13),
                    Location = new Point(0, y + 44),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                string sec = item.section;
                btn.Click += (s, e) => ActivateSection(sec);

                _sidebarBtns.Add(btn);
                sb.Controls.Add(btn);
                sb.Controls.Add(lbl);
                y += 62;
            }

            // Botão sair
            var btnOut = new Button
            {
                Text = "↩",
                Font = new Font("Arial", 14),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(52, 40),
                Dock = DockStyle.Bottom,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnOut.FlatAppearance.BorderSize = 0;
            btnOut.Click += (s, e) =>
            {
                SessionService.Clear();
                Hide();
                RoloxAppContext.Instance.ShowLogin();
            };
            sb.Controls.Add(btnOut);

            Controls.Add(sb);
            // Não chamar BringToFront — deixa o Dock resolver
        }

        // ── TOP BAR ────────────────────────────────────────────
        private void BuildTopBar()
        {
            var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(23, 23, 23) };

            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(17, 17, 17)
            };

            lblPageTitle = new Label
            {
                Text = "Início",
                Font = new Font("Arial", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(12, 12),
                Size = new Size(200, 22)
            };

            var pnlSearch = new Panel
            {
                Size = new Size(300, 28),
                Location = new Point(400, 9),
                BackColor = Color.FromArgb(38, 38, 38)
            };
            var lblSrchIco = new Label { Text = "🔍", Font = new Font("Arial", 9), ForeColor = Color.Gray, Location = new Point(5, 5), AutoSize = true };
            var txtSearch = new TextBox { PlaceholderText = "Buscar", Font = new Font("Arial", 10), BackColor = Color.FromArgb(38, 38, 38), ForeColor = Color.White, BorderStyle = BorderStyle.None, Location = new Point(25, 5), Size = new Size(268, 20) };
            txtSearch.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await SearchAsync(txtSearch.Text.Trim()); };
            pnlSearch.Controls.AddRange(new Control[] { lblSrchIco, txtSearch });

            var avatarBox = new PictureBox { Size = new Size(28, 28), Location = new Point(870, 9), SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.FromArgb(50, 50, 50) };
            if (!string.IsNullOrEmpty(_user.AvatarUrl)) avatarBox.LoadAsync(_user.AvatarUrl);
            var lblUname = new Label { Text = _user.DisplayName, Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = Color.White, Location = new Point(903, 15), AutoSize = true };

            topBar.Controls.AddRange(new Control[] { lblPageTitle, pnlSearch, avatarBox, lblUname });

            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(23, 23, 23), Padding = new Padding(16, 10, 10, 10) };

            wrap.Controls.Add(pnlContent);
            wrap.Controls.Add(topBar);
            Controls.Add(wrap);
        }

        // ── CONTENT PANELS ─────────────────────────────────────
        private void BuildContentPanels()
        {
            BuildHomePanel();
            BuildDestaquesPanel();
            BuildTurmaPanel();
            BuildMaisPanel();
            BuildProfilePanel();
            BuildServerPanel();
            BuildChatPanel();
            BuildConfigPanel();
        }

        private void ActivateSection(string name)
        {
            ActivateSectionPublic(name);
        }

        public void ActivateSectionPublic(string name)
        {
            pnlHome.Visible      = name == "Home";
            pnlDestaques.Visible = name == "Destaques";
            pnlTurma.Visible     = name == "Turma";
            pnlMais.Visible      = name == "Mais";
            pnlProfile.Visible   = name == "Avatar";
            pnlServer.Visible    = name == "Servidor";
            pnlChat.Visible      = name == "Chat";
            pnlConfig.Visible    = name == "Config";

            lblPageTitle.Text = name switch
            {
                "Home"      => "Início",
                "Destaques" => "Destaques",
                "Turma"     => "Turma",
                "Mais"      => "Mais",
                "Avatar"    => "Avatar",
                "Config"    => "Configurações",
                _           => name
            };

            foreach (var b in _sidebarBtns)
                b.BackColor = b.Tag?.ToString() == name
                    ? Color.FromArgb(35, 35, 35)
                    : Color.Transparent;
        }

        // ── HOME ───────────────────────────────────────────────
        private void BuildHomePanel()
        {
            pnlHome = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(23, 23, 23) };

            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(23, 23, 23) };

            // Amizades
            var lblFriends = SectionLabel("Amizades");
            lblFriends.Location = new Point(15, 12);

            flpFriends = new FlowLayoutPanel
            {
                Location = new Point(15, 36),
                Height = 100,
                Width = 1100,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(23, 23, 23),
                AutoScroll = false
            };

            // Recomendações
            var lblRec = SectionLabel("Recomendações para você");
            lblRec.Location = new Point(15, 148);

            flpRecommended = GameFlow();
            flpRecommended.Location = new Point(15, 172);
            flpRecommended.Width = 1100;
            flpRecommended.Height = 180;
            flpRecommended.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Continuar
            var lblCont = SectionLabel("Continuar");
            lblCont.Location = new Point(15, 365);

            flpContinue = GameFlow();
            flpContinue.Location = new Point(15, 389);
            flpContinue.Width = 1100;
            flpContinue.Height = 180;
            flpContinue.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            scroll.Controls.AddRange(new Control[] { lblFriends, flpFriends, lblRec, flpRecommended, lblCont, flpContinue });
            pnlHome.Controls.Add(scroll);
            pnlContent.Controls.Add(pnlHome);
        }

        private async Task LoadHomeAsync()
        {
            var friends = await _api.GetFriendsAsync(_user.Id);
            if (flpFriends.InvokeRequired) flpFriends.Invoke(() => PopulateFriends(friends));
            else PopulateFriends(friends);
            if (pnlTurmaList.InvokeRequired) pnlTurmaList.Invoke(() => PopulateTurma(friends));
            else PopulateTurma(friends);

            // Recomendações e Continuar buscam listas diferentes
            var recommended = await _api.GetRecommendedGamesAsync();
            if (flpRecommended.InvokeRequired) flpRecommended.Invoke(() => PopulateGames(recommended, flpRecommended));
            else PopulateGames(recommended, flpRecommended);

            var continueGames = await _api.GetContinueGamesAsync();
            if (flpContinue.InvokeRequired) flpContinue.Invoke(() => PopulateGames(continueGames, flpContinue));
            else PopulateGames(continueGames, flpContinue);

            // Destaques usa lista popular
            var games = await _api.GetPopularGamesAsync();
            void PopDest() {
                PopulateGames(games, flpDestaque);
                PopulateGames(games.Count > 3 ? games.GetRange(0, Math.Max(1, games.Count / 2)) : games, flpRevelacoes);
                PopulateGames(games, flpMaisJogadas);
            }
            if (flpDestaque.InvokeRequired) flpDestaque.Invoke(PopDest);
            else PopDest();
        }

        private void PopulateTurma(List<RobloxUser> friends)
        {
            pnlTurmaList.Controls.Clear();
            int y = 0;
            foreach (var f in friends)
            {
                var friend = f;
                string name = string.IsNullOrWhiteSpace(f.DisplayName) ? f.Username : f.DisplayName;
                var row = new Panel { Location = new Point(0, y), Size = new Size(880, 48), BackColor = Color.FromArgb(23, 23, 23), Cursor = Cursors.Hand };
                var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(35, 35, 35) };
                var pic = new PictureBox { Size = new Size(36, 36), Location = new Point(0, 6), SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.FromArgb(50, 50, 50) };
                if (!string.IsNullOrEmpty(f.AvatarUrl)) pic.LoadAsync(f.AvatarUrl);
                var lblName = new Label { Text = name, Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.White, Location = new Point(45, 8), AutoSize = true };
                var lblDate = new Label { Text = DateTime.Now.ToString("d 'de' MMM 'de' yyyy"), Font = new Font("Arial", 8), ForeColor = Color.Gray, Location = new Point(750, 16), AutoSize = true };
                row.Click += (s, e) => new PlayerProfilePage(friend, _user, _p2p).Show();
                pic.Click  += (s, e) => new PlayerProfilePage(friend, _user, _p2p).Show();
                row.Controls.AddRange(new Control[] { pic, lblName, lblDate, sep });
                pnlTurmaList.Controls.Add(row);
                y += 48;
            }
            pnlTurmaList.Height = Math.Max(y, 100);
        }

        private async Task SearchAsync(string q)
        {
            if (string.IsNullOrEmpty(q)) return;
            var games = await _api.GetPopularGamesAsync();
            var res = games.FindAll(g => g.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
            PopulateGames(res.Count > 0 ? res : games, flpRecommended);
        }

        private void PopulateFriends(List<RobloxUser> friends)
        {
            flpFriends.Controls.Clear();

            // Botão adicionar
            var addCard = FriendCard(null, "+", "Adicionar...");
            addCard.Click += (s, e) => new AddFriendForm(_user, _api).Show();
            foreach (Control c in addCard.Controls)
                c.Click += (s, e) => new AddFriendForm(_user, _api).Show();
            flpFriends.Controls.Add(addCard);

            foreach (var f in friends)
            {
                var friend = f;
                // Mostra displayName, se vazio usa username, se ainda vazio usa ID
                string name = string.IsNullOrWhiteSpace(f.DisplayName)
                    ? (string.IsNullOrWhiteSpace(f.Username) ? f.Id.ToString() : f.Username)
                    : f.DisplayName;

                var card = FriendCard(f.AvatarUrl, null, name);
                card.Click += (s, e) => new PlayerProfilePage(friend, _user, _p2p).Show();
                foreach (Control c in card.Controls)
                    c.Click += (s, e) => new PlayerProfilePage(friend, _user, _p2p).Show();
                flpFriends.Controls.Add(card);
            }
        }

        private Panel FriendCard(string? avatarUrl, string? iconText, string name)
        {
            var card = new Panel
            {
                Size = new Size(64, 90),
                Margin = new Padding(0, 0, 4, 0),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            if (iconText != null)
            {
                var circle = new Panel { Size = new Size(48, 48), Location = new Point(8, 0), BackColor = Color.FromArgb(40, 40, 40) };
                var lbl = new Label { Text = iconText, Font = new Font("Arial", 20), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                circle.Controls.Add(lbl);
                card.Controls.Add(circle);
            }
            else
            {
                var pic = new PictureBox { Size = new Size(48, 48), Location = new Point(8, 0), SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.FromArgb(50, 50, 50) };
                if (!string.IsNullOrEmpty(avatarUrl)) pic.LoadAsync(avatarUrl);
                card.Controls.Add(pic);
            }

            var nameLbl = new Label
            {
                Text = name,
                Font = new Font("Arial", 7),
                ForeColor = Color.White,
                Location = new Point(0, 52),
                Size = new Size(64, 35),
                TextAlign = ContentAlignment.TopCenter,
                AutoEllipsis = true
            };
            card.Controls.Add(nameLbl);
            return card;
        }

        private void PopulateGames(List<RobloxGame> games, FlowLayoutPanel panel)
        {
            panel.Controls.Clear();
            var rng = new Random();
            foreach (var g in games)
            {
                var game = g; // captura local — ESSENCIAL para evitar bug de closure
                var card = new Panel
                {
                    Size = new Size(180, 150),
                    BackColor = Color.FromArgb(30, 30, 30),
                    Margin = new Padding(0, 0, 8, 0),
                    Cursor = Cursors.Hand
                };

                var thumb = new PictureBox
                {
                    Size = new Size(180, 100),
                    Location = new Point(0, 0),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    BackColor = Color.FromArgb(45, 45, 45)
                };
                if (!string.IsNullOrEmpty(g.ThumbnailUrl)) thumb.LoadAsync(g.ThumbnailUrl);

                var lblName = new Label
                {
                    Text = g.Name,
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(5, 103),
                    Size = new Size(170, 28),
                    AutoEllipsis = true
                };

                var lblRating = new Label
                {
                    Text = $"👍 {rng.Next(60, 99)}%",
                    Font = new Font("Arial", 7),
                    ForeColor = Color.Gray,
                    Location = new Point(5, 132),
                    Size = new Size(170, 14)
                };

                EventHandler open = (s, e) => new GamePage(game, _user, _p2p).Show();
                card.Click += open; thumb.Click += open; lblName.Click += open;
                card.Controls.AddRange(new Control[] { thumb, lblName, lblRating });
                panel.Controls.Add(card);
            }
        }

        // ── DESTAQUES ──────────────────────────────────────────
        private FlowLayoutPanel flpDestaque = null!;
        private FlowLayoutPanel flpRevelacoes = null!;
        private FlowLayoutPanel flpMaisJogadas = null!;

        private void BuildDestaquesPanel()
        {
            pnlDestaques = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(23, 23, 23) };
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(23, 23, 23) };

            var lblPop = new Label { Text = "Populares em:", Font = new Font("Arial", 9), ForeColor = Color.Gray, Location = new Point(15, 12), AutoSize = true };
            var btnPC  = MakeChip("Computador", new Point(15, 32));
            var btnBR  = MakeChip("Brasil", new Point(130, 32));

            var lblDest = SectionLabel("Em destaque"); lblDest.Location = new Point(15, 72);
            flpDestaque = GameFlow(); flpDestaque.Location = new Point(15, 98); flpDestaque.Width = 1100; flpDestaque.Height = 175;

            var lblRev = SectionLabel("Revelações"); lblRev.Location = new Point(15, 285);
            flpRevelacoes = GameFlow(); flpRevelacoes.Location = new Point(15, 311); flpRevelacoes.Width = 1100; flpRevelacoes.Height = 175;

            var lblMJ = SectionLabel("Mais jogadas agora"); lblMJ.Location = new Point(15, 498);
            var lblSub = new Label { Text = "Resultados para todos os dispositivos e locais", Font = new Font("Arial", 8), ForeColor = Color.Gray, Location = new Point(15, 522), AutoSize = true };
            flpMaisJogadas = GameFlow(); flpMaisJogadas.Location = new Point(15, 542); flpMaisJogadas.Width = 1100; flpMaisJogadas.Height = 175;

            scroll.Controls.AddRange(new Control[] { lblPop, btnPC, btnBR, lblDest, flpDestaque, lblRev, flpRevelacoes, lblMJ, lblSub, flpMaisJogadas });
            pnlDestaques.Controls.Add(scroll);
            pnlContent.Controls.Add(pnlDestaques);
        }

        private Button MakeChip(string text, Point loc)
        {
            var b = new Button
            {
                Text = text + " ▾",
                Location = loc,
                Size = new Size(100, 26),
                Font = new Font("Arial", 9),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 70);
            return b;
        }

        // ── TURMA ──────────────────────────────────────────────
        private Panel pnlTurmaList = null!;

        private void BuildTurmaPanel()
        {
            pnlTurma = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(23, 23, 23) };
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(23, 23, 23) };

            // Banner de verificação de idade (igual Roblox)
            var banner = new Panel { Location = new Point(15, 12), Size = new Size(900, 55), BackColor = Color.FromArgb(30, 30, 30) };
            var lblBanner = new Label { Text = "Vamos conferir sua idade para você poder usar o chat\nVocê só pode ver as mensagens do sistema aqui.", Font = new Font("Arial", 9), ForeColor = Color.LightGray, Location = new Point(45, 8), Size = new Size(820, 38) };
            var lblBannerIcon = new Label { Text = "💬", Font = new Font("Arial", 14), ForeColor = Color.White, Location = new Point(8, 12), AutoSize = true };
            banner.Controls.AddRange(new Control[] { lblBannerIcon, lblBanner });

            // Começar Turma
            var pnlStartTurma = MakeTurmaRow("⚇", "Começar Turma", "Entrar em experiências juntos", new Point(15, 80));
            pnlStartTurma.Click += (s, e) => { };

            // Comunidades
            var pnlComunidades = MakeTurmaRow("⚇", "Comunidades", "Criar e explorar comunidades", new Point(15, 130));
            pnlComunidades.Click += (s, e) =>
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.roblox.com/pt/communities/1005332769/one-mapas-officeal",
                    UseShellExecute = true
                });

            // Lista de amigos
            pnlTurmaList = new Panel { Location = new Point(15, 185), Size = new Size(900, 400), BackColor = Color.FromArgb(23, 23, 23), AutoScroll = true };

            scroll.Controls.AddRange(new Control[] { banner, pnlStartTurma, pnlComunidades, pnlTurmaList });
            pnlTurma.Controls.Add(scroll);
            pnlContent.Controls.Add(pnlTurma);
        }

        private Panel MakeTurmaRow(string icon, string title, string sub, Point loc)
        {
            var p = new Panel { Location = loc, Size = new Size(900, 46), BackColor = Color.FromArgb(23, 23, 23), Cursor = Cursors.Hand };
            var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(40, 40, 40) };
            var lblIcon  = new Label { Text = icon, Font = new Font("Arial", 14), ForeColor = Color.White, Location = new Point(5, 8), AutoSize = true };
            var lblTitle = new Label { Text = title, Font = new Font("Arial", 11, FontStyle.Bold), ForeColor = Color.White, Location = new Point(35, 5), AutoSize = true };
            var lblSub   = new Label { Text = sub, Font = new Font("Arial", 8), ForeColor = Color.Gray, Location = new Point(35, 25), AutoSize = true };
            var lblArr   = new Label { Text = "→", Font = new Font("Arial", 14), ForeColor = Color.Gray, Location = new Point(860, 12), AutoSize = true };
            p.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblSub, lblArr, sep });
            return p;
        }

        // ── MAIS ───────────────────────────────────────────────
        private void BuildMaisPanel()
        {
            pnlMais = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(23, 23, 23) };
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(23, 23, 23) };

            var items = new[]
            {
                ("🛍", "Mercado",           "Mercado"),
                ("💎", "Premium",           "Premium"),
                ("👤", "Perfil",            $"https://www.roblox.com/users/{_user.Id}/profile"),
                ("👥", "Amizades",          "https://www.roblox.com/users/friends"),
                ("🏘", "Comunidades",       "Comunidades"),
                ("🎒", "Inventário",        "Inventário"),
                ("💬", "Mensagens",         "Mensagens"),
                ("🔨", "Criar",             "Criar"),
                ("🎨", "Avatar",            "AvatarEditor"),
                ("🔔", "Notificações",      "Notificações"),
                ("📝", "Blog",              "https://blog.roblox.com"),
                ("📚", "Aprenda",           "https://education.roblox.com"),
                ("🎁", "Cartões presente",  "https://www.roblox.com/giftcards"),
                ("🛒", "Loja Oficial",      "https://shop.roblox.com"),
                ("⚙", "Configurações",     "Config"),
                ("ℹ", "Sobre",             "https://corp.roblox.com"),
                ("🔒", "Ajuda e Segurança","https://en.help.roblox.com"),
                ("⚡", "Acesso rápido",    "https://www.roblox.com"),
            };

            var flow = new FlowLayoutPanel
            {
                Location = new Point(15, 15),
                Size = new Size(950, 420),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.FromArgb(23, 23, 23)
            };

            foreach (var (icon, label, url) in items)
            {
                var card = new Panel
                {
                    Size = new Size(160, 110),
                    BackColor = Color.FromArgb(30, 30, 30),
                    Margin = new Padding(6),
                    Cursor = Cursors.Hand
                };
                var lblIcon = new Label { Text = icon, Font = new Font("Arial", 22), ForeColor = Color.White, AutoSize = false, Size = new Size(160, 55), Location = new Point(0, 10), TextAlign = ContentAlignment.MiddleCenter };
                var lblName = new Label { Text = label, Font = new Font("Arial", 9), ForeColor = Color.White, AutoSize = false, Size = new Size(160, 35), Location = new Point(0, 65), TextAlign = ContentAlignment.MiddleCenter };

                string target = url;
                EventHandler click = (s, e) =>
                {
                    switch (target)
                    {
                        case "Config":        ActivateSection("Config"); break;
                        case "Mercado":       new MarketplaceForm(_user).Show(); break;
                        case "Premium":       new PremiumForm(_user).Show(); break;
                        case "Comunidades":   new CommunitiesForm(_user).Show(); break;
                        case "Inventário":    new InventoryForm(_user, _api).Show(); break;
                        case "Mensagens":     new MessagesForm(_user).Show(); break;
                        case "Criar":         new CreateForm(_user).Show(); break;
                        case "AvatarEditor":  new AvatarEditorForm(_user, _api).Show(); break;
                        case "Notificações":  new NotificationsForm(_user).Show(); break;
                        default:
                            if (target.StartsWith("http"))
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = target, UseShellExecute = true });
                            break;
                    }
                };
                card.Click += click; lblIcon.Click += click; lblName.Click += click;
                card.Controls.AddRange(new Control[] { lblIcon, lblName });
                flow.Controls.Add(card);
            }

            // Botão Sair
            var btnSair = Btn("Sair", new Point(350, 450), new Size(250, 38), Color.FromArgb(40, 40, 40), Color.White);
            btnSair.Click += (s, e) => { SessionService.Clear(); Hide(); RoloxAppContext.Instance.ShowLogin(); };

            scroll.Controls.AddRange(new Control[] { flow, btnSair });
            pnlMais.Controls.Add(scroll);
            pnlContent.Controls.Add(pnlMais);
        }
        private void BuildProfilePanel()
        {
            pnlProfile = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(23, 23, 23) };
            var avatar = new PictureBox { Size = new Size(100, 100), Location = new Point(30, 30), SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.FromArgb(50, 50, 50) };
            if (!string.IsNullOrEmpty(_user.AvatarUrl)) avatar.LoadAsync(_user.AvatarUrl);
            var lblDN  = new Label { Text = _user.DisplayName, Font = new Font("Arial", 18, FontStyle.Bold), ForeColor = Color.White, Location = new Point(145, 30), AutoSize = true };
            var lblUN  = new Label { Text = "@" + _user.Username, Font = new Font("Arial", 11), ForeColor = Color.Gray, Location = new Point(145, 65), AutoSize = true };
            var lblID  = new Label { Text = "ID: " + _user.Id, Font = new Font("Arial", 9), ForeColor = Color.FromArgb(80,80,80), Location = new Point(145, 90), AutoSize = true };
            var lblDesc= new Label { Text = string.IsNullOrEmpty(_user.Description) ? "Sem descrição." : _user.Description, Font = new Font("Arial", 10), ForeColor = Color.LightGray, Location = new Point(30, 150), Size = new Size(600, 60) };
            var lblIp  = new Label { Text = "IP host: " + GetLocalIp(), Font = new Font("Arial", 10), ForeColor = Color.FromArgb(0,162,255), Location = new Point(30, 230), AutoSize = true };
            pnlProfile.Controls.AddRange(new Control[] { avatar, lblDN, lblUN, lblID, lblDesc, lblIp });
            pnlContent.Controls.Add(pnlProfile);
        }

        // ── SERVER ─────────────────────────────────────────────
        private void BuildServerPanel()
        {
            pnlServer = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(23, 23, 23) };
            lblServerStatus = new Label { Text = "Servidor: Offline", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.OrangeRed, Location = new Point(20, 20), AutoSize = true };
            var btnHost = Btn("Hospedar", new Point(20, 60), new Size(200, 40));
            btnHost.Click += async (s, e) =>
            {
                await _p2p.StartHostAsync(_user.Username);
                lblServerStatus.Text = "Online | Porta: " + _p2p.Port;
                lblServerStatus.ForeColor = Color.LimeGreen;
                _p2p.OnPlayerJoined += _ => UpdatePlayers();
                _p2p.OnPlayerLeft   += _ => UpdatePlayers();
            };
            var btnStop = Btn("Parar", new Point(228, 60), new Size(100, 40), Color.FromArgb(60,20,20), Color.OrangeRed);
            btnStop.Click += (s, e) => { _p2p.StopHost(); lblServerStatus.Text = "Servidor: Offline"; lblServerStatus.ForeColor = Color.OrangeRed; lstPlayers.Items.Clear(); };
            var lblJoin = new Label { Text = "Entrar em servidor:", Font = new Font("Arial", 10), ForeColor = Color.White, Location = new Point(20, 115), AutoSize = true };
            var txtIp   = new TextBox { Location = new Point(20,138), Size = new Size(200,30), Font = new Font("Arial",11), BackColor = Color.FromArgb(40,40,40), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "192.168.x.x" };
            var btnJoin = Btn("Entrar", new Point(228,136), new Size(90,32));
            btnJoin.Click += async (s, e) =>
            {
                bool ok = await _p2p.JoinServerAsync(txtIp.Text.Trim(), 7777, _user.Username);
                lblServerStatus.Text = ok ? "Conectado" : "Falha.";
                lblServerStatus.ForeColor = ok ? Color.LimeGreen : Color.OrangeRed;
            };
            var lblPl = new Label { Text = "Jogadores:", Font = new Font("Arial",10), ForeColor = Color.White, Location = new Point(20,185), AutoSize = true };
            lstPlayers = new ListBox { Location = new Point(20,207), Size = new Size(320,180), BackColor = Color.FromArgb(35,35,35), ForeColor = Color.White, Font = new Font("Arial",10), BorderStyle = BorderStyle.FixedSingle };
            pnlServer.Controls.AddRange(new Control[] { lblServerStatus, btnHost, btnStop, lblJoin, txtIp, btnJoin, lblPl, lstPlayers });
            pnlContent.Controls.Add(pnlServer);
        }

        private void UpdatePlayers()
        {
            if (lstPlayers.InvokeRequired) { lstPlayers.Invoke(UpdatePlayers); return; }
            lstPlayers.Items.Clear();
            foreach (var p in _p2p.ConnectedPlayers) lstPlayers.Items.Add(p);
        }

        // ── CHAT ───────────────────────────────────────────────
        private void BuildChatPanel()
        {
            pnlChat = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(23, 23, 23) };
            rtbChat = new RichTextBox { Dock = DockStyle.None, Location = new Point(0,0), Anchor = AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom, Size = new Size(1100,560), BackColor = Color.FromArgb(28,28,28), ForeColor = Color.White, Font = new Font("Arial",10), ReadOnly = true, BorderStyle = BorderStyle.None };
            txtChatInput = new TextBox { Anchor = AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right, Location = new Point(0,570), Size = new Size(1000,32), Font = new Font("Arial",11), BackColor = Color.FromArgb(40,40,40), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Diga algo..." };
            var btnSend = Btn("Enviar", new Point(1010, 568), new Size(90, 34));
            btnSend.Click += DoSend;
            txtChatInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { DoSend(s, e); e.SuppressKeyPress = true; } };
            _p2p.OnMessageReceived += (u, m) => { if (rtbChat.InvokeRequired) rtbChat.Invoke(() => ChatAppend(u, m)); else ChatAppend(u, m); };
            pnlChat.Controls.AddRange(new Control[] { rtbChat, txtChatInput, btnSend });
            pnlContent.Controls.Add(pnlChat);
        }

        private void DoSend(object? s, EventArgs e)
        {
            string msg = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;
            if (!_mod.CheckMessage(_user.Username, msg, out string reason)) { ChatAppend("⚠ Sistema", reason, Color.OrangeRed); txtChatInput.Clear(); return; }
            _p2p.SendMessage(msg);
            ChatAppend(_user.Username, msg, Color.FromArgb(0,162,255));
            txtChatInput.Clear();
        }

        private void ChatAppend(string user, string msg, Color? color = null)
        {
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = color ?? Color.White;
            rtbChat.AppendText($"[{DateTime.Now:HH:mm}] {user}: {msg}\n");
            rtbChat.ScrollToCaret();
        }

        // ── CONFIG ─────────────────────────────────────────────
        private void BuildConfigPanel()
        {
            pnlConfig = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(23, 23, 23) };

            var lblTitle = new Label { Text = "Configurações", Font = new Font("Arial", 16, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 20), AutoSize = true };

            // Trocar conta
            var lblAccount = SectionLabel("Conta"); lblAccount.Location = new Point(20, 65);
            var btnSwitch = Btn("Trocar conta / Sair", new Point(20, 95), new Size(220, 38));
            btnSwitch.Click += (s, e) => { SessionService.Clear(); Hide(); RoloxAppContext.Instance.ShowLogin(); };

            // Sobre
            var lblAbout = SectionLabel("Sobre"); lblAbout.Location = new Point(20, 155);
            var lblVer = new Label { Text = "Rolox v1.0\nCriador Principal: Roblox Official\nPublicador: One Mapas Official", Font = new Font("Arial", 10), ForeColor = Color.Gray, Location = new Point(20, 183), AutoSize = true };

            var btnTerms = Btn("Ver Termos de Uso", new Point(20, 255), new Size(200, 35), Color.FromArgb(35,35,35), Color.White);
            btnTerms.Click += (s, e) => new TermsForm().ShowDialog();

            // Extensão do Chrome/Edge
            var lblExt = SectionLabel("Extensão do Navegador"); lblExt.Location = new Point(20, 305);
            var lblExtInfo = new Label { Text = "Instala o ícone Rolox no Chrome/Edge e adiciona botão nas páginas do Roblox.", Font = new Font("Arial", 9), ForeColor = Color.Gray, Location = new Point(20, 333), Size = new Size(560, 32) };
            var btnExt = Btn("Instalar Extensão Rolox", new Point(20, 370), new Size(220, 36), Color.FromArgb(0, 100, 50), Color.LimeGreen);
            btnExt.Click += (s, e) =>
            {
                string extPath = RoloxApp.Services.ExtensionService.GetExtensionPath();
                System.Windows.Forms.MessageBox.Show(
                    $"1. O Edge/Chrome vai abrir na página de extensões.\n" +
                    $"2. Ative o 'Modo do desenvolvedor' (canto superior direito).\n" +
                    $"3. Clique em 'Carregar sem compactação'.\n" +
                    $"4. Selecione a pasta:\n{extPath}",
                    "Instalar Extensão Rolox",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
                RoloxApp.Services.ExtensionService.OpenInstallGuide();
            };

            // Servidor P2P
            var lblSrv = SectionLabel("Servidor P2P"); lblSrv.Location = new Point(20, 265);
            var lblPort = new Label { Text = "Porta padrão: 7777", Font = new Font("Arial", 10), ForeColor = Color.LightGray, Location = new Point(20, 293), AutoSize = true };
            var lblIp   = new Label { Text = "Seu IP: " + GetLocalIp(), Font = new Font("Arial", 10), ForeColor = Color.FromArgb(0,162,255), Location = new Point(20, 318), AutoSize = true };

            pnlConfig.Controls.AddRange(new Control[] { lblTitle, lblAccount, btnSwitch, lblAbout, lblVer, btnTerms, lblExt, lblExtInfo, btnExt, lblSrv, lblPort, lblIp });
            pnlContent.Controls.Add(pnlConfig);
        }

        // ── HELPERS ────────────────────────────────────────────
        private Label SectionLabel(string text) => new Label
        {
            Text = text,
            Font = new Font("Arial", 12, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true
        };

        private FlowLayoutPanel GameFlow() => new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.FromArgb(23, 23, 23),
            AutoScroll = false
        };

        private Button Btn(string text, Point loc, Size size, Color? bg = null, Color? fg = null)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = size,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = bg ?? Color.FromArgb(0, 162, 255),
                ForeColor = fg ?? Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private string GetLocalIp()
        {
            try { foreach (var ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList) if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return ip.ToString(); }
            catch { }
            return "127.0.0.1";
        }
    }
}
