using System;
using System.IO;
using System.Text;

namespace RoloxLife
{
    public static class SessionStore
    {
        private static readonly string _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoloxLife", "session.txt");

        public static void Save(string username)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path, Convert.ToBase64String(Encoding.UTF8.GetBytes(username)));
            }
            catch { }
        }

        public static string? Load()
        {
            try
            {
                if (!File.Exists(_path)) return null;
                string raw = File.ReadAllText(_path).Trim();
                return Encoding.UTF8.GetString(Convert.FromBase64String(raw));
            }
            catch { return null; }
        }

        public static void Clear()
        {
            try { if (File.Exists(_path)) File.Delete(_path); }
            catch { }
        }
    }
}
