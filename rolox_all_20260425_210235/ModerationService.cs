using System;
using System.Collections.Generic;

namespace RoloxLife
{
    public class ModerationService
    {
        private readonly HashSet<string> _bad = new(StringComparer.OrdinalIgnoreCase)
        {
            "palavrao","merda","porra","caralho","puta","viado","idiota","imbecil","fdp","vsf"
        };

        private readonly Dictionary<string, (DateTime Until, int Count)> _bans = new();

        public bool IsBanned(string user) =>
            _bans.TryGetValue(user, out var b) && DateTime.Now < b.Until;

        public TimeSpan Remaining(string user) =>
            _bans.TryGetValue(user, out var b) && DateTime.Now < b.Until
                ? b.Until - DateTime.Now : TimeSpan.Zero;

        public bool Check(string user, string msg, out string reason)
        {
            reason = "";
            if (IsBanned(user))
            {
                reason = $"Banido por {(int)Remaining(user).TotalSeconds}s.";
                return false;
            }
            foreach (var w in _bad)
            {
                if (msg.Contains(w, StringComparison.OrdinalIgnoreCase))
                {
                    int count = _bans.TryGetValue(user, out var b) ? b.Count + 1 : 1;
                    int mins = (int)Math.Pow(2, count - 1);
                    _bans[user] = (DateTime.Now.AddMinutes(mins), count);
                    reason = $"Mensagem bloqueada. Banido por {mins * 60}s.";
                    return false;
                }
            }
            return true;
        }
    }
}
