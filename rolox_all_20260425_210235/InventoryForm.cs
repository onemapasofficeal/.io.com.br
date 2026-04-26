using System;
using System.Drawing;
using System.Windows.Forms;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    public class InventoryForm : Form
    {
        private readonly RobloxUser _user;
        private readonly RobloxApiService _api;
        private FlowLayoutPanel flpItems = null!;
        private Label lblStatus = null!;

        public InventoryForm(RobloxUser user, RobloxApiService api)
        {
            _user = user; _api = api;
            Text = "Inventário — Rolox";
            Size = new Size(860, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            Build();
            _ = LoadAsync();
        }

        private void Build()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(17, 17, 17) };
            var lblTitle = new Label { Text = "Inventário", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 12), AutoSize = true };
            var lblSub = new Label { Text = $"@{_user.Username}", Font = new Font("Arial", 9), ForeColor = Color.Gray, Location = new Point(16, 32), AutoSize = true };
            top.Controls.AddRange(new Control[] { lblTitle, lblSub });

            lblStatus = new Label { Text = "Carregando...", Font = new Font("Arial", 10), ForeColor = Color.Gray, Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter };

            flpItems = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(23, 23, 23), Padding = new Padding(10) };

            var btnRoblox = new Button { Text = "Ver Inventário Completo no Roblox", Dock = DockStyle.Bottom, Height = 40, Font = new Font("Arial", 10, FontStyle.Bold), BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnRoblox.FlatAppearance.BorderSize = 0;
            btnRoblox.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = $"https://www.roblox.com/users/{_user.Id}/inventory", UseShellExecute = true });

            Controls.AddRange(new Control[] { flpItems, lblStatus, top, btnRoblox });
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var http = new System.Net.Http.HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "Rolox/1.0");
                var r = await http.GetAsync($"https://inventory.roblox.com/v2/users/{_user.Id}/inventory?assetTypes=Hat,Shirt,Pants,Gear&limit=50&sortOrder=Asc");
                if (r.IsSuccessStatusCode)
                {
                    var j = Newtonsoft.Json.Linq.JObject.Parse(await r.Content.ReadAsStringAsync());
                    var data = j["data"] as Newtonsoft.Json.Linq.JArray;
                    if (data != null && data.Count > 0)
                    {
                        lblStatus.Invoke(() => lblStatus.Text = $"{data.Count} itens encontrados");
                        foreach (var item in data)
                        {
                            string name = (item as Newtonsoft.Json.Linq.JObject)?["name"]?.ToString() ?? "Item";
                            var card = MakeCard(name);
                            flpItems.Invoke(() => flpItems.Controls.Add(card));
                        }
                    }
                }
            }
            catch { }
            lblStatus.Invoke(() => lblStatus.Text = "Inventário privado ou sem itens públicos.");
            var btnOpen = new Button { Text = "Abrir no Roblox", Size = new Size(200, 40), Font = new Font("Arial", 10, FontStyle.Bold), BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(20) };
            btnOpen.FlatAppearance.BorderSize = 0;
            btnOpen.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = $"https://www.roblox.com/users/{_user.Id}/inventory", UseShellExecute = true });
            flpItems.Invoke(() => flpItems.Controls.Add(btnOpen));
        }

        private Panel MakeCard(string name)
        {
            var card = new Panel { Size = new Size(140, 140), BackColor = Color.FromArgb(30, 30, 30), Margin = new Padding(6) };
            var pic = new PictureBox { Size = new Size(140, 100), Location = new Point(0, 0), BackColor = Color.FromArgb(45, 45, 45), SizeMode = PictureBoxSizeMode.StretchImage };
            var lbl = new Label { Text = name, Font = new Font("Arial", 7), ForeColor = Color.White, Location = new Point(4, 104), Size = new Size(132, 32), AutoEllipsis = true };
            card.Controls.AddRange(new Control[] { pic, lbl });
            return card;
        }
    }
}
