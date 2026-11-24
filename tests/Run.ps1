$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$work = Join-Path $root ('.work\test-' + [guid]::NewGuid().ToString('N'))
New-Item -Path $work -ItemType Directory -Force | Out-Null
$csc = Join-Path $env:SystemRoot 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$src = 'Playlist','Player','NativeMusic' | ForEach-Object { Join-Path $root ('src\' + $_ + '.cs') }
$tempBefore = $env:TEMP
$tmpBefore = $env:TMP
try {
    $env:TEMP = $work
    $env:TMP = $work
    & $csc /nologo /target:exe "/out:$work\Test.exe" @src "$PSScriptRoot\AudioSimulation.cs" "$PSScriptRoot\Acceptance.cs"
    if ($LASTEXITCODE) { throw 'Test compilation failed.' }
    & "$work\Test.exe" $work
    if ($LASTEXITCODE) { throw 'Playback regression.' }
} finally {
    $env:TEMP = $tempBefore
    $env:TMP = $tmpBefore
}
