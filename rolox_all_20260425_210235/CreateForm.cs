using System;
using System.Drawing;
using System.Windows.Forms;
using RoloxApp.Models;

namespace RoloxApp
{
    public class CreateForm : Form
    {
        private readonly RobloxUser _user;

        public CreateForm(RobloxUser user)
        {
            _user = user;
            Text = "Criar — Rolox";
            Size = new Size(760, 560);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            Build();
        }

        private void Build()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(17, 17, 17) };
            var lblTitle = new Label { Text = "Criar", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 14), AutoSize = true };
            top.Controls.Add(lblTitle);

            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(23, 23, 23), Padding = new Padding(16), FlowDirection = FlowDirection.LeftToRight, WrapContents = true };

            var items = new[]
            {
                ("🎮", "Experiências",    "Crie e publique jogos no Roblox Studio", "https://create.roblox.com/dashboard/creations"),
                ("👕", "Roupas",          "Crie camisas, calças e acessórios",       "https://create.roblox.com/dashboard/creations?activeTab=ClassicShirt"),
                ("🎵", "Áudio",           "Faça upload de músicas e sons",           "https://create.roblox.com/dashboard/creations?activeTab=Audio"),
                ("🖼", "Decalques",       "Faça upload de imagens e texturas",       "https://create.roblox.com/dashboard/creations?activeTab=Decal"),
                ("🏷", "Passes de Jogo", "Crie passes para seus jogos",             "https://create.roblox.com/dashboard/creations?activeTab=GamePass"),
                ("💰", "Produtos Dev",   "Crie produtos para seus jogos",           "https://create.roblox.com/dashboard/creations?activeTab=DeveloperProduct"),
                ("📊", "Analytics",      "Veja estatísticas dos seus jogos",        "https://create.roblox.com/dashboard/analytics"),
                ("💵", "Ganhos",         "Veja seus ganhos em Robux",               "https://create.roblox.com/dashboard/robux"),
            };

            foreach (var (icon, title, desc, url) in items)
            {
                string u = url;
                var card = new Panel { Size = new Size(200, 130), BackColor = Color.FromArgb(30, 30, 30), Margin = new Padding(8), Cursor = Cursors.Hand };
                var lblIcon = new Label { Text = icon, Font = new Font("Arial", 22), ForeColor = Color.White, AutoSize = false, Size = new Size(200, 50), Location = new Point(0, 10), TextAlign = ContentAlignment.MiddleCenter };
                var lblName = new Label { Text = title, Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = Color.White, AutoSize = false, Size = new Size(200, 20), Location = new Point(0, 62), TextAlign = ContentAlignment.MiddleCenter };
                var lblDesc = new Label { Text = desc, Font = new Font("Arial", 7), ForeColor = Color.Gray, AutoSize = false, Size = new Size(196, 30), Location = new Point(2, 84), TextAlign = ContentAlignment.TopCenter, AutoEllipsis = true };
                EventHandler click = (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = u, UseShellExecute = true });
                card.Click += click; lblIcon.Click += click; lblName.Click += click;
                card.Controls.AddRange(new Control[] { lblIcon, lblName, lblDesc });
                flow.Controls.Add(card);
            }

            Controls.AddRange(new Control[] { flow, top });
        }
    }
}
