using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using RoloxApp.Models;

namespace RoloxApp
{
    public class MarketplaceForm : Form
    {
        private readonly RobloxUser _user;
        private FlowLayoutPanel flpItems = null!;
        private TextBox txtSearch = null!;
        private Label lblStatus = null!;
        private static readonly HttpClient _http = new();

        public MarketplaceForm(RobloxUser user)
        {
            _user = user;
            Text = "Mercado — Rolox";
            Size = new Size(960, 660);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            Build();
            _ = LoadAsync("Hat");
        }

        private void Build()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.FromArgb(17, 17, 17) };
            var lblTitle = new Label { Text = "Mercado", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 10), AutoSize = true };

            // Categorias
            string[] cats = { "Hat", "Shirt", "Pants", "Gear", "Face", "Head", "Package" };
            int x = 16;
            foreach (var cat in cats)
            {
                string c = cat;
                var btn = new Button { Text = c, Location = new Point(x, 38), Size = new Size(80, 26), Font = new Font("Arial", 8), BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => _ = LoadAsync(c);
                top.Controls.Add(btn);
                x += 86;
            }

            // Search
            txtSearch = new TextBox { Location = new Point(x + 10, 38), Size = new Size(200, 26), Font = new Font("Arial", 9), BackColor = Color.FromArgb(38, 38, 38), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Buscar no mercado..." };
            txtSearch.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await SearchAsync(txtSearch.Text.Trim()); };
            top.Controls.Add(txtSearch);
            top.Controls.Add(lblTitle);

            lblStatus = new Label { Text = "Carregando...", Font = new Font("Arial", 9), ForeColor = Color.Gray, Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleCenter };

            flpItems = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(23, 23, 23), Padding = new Padding(10) };

            Controls.AddRange(new Control[] { flpItems, lblStatus, top });
        }

        private async Task LoadAsync(string category)
        {
            lblStatus.Invoke(() => lblStatus.Text = $"Carregando {category}...");
            flpItems.Invoke(() => flpItems.Controls.Clear());
            try
            {
                var r = await _http.GetAsync($"https://catalog.roblox.com/v1/search/items?category={category}&limit=30&sortType=Relevance");
                if (r.IsSuccessStatusCode)
                {
                    var j = JObject.Parse(await r.Content.ReadAsStringAsync());
                    var data = j["data"] as JArray;
                    if (data != null)
                    {
                        lblStatus.Invoke(() => lblStatus.Text = $"{data.Count} itens em {category}");
                        foreach (var item in data)
                        {
                            long id = item["id"]?.Value<long>() ?? 0;
                            string name = item["name"]?.Value<string>() ?? "Item";
                            long price = item["price"]?.Value<long>() ?? 0;
                            var card = MakeCard(id, name, price);
                            flpItems.Invoke(() => flpItems.Controls.Add(card));
                        }
                        return;
                    }
                }
            }
            catch { }
            lblStatus.Invoke(() => lblStatus.Text = "Erro ao carregar. Abrindo no Roblox...");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.roblox.com/catalog", UseShellExecute = true });
        }

        private async Task SearchAsync(string q)
        {
            if (string.IsNullOrEmpty(q)) return;
            lblStatus.Invoke(() => lblStatus.Text = $"Buscando '{q}'...");
            flpItems.Invoke(() => flpItems.Controls.Clear());
            try
            {
                var r = await _http.GetAsync($"https://catalog.roblox.com/v1/search/items?keyword={Uri.EscapeDataString(q)}&limit=30");
                if (r.IsSuccessStatusCode)
                {
                    var j = JObject.Parse(await r.Content.ReadAsStringAsync());
                    var data = j["data"] as JArray;
                    if (data != null)
                    {
                        lblStatus.Invoke(() => lblStatus.Text = $"{data.Count} resultados para '{q}'");
                        foreach (var item in data)
                        {
                            long id = item["id"]?.Value<long>() ?? 0;
                            string name = item["name"]?.Value<string>() ?? "Item";
                            long price = item["price"]?.Value<long>() ?? 0;
                            var card = MakeCard(id, name, price);
                            flpItems.Invoke(() => flpItems.Controls.Add(card));
                        }
                    }
                }
            }
            catch { }
        }

        private Panel MakeCard(long id, string name, long price)
        {
            var card = new Panel { Size = new Size(160, 180), BackColor = Color.FromArgb(30, 30, 30), Margin = new Padding(6), Cursor = Cursors.Hand };
            var pic = new PictureBox { Size = new Size(160, 110), Location = new Point(0, 0), BackColor = Color.FromArgb(45, 45, 45), SizeMode = PictureBoxSizeMode.StretchImage };
            if (id > 0) pic.LoadAsync($"https://www.roblox.com/asset-thumbnail/image?assetId={id}&width=150&height=150&format=png");
            var lblName = new Label { Text = name, Font = new Font("Arial", 7, FontStyle.Bold), ForeColor = Color.White, Location = new Point(4, 114), Size = new Size(152, 28), AutoEllipsis = true };
            var lblPrice = new Label { Text = price == 0 ? "Grátis" : $"R$ {price}", Font = new Font("Arial", 8), ForeColor = price == 0 ? Color.LimeGreen : Color.FromArgb(0, 162, 255), Location = new Point(4, 142), AutoSize = true };
            var btnBuy = new Button { Text = "Ver", Location = new Point(4, 158), Size = new Size(152, 18), Font = new Font("Arial", 7), BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBuy.FlatAppearance.BorderSize = 0;
            btnBuy.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = $"https://www.roblox.com/catalog/{id}", UseShellExecute = true });
            card.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = $"https://www.roblox.com/catalog/{id}", UseShellExecute = true });
            card.Controls.AddRange(new Control[] { pic, lblName, lblPrice, btnBuy });
            return card;
        }
    }
}
