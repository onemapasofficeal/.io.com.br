using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    public class RegisterForm : Form
    {
        private readonly RobloxApiService _api = new();
        private TextBox  txtUsername = null!;
        private TextBox  txtPassword = null!;
        private TextBox  txtConfirm  = null!;
        private Label    lblStatus   = null!;
        private Button   btnRegister = null!;
        private Button   btnGuest    = null!;
        private ProgressBar bar      = null!;

        // Guests banidos por comportamento inadequado
        private static readonly string[] _bannedGuests =
        {
            "guest_67", "guest_42", "guest_666",
            "Guest_67", "Guest_42", "Guest_666"
        };

        public RegisterForm()
        {
            Text            = "Rolox - Criar Conta";
            Size            = new Size(480, 560);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(23, 23, 23);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            Build();
        }

        private void Build()
        {
            // Logo
            var logo = new Label
            {
                Text = "ROLOX",
                Font = new Font("Arial", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 162, 255),
                AutoSize = false, Size = new Size(420, 50),
                Location = new Point(20, 18),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblSub = new Label
            {
                Text = "Criar nova conta",
                Font = new Font("Arial", 10),
                ForeColor = Color.Gray,
                AutoSize = false, Size = new Size(420, 20),
                Location = new Point(20, 68),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Username
            var lblUser = new Label
            {
                Text = "Nome de usuário",
                Font = new Font("Arial", 9), ForeColor = Color.LightGray,
                Location = new Point(30, 105), AutoSize = true
            };
            txtUsername = new TextBox
            {
                Location = new Point(30, 123), Size = new Size(400, 30),
                Font = new Font("Arial", 11),
                BackColor = Color.FromArgb(38, 38, 38), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Ex: MeuUsuario123"
            };
            txtUsername.TextChanged += async (s, e) => await CheckUsernameAsync();

            // Senha
            var lblPass = new Label
            {
                Text = "Senha (8 a 200 caracteres)",
                Font = new Font("Arial", 9), ForeColor = Color.LightGray,
                Location = new Point(30, 168), AutoSize = true
            };
            txtPassword = new TextBox
            {
                Location = new Point(30, 186), Size = new Size(400, 30),
                Font = new Font("Arial", 11),
                BackColor = Color.FromArgb(38, 38, 38), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true,
                PlaceholderText = "Mínimo 8 caracteres"
            };
            txtPassword.TextChanged += (s, e) => ValidateForm();

            // Confirmar senha
            var lblConf = new Label
            {
                Text = "Confirmar senha",
                Font = new Font("Arial", 9), ForeColor = Color.LightGray,
                Location = new Point(30, 231), AutoSize = true
            };
            txtConfirm = new TextBox
            {
                Location = new Point(30, 249), Size = new Size(400, 30),
                Font = new Font("Arial", 11),
                BackColor = Color.FromArgb(38, 38, 38), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true,
                PlaceholderText = "Repita a senha"
            };
            txtConfirm.TextChanged += (s, e) => ValidateForm();

            // Status
            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Arial", 8), ForeColor = Color.OrangeRed,
                AutoSize = false, Size = new Size(420, 22),
                Location = new Point(30, 292),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Barra de progresso
            bar = new ProgressBar
            {
                Location = new Point(30, 318), Size = new Size(400, 6),
                Minimum = 0, Maximum = 100, Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            // Botão registrar
            btnRegister = new Button
            {
                Text = "Criar Conta no Roblox",
                Location = new Point(30, 336), Size = new Size(400, 46),
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Enabled = false
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += async (s, e) => await DoRegisterAsync();

            // Separador
            var sep = new Label
            {
                Text = "─────────────  ou  ─────────────",
                Font = new Font("Arial", 8), ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = false, Size = new Size(420, 18),
                Location = new Point(30, 394),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Botão guest
            btnGuest = new Button
            {
                Text = "Entrar como Convidado (Guest)",
                Location = new Point(30, 418), Size = new Size(400, 40),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(35, 35, 35), ForeColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnGuest.FlatAppearance.BorderSize = 1;
            btnGuest.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btnGuest.Click += (s, e) => EnterAsGuest();

            // Link login
            var lnkLogin = new LinkLabel
            {
                Text = "Já tem conta? Fazer login",
                Font = new Font("Arial", 9),
                Location = new Point(30, 472), AutoSize = true,
                LinkColor = Color.FromArgb(0, 162, 255),
                ActiveLinkColor = Color.White
            };
            lnkLogin.LinkClicked += (s, e) => { Hide(); new LoginForm().Show(); };

            Controls.AddRange(new Control[]
            {
                logo, lblSub, lblUser, txtUsername, lblPass, txtPassword,
                lblConf, txtConfirm, lblStatus, bar, btnRegister, sep, btnGuest, lnkLogin
            });
        }

        // ── Verifica username na API do Roblox ──────────────────
        private async Task CheckUsernameAsync()
        {
            string u = txtUsername.Text.Trim();
            if (u.Length < 3) { SetStatus("", Color.Gray); ValidateForm(); return; }

            // Verifica guest banido
            foreach (var banned in _bannedGuests)
                if (u.Equals(banned, StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus($"❌ '{u}' está banido do Rolox.", Color.OrangeRed);
                    btnRegister.Enabled = false;
                    return;
                }

            SetStatus("Verificando username...", Color.LightBlue);
            var user = await _api.GetUserByUsernameAsync(u);
            if (user != null)
                SetStatus($"❌ '{u}' já está em uso no Roblox.", Color.OrangeRed);
            else
                SetStatus($"✅ '{u}' está disponível!", Color.LimeGreen);

            ValidateForm();
        }

        private void ValidateForm()
        {
            string u    = txtUsername.Text.Trim();
            string pass = txtPassword.Text;
            string conf = txtConfirm.Text;

            // Força da senha
            int strength = 0;
            if (pass.Length >= 8)   strength += 25;
            if (pass.Length >= 12)  strength += 25;
            if (Regex.IsMatch(pass, @"[A-Z]")) strength += 25;
            if (Regex.IsMatch(pass, @"[0-9!@#$%^&*]")) strength += 25;
            bar.Value = strength;
            bar.ForeColor = strength < 50 ? Color.OrangeRed : strength < 75 ? Color.Orange : Color.LimeGreen;

            bool usernameOk = u.Length >= 3 && !IsBannedGuest(u);
            bool passOk     = pass.Length >= 8 && pass.Length <= 200;
            bool confirmOk  = pass == conf;

            if (!passOk && pass.Length > 0)
                SetStatus(pass.Length < 8 ? "Senha muito curta (mínimo 8)." : "Senha muito longa (máximo 200).", Color.OrangeRed);
            else if (!confirmOk && conf.Length > 0)
                SetStatus("As senhas não coincidem.", Color.OrangeRed);

            btnRegister.Enabled = usernameOk && passOk && confirmOk
                && lblStatus.ForeColor == Color.LimeGreen;
        }

        private bool IsBannedGuest(string name)
        {
            foreach (var b in _bannedGuests)
                if (name.Equals(b, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // ── Abre Edge em segundo plano para registrar ───────────
        private async Task DoRegisterAsync()
        {
            btnRegister.Enabled = false;
            SetStatus("Abrindo registro no Roblox...", Color.LightBlue);

            string username = txtUsername.Text.Trim();

            try
            {
                // Abre Edge em segundo plano (minimizado) na página de signup do Roblox
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName  = "msedge",
                    Arguments = $"--new-window --window-state=minimized " +
                                $"\"https://www.roblox.com/account/signupredir?username={Uri.EscapeDataString(username)}\"",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);

                await Task.Delay(2000);
                SetStatus("✅ Página de registro aberta! Complete no Edge e volte aqui.", Color.LimeGreen);

                // Botão para confirmar após registro
                btnRegister.Text      = "✅ Já criei minha conta — Fazer Login";
                btnRegister.BackColor = Color.FromArgb(20, 80, 20);
                btnRegister.Enabled   = true;
                btnRegister.Click    -= null;
                btnRegister.Click    += (s, e) =>
                {
                    Hide();
                    new LoginForm().Show();
                };
            }
            catch
            {
                // Fallback: abre no navegador padrão
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = $"https://www.roblox.com/account/signupredir?username={Uri.EscapeDataString(username)}",
                    UseShellExecute = true
                });
                SetStatus("Registro aberto no navegador.", Color.LightGray);
                btnRegister.Enabled = true;
            }
        }

        // ── Guest local ─────────────────────────────────────────
        private void EnterAsGuest()
        {
            // Gera nome de guest único
            var rng      = new Random();
            string guest = $"Guest_{rng.Next(1000, 9999)}";

            // Garante que não é um guest banido
            while (IsBannedGuest(guest))
                guest = $"Guest_{rng.Next(1000, 9999)}";

            // Cria usuário guest local (sem conta Roblox)
            var guestUser = new RobloxUser
            {
                Id          = -rng.Next(1000, 9999),
                Username    = guest,
                DisplayName = guest,
                Description = "Conta de convidado — funcionalidades limitadas.",
                AvatarUrl   = ""
            };

            SessionService.Save(guest);
            Hide();
            new MainForm(guestUser, new RobloxApiService()).Show();
        }

        private void SetStatus(string msg, Color color)
        {
            if (lblStatus.InvokeRequired) { lblStatus.Invoke(() => SetStatus(msg, color)); return; }
            lblStatus.Text      = msg;
            lblStatus.ForeColor = color;
        }
    }
}
