using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RoloxApp.Models;

namespace RoloxApp.Services
{
    public class RobloxApiService
    {
        private static readonly HttpClient _http = new HttpClient();

        static RobloxApiService()
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "Rolox/1.0");
        }

        public async Task<RobloxUser?> GetUserByUsernameAsync(string username)
        {
            try
            {
                var body = new StringContent(
                    $"{{\"usernames\":[\"{username}\"],\"excludeBannedUsers\":false}}",
                    System.Text.Encoding.UTF8, "application/json");
                var resp = await _http.PostAsync("https://users.roblox.com/v1/usernames/users", body);
                if (!resp.IsSuccessStatusCode) return null;
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                var data = json["data"]?.FirstOrDefault();
                if (data == null) return null;
                long id = data["id"]!.Value<long>();
                return await GetUserByIdAsync(id);
            }
            catch { return null; }
        }

        public async Task<RobloxUser?> GetUserByIdAsync(long userId)
        {
            try
            {
                var resp = await _http.GetAsync($"https://users.roblox.com/v1/users/{userId}");
                if (!resp.IsSuccessStatusCode) return null;
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                var user = new RobloxUser
                {
                    Id = json["id"]!.Value<long>(),
                    Username = json["name"]!.Value<string>()!,
                    DisplayName = json["displayName"]!.Value<string>()!,
                    Description = json["description"]?.Value<string>() ?? ""
                };
                user.AvatarUrl = await GetAvatarUrlAsync(userId);
                return user;
            }
            catch { return null; }
        }

        public async Task<string> GetAvatarUrlAsync(long userId)
        {
            try
            {
                var resp = await _http.GetAsync(
                    $"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userId}&size=150x150&format=Png");
                if (!resp.IsSuccessStatusCode) return "";
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                return json["data"]?[0]?["imageUrl"]?.Value<string>() ?? "";
            }
            catch { return ""; }
        }

        public async Task<List<RobloxUser>> GetFriendsAsync(long userId)
        {
            var friends = new List<RobloxUser>();
            try
            {
                var resp = await _http.GetAsync($"https://friends.roblox.com/v1/users/{userId}/friends");
                if (!resp.IsSuccessStatusCode) return friends;
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                var data = json["data"] as JArray;
                if (data == null) return friends;

                // Coleta IDs para buscar nomes em batch
                var ids = new List<long>();
                foreach (var f in data)
                {
                    long fid = f["id"]?.Value<long>() ?? 0;
                    if (fid > 0) ids.Add(fid);
                }

                // Busca nomes reais em batch (até 100 por vez)
                var nameMap = new Dictionary<long, (string Username, string DisplayName)>();
                for (int i = 0; i < ids.Count; i += 100)
                {
                    var batch = ids.GetRange(i, Math.Min(100, ids.Count - i));
                    try
                    {
                        var batchBody = new StringContent(
                            "{\"userIds\":[" + string.Join(",", batch) + "],\"excludeBannedUsers\":false}",
                            System.Text.Encoding.UTF8, "application/json");
                        var batchResp = await _http.PostAsync("https://users.roblox.com/v1/users", batchBody);
                        if (batchResp.IsSuccessStatusCode)
                        {
                            var bj = JObject.Parse(await batchResp.Content.ReadAsStringAsync());
                            foreach (var u in bj["data"] as JArray ?? new JArray())
                            {
                                long uid = u["id"]?.Value<long>() ?? 0;
                                string uname = u["name"]?.Value<string>() ?? "";
                                string dname = u["displayName"]?.Value<string>() ?? uname;
                                if (uid > 0) nameMap[uid] = (uname, string.IsNullOrWhiteSpace(dname) ? uname : dname);
                            }
                        }
                    }
                    catch { }
                }

                // Busca avatares em batch
                var avatarMap = await GetAvatarsBatchAsync(ids);

                foreach (var f in data)
                {
                    long fid = f["id"]?.Value<long>() ?? 0;
                    if (fid == 0) continue;

                    nameMap.TryGetValue(fid, out var names);
                    string username = names.Username;
                    string display  = names.DisplayName;

                    // Fallback para o que veio na resposta de amigos
                    if (string.IsNullOrWhiteSpace(username))
                        username = f["name"]?.Value<string>() ?? fid.ToString();
                    if (string.IsNullOrWhiteSpace(display))
                        display = f["displayName"]?.Value<string>() ?? username;

                    avatarMap.TryGetValue(fid, out string? avatarUrl);

                    friends.Add(new RobloxUser
                    {
                        Id = fid,
                        Username = username,
                        DisplayName = display,
                        AvatarUrl = avatarUrl ?? ""
                    });
                }
            }
            catch { }
            return friends;
        }

        private async Task<Dictionary<long, string>> GetAvatarsBatchAsync(List<long> ids)
        {
            var map = new Dictionary<long, string>();
            if (ids.Count == 0) return map;
            try
            {
                string joined = string.Join(",", ids);
                var resp = await _http.GetAsync(
                    $"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={joined}&size=150x150&format=Png");
                if (!resp.IsSuccessStatusCode) return map;
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                foreach (var item in json["data"] as JArray ?? new JArray())
                {
                    long uid = item["targetId"]?.Value<long>() ?? 0;
                    string url = item["imageUrl"]?.Value<string>() ?? "";
                    if (uid > 0 && !string.IsNullOrEmpty(url)) map[uid] = url;
                }
            }
            catch { }
            return map;
        }

        // Busca sorts da explore-api (pública, sem auth) — retorna jogos reais
        // sortIndex: 1 = Recomendados, 2 = Revelações, 3 = Populares, etc.
        private async Task<List<RobloxGame>> GetExploreSortAsync(int sortIndex)
        {
            var games = new List<RobloxGame>();
            try
            {
                var resp = await _http.GetAsync(
                    "https://apis.roblox.com/explore-api/v1/get-sorts?sessionId=rolox");
                if (!resp.IsSuccessStatusCode) return games;

                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                var sorts = json["sorts"] as JArray;
                if (sorts == null || sorts.Count <= sortIndex) return games;

                var sort = sorts[sortIndex];
                var gamesArr = sort["games"] as JArray;
                if (gamesArr == null) return games;

                foreach (var g in gamesArr)
                {
                    long uid = g["universeId"]?.Value<long>() ?? 0;
                    if (uid == 0) continue;
                    games.Add(new RobloxGame
                    {
                        UniverseId  = uid,
                        PlaceId     = g["placeId"]?.Value<long>() ?? uid,
                        Name        = g["name"]?.Value<string>() ?? "Jogo",
                        PlayerCount = g["playerCount"]?.Value<int>() ?? 0,
                        Creator     = "Roblox Official",
                        Publisher   = "One Mapas Official"
                    });
                }
            }
            catch { }
            return games;
        }

        public async Task<List<RobloxGame>> GetRecommendedGamesAsync()
        {
            var games = await GetExploreSortAsync(1); // sort index 1 = Recomendados
            if (games.Count == 0) return await GetPopularGamesAsync();
            await LoadGameThumbnailsAsync(games);
            return games;
        }

        public async Task<List<RobloxGame>> GetContinueGamesAsync()
        {
            var games = await GetExploreSortAsync(3); // sort index 3 = diferente dos recomendados
            if (games.Count == 0)
            {
                var pop = await GetPopularGamesAsync();
                return pop.Count > 4 ? pop.GetRange(pop.Count / 2, pop.Count - pop.Count / 2) : pop;
            }
            await LoadGameThumbnailsAsync(games);
            return games;
        }

        public async Task<List<RobloxGame>> GetPopularGamesAsync()
        {
            // Tenta explore-api em múltiplos sorts
            for (int i = 0; i <= 4; i++)
            {
                var games = await GetExploreSortAsync(i);
                if (games.Count > 0)
                {
                    await LoadGameThumbnailsAsync(games);
                    return games;
                }
            }

            // Fallback: games list API pública
            try
            {
                var resp = await _http.GetAsync("https://games.roblox.com/v1/games/list?sortToken=&gameFilter=default&startRows=0&maxRows=20");
                if (resp.IsSuccessStatusCode)
                {
                    var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                    var list = new List<RobloxGame>();
                    foreach (var g in json["games"] as JArray ?? new JArray())
                    {
                        list.Add(new RobloxGame
                        {
                            PlaceId     = g["placeId"]?.Value<long>() ?? 0,
                            UniverseId  = g["universeId"]?.Value<long>() ?? 0,
                            Name        = g["name"]?.Value<string>() ?? "Jogo",
                            PlayerCount = g["playerCount"]?.Value<int>() ?? 0,
                            Creator     = "Roblox Official",
                            Publisher   = "One Mapas Official"
                        });
                    }
                    if (list.Count > 0)
                    {
                        await LoadGameThumbnailsAsync(list);
                        return list;
                    }
                }
            }
            catch { }

            return new List<RobloxGame>();
        }

        private async Task LoadGameThumbnailsAsync(List<RobloxGame> games, string thumbBase = "https://thumbnails.roblox.com")
        {
            try
            {
                var ids = string.Join(",", games.ConvertAll(g => g.UniverseId));
                var resp = await _http.GetAsync(
                    $"{thumbBase}/v1/games/multiget/thumbnails?universeIds={ids}&size=768x432&format=Png&isCircular=false");
                if (!resp.IsSuccessStatusCode) return;
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                var data = json["data"] as JArray;
                if (data == null) return;
                foreach (var item in data)
                {
                    long uid = item["universeId"]?.Value<long>() ?? 0;
                    string url = item["thumbnails"]?[0]?["imageUrl"]?.Value<string>() ?? "";
                    var game = games.Find(g => g.UniverseId == uid);
                    if (game != null) game.ThumbnailUrl = url;
                }
            }
            catch { }
        }
    }
}
