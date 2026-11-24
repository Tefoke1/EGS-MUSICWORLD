function Find-Empyrion([string]$GamePath) {
    $candidates = @()
    if ($GamePath) {
        $candidates += $GamePath
    } elseif ($env:EMPYRION_GAME) {
        $candidates += $env:EMPYRION_GAME
    } else {
        $steamRoots = @()
        foreach ($key in 'HKCU:\Software\Valve\Steam','HKLM:\SOFTWARE\WOW6432Node\Valve\Steam') {
            $item = Get-ItemProperty -LiteralPath $key -ErrorAction SilentlyContinue
            if ($item) { $steamRoots += @($item.SteamPath, $item.InstallPath) | Where-Object { $_ } }
        }
        $steamRoots += @('E:\Steam', (Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Steam'))
        foreach ($steam in ($steamRoots | Select-Object -Unique)) {
            $libraries = @($steam)
            $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
            if (Test-Path -LiteralPath $vdf) {
                foreach ($line in (Get-Content -LiteralPath $vdf)) {
                    if ($line -match '^\s*"path"\s*"([^"]+)"') { $libraries += $matches[1].Replace('\\','\') }
                }
            }
            foreach ($library in $libraries) { $candidates += Join-Path $library 'steamapps\common\Empyrion - Galactic Survival' }
        }
    }
    foreach ($path in $candidates) {
        if (Test-Path -LiteralPath (Join-Path $path 'Client\Empyrion_Data\Managed\ModApi.dll')) {
            return (Get-Item -LiteralPath $path).FullName
        }
    }
    throw 'Game not found. Set EMPYRION_GAME to the Empyrion installation directory or supply -GamePath.'
}
