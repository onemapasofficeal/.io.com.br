using System;
using System.Drawing;
using System.Windows.Forms;
using RoloxApp.Models;

namespace RoloxApp
{
    public class PremiumForm : Form
    {
        private readonly RobloxUser _user;

        public PremiumForm(RobloxUser user)
        {
            _user = user;
            Text = "Premium — Rolox";
            Size = new Size(560, 520);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            Build();
        }

        private void Build()
        {
            var lblTitle = new Label { Text = "Roblox Premium", Font = new Font("Arial", 18, FontStyle.Bold), ForeColor = Color.FromArgb(255, 196, 0), AutoSize = false, Size = new Size(520, 40), Location = new Point(20, 20), TextAlign = ContentAlignment.MiddleCenter };
            var lblSub = new Label { Text = "Assine o Premium e ganhe Robux mensalmente!", Font = new Font("Arial", 10), ForeColor = Color.LightGray, AutoSize = false, Size = new Size(520, 22), Location = new Point(20, 62), TextAlign = ContentAlignment.MiddleCenter };

            var plans = new[]
            {
                ("Premium 450",  "450 Robux/mês",  "R$ 9,90/mês"),
                ("Premium 1000", "1000 Robux/mês", "R$ 19,90/mês"),
                ("Premium 2200", "2200 Robux/mês", "R$ 39,90/mês"),
            };

            int y = 100;
            foreach (var (name, robux, price) in plans)
            {
                var card = new Panel { Location = new Point(20, y), Size = new Size(520, 70), BackColor = Color.FromArgb(30, 30, 30) };
                var lblName = new Label { Text = name, Font = new Font("Arial", 12, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 10), AutoSize = true };
                var lblRobux = new Label { Text = robux, Font = new Font("Arial", 10), ForeColor = Color.FromArgb(255, 196, 0), Location = new Point(16, 36), AutoSize = true };
                var lblPrice = new Label { Text = price, Font = new Font("Arial", 10), ForeColor = Color.Gray, Location = new Point(300, 24), AutoSize = true };
                var btnSub = new Button { Text = "Assinar", Location = new Point(420, 18), Size = new Size(84, 34), Font = new Font("Arial", 9, FontStyle.Bold), BackColor = Color.FromArgb(255, 196, 0), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
                btnSub.FlatAppearance.BorderSize = 0;
                btnSub.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.roblox.com/premium/membership", UseShellExecute = true });
                card.Controls.AddRange(new Control[] { lblName, lblRobux, lblPrice, btnSub });
                Controls.Add(card);
                y += 80;
            }

            var lblBenefits = new Label { Text = "Benefícios: Robux mensais • 10% bônus em compras • Acesso a itens exclusivos • Vender itens no Mercado", Font = new Font("Arial", 8), ForeColor = Color.Gray, Location = new Point(20, y + 10), Size = new Size(520, 32), AutoEllipsis = true };

            Controls.AddRange(new Control[] { lblTitle, lblSub, lblBenefits });
        }
    }
}
