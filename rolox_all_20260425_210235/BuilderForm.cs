using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoloxBuilder
{
    public class BuilderForm : Form
    {
        private ListBox     lstFiles  = null!;
        private RichTextBox rtbInfo   = null!;
        private Label       lblStatus = null!;
        private string      _rootPath = "";

        public BuilderForm()
        {
            Text          = "Rolox DLL Analyzer";
            Size          = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = Color.FromArgb(18, 18, 18);
            MinimumSize   = new Size(800, 500);
            BuildUI();
            Load += async (s, e) => await AutoLoad();
        }

        private void BuildUI()
        {
            // Top bar
            var top = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(13, 13, 13) };
            var lblTitle = new Label
            {
                Text = "ROLOX  DLL Analyzer",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 162, 255),
                Location = new Point(12, 10), AutoSize = true
            };
            var btnOpen = new Button
            {
                Text = "Abrir pasta...", Location = new Point(780, 8), Size = new Size(130, 30),
                Font = new Font("Arial", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnOpen.FlatAppearance.BorderSize = 0;
            btnOpen.Click += (s, e) => PickFolder();

            var btnExport = new Button
            {
                Text = "Exportar .txt", Location = new Point(920, 8), Size = new Size(150, 30),
                Font = new Font("Arial", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 100, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += (s, e) => ExportReport();
            top.Controls.AddRange(new Control[] { lblTitle, btnOpen, btnExport });

            // Status bar
            lblStatus = new Label
            {
                Dock = DockStyle.Bottom, Height = 24,
                BackColor = Color.FromArgb(10, 10, 10), ForeColor = Color.Gray,
                Font = new Font("Arial", 8), TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0), Text = "Aguardando..."
            };

            // Split
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 260,
                BackColor = Color.FromArgb(18, 18, 18)
            };

            lstFiles = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 22, 22), ForeColor = Color.White,
                Font = new Font("Consolas", 9), BorderStyle = BorderStyle.None
            };
            lstFiles.SelectedIndexChanged += async (s, e) => await ShowInfo();

            rtbInfo = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 18), ForeColor = Color.White,
                Font = new Font("Consolas", 9), ReadOnly = true,
                BorderStyle = BorderStyle.None, WordWrap = false
            };

            split.Panel1.Controls.Add(lstFiles);
            split.Panel2.Controls.Add(rtbInfo);

            Controls.Add(split);
            Controls.Add(lblStatus);
            Controls.Add(top);
        }

        private async Task AutoLoad()
        {
            // Sobe pastas para encontrar ROBLOX-GO
            string? dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 12; i++)
            {
                if (dir == null) break;
                string c = Path.Combine(dir, "ROBLOX-GO");
                if (Directory.Exists(c)) { await LoadFolder(c); return; }
                dir = Path.GetDirectoryName(dir);
            }
            lblStatus.Text = "ROBLOX-GO não encontrado. Use 'Abrir pasta...'";
        }

        private void PickFolder()
        {
            using var dlg = new FolderBrowserDialog { Description = "Selecione a pasta com as DLLs" };
            if (dlg.ShowDialog() == DialogResult.OK)
                _ = LoadFolder(dlg.SelectedPath);
        }

        private async Task LoadFolder(string folder)
        {
            _rootPath = folder;
            lstFiles.Items.Clear();
            rtbInfo.Clear();
            lblStatus.Text = $"Carregando: {folder}";

            var dlls = await Task.Run(() =>
                Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories)
                         .Select(f => f.Replace(folder, "").TrimStart('\\', '/'))
                         .ToArray());

            foreach (var d in dlls) lstFiles.Items.Add(d);
            lblStatus.Text = $"{dlls.Length} DLLs encontradas em {folder}";
        }

        private async Task ShowInfo()
        {
            if (lstFiles.SelectedItem == null) return;
            string rel  = lstFiles.SelectedItem.ToString()!;
            string full = Path.Combine(_rootPath, rel);
            if (!File.Exists(full)) return;

            lblStatus.Text = $"Analisando: {rel}...";
            rtbInfo.Clear();

            var info = await Task.Run(() => PeReader.Read(full));

            if (!string.IsNullOrEmpty(info.Error))
            {
                Append("ERRO: " + info.Error, Color.OrangeRed);
                return;
            }

            Append("═══════════════════════════════════════════════════════", Color.FromArgb(0, 162, 255));
            Append($"  {info.FileName}", Color.White, bold: true);
            Append("═══════════════════════════════════════════════════════", Color.FromArgb(0, 162, 255));
            Append($"  Arquitetura : {info.Machine}");
            Append($"  Tipo        : {(info.IsDll ? "DLL" : "EXE")}");
            Append($"  Subsistema  : {info.Subsystem}");
            Append($"  Compilado   : {info.TimeDateStamp?.ToString("dd/MM/yyyy HH:mm:ss") ?? "?"} UTC");
            Append($"  Tamanho     : {new FileInfo(full).Length / 1024.0:F1} KB");
            Append("");

            Append($"── SEÇÕES ({info.Sections.Count}) ──────────────────────────────────", Color.FromArgb(0, 200, 100));
            foreach (var s in info.Sections) Append("  " + s, Color.LightGreen);
            Append("");

            Append($"── IMPORTS ({info.Imports.Count} DLLs) ─────────────────────────────", Color.FromArgb(255, 180, 0));
            foreach (var imp in info.Imports) Append("  " + imp, Color.Yellow);
            Append("");

            Append($"── EXPORTS ({info.Exports.Count} funções) ──────────────────────────", Color.FromArgb(0, 162, 255));
            foreach (var exp in info.Exports) Append("  " + exp, Color.Cyan);
            Append("");

            if (info.Strings.Count > 0)
            {
                Append($"── STRINGS ({info.Strings.Count}) ──────────────────────────────────", Color.FromArgb(180, 100, 255));
                foreach (var str in info.Strings) Append("  " + str, Color.Plum);
            }

            lblStatus.Text = $"{rel}  |  {info.Exports.Count} exports  |  {info.Imports.Count} imports  |  {info.Sections.Count} seções";
        }

        private void Append(string text, Color? color = null, bool bold = false)
        {
            rtbInfo.SelectionStart  = rtbInfo.TextLength;
            rtbInfo.SelectionLength = 0;
            rtbInfo.SelectionColor  = color ?? Color.LightGray;
            rtbInfo.SelectionFont   = bold
                ? new Font("Consolas", 9, FontStyle.Bold)
                : new Font("Consolas", 9);
            rtbInfo.AppendText(text + "\n");
        }

        private void ExportReport()
        {
            if (lstFiles.Items.Count == 0) { lblStatus.Text = "Nenhuma DLL carregada."; return; }
            using var dlg = new SaveFileDialog { Filter = "Texto|*.txt", FileName = "rolox_dll_report.txt" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"ROLOX DLL ANALYZER — {DateTime.Now}");
            sb.AppendLine($"Pasta: {_rootPath}");
            sb.AppendLine(new string('=', 60));

            foreach (string rel in lstFiles.Items)
            {
                var info = PeReader.Read(Path.Combine(_rootPath, rel));
                sb.AppendLine($"\n[{info.FileName}]");
                if (!string.IsNullOrEmpty(info.Error)) { sb.AppendLine("  ERRO: " + info.Error); continue; }
                sb.AppendLine($"  Arch={info.Machine}  DLL={info.IsDll}  {info.Subsystem}");
                sb.AppendLine($"  Imports: {string.Join(", ", info.Imports)}");
                sb.AppendLine($"  Exports: {string.Join(", ", info.Exports.Take(30))}");
            }

            File.WriteAllText(dlg.FileName, sb.ToString());
            lblStatus.Text = $"Exportado: {dlg.FileName}";
        }
    }
}
