@echo off
setlocal EnableExtensions
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] .NET 10 SDK is not installed.
  echo Install with: winget install Microsoft.DotNet.SDK.10
  pause
  exit /b 1
)

dotnet --list-sdks | findstr /b /c:"10." >nul
if errorlevel 1 (
  echo [ERROR] .NET 10 SDK is required to build this release.
  echo Install with: winget install Microsoft.DotNet.SDK.10
  pause
  exit /b 1
)

echo.
echo =====================================================
echo Football Logo Downloader v2.0.0 - Secure Windows Build
echo =====================================================
echo.

if exist "publish" rmdir /s /q "publish"
if exist "release" rmdir /s /q "release"
if exist "src\FootballLogoDownloader\bin" rmdir /s /q "src\FootballLogoDownloader\bin"
if exist "src\FootballLogoDownloader\obj" rmdir /s /q "src\FootballLogoDownloader\obj"
mkdir "release" >nul 2>nul

echo [1/4] Restoring project and running NuGet audit...
dotnet restore "src\FootballLogoDownloader\FootballLogoDownloader.csproj"
if errorlevel 1 goto :fail

dotnet list "src\FootballLogoDownloader\FootballLogoDownloader.csproj" package --vulnerable --include-transitive
if errorlevel 1 goto :fail

echo.
echo [2/4] Publishing self-contained Windows application...
dotnet publish "src\FootballLogoDownloader\FootballLogoDownloader.csproj" -c Release -r win-x64 --self-contained true --no-restore -o "publish"
if errorlevel 1 goto :fail

if not exist "publish\Football Logo Downloader.exe" (
  echo [ERROR] Build finished but the EXE was not found.
  goto :fail
)

echo.
echo [3/4] Generating SHA-256 checksum...
powershell.exe -NoProfile -Command "$ErrorActionPreference='Stop'; $p=(Resolve-Path 'publish\Football Logo Downloader.exe').Path; $h=(Get-FileHash -Algorithm SHA256 -LiteralPath $p).Hash.ToLowerInvariant(); Set-Content -Encoding ascii -LiteralPath 'publish\SHA256SUMS.txt' -Value ($h + '  Football Logo Downloader.exe')"
if errorlevel 1 goto :fail

echo.
echo [4/4] Preparing release assets...
copy /y "publish\Football Logo Downloader.exe" "release\Football Logo Downloader.exe" >nul
copy /y "publish\LICENSE.txt" "release\LICENSE.txt" >nul
copy /y "publish\THIRD_PARTY_NOTICE.txt" "release\THIRD_PARTY_NOTICE.txt" >nul
copy /y "publish\SHA256SUMS.txt" "release\SHA256SUMS.txt" >nul
copy /y "publish\README.txt" "release\README.txt" >nul

powershell.exe -NoProfile -Command "$ErrorActionPreference='Stop'; Compress-Archive -Path 'publish\Football Logo Downloader.exe','publish\LICENSE.txt','publish\THIRD_PARTY_NOTICE.txt','publish\SHA256SUMS.txt','publish\README.txt' -DestinationPath 'release\Football-Logo-Downloader-v2.0.0-win-x64.zip' -Force"
if errorlevel 1 goto :fail

echo.
echo =====================================================
echo BUILD SUCCESSFUL
echo =====================================================
echo PUBLISH EXE: %~dp0publish\Football Logo Downloader.exe
echo HASH: %~dp0publish\SHA256SUMS.txt
echo RELEASE EXE: %~dp0release\Football Logo Downloader.exe
echo ZIP: %~dp0release\Football-Logo-Downloader-v2.0.0-win-x64.zip
echo.
echo NOTE: PowerShell is used only by this developer build script to create
echo       the checksum and ZIP. The released application does not launch PowerShell.
echo.
pause
exit /b 0

:fail
echo.
echo [ERROR] Build failed. See the error above.
pause
exit /b 1
