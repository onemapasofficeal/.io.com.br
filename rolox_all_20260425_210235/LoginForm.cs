using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    public class LoginForm : Form
    {
        private readonly RobloxApiService _api = new();
        private Panel pnlCredentials = null!;
        private Panel pnlWaiting = null!;
        private Label lblStatus = null!;
        private Label lblWaitMsg = null!;
        private ProgressBar progressBar = null!;
        private int _waitSeconds = 30;
        private System.Windows.Forms.Timer _waitTimer = null!;
        private RobloxUser? _loadedUser;

        public LoginForm()
        {
            Text = "Rolox - Login";
            Size = new Size(520, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;

            BuildCredentialsPanel();
            BuildWaitingPanel();
            ShowPanel("credentials");
        }

        private void ShowPanel(string which)
        {
            pnlCredentials.Visible = which == "credentials";
            pnlWaiting.Visible     = which == "waiting";
        }

        private void BuildCredentialsPanel()
        {
            pnlCredentials = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(23, 23, 23) };

            var logoBox = new Panel
            {
                Size = new Size(120, 120),
                Location = new Point(190, 25),
                BackColor = Color.FromArgb(0, 162, 255)
            };
            var logoLetter = new Label
            {
                Text = "R",
                Font = new Font("Arial", 60, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(120, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 0)
            };
            logoBox.Controls.Add(logoLetter);

            var lblTitle = new Label
            {
                Text = "ROLOX",
                Font = new Font("Arial", 28, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(460, 45),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 158)
            };

            var lblSub = new Label
            {
                Text = "Criador Principal: Roblox Official  |  Publicador: One Mapas Official",
                Font = new Font("Arial", 8),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(460, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 206)
            };

            var lblInfo = new Label
            {
                Text = "Clique em \"Login in Roblox\" para entrar com sua conta Roblox.",
                Font = new Font("Arial", 9),
                ForeColor = Color.LightGray,
                AutoSize = false,
                Size = new Size(460, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 240)
            };

            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Arial", 9),
                ForeColor = Color.OrangeRed,
                AutoSize = false,
                Size = new Size(430, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(40, 278)
            };

            // Botão principal
            var btnLogin = new Button
            {
                Text = "Login in Roblox",
                Location = new Point(40, 315),
                Size = new Size(430, 55),
                Font = new Font("Arial", 14, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 162, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += async (s, e) => await DoRobloxLogin(btnLogin);

            var lblCommunity = new Label
            {
                Text = "Ao entrar, você se juntará à comunidade One Mapas Official no Roblox.",
                Font = new Font("Arial", 8),
                ForeColor = Color.FromArgb(70, 70, 70),
                AutoSize = false,
                Size = new Size(430, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(40, 385)
            };

            var btnRegister = new Button
            {
                Text = "Criar nova conta",
                Location = new Point(40, 410),
                Size = new Size(430, 36),
                Font = new Font("Arial", 10),
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 1;
            btnRegister.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btnRegister.Click += (s, e) => { Hide(); new RegisterForm().Show(); };

            pnlCredentials.Controls.AddRange(new Control[] {
                logoBox, lblTitle, lblSub, lblInfo, lblStatus, btnLogin, lblCommunity, btnRegister
            });
            Controls.Add(pnlCredentials);
        }

        private async Task DoRobloxLogin(Button btnLogin)
        {
            btnLogin.Enabled = false;
            lblStatus.ForeColor = Color.LightBlue;
            lblStatus.Text = "Buscando conta do Roblox no seu PC...";

            // Tenta ler o username da instalação local do Roblox
            string? username = TryGetRobloxUsername();

            if (!string.IsNullOrEmpty(username))
            {
                lblStatus.ForeColor = Color.LimeGreen;
                lblStatus.Text = $"Conta encontrada: {username}";
                await Task.Delay(800);
                await ContinueWithUsername(username);
                return;
            }

            // Roblox não está instalado ou não tem conta salva — abre o site
            lblStatus.ForeColor = Color.LightGray;
            lblStatus.Text = "Abrindo Roblox... faça login e volte aqui.";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "msedge",
                    Arguments = "--new-window https://www.roblox.com/login",
                    UseShellExecute = true
                });
            }
            catch
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.roblox.com/login",
                    UseShellExecute = true
                });
            }

            // Troca botão para confirmar após login
            btnLogin.Text = "✅  Já fiz login no Roblox";
            btnLogin.BackColor = Color.FromArgb(20, 100, 20);
            btnLogin.Enabled = true;
            btnLogin.Click += async (s, e) =>
            {
                btnLogin.Enabled = false;
                lblStatus.ForeColor = Color.LightBlue;
                lblStatus.Text = "Buscando conta...";

                string? u = TryGetRobloxUsername();
                if (string.IsNullOrEmpty(u))
                {
                    // Pede o username manualmente como último recurso
                    u = Microsoft.VisualBasic.Interaction.InputBox(
                        "Digite seu username do Roblox:", "Rolox", "");
                }

                if (string.IsNullOrEmpty(u))
                {
                    lblStatus.ForeColor = Color.OrangeRed;
                    lblStatus.Text = "Username não informado.";
                    btnLogin.Enabled = true;
                    return;
                }

                await ContinueWithUsername(u);
            };
        }

        private async Task ContinueWithUsername(string username)
        {
            lblStatus.ForeColor = Color.LightBlue;
            lblStatus.Text = "Carregando perfil...";

            _loadedUser = await _api.GetUserByUsernameAsync(username);

            if (_loadedUser == null)
            {
                lblStatus.ForeColor = Color.OrangeRed;
                lblStatus.Text = "Não foi possível carregar o perfil. Verifique o username.";
                return;
            }

            // Fecha Edge com Roblox
            try
            {
                foreach (var proc in Process.GetProcessesByName("msedge"))
                    if (proc.MainWindowTitle.Contains("Roblox") || proc.MainWindowTitle.Contains("roblox"))
                        proc.CloseMainWindow();
            }
            catch { }

            // Abre comunidade One Mapas Official
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.roblox.com/pt/communities/1005332769/one-mapas-officeal",
                    UseShellExecute = true
                });
            }
            catch { }

            // Salva sessão
            SessionService.Save(_loadedUser.Username);

            StartWaiting();
        }

        // Tenta ler o username da conta logada no Roblox instalado no PC
        private string? TryGetRobloxUsername()
        {
            try
            {
                // Roblox salva dados em %LocalAppData%\Roblox\GlobalBasicSettings_13.xml
                // e também em %AppData%\Local\Roblox
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string[] possiblePaths = {
                    Path.Combine(localApp, "Roblox", "GlobalBasicSettings_13.xml"),
                    Path.Combine(localApp, "Roblox", "GlobalBasicSettings_13_local.xml"),
                };

                foreach (var path in possiblePaths)
                {
                    if (!File.Exists(path)) continue;
                    string content = File.ReadAllText(path);

                    // Procura pelo campo de username
                    int idx = content.IndexOf("<username>", StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) idx = content.IndexOf("\"username\":", StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) continue;

                    int start = content.IndexOf('>', idx) + 1;
                    int end = content.IndexOf('<', start);
                    if (start > 0 && end > start)
                    {
                        string name = content.Substring(start, end - start).Trim();
                        if (!string.IsNullOrEmpty(name)) return name;
                    }
                }

                // Tenta via registro do Windows
                using var key = Microsoft.Win32.Registry.CurrentUser
                    .OpenSubKey(@"Software\Roblox\RobloxStudioBrowser");
                if (key != null)
                {
                    string? val = key.GetValue("username") as string;
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
            catch { }
            return null;
        }

        private void BuildWaitingPanel()
        {
            pnlWaiting = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(23, 23, 23), Visible = false };

            var logo = new Label
            {
                Text = "ROLOX",
                Font = new Font("Arial", 32, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 162, 255),
                AutoSize = false,
                Size = new Size(460, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 80)
            };

            lblWaitMsg = new Label
            {
                Text = "Aguarde 30 segundos...\naté copiamos tudo da página oficial\ne seus amigos e carregar o restante do app\n(isso também depende do seu PC/internet).",
                Font = new Font("Arial", 11),
                ForeColor = Color.LightGray,
                AutoSize = false,
                Size = new Size(460, 110),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 175)
            };

            progressBar = new ProgressBar
            {
                Location = new Point(60, 305),
                Size = new Size(380, 16),
                Minimum = 0,
                Maximum = 30,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            var lblJoin = new Label
            {
                Text = "Entrando na comunidade One Mapas Official...",
                Font = new Font("Arial", 9),
                ForeColor = Color.FromArgb(0, 162, 255),
                AutoSize = false,
                Size = new Size(460, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 335)
            };

            _waitTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _waitTimer.Tick += (s, e) =>
            {
                _waitSeconds--;
                progressBar.Value = 30 - _waitSeconds;
                lblWaitMsg.Text = $"Aguarde {_waitSeconds} segundos...\naté copiamos tudo da página oficial\ne seus amigos e carregar o restante do app\n(isso também depende do seu PC/internet).";
                if (_waitSeconds <= 0) { _waitTimer.Stop(); FinishLogin(); }
            };

            pnlWaiting.Controls.AddRange(new Control[] { logo, lblWaitMsg, progressBar, lblJoin });
            Controls.Add(pnlWaiting);
        }

        private void StartWaiting()
        {
            if (InvokeRequired) { Invoke(StartWaiting); return; }
            _waitSeconds = 30;
            progressBar.Value = 0;
            ShowPanel("waiting");
            _waitTimer.Start();
        }

        private void FinishLogin()
        {
            if (_loadedUser == null)
            {
                ShowPanel("credentials");
                lblStatus.ForeColor = Color.OrangeRed;
                lblStatus.Text = "Erro ao carregar perfil.";
                return;
            }
            Hide();
            // Se não tem perfil de idade definido, pergunta
            if (!AgeProfileService.HasProfile())
                new AgeSelectorForm(_loadedUser, _api).Show();
            else
            {
                var (age, mode) = AgeProfileService.Load(_loadedUser.Username);
                switch (mode)
                {
                    case RoloxMode.Kid:   new MainFormKid(_loadedUser, _api).Show(); break;
                    case RoloxMode.Admin: new AdminForm(_loadedUser, _api).Show(); break;
                    default:              new MainForm(_loadedUser, _api).Show(); break;
                }
            }
        }
    }
}
