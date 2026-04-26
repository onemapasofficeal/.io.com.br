using System;
using System.IO;
using Microsoft.Win32;

namespace RoloxApp.Services
{
    /// <summary>
    /// Registra/remove o protocolo rolox:// no registro do Windows
    /// e faz o parse de URLs rolox://.
    /// </summary>
    public static class ProtocolService
    {
        private const string ProtocolName = "rolox";

        // ── Registro do protocolo ───────────────────────────────
        public static bool IsRegistered()
        {
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(ProtocolName);
                return key != null;
            }
            catch { return false; }
        }

        /// <summary>
        /// Registra rolox:// apontando para este executável.
        /// Requer elevação (admin) — chama via runas se necessário.
        /// </summary>
        public static bool Register(out string error)
        {
            error = "";
            try
            {
                string exe = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;

                // HKEY_CLASSES_ROOT\rolox
                using var root = Registry.ClassesRoot.CreateSubKey(ProtocolName);
                root.SetValue("",               "URL:Rolox Protocol");
                root.SetValue("URL Protocol",   "");

                // HKEY_CLASSES_ROOT\rolox\DefaultIcon
                using var icon = root.CreateSubKey("DefaultIcon");
                icon.SetValue("", $"\"{exe}\",0");

                // HKEY_CLASSES_ROOT\rolox\shell\open\command
                using var cmd = root.CreateSubKey(@"shell\open\command");
                cmd.SetValue("", $"\"{exe}\" \"%1\"");

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static void Unregister()
        {
            try { Registry.ClassesRoot.DeleteSubKeyTree(ProtocolName, false); }
            catch { }
        }

        // ── Parse de URL rolox:// ───────────────────────────────
        /// <summary>
        /// Faz o parse de uma URL rolox:// e retorna os parâmetros.
        /// Exemplos:
        ///   rolox://placeId=123456
        ///   rolox://placeId=123456&username=Player1
        ///   rolox://communityId=789
        /// </summary>
        public static RoloxUrl? Parse(string url)
        {
            try
            {
                if (!url.StartsWith("rolox://", StringComparison.OrdinalIgnoreCase))
                    return null;

                string body = url.Substring("rolox://".Length).TrimEnd('/');
                var result  = new RoloxUrl();

                foreach (var part in body.Split('&'))
                {
                    var kv = part.Split('=');
                    if (kv.Length != 2) continue;
                    string key = kv[0].ToLowerInvariant();
                    string val = Uri.UnescapeDataString(kv[1]);

                    switch (key)
                    {
                        case "placeid":     result.PlaceId     = long.TryParse(val, out long pid) ? pid : 0; break;
                        case "universeid":  result.UniverseId  = long.TryParse(val, out long uid) ? uid : 0; break;
                        case "communityid":
                        case "groupid":     result.GroupId     = long.TryParse(val, out long gid) ? gid : 0; break;
                        case "username":    result.Username    = val; break;
                        case "launchtime":  result.LaunchTime  = val; break;
                    }
                }

                return result;
            }
            catch { return null; }
        }
    }

    public class RoloxUrl
    {
        public long   PlaceId    { get; set; }
        public long   UniverseId { get; set; }
        public long   GroupId    { get; set; }
        public string Username   { get; set; } = "";
        public string LaunchTime { get; set; } = "";

        public bool IsGame      => PlaceId > 0;
        public bool IsCommunity => GroupId > 0;
    }
}
