using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using RoloxApp.Models;
using RoloxApp.Services;

namespace RoloxApp
{
    public class AvatarEditorForm : Form
    {
        private readonly RobloxUser _user;
        private readonly RobloxApiService _api;
        private PictureBox picAvatar = null!;
        private Panel pnlItems = null!;
        private Label lblStatus = null!;
        private static readonly HttpClient _http = new();

        public AvatarEditorForm(RobloxUser user, RobloxApiService api)
        {
            _user = user; _api = api;
            Text = "Editor de Avatar — Rolox";
            Size = new Size(860, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            Build();
            _ = LoadAsync();
        }

        private void Build()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(17, 17, 17) };
            var lblTitle = new Label { Text = "Editor de Avatar", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 12), AutoSize = true };
            top.Controls.Add(lblTitle);

            // Avatar preview
            var pnlLeft = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Color.FromArgb(20, 20, 20) };
            picAvatar = new PictureBox { Size = new Size(180, 180), Location = new Point(20, 20), SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.FromArgb(40, 40, 40) };
            if (!string.IsNullOrEmpty(_user.AvatarUrl)) picAvatar.LoadAsync(_user.AvatarUrl);

            var lblName = new Label { Text = _user.DisplayName, Font = new Font("Arial", 11, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 210), Size = new Size(180, 22), TextAlign = ContentAlignment.MiddleCenter };
            var lblUser = new Label { Text = "@" + _user.Username, Font = new Font("Arial", 9), ForeColor = Color.Gray, Location = new Point(20, 232), Size = new Size(180, 18), TextAlign = ContentAlignment.MiddleCenter };

            var btnEdit = new Button { Text = "Editar no Roblox", Location = new Point(20, 265), Size = new Size(180, 36), Font = new Font("Arial", 9, FontStyle.Bold), BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.roblox.com/my/avatar", UseShellExecute = true });

            pnlLeft.Controls.AddRange(new Control[] { picAvatar, lblName, lblUser, btnEdit });

            // Items panel
            lblStatus = new Label { Dock = DockStyle.Top, Height = 24, Font = new Font("Arial", 9), ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleCenter, Text = "Carregando itens equipados..." };
            pnlItems = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(23, 23, 23) };

            var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(23, 23, 23) };
            pnlRight.Controls.Add(pnlItems);
            pnlRight.Controls.Add(lblStatus);

            Controls.Add(pnlRight);
            Controls.Add(pnlLeft);
            Controls.Add(top);
        }

        private async Task LoadAsync()
        {
            try
            {
                var r = await _http.GetAsync($"https://avatar.roblox.com/v1/users/{_user.Id}/avatar");
                if (r.IsSuccessStatusCode)
                {
                    var j = JObject.Parse(await r.Content.ReadAsStringAsync());
                    var assets = j["assets"] as JArray;
                    if (assets != null)
                    {
                        lblStatus.Invoke(() => lblStatus.Text = $"{assets.Count} itens equipados");
                        int x = 10, y = 10;
                        foreach (var a in assets)
                        {
                            string name = a["name"]?.Value<string>() ?? "Item";
                            string type = a["assetType"]?["name"]?.Value<string>() ?? "";
                            var lbl = new Label { Text = $"• {name} ({type})", Font = new Font("Arial", 9), ForeColor = Color.LightGray, Location = new Point(x, y), AutoSize = true };
                            pnlItems.Invoke(() => pnlItems.Controls.Add(lbl));
                            y += 22;
                        }
                        return;
                    }
                }
            }
            catch { }
            lblStatus.Invoke(() => lblStatus.Text = "Não foi possível carregar itens.");
        }
    }
}
