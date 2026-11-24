param([string]$GamePath)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'tools\FindGame.ps1')
$game = Find-Empyrion $GamePath
$buildDll = Join-Path $PSScriptRoot 'MusicWorld.dll'
if (-not (Test-Path -LiteralPath $buildDll)) {
    & (Join-Path $PSScriptRoot 'Build.ps1') -GamePath $game
    $buildDll = Join-Path $PSScriptRoot 'build\MusicWorld.dll'
}
$dir = Join-Path $game 'Content\Mods\MusicWorld'
New-Item -Path $dir,(Join-Path $dir 'Music\Exploration'),(Join-Path $dir 'Music\Combat') -ItemType Directory -Force | Out-Null
foreach ($pair in @(@('MusicWorld_Info.yaml',(Join-Path $PSScriptRoot 'MusicWorld_Info.yaml')), @('musicworld.json',(Join-Path $PSScriptRoot 'musicworld.json')), @('MusicWorld.dll',$buildDll))) {
    $file = Join-Path $dir $pair[0]
    if ($pair[0] -eq 'musicworld.json' -and (Test-Path -LiteralPath $file)) { continue }
    if ((Test-Path -LiteralPath $file) -and ((Get-FileHash -LiteralPath $file).Hash -eq (Get-FileHash -LiteralPath $pair[1]).Hash)) { continue }
    $tempFile = Join-Path $dir ([IO.Path]::GetRandomFileName())
    try {
        [IO.File]::Copy($pair[1],$tempFile)
        if (Test-Path -LiteralPath $file) { [IO.File]::Replace($tempFile,$file,[System.Management.Automation.Language.NullString]::Value) }
        else { [IO.File]::Move($tempFile,$file) }
    } finally {
        if (Test-Path -LiteralPath $tempFile) { Remove-Item -LiteralPath $tempFile -Force }
    }
}
Write-Host "Installed in $dir"
Write-Host 'On the next launch, use single-player with Easy Anti-Cheat off. Built-in playlists are ready.'
