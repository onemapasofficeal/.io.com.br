using System;
using System.Drawing;
using System.Windows.Forms;
using RoloxApp.Models;

namespace RoloxApp
{
    public class NotificationsForm : Form
    {
        private readonly RobloxUser _user;
        private Panel pnlList = null!;

        public NotificationsForm(RobloxUser user)
        {
            _user = user;
            Text = "Notificações — Rolox";
            Size = new Size(520, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            Build();
        }

        private void Build()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(17, 17, 17) };
            var lblTitle = new Label { Text = "Notificações", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 12), AutoSize = true };
            top.Controls.Add(lblTitle);

            pnlList = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(23, 23, 23) };

            // Notificações simuladas (API de notificações requer auth)
            var items = new[]
            {
                ("🎮", "Seu amigo está jogando Blox Fruits", "Agora"),
                ("👥", "Você recebeu um pedido de amizade", "2 min atrás"),
                ("🏆", "Novo badge desbloqueado!", "1h atrás"),
                ("💬", "Nova mensagem recebida", "3h atrás"),
                ("🎁", "Oferta especial no Mercado", "1 dia atrás"),
            };

            int y = 8;
            foreach (var (icon, msg, time) in items)
            {
                var row = new Panel { Location = new Point(0, y), Size = new Size(500, 52), BackColor = Color.FromArgb(23, 23, 23), Cursor = Cursors.Hand };
                var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(35, 35, 35) };
                var lblIcon = new Label { Text = icon, Font = new Font("Arial", 16), Location = new Point(12, 12), AutoSize = true };
                var lblMsg = new Label { Text = msg, Font = new Font("Arial", 10), ForeColor = Color.White, Location = new Point(48, 8), Size = new Size(380, 22), AutoEllipsis = true };
                var lblTime = new Label { Text = time, Font = new Font("Arial", 8), ForeColor = Color.Gray, Location = new Point(48, 30), AutoSize = true };
                row.Controls.AddRange(new Control[] { lblIcon, lblMsg, lblTime, sep });
                pnlList.Controls.Add(row);
                y += 52;
            }

            var btnAll = new Button { Text = "Ver todas no Roblox", Dock = DockStyle.Bottom, Height = 38, Font = new Font("Arial", 10, FontStyle.Bold), BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnAll.FlatAppearance.BorderSize = 0;
            btnAll.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.roblox.com/my/notifications", UseShellExecute = true });

            Controls.AddRange(new Control[] { pnlList, top, btnAll });
        }
    }
}
