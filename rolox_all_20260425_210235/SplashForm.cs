using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    public class SplashForm : Form
    {
        private readonly string _username;
        private readonly RobloxApiService _api = new();
        private Label lblMsg = null!;
        private ProgressBar progress = null!;
        private int _seconds = 30;
        private System.Windows.Forms.Timer _timer = null!;
        private RobloxUser? _user;

        public SplashForm(string username)
        {
            _username = username;
            InitUI();
            _ = LoadAsync();
        }

        private void InitUI()
        {
            Text = "Rolox";
            Size = new Size(420, 260);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;

            var lblLogo = new Label
            {
                Text = "ROLOX",
                Font = new Font("Arial", 28, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 162, 255),
                AutoSize = false,
                Size = new Size(380, 55),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 25)
            };

            lblMsg = new Label
            {
                Text = "Carregando sua conta...",
                Font = new Font("Arial", 10),
                ForeColor = Color.LightGray,
                AutoSize = false,
                Size = new Size(380, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 90)
            };

            progress = new ProgressBar
            {
                Location = new Point(30, 130),
                Size = new Size(350, 12),
                Minimum = 0,
                Maximum = 30,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            var lblSub = new Label
            {
                Text = "Criador Principal: Roblox Official  |  Publicador: One Mapas Official",
                Font = new Font("Arial", 7),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = false,
                Size = new Size(380, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 160)
            };

            // Botão trocar conta
            var btnSwitch = new Button
            {
                Text = "Trocar conta",
                Location = new Point(140, 190),
                Size = new Size(130, 28),
                Font = new Font("Arial", 8),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.Gray,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSwitch.FlatAppearance.BorderSize = 0;
            btnSwitch.Click += (s, e) =>
            {
                _timer?.Stop();
                SessionService.Clear();
                Hide();
                new LoginForm().Show();
                Close();
            };

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) =>
            {
                _seconds--;
                progress.Value = 30 - _seconds;
                if (_seconds <= 0) { _timer.Stop(); FinishLoad(); }
            };

            Controls.AddRange(new Control[] { lblLogo, lblMsg, progress, lblSub, btnSwitch });
        }

        private async Task LoadAsync()
        {
            _user = await _api.GetUserByUsernameAsync(_username);
            if (_user != null)
                lblMsg.Text = $"Bem-vindo de volta, {_user.DisplayName}!";
            else
                lblMsg.Text = "Carregando...";

            _seconds = 30;
            _timer.Start();
        }

        private void FinishLoad()
        {
            if (_user == null)
            {
                SessionService.Clear();
                Hide();
                new LoginForm().Show();
                Close();
                return;
            }
            Hide();
            if (!AgeProfileService.HasProfile())
                new AgeSelectorForm(_user, _api).Show();
            else
            {
                var (age, mode) = AgeProfileService.Load(_user.Username);
                switch (mode)
                {
                    case RoloxMode.Kid:   new MainFormKid(_user, _api).Show(); break;
                    case RoloxMode.Admin: new AdminForm(_user, _api).Show(); break;
                    default:              new MainForm(_user, _api).Show(); break;
                }
            }
        }
    }
}
