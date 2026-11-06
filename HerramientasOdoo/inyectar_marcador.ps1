$BookmarksPath = "$env:LOCALAPPDATA\Google\Chrome\User Data\Default\Bookmarks"
$BmkFile = "D:\Bussersa\Bussersa\src\Bussersa.Operaciones\HerramientasOdoo\bookmarklet_autonomo.txt"

if (-not (Test-Path $BookmarksPath)) {
    Write-Host " [!] No se encontró el archivo de marcadores de Chrome en: $BookmarksPath" -ForegroundColor Yellow
    exit 0
}

if (-not (Test-Path $BmkFile)) {
    Write-Host " [!] No se encontró el archivo bookmarklet_autonomo.txt" -ForegroundColor Red
    exit 0
}

try {
    $BmkCode = [System.IO.File]::ReadAllText($BmkFile)
    $RawJson = [System.IO.File]::ReadAllText($BookmarksPath)
    
    # Crear backup
    [System.IO.File]::WriteAllText("$BookmarksPath.bak", $RawJson)

    $Bookmarks = $RawJson | ConvertFrom-Json

    if ($null -eq $Bookmarks.roots.bookmark_bar.children) {
        Write-Host " [!] Estructura de bookmark_bar no encontrada." -ForegroundColor Yellow
        exit 0
    }

    $Children = [System.Collections.ArrayList]@($Bookmarks.roots.bookmark_bar.children)
    
    $ExistingIdx = -1
    for ($i = 0; $i -lt $Children.Count; $i++) {
        if ($Children[$i].name -like "*Calcular N*mina*") {
            $ExistingIdx = $i
            break
        }
    }

    $NewBookmark = [PSCustomObject]@{
        date_added = "$([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())000"
        date_last_used = "0"
        id = "$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
        name = "⚡ Calcular Nómina (Odoo)"
        type = "url"
        url = $BmkCode
    }

    if ($ExistingIdx -ge 0) {
        $Children[$ExistingIdx] = $NewBookmark
        Write-Host " [OK] Marcador '⚡ Calcular Nómina (Odoo)' actualizado en Chrome." -ForegroundColor Green
    } else {
        $Children.Insert(0, $NewBookmark)
        Write-Host " [OK] Marcador '⚡ Calcular Nómina (Odoo)' agregado a la barra de marcadores de Chrome." -ForegroundColor Green
    }

    $Bookmarks.roots.bookmark_bar.children = $Children
    $UpdatedJson = $Bookmarks | ConvertTo-Json -Depth 32

    [System.IO.File]::WriteAllText($BookmarksPath, $UpdatedJson)
}
catch {
    Write-Host " [!] Advertencia al inyectar marcador: $($_.Exception.Message)" -ForegroundColor Yellow
}
