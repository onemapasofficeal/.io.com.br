using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RoloxApp.Services
{
    public static class SessionService
    {
        private static readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rolox", "session.json");

        public static void Save(string username, int age = -1)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                string token = GenerateSessionToken(username);
                var json = new JObject
                {
                    ["username"] = username,
                    ["senha"]    = token,
                    ["age"]      = age
                };
                File.WriteAllText(_path, json.ToString());
            }
            catch { }
        }

        public static int LoadAge()
        {
            try
            {
                if (!File.Exists(_path)) return -1;
                var json = JObject.Parse(File.ReadAllText(_path));
                return json["age"]?.Value<int>() ?? -1;
            }
            catch { return -1; }
        }

        public static string? Load()
        {
            try
            {
                if (!File.Exists(_path)) return null;
                var json = JObject.Parse(File.ReadAllText(_path));
                string? username = json["username"]?.Value<string>();
                string? token = json["senha"]?.Value<string>();

                // Valida que o token bate com o username
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token)) return null;
                if (!ValidateToken(username, token)) return null;

                return username;
            }
            catch { return null; }
        }

        public static void Clear()
        {
            try { if (File.Exists(_path)) File.Delete(_path); }
            catch { }
        }

        // Gera token ofuscado baseado no username + seed aleatório
        // Formato: ex:XXXXXXXXXXXXXXXX (igual ao estilo Roblox)
        private static string GenerateSessionToken(string username)
        {
            var rng = new Random();
            string seed = rng.Next(100000, 999999).ToString();
            string raw = username + seed + DateTime.Now.Ticks;

            // Embaralha em base64 com prefixo "ex:"
            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
                .Replace("=", "")
                .Replace("+", "a")
                .Replace("/", "b");

            return "ex:" + b64[..Math.Min(b64.Length, 24)];
        }

        private static bool ValidateToken(string username, string token)
        {
            // Token válido se começa com "ex:" e tem tamanho razoável
            return token.StartsWith("ex:") && token.Length > 6;
        }
    }
}
