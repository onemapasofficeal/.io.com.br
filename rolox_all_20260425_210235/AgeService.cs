using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RoloxApp.Services
{
    public enum RoloxMode
    {
        Kid,    // < 9 anos
        Select, // 9–18 anos
        Normal, // 19+ anos
        Admin   // administrador
    }

    public static class AgeService
    {
        private static readonly HttpClient _http = new();

        // Admins do Rolox (usernames)
        private static readonly string[] _admins = { "0p_409" };

        public static RoloxMode GetMode(string username, int? age)
        {
            // Verifica admin primeiro
            foreach (var a in _admins)
                if (username.Equals(a, StringComparison.OrdinalIgnoreCase))
                    return RoloxMode.Admin;

            if (age == null) return RoloxMode.Select; // padrão se não souber

            return age switch
            {
                < 9  => RoloxMode.Kid,
                < 19 => RoloxMode.Select,
                _    => RoloxMode.Normal
            };
        }

        /// <summary>
        /// Tenta buscar a idade via API do Roblox.
        /// Retorna null se não conseguir (conta privada ou sem permissão).
        /// </summary>
        public static async Task<int?> GetAgeFromRobloxAsync(long userId)
        {
            try
            {
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("User-Agent", "Rolox/1.0");
                var r = await _http.GetAsync($"https://users.roblox.com/v1/users/{userId}");
                if (!r.IsSuccessStatusCode) return null;
                var j = JObject.Parse(await r.Content.ReadAsStringAsync());
                // created = data de criação da conta (não é a idade real, mas é o que a API pública expõe)
                // A API de birthdate requer autenticação — usamos a data de criação como proxy
                string? created = j["created"]?.Value<string>();
                if (created == null) return null;
                var dt = DateTime.Parse(created);
                // Estimativa: conta criada há X anos → usuário tem pelo menos X anos
                int accountAge = (int)((DateTime.Now - dt).TotalDays / 365);
                return accountAge;
            }
            catch { return null; }
        }

        public static string GetModeName(RoloxMode mode) => mode switch
        {
            RoloxMode.Kid    => "Rolox Kid",
            RoloxMode.Select => "Rolox Select",
            RoloxMode.Normal => "Rolox",
            RoloxMode.Admin  => "Rolox Adm",
            _                => "Rolox"
        };

        public static string GetModeColor(RoloxMode mode) => mode switch
        {
            RoloxMode.Kid    => "#FF6B35", // laranja
            RoloxMode.Select => "#00A2FF", // azul
            RoloxMode.Normal => "#00A2FF", // azul
            RoloxMode.Admin  => "#FF0000", // vermelho
            _                => "#00A2FF"
        };
    }
}
