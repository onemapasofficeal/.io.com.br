using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>
/// UmbrelInstaller — Instala uma ISO/IMG no sistema Linux
/// Extrai os arquivos da ISO para /umbrel-boot e configura o GRUB
/// para oferecer a opção de boot na próxima inicialização.
/// 
/// REQUER: Linux + root (sudo) + pacotes: mount, grub2
/// </summary>

if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Este app só roda em Linux.");
    Console.ResetColor();
    return;
}

if (Environment.GetEnvironmentVariable("USER") != "root" &&
    Environment.GetEnvironmentVariable("SUDO_USER") == null)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Execute com sudo: sudo ./UmbrelInstaller");
    Console.ResetColor();
    return;
}

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════╗");
Console.WriteLine("║       UmbrelInstaller v1.0           ║");
Console.WriteLine("║  Instala ISO/IMG com opção de boot   ║");
Console.WriteLine("╚══════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

// ── 1. Seleciona a ISO/IMG ───────────────────────────────────────────────
Console.Write("Caminho da ISO/IMG: ");
string? isoPath = Console.ReadLine()?.Trim().Trim('"');

if (string.IsNullOrEmpty(isoPath) || !File.Exists(isoPath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Arquivo não encontrado: {isoPath}");
    Console.ResetColor();
    return;
}

string isoName = Path.GetFileNameWithoutExtension(isoPath);
Console.WriteLine($"ISO selecionada: {isoName}");
Console.WriteLine();

// ── 2. Cria pasta de destino em /boot ────────────────────────────────────
string bootDir   = $"/boot/umbrel-{isoName}";
string mountDir  = $"/mnt/umbrel-iso-{Guid.NewGuid():N}";

Console.WriteLine($"Criando pasta: {bootDir}");
Directory.CreateDirectory(bootDir);
Directory.CreateDirectory(mountDir);

// ── 3. Monta a ISO e copia os arquivos ───────────────────────────────────
Console.WriteLine("Montando ISO...");
int mountResult = Run("mount", $"-o loop \"{isoPath}\" \"{mountDir}\"");
if (mountResult != 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Falha ao montar a ISO. Verifique se o arquivo é válido.");
    Console.ResetColor();
    Directory.Delete(mountDir, true);
    return;
}

Console.WriteLine("Copiando arquivos da ISO...");
Run("cp", $"-r \"{mountDir}/.\" \"{bootDir}/\"");

Console.WriteLine("Desmontando ISO...");
Run("umount", $"\"{mountDir}\"");
Directory.Delete(mountDir, true);

// ── 4. Encontra vmlinuz e initrd ─────────────────────────────────────────
string? kernel = FindFile(bootDir, "vmlinuz*") ?? FindFile(bootDir, "kernel*");
string? initrd = FindFile(bootDir, "initrd*")  ?? FindFile(bootDir, "initramfs*");

if (kernel == null || initrd == null)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Aviso: kernel ou initrd não encontrado na ISO.");
    Console.WriteLine("A entrada GRUB será criada com caminhos genéricos.");
    Console.ResetColor();
    kernel ??= $"{bootDir}/vmlinuz";
    initrd ??= $"{bootDir}/initrd.img";
}

Console.WriteLine($"Kernel : {kernel}");
Console.WriteLine($"Initrd : {initrd}");

// ── 5. Cria entrada no GRUB ──────────────────────────────────────────────
string grubEntry = $@"
menuentry ""{isoName} (UmbrelInstaller)"" {{
    insmod part_gpt
    insmod ext2
    linux   {kernel} quiet splash
    initrd  {initrd}
}}
";

string grubCustom = "/etc/grub.d/40_custom";
Console.WriteLine($"\nAdicionando entrada ao GRUB: {grubCustom}");

string existing = File.Exists(grubCustom) ? File.ReadAllText(grubCustom) : "#!/bin/sh\nexec tail -n +3 $0\n";
if (!existing.Contains(isoName))
    File.WriteAllText(grubCustom, existing + grubEntry);

Run("chmod", $"+x \"{grubCustom}\"");

// ── 6. Atualiza o GRUB ───────────────────────────────────────────────────
Console.WriteLine("Atualizando GRUB...");
int grubResult = Run("update-grub", "");
if (grubResult != 0)
    grubResult = Run("grub2-mkconfig", "-o /boot/grub2/grub.cfg");

// ── 7. Resultado ─────────────────────────────────────────────────────────
Console.WriteLine();
if (grubResult == 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✅ Instalação concluída!");
    Console.WriteLine();
    Console.WriteLine("Na próxima inicialização aparecerá no menu GRUB:");
    Console.WriteLine($"  → {isoName} (UmbrelInstaller)");
    Console.WriteLine("  → Sistema atual");
    Console.WriteLine();
    Console.WriteLine("Reinicie o PC para ver o menu de boot.");
}
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("⚠️  Arquivos copiados, mas GRUB não foi atualizado automaticamente.");
    Console.WriteLine("Execute manualmente: sudo update-grub");
}
Console.ResetColor();

// ── Helpers ──────────────────────────────────────────────────────────────
static int Run(string cmd, string args)
{
    try
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName               = cmd,
            Arguments              = args,
            UseShellExecute        = false,
            RedirectStandardOutput = false,
            RedirectStandardError  = false
        });
        p?.WaitForExit();
        return p?.ExitCode ?? -1;
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Erro ao executar {cmd}: {ex.Message}");
        Console.ResetColor();
        return -1;
    }
}

static string? FindFile(string dir, string pattern)
{
    try
    {
        var files = Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
        return files.Length > 0 ? files[0] : null;
    }
    catch { return null; }
}
