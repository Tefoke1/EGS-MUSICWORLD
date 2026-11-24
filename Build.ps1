param([string]$GamePath)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'tools\FindGame.ps1')
$game = Find-Empyrion $GamePath
$managed = Join-Path $game 'Client\Empyrion_Data\Managed'
$build = Join-Path $PSScriptRoot 'build'
$work = Join-Path $PSScriptRoot '.work'
$tempBefore = $env:TEMP
$tmpBefore = $env:TMP
try {
    New-Item -Path $build,$work -ItemType Directory -Force | Out-Null
    $env:TEMP = $work
    $env:TMP = $work
    $csc = Join-Path $env:SystemRoot 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $refs = 'mscorlib','System','System.Core','netstandard','Mif','ModApi','UnityEngine.CoreModule','UnityEngine.AudioModule','UnityEngine.JSONSerializeModule','UnityEngine.UnityWebRequestModule','UnityEngine.UnityWebRequestAudioModule'
    $arguments = $refs | ForEach-Object { '/reference:' + (Join-Path $managed ($_ + '.dll')) }
    $sources = (Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs').FullName
    & $csc /nologo /noconfig /nostdlib+ /optimize+ /target:library "/out:$build\MusicWorld.dll" @arguments @sources
    if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
    Write-Output (Join-Path $build 'MusicWorld.dll')
} finally {
    $env:TEMP = $tempBefore
    $env:TMP = $tmpBefore
}
