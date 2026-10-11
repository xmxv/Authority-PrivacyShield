@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo.
  echo   .NET SDK not found.
  echo   Install the .NET 8 SDK from https://dotnet.microsoft.com/download
  echo   then run this again.
  echo.
  pause
  exit /b 1
)

echo Building PrivacyShield ^(single-file, self-contained^)...
echo.

dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o "%~dp0build"

if errorlevel 1 (
  echo.
  echo   BUILD FAILED. See the messages above.
  echo.
  pause
  exit /b 1
)

echo.
echo   Done. Your executable is here:
echo   %~dp0build\PrivacyShield.exe
echo.
pause
