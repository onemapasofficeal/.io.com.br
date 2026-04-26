using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace RoloxBuilder.NativeCompiler
{
    // Implementação própria em C# puro — sem cl.exe, sem compilador externo
    // Usa Win32 P/Invoke direto para lançar RobloxPlayerBeta.exe e embutir a janela
    public static class PEBuilder
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern bool CreateProcessW(string? app, string cmd,
            IntPtr pa, IntPtr ta, bool inherit, uint flags,
            IntPtr env, string? dir, ref STARTUPINFOW si, out PROCESS_INFORMATION pi);

        [DllImport("kernel32")] static extern bool TerminateProcess(IntPtr h, uint code);
        [DllImport("kernel32")] static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32")] static extern bool GetExitCodeProcess(IntPtr h, out uint code);

        [DllImport("user32")] static extern IntPtr FindWindowEx(IntPtr p, IntPtr a, string? c, string? t);
        [DllImport("user32")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32")] static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32")] static extern bool MoveWindow(IntPtr h, int x, int y, int w, int hh, bool r);
        [DllImport("user32")] static extern int SetWindowLong(IntPtr h, int idx, int val);
        [DllImport("user32")] static extern int GetWindowLong(IntPtr h, int idx);
        [DllImport("user32")] static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32")] static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32", CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr h, StringBuilder s, int max);
        [DllImport("user32")] static extern bool GetClientRect(IntPtr h, out RECT rc);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFOW
        {
            public int cb;
            string? r1, r2, r3;
            public int x, y, w, hh, xc, yc, f, flags;
            public short show, r4;
            public IntPtr r5, r6, r7;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess, hThread;
            public uint pid, tid;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int L, T, R, B; }

        private static IntPtr _parentHwnd;
        private static string _robloxGoPath = "";
        private static PROCESS_INFORMATION _pi;
        private static IntPtr _gameHwnd = IntPtr.Zero;

        public static bool Init(IntPtr parentHwnd, string robloxGoPath)
        {
            _parentHwnd   = parentHwnd;
            _robloxGoPath = robloxGoPath;
            return Directory.Exists(robloxGoPath);
        }

        public static bool LaunchGame(long placeId, string username, out string error)
        {
            error = "";

            string exe = Path.Combine(_robloxGoPath, "RobloxPlayerBeta.exe");
            if (!File.Exists(exe))
            {
                // Tenta Roblox instalado no sistema
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string versions = Path.Combine(localApp, "Roblox", "Versions");
                if (Directory.Exists(versions))
                    foreach (var dir in Directory.GetDirectories(versions))
                    {
                        string c = Path.Combine(dir, "RobloxPlayerBeta.exe");
                        if (File.Exists(c)) { exe = c; break; }
                    }
            }

            if (!File.Exists(exe))
            {
                error = "RobloxPlayerBeta.exe não encontrado.";
                return false;
            }

            string workDir = Path.GetDirectoryName(exe)!;
            string args = $"\"{exe}\" --app --launchtime={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" +
                          $" --rloc pt_br --gloc pt_br --channel LIVE" +
                          $" roblox://experiences/start?placeId={placeId}";

            var si = new STARTUPINFOW { cb = Marshal.SizeOf<STARTUPINFOW>() };
            bool ok = CreateProcessW(null, args, IntPtr.Zero, IntPtr.Zero,
                false, 0, IntPtr.Zero, workDir, ref si, out _pi);

            if (!ok)
            {
                error = "CreateProcess falhou — verifique o RobloxPlayerBeta.exe.";
                return false;
            }

            // Aguarda janela aparecer (máx 25s)
            _gameHwnd = WaitForWindow(_pi.pid, 25000);

            if (_gameHwnd == IntPtr.Zero)
            {
                error = "A janela do Roblox não apareceu no tempo esperado.";
                return false;
            }

            // Remove bordas e barra de título
            int style = GetWindowLong(_gameHwnd, -16);
            style &= ~(0x00C00000 | 0x00040000 | 0x00800000 | 0x00080000 | 0x00020000);
            SetWindowLong(_gameHwnd, -16, style);

            // Embute no painel pai
            SetParent(_gameHwnd, _parentHwnd);
            ShowWindow(_gameHwnd, 3); // SW_MAXIMIZE

            // Redimensiona para preencher o painel
            GetClientRect(_parentHwnd, out RECT rc);
            int w = rc.R - rc.L;
            int h = rc.B - rc.T;
            if (w == 0) w = 1280;
            if (h == 0) h = 720;
            MoveWindow(_gameHwnd, 0, 0, w, h, true);

            return true;
        }

        public static void Resize(int width, int height)
        {
            if (_gameHwnd != IntPtr.Zero && IsWindow(_gameHwnd))
                MoveWindow(_gameHwnd, 0, 0, width, height, true);
        }

        public static void Shutdown()
        {
            if (_pi.hProcess != IntPtr.Zero)
            {
                TerminateProcess(_pi.hProcess, 0);
                CloseHandle(_pi.hProcess);
                CloseHandle(_pi.hThread);
                _pi = default;
            }
            _gameHwnd = IntPtr.Zero;
        }

        public static bool IsRunning()
        {
            if (_pi.hProcess == IntPtr.Zero) return false;
            GetExitCodeProcess(_pi.hProcess, out uint code);
            return code == 259; // STILL_ACTIVE
        }

        [DllImport("user32")] static extern bool IsWindow(IntPtr h);

        private static IntPtr WaitForWindow(uint pid, int timeoutMs)
        {
            int elapsed = 0;
            while (elapsed < timeoutMs)
            {
                Thread.Sleep(500);
                elapsed += 500;

                IntPtr cur = IntPtr.Zero;
                while (true)
                {
                    cur = FindWindowEx(IntPtr.Zero, cur, null, null);
                    if (cur == IntPtr.Zero) break;
                    GetWindowThreadProcessId(cur, out uint wpid);
                    if (wpid == pid && IsWindowVisible(cur))
                    {
                        var sb = new StringBuilder(256);
                        GetWindowText(cur, sb, 256);
                        if (sb.Length > 0) return cur;
                    }
                }
            }
            return IntPtr.Zero;
        }
    }
}
