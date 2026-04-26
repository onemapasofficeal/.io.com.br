using System;
using System.Drawing;
using System.Windows.Forms;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    /// <summary>
    /// Pergunta a idade do usuário na primeira vez e define o modo do Rolox.
    /// </summary>
    public class AgeSelectorForm : Form
    {
        private readonly RobloxUser _user;
        private readonly RobloxApiService _api;
        private NumericUpDown numAge = null!;
        private Label lblMode = null!;
        private Button btnConfirm = null!;

        public AgeSelectorForm(RobloxUser user, RobloxApiService api)
        {
            _user = user;
            _api  = api;
            Text            = "Rolox — Configurar Perfil";
            Size            = new Size(480, 420);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(23, 23, 23);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            ControlBox      = false; // não pode fechar sem confirmar
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            Build();
        }

        private void Build()
        {
            var lblLogo = new Label
            {
                Text = "ROLOX", Font = new Font("Arial", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 162, 255),
                AutoSize = false, Size = new Size(420, 44),
                Location = new Point(20, 18), TextAlign = ContentAlignment.MiddleCenter
            };

            var lblTitle = new Label
            {
                Text = $"Olá, {_user.DisplayName}! Qual é a sua idade?",
                Font = new Font("Arial", 12), ForeColor = Color.White,
                AutoSize = false, Size = new Size(420, 26),
                Location = new Point(20, 68), TextAlign = ContentAlignment.MiddleCenter
            };

            var lblSub = new Label
            {
                Text = "Isso define qual versão do Rolox você vai usar.MAIS VOCÊ AINDA VAI TER O CHAT",
                Font = new Font("Arial", 9), ForeColor = Color.Gray,
                AutoSize = false, Size = new Size(420, 20),
                Location = new Point(20, 96), TextAlign = ContentAlignment.MiddleCenter
            };

            numAge = new NumericUpDown
            {
                Location = new Point(160, 130), Size = new Size(140, 36),
                Font = new Font("Arial", 16), Minimum = 1, Maximum = 120, Value = 13,
                BackColor = Color.FromArgb(38, 38, 38), ForeColor = Color.White,
                TextAlign = HorizontalAlignment.Center, BorderStyle = BorderStyle.FixedSingle
            };
            numAge.ValueChanged += (s, e) => UpdateMode();

            lblMode = new Label
            {
                Text = "", Font = new Font("Arial", 14, FontStyle.Bold),
                AutoSize = false, Size = new Size(420, 36),
                Location = new Point(20, 180), TextAlign = ContentAlignment.MiddleCenter
            };

            // Cards explicativos
            var cards = new[]
            {
                ("Rolox Kid",    "< 9 anos",   "Interface simplificada\nChat com moderação extra",  Color.FromArgb(255, 140, 0)),
                ("Rolox Select", "9–18 anos",  "Todas as funcionalidades\nChat normal",             Color.FromArgb(0, 162, 255)),
                ("Rolox",        "19+ anos",   "Todas as funcionalidades\nSem restrições de idade", Color.FromArgb(0, 200, 100)),
                ("Rolox ADM",    "Admins",     "Painel de administração\nAcesso total",             Color.FromArgb(200, 0, 255)),
            };

            int x = 20;
            foreach (var (name, age, desc, color) in cards)
            {
                var card = new Panel
                {
                    Location = new Point(x, 228), Size = new Size(100, 90),
                    BackColor = Color.FromArgb(30, 30, 30)
                };
                var lblN = new Label { Text = name, Font = new Font("Arial", 7, FontStyle.Bold), ForeColor = color, AutoSize = false, Size = new Size(96, 16), Location = new Point(2, 4), TextAlign = ContentAlignment.MiddleCenter };
                var lblA = new Label { Text = age,  Font = new Font("Arial", 7), ForeColor = Color.Gray, AutoSize = false, Size = new Size(96, 14), Location = new Point(2, 20), TextAlign = ContentAlignment.MiddleCenter };
                var lblD = new Label { Text = desc, Font = new Font("Arial", 6), ForeColor = Color.LightGray, AutoSize = false, Size = new Size(96, 40), Location = new Point(2, 36), TextAlign = ContentAlignment.MiddleCenter };
                card.Controls.AddRange(new Control[] { lblN, lblA, lblD });
                Controls.Add(card);
                x += 108;
            }

            btnConfirm = new Button
            {
                Text = "Confirmar e Entrar",
                Location = new Point(100, 332), Size = new Size(260, 44),
                Font = new Font("Arial", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += (s, e) => Confirm();

            Controls.AddRange(new Control[] { lblLogo, lblTitle, lblSub, numAge, lblMode, btnConfirm });
            UpdateMode();
        }

        private void UpdateMode()
        {
            int age  = (int)numAge.Value;
            var mode = AgeProfileService.GetMode(_user.Username, age);
            string name  = AgeProfileService.GetModeName(mode);
            Color  color = AgeProfileService.GetModeColor(mode);
            lblMode.Text      = $"→ {name}";
            lblMode.ForeColor = color;
            btnConfirm.BackColor = color;
        }

        private void Confirm()
        {
            int age = (int)numAge.Value;
            AgeProfileService.Save(_user.Username, age);
            var mode = AgeProfileService.GetMode(_user.Username, age);
            Hide();
            OpenModeForm(mode);
        }

        private void OpenModeForm(RoloxMode mode)
        {
            switch (mode)
            {
                case RoloxMode.Kid:
                    new MainFormKid(_user, _api).Show();
                    break;
                case RoloxMode.Admin:
                    new AdminForm(_user, _api).Show();
                    break;
                default: // Select e Normal usam o MainForm padrão
                    new MainForm(_user, _api).Show();
                    break;
            }
        }
    }
}
