using System;
using System.Drawing;
using System.Windows.Forms;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    public class AdminForm : Form
    {
        private readonly RobloxUser _user;
        private readonly RobloxApiService _api;
        private RichTextBox rtbLog = null!;

        public AdminForm(RobloxUser user, RobloxApiService api)
        {
            _user = user; _api = api;
            Text          = "Rolox ADM";
            Size          = new Size(1000, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = Color.FromArgb(10, 10, 20);
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            FormClosed += (s, e) => SessionService.Save(_user.Username);
            RoloxAppContext.Instance?.SetMainForm(this);
            Build();
        }

        private void Build()
        {
            // Top bar roxa
            var top = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(80, 0, 120) };
            var lblTitle = new Label { Text = "⚙ ROLOX ADM", Font = new Font("Arial", 15, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 12), AutoSize = true };
            var lblUser  = new Label { Text = $"Admin: {_user.DisplayName} (@{_user.Username})", Font = new Font("Arial", 9), ForeColor = Color.Plum, Location = new Point(300, 16), AutoSize = true };
            var btnRolox = new Button { Text = "Abrir Rolox Normal", Location = new Point(750, 10), Size = new Size(160, 30), Font = new Font("Arial", 9, FontStyle.Bold), BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnRolox.FlatAppearance.BorderSize = 0;
            btnRolox.Click += (s, e) => { Hide(); new MainForm(_user, _api).Show(); };
            top.Controls.AddRange(new Control[] { lblTitle, lblUser, btnRolox });

            // Split: painel esquerdo (ações) + direito (log)
            var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 280, BackColor = Color.FromArgb(10, 10, 20) };

            // ── Painel de ações ──────────────────────────────
            var pnlActions = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(15, 15, 30), AutoScroll = true };

            var sections = new[]
            {
                ("👥 Usuários",    new[] { "Ver todos os usuários", "Banir usuário", "Desbanir usuário", "Ver perfil" }),
                ("🎮 Jogos",       new[] { "Jogos populares", "Jogos reportados", "Remover jogo" }),
                ("💬 Chat",        new[] { "Ver logs do chat", "Banir do chat", "Limpar chat" }),
                ("🖥 Servidores",  new[] { "Ver servidores ativos", "Encerrar servidor", "Ver jogadores" }),
                ("📊 Estatísticas",new[] { "Usuários online", "Jogos ativos", "Relatório geral" }),
            };

            int y = 8;
            foreach (var (section, actions) in sections)
            {
                var lblSec = new Label { Text = section, Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.Plum, Location = new Point(8, y), AutoSize = true };
                pnlActions.Controls.Add(lblSec);
                y += 24;
                foreach (var action in actions)
                {
                    string act = action;
                    var btn = new Button
                    {
                        Text = action, Location = new Point(8, y), Size = new Size(256, 28),
                        Font = new Font("Arial", 9), BackColor = Color.FromArgb(30, 30, 50),
                        ForeColor = Color.LightGray, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleLeft
                    };
                    btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 80);
                    btn.Click += (s, e) => LogAction(act);
                    pnlActions.Controls.Add(btn);
                    y += 32;
                }
                y += 8;
            }

            split.Panel1.Controls.Add(pnlActions);

            // ── Log ──────────────────────────────────────────
            var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 10, 20) };
            var lblLog = new Label { Text = "Log de Ações", Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.Plum, Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
            rtbLog = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(8, 8, 18), ForeColor = Color.LightGray, Font = new Font("Consolas", 9), ReadOnly = true, BorderStyle = BorderStyle.None };
            var btnClear = new Button { Text = "Limpar Log", Dock = DockStyle.Bottom, Height = 30, Font = new Font("Arial", 9), BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.Gray, FlatStyle = FlatStyle.Flat };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => rtbLog.Clear();
            pnlRight.Controls.Add(rtbLog);
            pnlRight.Controls.Add(btnClear);
            pnlRight.Controls.Add(lblLog);
            split.Panel2.Controls.Add(pnlRight);

            Controls.Add(split);
            Controls.Add(top);

            Log($"[{DateTime.Now:HH:mm:ss}] Rolox ADM iniciado por {_user.Username}", Color.Plum);
            Log($"[{DateTime.Now:HH:mm:ss}] Bem-vindo ao painel de administração.", Color.LightGray);
        }

        private void LogAction(string action)
        {
            Log($"[{DateTime.Now:HH:mm:ss}] Ação: {action}", Color.Cyan);
            // Aqui futuramente cada ação abre um form específico
            MessageBox.Show($"Ação: {action}\n\nEm desenvolvimento.", "Rolox ADM", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Log(string msg, Color color)
        {
            rtbLog.SelectionStart  = rtbLog.TextLength;
            rtbLog.SelectionColor  = color;
            rtbLog.AppendText(msg + "\n");
            rtbLog.ScrollToCaret();
        }
    }
}
