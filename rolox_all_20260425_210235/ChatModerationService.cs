using System;
using System.Collections.Generic;

namespace RoloxApp.Services
{
    // Moderação progressiva: 1min, 2min, 4min, 8min...
    public class ChatModerationService
    {
        private readonly HashSet<string> _badWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "palavrao","merda","porra","caralho","puta","viado","idiota","imbecil","fdp","vsf"
        };

        // username -> (banUntil, infrações)
        private readonly Dictionary<string, (DateTime BanUntil, int Count)> _bans = new();

        public bool IsBanned(string username)
        {
            if (_bans.TryGetValue(username, out var ban))
                return DateTime.Now < ban.BanUntil;
            return false;
        }

        public TimeSpan GetRemainingBan(string username)
        {
            if (_bans.TryGetValue(username, out var ban) && DateTime.Now < ban.BanUntil)
                return ban.BanUntil - DateTime.Now;
            return TimeSpan.Zero;
        }

        // Retorna true se a mensagem passou, false se foi bloqueada
        public bool CheckMessage(string username, string message, out string reason)
        {
            reason = "";

            if (IsBanned(username))
            {
                var rem = GetRemainingBan(username);
                reason = $"Você está banido do chat por {(int)rem.TotalSeconds}s.";
                return false;
            }

            foreach (var word in _badWords)
            {
                if (message.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    ApplyBan(username);
                    var rem = GetRemainingBan(username);
                    reason = $"Mensagem bloqueada. Banido do chat por {(int)rem.TotalSeconds}s.";
                    return false;
                }
            }

            return true;
        }

        private void ApplyBan(string username)
        {
            int count = 1;
            if (_bans.TryGetValue(username, out var existing))
                count = existing.Count + 1;

            // 1min * 2^(count-1)
            int minutes = (int)Math.Pow(2, count - 1);
            _bans[username] = (DateTime.Now.AddMinutes(minutes), count);
        }
    }
}
