<#
    音乐桌面 · 本地编译脚本
    用法： powershell -ExecutionPolicy Bypass -File build.ps1
    产物： publish\win-x64\MusicDesktop.exe 与 MusicDesktop-win-x64.zip
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj = Join-Path $root "src\MusicDesktop\MusicDesktop.csproj"
$out  = Join-Path $root "publish\$Runtime"

Write-Host "==> dotnet --version" -ForegroundColor Cyan
dotnet --version

Write-Host "==> restore" -ForegroundColor Cyan
dotnet restore $proj

Write-Host "==> build ($Configuration)" -ForegroundColor Cyan
dotnet build $proj -c $Configuration --no-restore

$publishArgs = @(
    "publish", $proj, "-c", $Configuration, "-r", $Runtime,
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-o", $out
)
if ($FrameworkDependent) {
    $publishArgs += "--self-contained"; $publishArgs += "false"
} else {
    $publishArgs += "--self-contained"; $publishArgs += "true"
}

Write-Host "==> publish -> $out" -ForegroundColor Cyan
dotnet @publishArgs

$zip = Join-Path $root "MusicDesktop-$Runtime.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zip -Force

$hash = Get-FileHash $zip -Algorithm SHA256
Write-Host ""
Write-Host "==> 完成" -ForegroundColor Green
Write-Host ("    ZIP    : {0}" -f $zip)
Write-Host ("    大小   : {0:N1} MB" -f ((Get-Item $zip).Length / 1MB))
Write-Host ("    SHA256 : {0}" -f $hash.Hash)
