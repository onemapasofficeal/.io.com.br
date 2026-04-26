using System;
using System.Diagnostics;
using System.IO;

namespace RoloxApp.Services
{
    /// <summary>
    /// Lança o RobloxPlayerBeta.exe do ROBLOX-GO com o placeId recebido via rolox://.
    /// Não usa P/Invoke nem DLL injection — apenas Process.Start com os argumentos corretos.
    /// </summary>
    public static class RoloxLauncher
    {
        // ── Localiza o ROBLOX-GO ────────────────────────────────
        public static string FindRobloxGoPath()
        {
            // 1. Sobe pastas a partir do executável
            string? dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 10; i++)
            {
                if (dir == null) break;
                string c = Path.Combine(dir, "ROBLOX-GO");
                if (Directory.Exists(c)) return c;
                dir = Path.GetDirectoryName(dir);
            }

            // 2. Roblox instalado no sistema (fallback)
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string ver   = Path.Combine(local, "Roblox", "Versions");
            if (Directory.Exists(ver))
                foreach (var d in Directory.GetDirectories(ver))
                {
                    string exe = Path.Combine(d, "RobloxPlayerBeta.exe");
                    if (File.Exists(exe)) return d;
                }

            return "";
        }

        public static string FindExe()
        {
            string path = FindRobloxGoPath();
            if (string.IsNullOrEmpty(path)) return "";
            string exe = Path.Combine(path, "RobloxPlayerBeta.exe");
            return File.Exists(exe) ? exe : "";
        }

        // ── Lança o jogo ────────────────────────────────────────
        /// <summary>
        /// Lança RobloxPlayerBeta.exe com os argumentos de launch do Roblox.
        /// Retorna true se o processo foi iniciado com sucesso.
        /// </summary>
        public static bool Launch(long placeId, string username, out string error)
        {
            error = "";
            string exe = FindExe();

            if (string.IsNullOrEmpty(exe))
            {
                error = "RobloxPlayerBeta.exe não encontrado em ROBLOX-GO.";
                return false;
            }

            try
            {
                long   ts   = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string args = $"--app --launchtime={ts}" +
                              $" --rloc pt_br --gloc pt_br --channel LIVE" +
                              $" --placeId {placeId}" +
                              $" --gameInfo rolox_{placeId}_{ts}";

                var psi = new ProcessStartInfo
                {
                    FileName         = exe,
                    Arguments        = args,
                    WorkingDirectory = Path.GetDirectoryName(exe)!,
                    UseShellExecute  = false
                };

                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // ── Lança via URL rolox:// ───────────────────────────────
        public static bool LaunchFromUrl(string url, out string error)
        {
            error = "";
            var parsed = ProtocolService.Parse(url);
            if (parsed == null)        { error = "URL rolox:// inválida."; return false; }
            if (!parsed.IsGame)        { error = "URL não contém placeId."; return false; }
            return Launch(parsed.PlaceId, parsed.Username, out error);
        }
    }
}
