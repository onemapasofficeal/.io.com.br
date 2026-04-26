#include "RoloxNative.h"
#include <string>
#include <sstream>

// Estado global
static HWND      g_parentHwnd   = nullptr;
static HWND      g_playerHwnd   = nullptr;
static HMODULE   g_playerDll    = nullptr;
static PROCESS_INFORMATION g_pi = {};
static std::wstring g_robloxGoPath;

// Aguarda a janela principal de um processo aparecer
static HWND WaitForProcessWindow(DWORD pid, int timeoutMs = 20000)
{
    HWND found = nullptr;
    int elapsed = 0;
    while (elapsed < timeoutMs)
    {
        Sleep(500);
        elapsed += 500;

        HWND hwnd = nullptr;
        while ((hwnd = FindWindowEx(nullptr, hwnd, nullptr, nullptr)) != nullptr)
        {
            DWORD wndPid = 0;
            GetWindowThreadProcessId(hwnd, &wndPid);
            if (wndPid == pid && IsWindowVisible(hwnd))
            {
                wchar_t title[256] = {};
                GetWindowTextW(hwnd, title, 256);
                // Janela principal do Roblox tem título não vazio
                if (wcslen(title) > 0)
                {
                    found = hwnd;
                    break;
                }
            }
        }
        if (found) break;
    }
    return found;
}

// Remove bordas e barra de título de uma janela
static void StripWindowDecorations(HWND hwnd)
{
    LONG style = GetWindowLong(hwnd, GWL_STYLE);
    style &= ~(WS_CAPTION | WS_THICKFRAME | WS_BORDER | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
    SetWindowLong(hwnd, GWL_STYLE, style);

    LONG exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
    exStyle &= ~(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
    SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

    SetWindowPos(hwnd, nullptr, 0, 0, 0, 0,
        SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
}

extern "C" {

ROLOX_API BOOL RoloxInit(HWND parentHwnd, const wchar_t* robloxGoPath)
{
    g_parentHwnd   = parentHwnd;
    g_robloxGoPath = robloxGoPath;
    return TRUE;
}

ROLOX_API BOOL RoloxLaunchGame(long long placeId, const wchar_t* username)
{
    if (g_robloxGoPath.empty()) return FALSE;

    // Monta o caminho do executável
    std::wstring exePath = g_robloxGoPath + L"\\RobloxPlayerBeta.exe";

    // Verifica se existe
    if (GetFileAttributesW(exePath.c_str()) == INVALID_FILE_ATTRIBUTES)
        return FALSE;

    // Monta argumentos
    std::wostringstream args;
    args << L"\"" << exePath << L"\""
         << L" --app"
         << L" --launchtime=" << GetTickCount64()
         << L" --rloc pt_br"
         << L" --gloc pt_br"
         << L" --channel LIVE"
         << L" roblox://experiences/start?placeId=" << placeId;

    std::wstring argsStr = args.str();
    std::vector<wchar_t> cmdLine(argsStr.begin(), argsStr.end());
    cmdLine.push_back(L'\0');

    STARTUPINFOW si = {};
    si.cb = sizeof(si);

    // Define diretório de trabalho como ROBLOX-GO
    if (!CreateProcessW(
        exePath.c_str(),
        cmdLine.data(),
        nullptr, nullptr,
        FALSE,
        0,
        nullptr,
        g_robloxGoPath.c_str(),
        &si,
        &g_pi))
    {
        return FALSE;
    }

    // Aguarda janela aparecer e embute no painel pai
    g_playerHwnd = WaitForProcessWindow(g_pi.dwProcessId);
    if (!g_playerHwnd) return FALSE;

    StripWindowDecorations(g_playerHwnd);
    SetParent(g_playerHwnd, g_parentHwnd);

    // Redimensiona para preencher o painel pai
    RECT rc;
    GetClientRect(g_parentHwnd, &rc);
    MoveWindow(g_playerHwnd, 0, 0, rc.right, rc.bottom, TRUE);
    ShowWindow(g_playerHwnd, SW_SHOW);

    return TRUE;
}

ROLOX_API void RoloxResize(int width, int height)
{
    if (g_playerHwnd && IsWindow(g_playerHwnd))
        MoveWindow(g_playerHwnd, 0, 0, width, height, TRUE);
}

ROLOX_API void RoloxShutdown()
{
    if (g_pi.hProcess)
    {
        TerminateProcess(g_pi.hProcess, 0);
        CloseHandle(g_pi.hProcess);
        CloseHandle(g_pi.hThread);
        g_pi = {};
    }
    g_playerHwnd = nullptr;
}

ROLOX_API BOOL RoloxIsRunning()
{
    if (!g_pi.hProcess) return FALSE;
    DWORD code = 0;
    GetExitCodeProcess(g_pi.hProcess, &code);
    return code == STILL_ACTIVE ? TRUE : FALSE;
}

} // extern "C"
