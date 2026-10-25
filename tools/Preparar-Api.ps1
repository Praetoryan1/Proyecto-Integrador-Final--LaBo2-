#Requires -Version 7.2
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    # Capturar sin imprimir secretos. La CLI envuelve el JSON en comentarios BEGIN/END.
    $raw = dotnet user-secrets list --json --project src/Trazo.Web
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron leer los secretos locales.' }
    $json = ($raw | Where-Object { $_ -notmatch '^\s*//' }) -join "`n"
    $settings = $json | ConvertFrom-Json -AsHashtable
    if ($settings['Jwt:SigningKey']) {
        Write-Host 'La clave JWT local ya existe. Se conserva; no se muestra ni se rota.'
    } else {
        $key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
        @{Jwt=@{SigningKey=$key}} | ConvertTo-Json -Compress | dotnet user-secrets set --project src/Trazo.Web
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo guardar la clave JWT.' }
        Write-Host 'Clave JWT aleatoria creada en User Secrets, fuera de Git.'
    }
    Write-Host 'Reiniciá Trazo para habilitar la API de evaluación. Solo funciona en Development/Testing.'
} finally { Pop-Location }
