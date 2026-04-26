using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RoloxLife
{
    public class UserInfo
    {
        public long Id { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string AvatarUrl { get; set; } = "";
    }

    public class GameInfo
    {
        public long PlaceId { get; set; }
        public long UniverseId { get; set; }
        public string Name { get; set; } = "";
        public int Players { get; set; }
        public string Thumb { get; set; } = "";
    }

    public static class ApiService
    {
        private static readonly HttpClient _http = new();

        static ApiService()
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "RoloxLife/1.0");
        }

        public static async Task<UserInfo?> GetUserAsync(string username)
        {
            try
            {
                var body = new StringContent(
                    $"{{\"usernames\":[\"{username}\"],\"excludeBannedUsers\":false}}",
                    System.Text.Encoding.UTF8, "application/json");
                var r = await _http.PostAsync("https://users.roblox.com/v1/usernames/users", body);
                if (!r.IsSuccessStatusCode) return null;
                var j = JObject.Parse(await r.Content.ReadAsStringAsync());
                var d = j["data"]?.FirstOrDefault();
                if (d == null) return null;

                long id = d["id"]!.Value<long>();
                string display = d["displayName"]?.Value<string>() ?? username;

                string avatar = await GetAvatarAsync(id);
                return new UserInfo { Id = id, Username = username, DisplayName = display, AvatarUrl = avatar };
            }
            catch { return null; }
        }

        public static async Task<string> GetAvatarAsync(long userId)
        {
            try
            {
                var r = await _http.GetAsync(
                    $"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userId}&size=150x150&format=Png");
                if (!r.IsSuccessStatusCode) return "";
                var j = JObject.Parse(await r.Content.ReadAsStringAsync());
                return j["data"]?[0]?["imageUrl"]?.Value<string>() ?? "";
            }
            catch { return ""; }
        }

        public static async Task<List<GameInfo>> GetGamesAsync()
        {
            // Tenta buscar da explore-api (pública, retorna jogos reais e dinâmicos)
            for (int sortIndex = 0; sortIndex <= 4; sortIndex++)
            {
                var games = await GetExploreSortAsync(sortIndex);
                if (games.Count > 0) return games;
            }

            // Fallback: games list API pública
            try
            {
                var r = await _http.GetAsync("https://games.roblox.com/v1/games/list?sortToken=&gameFilter=default&startRows=0&maxRows=20");
                if (r.IsSuccessStatusCode)
                {
                    var j = Newtonsoft.Json.Linq.JObject.Parse(await r.Content.ReadAsStringAsync());
                    var list = new List<GameInfo>();
                    foreach (var g in j["games"] as Newtonsoft.Json.Linq.JArray ?? new Newtonsoft.Json.Linq.JArray())
                    {
                        list.Add(new GameInfo
                        {
                            PlaceId    = g["placeId"]?.Value<long>() ?? 0,
                            UniverseId = g["universeId"]?.Value<long>() ?? 0,
                            Name       = g["name"]?.Value<string>() ?? "Jogo",
                            Players    = g["playerCount"]?.Value<int>() ?? 0
                        });
                    }
                    if (list.Count > 0) return list;
                }
            }
            catch { }

            return new List<GameInfo>();
        }

        public static async Task<List<GameInfo>> GetExploreSortAsync(int sortIndex)
        {
            var games = new List<GameInfo>();
            try
            {
                var resp = await _http.GetAsync(
                    "https://apis.roblox.com/explore-api/v1/get-sorts?sessionId=rolox");
                if (!resp.IsSuccessStatusCode) return games;

                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                var sorts = json["sorts"] as JArray;
                if (sorts == null || sorts.Count <= sortIndex) return games;

                var gamesArr = sorts[sortIndex]["games"] as JArray;
                if (gamesArr == null) return games;

                var uids = new List<long>();
                var tempGames = new List<GameInfo>();

                foreach (var g in gamesArr)
                {
                    long uid = g["universeId"]?.Value<long>() ?? 0;
                    if (uid == 0) continue;
                    var gi = new GameInfo
                    {
                        UniverseId = uid,
                        PlaceId    = g["placeId"]?.Value<long>() ?? uid,
                        Name       = g["name"]?.Value<string>() ?? "Jogo",
                        Players    = g["playerCount"]?.Value<int>() ?? 0
                    };
                    tempGames.Add(gi);
                    uids.Add(uid);
                }

                // Thumbnails em batch
                if (uids.Count > 0)
                {
                    try
                    {
                        string ids = string.Join(",", uids);
                        var tr = await _http.GetAsync(
                            $"https://thumbnails.roblox.com/v1/games/multiget/thumbnails?universeIds={ids}&size=768x432&format=Png&isCircular=false");
                        if (tr.IsSuccessStatusCode)
                        {
                            var tj = JObject.Parse(await tr.Content.ReadAsStringAsync());
                            foreach (var item in tj["data"] as JArray ?? new JArray())
                            {
                                long uid2 = item["universeId"]?.Value<long>() ?? 0;
                                string url = item["thumbnails"]?[0]?["imageUrl"]?.Value<string>() ?? "";
                                var match = tempGames.Find(g => g.UniverseId == uid2);
                                if (match != null) match.Thumb = url;
                            }
                        }
                    }
                    catch { }
                }

                games = tempGames;
            }
            catch { }
            return games;
        }

    }
}
