using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace RoloxApp.Services
{
    public enum RoloxMode
    {
        Kid,     // < 9 anos
        Select,  // 9-18 anos
        Normal,  // 19+ anos
        Admin    // Administrador
    }

    public static class AgeProfileService
    {
        private static readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rolox", "profile.json");

        // Admins conhecidos
        private static readonly string[] _admins = { "0p_409" };

        public static void Save(string username, int age)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                var j = new JObject
                {
                    ["username"] = username,
                    ["age"]      = age,
                    ["mode"]     = GetMode(username, age).ToString()
                };
                File.WriteAllText(_path, j.ToString());
            }
            catch { }
        }

        public static (int Age, RoloxMode Mode) Load(string username)
        {
            try
            {
                if (!File.Exists(_path)) return (-1, RoloxMode.Select);
                var j = JObject.Parse(File.ReadAllText(_path));
                int age = j["age"]?.Value<int>() ?? -1;
                return (age, GetMode(username, age));
            }
            catch { return (-1, RoloxMode.Select); }
        }

        public static RoloxMode GetMode(string username, int age)
        {
            // Admin tem prioridade
            foreach (var a in _admins)
                if (username.Equals(a, StringComparison.OrdinalIgnoreCase))
                    return RoloxMode.Admin;

            if (age < 0)   return RoloxMode.Select; // não definido
            if (age < 9)   return RoloxMode.Kid;
            if (age <= 18) return RoloxMode.Select;
            return RoloxMode.Normal;
        }

        public static bool HasProfile() => File.Exists(_path);

        public static string GetModeName(RoloxMode mode) => mode switch
        {
            RoloxMode.Kid    => "Rolox Kid",
            RoloxMode.Select => "Rolox Select",
            RoloxMode.Normal => "Rolox",
            RoloxMode.Admin  => "Rolox ADM",
            _                => "Rolox"
        };

        public static System.Drawing.Color GetModeColor(RoloxMode mode) => mode switch
        {
            RoloxMode.Kid    => System.Drawing.Color.FromArgb(255, 140, 0),   // laranja
            RoloxMode.Select => System.Drawing.Color.FromArgb(0, 162, 255),   // azul
            RoloxMode.Normal => System.Drawing.Color.FromArgb(0, 200, 100),   // verde
            RoloxMode.Admin  => System.Drawing.Color.FromArgb(200, 0, 255),   // roxo
            _                => System.Drawing.Color.White
        };
    }
}
