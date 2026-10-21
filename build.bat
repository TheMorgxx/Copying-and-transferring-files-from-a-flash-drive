@echo off
setlocal EnableExtensions
chcp 65001 >nul
cd /d "%~dp0"

rem ---------- Настройки ----------
rem Главный проект (WinForms-приложение), из которого собирается EXE.
set "APP_PROJECT=ConfigBackupTool\ConfigBackupTool.csproj"
set "APP_EXE=ConfigBackupTool.exe"
rem Версия берётся из version.txt (формат X.Y.Z), если файла нет - 1.0.0
set "APP_VERSION=1.0.0"
if exist "version.txt" set /p APP_VERSION=<version.txt

echo ==========================================
echo   ConfigBackupTool - автоматическая сборка
echo ==========================================
echo.

rem ---------- 1. Проверка инструментов ----------
where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ОШИБКА] dotnet не найден. Установи .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
    goto :fail
)
dotnet --list-sdks | findstr /b /c:"8." >nul
if errorlevel 1 (
    echo [ОШИБКА] Не найден .NET 8 SDK. Установи: https://dotnet.microsoft.com/download/dotnet/8.0
    goto :fail
)
if not exist "%APP_PROJECT%" (
    echo [ОШИБКА] Не найден проект %APP_PROJECT% ^(поправь APP_PROJECT в начале build.bat^).
    goto :fail
)
echo %APP_VERSION%| findstr /r /x "[0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*" >nul
if errorlevel 1 (
    echo [ОШИБКА] В version.txt должна быть версия вида X.Y.Z, сейчас: %APP_VERSION%
    goto :fail
)

rem ---------- 2. Сборка ВСЕХ проектов (*.csproj) в папке, включая новые ----------
echo [1/3] Сборка всех проектов ^(Release^)...
set /a PROJECT_COUNT=0
for /r "%~dp0" %%F in (*.csproj) do (
    set /a PROJECT_COUNT+=1
    echo     - %%~nxF
    dotnet build "%%F" -c Release -nologo -v:minimal
    if errorlevel 1 goto :fail
)
if %PROJECT_COUNT%==0 (
    echo [ОШИБКА] Не найдено ни одного .csproj
    goto :fail
)

rem ---------- 3. Публикация одного EXE (self-contained, .NET на ПК не нужен) ----------
echo.
echo [2/3] Публикация %APP_EXE% версии %APP_VERSION% ^(win-x64, один файл^)... это может занять минуту.
if not exist "release\" mkdir release
dotnet publish "%APP_PROJECT%" -c Release -r win-x64 --self-contained true -nologo -v:minimal -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:Version=%APP_VERSION% -o "release"
if errorlevel 1 goto :fail
if not exist "release\%APP_EXE%" (
    echo [ОШИБКА] После публикации не найден release\%APP_EXE%
    goto :fail
)

rem ---------- 4. Файлы настроек рядом с EXE ----------
echo.
echo [3/3] Копирование games.json в папку release...
rem Существующий games.json не перезаписываем, чтобы не потерять твои правки
if not exist "release\games.json" (
    if exist "games.json" copy /y "games.json" "release\games.json" >nul
)

echo.
echo ==========================================
echo   ГОТОВО: %~dp0release\%APP_EXE%
echo ==========================================
if /i not "%~1"=="nopause" pause
exit /b 0

:fail
echo.
echo ==========================================
echo   СБОРКА НЕ УДАЛАСЬ - смотри ошибки выше
echo ==========================================
if /i not "%~1"=="nopause" pause
exit /b 1
