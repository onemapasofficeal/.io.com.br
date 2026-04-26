using System;
using System.Drawing;
using System.Windows.Forms;
using RoloxApp.Models;

namespace RoloxApp
{
    public class MessagesForm : Form
    {
        private readonly RobloxUser _user;
        private ListBox lstMessages = null!;
        private RichTextBox rtbBody = null!;

        public MessagesForm(RobloxUser user)
        {
            _user = user;
            Text = "Mensagens — Rolox";
            Size = new Size(860, 580);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(23, 23, 23);
            if (RoloxAppContext.AppIcon != null) Icon = RoloxAppContext.AppIcon;
            Build();
        }

        private void Build()
        {
            var top = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(17, 17, 17) };
            var lblTitle = new Label { Text = "Mensagens", Font = new Font("Arial", 14, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 12), AutoSize = true };
            top.Controls.Add(lblTitle);

            var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 260, BackColor = Color.FromArgb(23, 23, 23) };

            lstMessages = new ListBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(22, 22, 22), ForeColor = Color.White, Font = new Font("Arial", 10), BorderStyle = BorderStyle.None };
            lstMessages.Items.Add("📬  Caixa de Entrada");
            lstMessages.Items.Add("📤  Enviadas");
            lstMessages.Items.Add("🗄  Arquivo");
            lstMessages.SelectedIndexChanged += (s, e) => ShowInfo();
            split.Panel1.Controls.Add(lstMessages);

            rtbBody = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(18, 18, 18), ForeColor = Color.LightGray, Font = new Font("Arial", 10), ReadOnly = true, BorderStyle = BorderStyle.None };
            rtbBody.Text = "Selecione uma pasta para ver as mensagens.\n\nAs mensagens são carregadas diretamente do Roblox.";

            var btnOpen = new Button { Text = "Abrir Mensagens no Roblox", Dock = DockStyle.Bottom, Height = 38, Font = new Font("Arial", 10, FontStyle.Bold), BackColor = Color.FromArgb(0, 162, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnOpen.FlatAppearance.BorderSize = 0;
            btnOpen.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.roblox.com/my/messages", UseShellExecute = true });

            split.Panel2.Controls.Add(rtbBody);
            split.Panel2.Controls.Add(btnOpen);

            Controls.Add(split);
            Controls.Add(top);
        }

        private void ShowInfo()
        {
            string[] info = {
                "Caixa de Entrada — suas mensagens recebidas no Rolox/roblox(lixão de atualizações inuteis ).",
                "Mensagens Enviadas — mensagens que você enviou.",
                "Arquivo — mensagens arquivadas."
            };
            int i = lstMessages.SelectedIndex;
            if (i >= 0 && i < info.Length) rtbBody.Text = info[i] + "\n\nClique em 'Abrir Mensagens no Roblox' para ver o conteúdo completo.";
        }
    }
}
