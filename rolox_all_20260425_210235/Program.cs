using System;
using System.Security.Cryptography;
using System.Text;

Console.WriteLine("╔══════════════════════════════════╗");
Console.WriteLine("║      Gerador de Tokens Teste     ║");
Console.WriteLine("╚══════════════════════════════════╝");
Console.WriteLine();

while (true)
{
    Console.WriteLine("1 - Gerar token aleatório");
    Console.WriteLine("2 - Gerar token com expiração (4 min)");
    Console.WriteLine("3 - Verificar se token expirou");
    Console.WriteLine("0 - Sair");
    Console.Write("\nEscolha: ");

    string? op = Console.ReadLine();

    switch (op)
    {
        case "1":
            string token = GerarToken();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nToken: {token}");
            Console.ResetColor();
            break;

        case "2":
            var (tok, exp) = GerarTokenComExpiracao(TimeSpan.FromMinutes(4));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\nToken:    {tok}");
            Console.WriteLine($"Expira:   {exp:HH:mm:ss}");
            Console.WriteLine($"Duração:  4 minutos");
            Console.ResetColor();
            break;

        case "3":
            Console.Write("Cole o token: ");
            string? t = Console.ReadLine();
            if (string.IsNullOrEmpty(t)) break;
            bool expirou = VerificarExpiracao(t);
            Console.ForegroundColor = expirou ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine(expirou ? "❌ Token expirado!" : "✅ Token válido!");
            Console.ResetColor();
            break;

        case "0":
            return;
    }
    Console.WriteLine();
}

static string GerarToken()
{
    byte[] bytes = RandomNumberGenerator.GetBytes(32);
    return "tok_" + Convert.ToBase64String(bytes)
        .Replace("+", "-").Replace("/", "_").Replace("=", "");
}

static (string token, DateTime expiracao) GerarTokenComExpiracao(TimeSpan duracao)
{
    DateTime exp    = DateTime.UtcNow.Add(duracao);
    long     ticks  = exp.Ticks;
    byte[]   random = RandomNumberGenerator.GetBytes(16);

    // Embute o timestamp no token
    byte[] tickBytes = BitConverter.GetBytes(ticks);
    byte[] payload   = new byte[tickBytes.Length + random.Length];
    Buffer.BlockCopy(tickBytes, 0, payload, 0,              tickBytes.Length);
    Buffer.BlockCopy(random,   0, payload, tickBytes.Length, random.Length);

    // HMAC para integridade
    byte[] key  = Encoding.UTF8.GetBytes("RoloxStudio_2026");
    using var h = new HMACSHA256(key);
    byte[] mac  = h.ComputeHash(payload);

    byte[] final = new byte[payload.Length + 8];
    Buffer.BlockCopy(payload, 0, final, 0,              payload.Length);
    Buffer.BlockCopy(mac,     0, final, payload.Length, 8); // só 8 bytes do MAC

    string token = "rtok_" + Convert.ToBase64String(final)
        .Replace("+", "-").Replace("/", "_").Replace("=", "");

    return (token, exp);
}

static bool VerificarExpiracao(string token)
{
    try
    {
        if (!token.StartsWith("rtok_")) return true;
        string b64 = token[5..].Replace("-", "+").Replace("_", "/");
        int pad = b64.Length % 4;
        if (pad > 0) b64 += new string('=', 4 - pad);

        byte[] final    = Convert.FromBase64String(b64);
        byte[] tickBytes = final[..8];
        long   ticks    = BitConverter.ToInt64(tickBytes);
        DateTime exp    = new DateTime(ticks, DateTimeKind.Utc);

        return DateTime.UtcNow > exp;
    }
    catch { return true; }
}
