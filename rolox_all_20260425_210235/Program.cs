using System;
using System.IO;
using System.Windows.Forms;
using RoloxApp.Services;

namespace RoloxApp
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            // ── Tratamento de URL rolox:// ──────────────────────
            // Quando o Windows chama o app via protocolo rolox://,
            // o argumento vem como: RoloxApp.exe "rolox://placeId=123"
            if (args.Length > 0 && args[0].StartsWith("rolox://", StringComparison.OrdinalIgnoreCase))
            {
                string url = args[0];
                bool ok = RoloxApp.Services.RoloxLauncher.LaunchFromUrl(url, out string err);
                if (!ok)
                    MessageBox.Show($"Erro ao abrir jogo:\n{err}", "Rolox",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Não abre a UI — só lança o jogo
            }

            // ── Registro do protocolo rolox:// ──────────────────
            if (!RoloxApp.Services.ProtocolService.IsRegistered())
            {
                try
                {
                    RoloxApp.Services.ProtocolService.Register(out _);
                }
                catch { /* sem admin — ignora silenciosamente */ }
            }

            if (!TermsForm.AlreadyAccepted())
            {
                var terms = new TermsForm();
                if (terms.ShowDialog() != DialogResult.OK)
                    return;
            }

            var ctx = new RoloxAppContext();
            Application.Run(ctx);
        }
    }

    public class RoloxAppContext : ApplicationContext
    {
        public static RoloxAppContext Instance { get; private set; } = null!;

        private NotifyIcon   _tray    = null!;
        private MainForm?    _mainForm;
        private ToolStripMenuItem _miConexoes = null!;

        public static System.Drawing.Icon AppIcon { get; private set; } = null!;

        public RoloxAppContext()
        {
            Instance = this;
            LoadIcon();
            BuildTray();
            ShowStart();
        }

        private void LoadIcon()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rolox_janela.ico");
            if (File.Exists(path))
                AppIcon = new System.Drawing.Icon(path);
        }

        private void BuildTray()
        {
            // Ícone da bandeja
            string trayPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rolox_system-bar.ico");
            var trayIcon = File.Exists(trayPath)
                ? new System.Drawing.Icon(trayPath)
                : AppIcon ?? SystemIcons.Application;

            // Itens de navegação
            var miInicio   = new ToolStripMenuItem("  Início",   null, (s, e) => OpenSection("Home"));
            var miGraficos = new ToolStripMenuItem("  Gráficos", null, (s, e) => OpenSection("Destaques"));
            var miAvatar   = new ToolStripMenuItem("  Avatar",   null, (s, e) => OpenSection("Avatar"));
            var miTurma    = new ToolStripMenuItem("  Turma",    null, (s, e) => OpenSection("Turma"));

            // Notificações e Conexões
            var miNotif    = new ToolStripMenuItem("Notificações", null, (s, e) => ShowMain());
            _miConexoes    = new ToolStripMenuItem("Conexões                 0 Online") { Enabled = false };

            var miSair     = new ToolStripMenuItem("Sair", null, (s, e) => ExitApp());

            var menu = new ContextMenuStrip();
            menu.Items.Add(miInicio);
            menu.Items.Add(miGraficos);
            menu.Items.Add(miAvatar);
            menu.Items.Add(miTurma);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miNotif);
            menu.Items.Add(_miConexoes);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miSair);

            // Estilo escuro
            menu.BackColor    = System.Drawing.Color.FromArgb(18, 18, 18);
            menu.ForeColor    = System.Drawing.Color.White;
            menu.Font         = new System.Drawing.Font("Segoe UI", 10f);
            menu.ShowImageMargin = false;
            foreach (ToolStripItem item in menu.Items)
            {
                item.BackColor = System.Drawing.Color.FromArgb(18, 18, 18);
                item.ForeColor = System.Drawing.Color.White;
            }

            _tray = new NotifyIcon
            {
                Icon             = trayIcon,
                Text             = "Rolox",
                ContextMenuStrip = menu,
                Visible          = true
            };

            _tray.DoubleClick += (s, e) => ShowMain();
        }

        // Atualiza o contador de amigos online no menu
        public void UpdateOnlineCount(int count)
        {
            if (_miConexoes == null) return;
            _miConexoes.Text = $"Conexões                 {count} Online";
        }

        public void ShowStart()
        {
            string? saved = SessionService.Load();
            if (!string.IsNullOrEmpty(saved))
            {
                // Verifica se já tem idade salva
                int age = SessionService.LoadAge();
                if (age < 0)
                {
                    // Primeira vez — pergunta a idade
                    var ageForm = new AgeSelectorForm();
                    if (ageForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        SessionService.Save(saved, ageForm.SelectedAge);
                }
                ShowSplash(saved);
            }
            else
                ShowLogin();
        }

        public void ShowLogin()
        {
            var form = new LoginForm();
            if (AppIcon != null) form.Icon = AppIcon;
            form.Show();
            MainForm = form;
        }

        public void ShowSplash(string username)
        {
            var form = new SplashForm(username);
            if (AppIcon != null) form.Icon = AppIcon;
            form.Show();
            MainForm = form;
        }

        public void SetMainForm(MainForm form)
        {
            _mainForm = form;
            MainForm  = form;
            if (AppIcon != null) form.Icon = AppIcon;

            form.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    form.Hide();
                    _tray.ShowBalloonTip(2000, "Rolox",
                        "Rolox continua rodando em segundo plano.", ToolTipIcon.Info);
                }
            };
        }

        private void OpenSection(string section)
        {
            ShowMain();
            _mainForm?.ActivateSectionPublic(section);
        }

        private void ShowMain()
        {
            if (_mainForm == null || _mainForm.IsDisposed)
            {
                ShowLogin();
                return;
            }
            _mainForm.Show();
            _mainForm.WindowState = FormWindowState.Normal;
            _mainForm.BringToFront();
        }

        private void ExitApp()
        {
            _tray.Visible = false;
            Application.Exit();
        }
    }
}
