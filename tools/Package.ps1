param([string]$GamePath)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
& (Join-Path $root 'Build.ps1') -GamePath $GamePath
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$dist = Join-Path $root 'dist'
New-Item -Path $dist -Force -ItemType Directory | Out-Null
$zipPath = Join-Path $dist 'EGS-MUSICWORLD-0.1.0.zip'
$stage = Join-Path $root ('.work\package-' + [guid]::NewGuid().ToString('N') + '.zip')
$entries = [ordered]@{
    'MusicWorld.dll' = 'build\MusicWorld.dll'; 'MusicWorld_Info.yaml' = 'MusicWorld_Info.yaml'
    'Install.cmd' = 'Install.cmd'; 'Install.ps1' = 'Install.ps1'; 'tools/FindGame.ps1' = 'tools\FindGame.ps1'
    'musicworld.json' = 'musicworld.json'; 'README.md' = 'README.md'; 'LICENSE' = 'LICENSE'
}
$zip = [IO.Compression.ZipFile]::Open($stage,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($name in $entries.Keys) {
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $root $entries[$name]),$name)
    }
} finally { $zip.Dispose() }
[IO.File]::Copy($stage,$zipPath,$true)
Remove-Item -LiteralPath $stage
Write-Output $zipPath
