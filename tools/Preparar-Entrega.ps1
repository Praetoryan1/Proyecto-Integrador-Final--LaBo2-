#Requires -Version 7.2
param([switch]$DesdeSql, [switch]$ExportarSql, [switch]$Verificacion, [switch]$IniciarWeb)
$ErrorActionPreference = 'Stop'
if ($DesdeSql -and $ExportarSql) { throw 'Elegí importar o exportar, no ambos.' }
$root = Split-Path $PSScriptRoot -Parent
$project = if ($Verificacion) { 'trazo-verificacion' } else { 'trazo-entrega' }
$port = if ($Verificacion) { 3309 } else { 3308 }
$webPort = if ($Verificacion) { 7282 } else { 7281 }
$envNames = @('ConnectionStrings__Trazo', 'Storage__Root', 'Media__Root', 'Jwt__SigningKey', 'ASPNETCORE_ENVIRONMENT', 'ASPNETCORE_URLS', 'TRAZO_ENTREGA_PORT')
$previous = @{}
foreach ($name in $envNames) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
function Check-Native([string]$message) { if ($LASTEXITCODE -ne 0) { throw $message } }
Push-Location $root
try {
    if (-not (Test-Path '.env.entrega')) {
        $lines = foreach ($name in @('TRAZO_ENTREGA_PASSWORD', 'TRAZO_ENTREGA_ROOT_PASSWORD', 'TRAZO_ENTREGA_JWT')) {
            $name + '=' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
        }
        Set-Content -LiteralPath '.env.entrega' -Value $lines -Encoding utf8NoBOM
    }
    $settings = Get-Content '.env.entrega' -Raw | ConvertFrom-StringData
    foreach ($name in @('TRAZO_ENTREGA_PASSWORD', 'TRAZO_ENTREGA_ROOT_PASSWORD', 'TRAZO_ENTREGA_JWT')) {
        if ($settings[$name] -notmatch '^[A-Fa-f0-9]{64}$') { throw 'Configuración .env.entrega incompleta o inválida. No se modifica el archivo existente.' }
    }
    $env:TRAZO_ENTREGA_PORT = "$port"
    $compose = @('compose', '-p', $project, '--env-file', '.env.entrega', '-f', 'compose.entrega.yaml')
    docker @compose up -d --wait
    Check-Native 'No se pudo iniciar MySQL de entrega. Revisá Docker Desktop y el puerto.'
    $count = docker @compose exec -T mysql sh -c 'MYSQL_PWD="$MYSQL_PASSWORD" exec mysql -u"$MYSQL_USER" "$MYSQL_DATABASE" -N -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema=DATABASE()"'
    Check-Native 'No se pudo comprobar si la base está vacía.'
    if ("$count" -notmatch '^\d+$') { throw 'MySQL no devolvió un número de tablas válido.' }
    $empty = [int]$count -eq 0
    if ($ExportarSql -and -not $empty) { throw 'Solo se exporta una demo recién creada en una base vacía. Esta base ya tiene tablas; se conserva intacta.' }
    $dataRoot = Join-Path $root "tmp/$project"
    $env:ConnectionStrings__Trazo = "Server=127.0.0.1;Port=$port;Database=trazo_entrega;User ID=trazo;Password=$($settings.TRAZO_ENTREGA_PASSWORD);SslMode=Disabled;AllowPublicKeyRetrieval=True;CharSet=utf8mb4"
    $env:Storage__Root = Join-Path $dataRoot 'avatars'
    $env:Media__Root = Join-Path $dataRoot 'medios'
    $env:Jwt__SigningKey = $settings.TRAZO_ENTREGA_JWT
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = "https://localhost:$webPort"
    dotnet restore Trazo.sln --locked-mode
    Check-Native 'Falló la restauración de dependencias.'
    dotnet build Trazo.sln -c Release --no-restore
    Check-Native 'Falló la compilación.'
    if ($empty) {
        if ($DesdeSql) {
            $sql = 'database/003-demo-completa.sql'
            if (-not (Test-Path $sql)) { throw 'Falta el SQL de entrega versionado.' }
            # Validar todos los archivos antes de importar; no tomar medios de App_Data local.
            $assets = 'src/Trazo.Web/DemoAssets'
            $manifest = Get-Content "$assets/manifest.json" -Raw | ConvertFrom-Json
            foreach ($item in $manifest) {
                if ($item.archivo -notmatch '^(avatars|medios)/[a-f0-9]{32}(\.preview)?\.(png|gif|mp4)$') { throw 'Ruta inválida en manifiesto.' }
                $source = Join-Path $assets $item.archivo
                if ((Get-Item $source).Length -ne $item.bytes -or (Get-FileHash $source -Algorithm SHA256).Hash -ne $item.sha256) { throw 'Paquete multimedia incompleto o alterado.' }
                $target = Join-Path $dataRoot $item.archivo
                if ((Test-Path $target) -and (Get-FileHash $target -Algorithm SHA256).Hash -ne $item.sha256) { throw 'Existe un archivo diferente en el destino. No se sobrescribe.' }
            }
            foreach ($item in $manifest) {
                $target = Join-Path $dataRoot $item.archivo
                New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
                if (-not (Test-Path $target)) { Copy-Item -LiteralPath (Join-Path $assets $item.archivo) -Destination $target }
            }
            Get-Content $sql -Raw -Encoding utf8 | docker @compose exec -T mysql sh -c 'MYSQL_PWD="$MYSQL_PASSWORD" exec mysql --default-character-set=utf8mb4 -u"$MYSQL_USER" "$MYSQL_DATABASE"'
            Check-Native 'Falló la importación SQL. No se borran los datos; revisá el error antes de reintentar.'
            # EF comprueba el historial restaurado y aplica solo migraciones posteriores si existiesen.
            dotnet run --project src/Trazo.Web -c Release --no-build --no-launch-profile -- --initialize
            Check-Native 'Falló la comprobación de migraciones tras importar.'
        } else {
            dotnet run --project src/Trazo.Web -c Release --no-build --no-launch-profile -- --seed-entrega
            Check-Native 'Falló la carga de la demo completa.'
        }
        if ($ExportarSql) {
            New-Item -ItemType Directory -Force -Path $dataRoot | Out-Null
            $temporary = Join-Path $dataRoot 'demo-export.sql'
            docker @compose exec -T mysql sh -c 'MYSQL_PWD="$MYSQL_PASSWORD" exec mysqldump -u"$MYSQL_USER" --no-tablespaces --single-transaction --skip-comments --skip-add-drop-table --set-gtid-purged=OFF --default-character-set=utf8mb4 "$MYSQL_DATABASE"' > $temporary
            Check-Native 'Falló la exportación de datos ficticios.'
            if ((Get-Item $temporary).Length -lt 1000) { throw 'El dump exportado está incompleto.' }
            Copy-Item -LiteralPath $temporary -Destination 'database/003-demo-completa.sql'
            Write-Host 'SQL exportado exclusivamente desde la demo recién creada.'
        }
    } else {
        Write-Host 'La base ya contiene tablas. Se conservan sus datos y archivos; no se importa ni se vuelve a sembrar.'
    }
    Write-Host "Demo preparada en $project (MySQL 127.0.0.1:$port)."
    if ($IniciarWeb) {
        Write-Host "Abrir https://localhost:$webPort . Ctrl+C detiene la web."
        dotnet run --project src/Trazo.Web -c Release --no-build --no-launch-profile
        Check-Native 'La web terminó con un error.'
    } else {
        Write-Host 'Para abrirla, repetir este comando agregando -IniciarWeb. Certificado: dotnet dev-certs https --trust.'
    }
} finally {
    foreach ($name in $envNames) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    Pop-Location
}
