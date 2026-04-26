@echo off
echo Compilando RoloxNative.dll...

set VS_BASE=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools

REM Tenta as duas versoes do MSVC encontradas
set CL=
for %%v in (14.44.35207 14.44.35112) do (
    if exist "%VS_BASE%\VC\Tools\MSVC\%%v\bin\Hostx64\x64\cl.exe" (
        set CL=%VS_BASE%\VC\Tools\MSVC\%%v\bin\Hostx64\x64\cl.exe
        set MSVC_VER=%%v
    )
    if exist "%VS_BASE%\VC\Tools\MSVC\%%v\bin\x64\cl.exe" (
        set CL=%VS_BASE%\VC\Tools\MSVC\%%v\bin\x64\cl.exe
        set MSVC_VER=%%v
    )
)

if "%CL%"=="" (
    echo ERRO: cl.exe nao encontrado.
    pause
    exit /b 1
)

echo Usando cl.exe: %CL%

REM Configura o ambiente
call "%VS_BASE%\VC\Auxiliary\Build\vcvars64.bat" >nul 2>&1

REM Compila
cd /d "%~dp0"

"%CL%" /nologo /EHsc /O2 /LD /DROLOXNATIVE_EXPORTS ^
    /Fe:RoloxNative.dll ^
    RoloxNative.cpp ^
    user32.lib kernel32.lib

if %ERRORLEVEL%==0 (
    echo.
    echo OK: RoloxNative.dll compilada!
    if not exist "..\RoloxApp\bin\Debug\net8.0-windows\" mkdir "..\RoloxApp\bin\Debug\net8.0-windows\"
    copy /Y "RoloxNative.dll" "..\RoloxApp\bin\Debug\net8.0-windows\RoloxNative.dll"
    echo Copiada para o output do projeto.
) else (
    echo ERRO na compilacao.
)
pause
