using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using RoloxApp.Models;

namespace RoloxApp
{
    public class CommunitiesForm : Form
    {
        private readonly RobloxUser _user;
        private FlowLayoutPanel flpGroups = null!;
        private Label lblStatus = null!;
        private static readonly HttpClient _http = new();

        public CommunitiesForm(RobloxUser user)
        {
            _user = user;
            Text = "Comunidades — Rolox";
            Size = new Size(860, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            Build();
            _ = LoadAsync();
        }

        private void Build()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(17, 17, 17) };
            var lblTitle = new Label { Text = "Comunidades", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 12), AutoSize = true };
            var btnCreate = new Button { Text = "+ Criar", Location = new Point(720, 10), Size = new Size(80, 28), Font = new Font("Arial", 9, FontStyle.Bold), BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.roblox.com/groups/create", UseShellExecute = true });
            top.Controls.AddRange(new Control[] { lblTitle, btnCreate });

            lblStatus = new Label { Dock = DockStyle.Top, Height = 24, Font = new Font("Arial", 9), ForeColor = Color.Gray, TextAlign = ContentAlignment.MiddleCenter, Text = "Carregando comunidades..." };

            flpGroups = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(23, 23, 23), Padding = new Padding(10), FlowDirection = FlowDirection.LeftToRight, WrapContents = true };

            Controls.AddRange(new Control[] { flpGroups, lblStatus, top });
        }

        private async Task LoadAsync()
        {
            try
            {
                var r = await _http.GetAsync($"https://groups.roblox.com/v2/users/{_user.Id}/groups/roles");
                if (r.IsSuccessStatusCode)
                {
                    var j = JObject.Parse(await r.Content.ReadAsStringAsync());
                    var data = j["data"] as JArray;
                    if (data != null && data.Count > 0)
                    {
                        lblStatus.Invoke(() => lblStatus.Text = $"{data.Count} comunidades");
                        foreach (var item in data)
                        {
                            var group = item["group"];
                            if (group == null) continue;
                            long gid = group["id"]?.Value<long>() ?? 0;
                            string name = group["name"]?.Value<string>() ?? "Comunidade";
                            string role = item["role"]?["name"]?.Value<string>() ?? "";
                            var card = MakeCard(gid, name, role);
                            flpGroups.Invoke(() => flpGroups.Controls.Add(card));
                        }
                        return;
                    }
                }
            }
            catch { }
            lblStatus.Invoke(() => lblStatus.Text = "Nenhuma comunidade encontrada.");
            var btnOpen = new Button { Text = "Ver Comunidades no Roblox", Size = new Size(260, 40), Font = new Font("Arial", 10, FontStyle.Bold), BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(20) };
            btnOpen.FlatAppearance.BorderSize = 0;
            btnOpen.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.roblox.com/communities", UseShellExecute = true });
            flpGroups.Invoke(() => flpGroups.Controls.Add(btnOpen));
        }

        private Panel MakeCard(long gid, string name, string role)
        {
            var card = new Panel { Size = new Size(240, 90), BackColor = Color.FromArgb(30, 30, 30), Margin = new Padding(8), Cursor = Cursors.Hand };
            var pic = new PictureBox { Size = new Size(60, 60), Location = new Point(10, 15), BackColor = Color.FromArgb(50, 50, 50), SizeMode = PictureBoxSizeMode.StretchImage };
            if (gid > 0) pic.LoadAsync($"https://thumbnails.roblox.com/v1/groups/icons?groupIds={gid}&size=150x150&format=Png");
            var lblName = new Label { Text = name, Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.White, Location = new Point(78, 18), Size = new Size(154, 22), AutoEllipsis = true };
            var lblRole = new Label { Text = role, Font = new Font("Arial", 8), ForeColor = Color.Gray, Location = new Point(78, 42), AutoSize = true };
            EventHandler click = (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = $"https://www.roblox.com/communities/{gid}", UseShellExecute = true });
            card.Click += click; lblName.Click += click;
            card.Controls.AddRange(new Control[] { pic, lblName, lblRole });
            return card;
        }
    }
}
