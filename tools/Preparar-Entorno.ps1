#Requires -Version 7.2
param([switch]$SinDocker)
$ErrorActionPreference = 'Stop'
$rootPath = Split-Path $PSScriptRoot -Parent
Push-Location $rootPath
try {
    if (-not (Test-Path -LiteralPath '.env')) {
        $dbPassword = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
        $rootPassword = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
        @("TRAZO_DB_PASSWORD=$dbPassword", "TRAZO_ROOT_PASSWORD=$rootPassword") | Set-Content -LiteralPath '.env' -Encoding utf8
    }
    $settings = Get-Content -LiteralPath '.env' | ConvertFrom-StringData
    if (-not $settings.TRAZO_DB_PASSWORD) { throw 'Falta TRAZO_DB_PASSWORD en .env.' }
    $connection = "Server=127.0.0.1;Port=3307;Database=trazo_dev;User ID=trazo;Password=$($settings.TRAZO_DB_PASSWORD);SslMode=Disabled;AllowPublicKeyRetrieval=True;CharSet=utf8mb4"
    # stdin evita mostrar la cadena de conexión como argumento del comando.
    @{ConnectionStrings=@{Trazo=$connection}} | ConvertTo-Json -Compress | dotnet user-secrets set --project src/Trazo.Web
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo configurar User Secrets.' }
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet tool restore.' }
    dotnet restore Trazo.sln
    if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet restore.' }
    if (-not $SinDocker) {
        docker compose up -d --wait --wait-timeout 180
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo iniciar MySQL. Abrir Docker Desktop y volver a ejecutar.' }
    }
    Write-Host 'Entorno configurado. Las credenciales locales están en .env y User Secrets; no se versionan.'
} finally { Pop-Location }
