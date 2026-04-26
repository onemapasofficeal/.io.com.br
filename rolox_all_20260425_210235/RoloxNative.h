#pragma once
#include <windows.h>

#ifdef ROLOXNATIVE_EXPORTS
#define ROLOX_API __declspec(dllexport)
#else
#define ROLOX_API __declspec(dllimport)
#endif

extern "C" {
    // Inicializa o RobloxPlayerBeta.dll e embute numa janela pai
    ROLOX_API BOOL  RoloxInit(HWND parentHwnd, const wchar_t* robloxGoPath);

    // Lança um jogo pelo placeId dentro da janela embutida
    ROLOX_API BOOL  RoloxLaunchGame(long long placeId, const wchar_t* username);

    // Redimensiona a janela do player
    ROLOX_API void  RoloxResize(int width, int height);

    // Finaliza e libera recursos
    ROLOX_API void  RoloxShutdown();

    // Retorna se o player está rodando
    ROLOX_API BOOL  RoloxIsRunning();
}
