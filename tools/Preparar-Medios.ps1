#Requires -Version 7.2
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'En Linux instalar ffmpeg y ffprobe con el gestor de paquetes del sistema. Ver docs/medios.md.' }
$projectPath = Split-Path $PSScriptRoot -Parent
$destinationPath = Join-Path $PSScriptRoot 'ffmpeg'
if ((Test-Path (Join-Path $destinationPath 'bin/ffmpeg.exe')) -and (Test-Path (Join-Path $destinationPath 'bin/ffprobe.exe'))) {
    Write-Host 'FFmpeg local ya está disponible en tools/ffmpeg/bin.'
    exit 0
}
$downloadDirectory = Join-Path $projectPath 'tmp/ffmpeg-9.0.2'
New-Item -ItemType Directory -Path $downloadDirectory -Force | Out-Null
$archivePath = Join-Path $downloadDirectory 'ffmpeg.zip'
$expectedHash = '60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba'
$url = 'https://github.com/GyanD/codexffmpeg/releases/download/9.0.2/ffmpeg-9.0.2-essentials_build.zip'
if (-not (Test-Path $archivePath)) { Invoke-WebRequest $url -OutFile $archivePath }
if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'El SHA-256 no coincide con la versión fijada. No se extrae ni ejecuta el archivo.'
}
Expand-Archive -LiteralPath $archivePath -DestinationPath $downloadDirectory -Force
$extractedPath = Join-Path $downloadDirectory 'ffmpeg-9.0.2-essentials_build'
New-Item -ItemType Directory -Path (Join-Path $destinationPath 'bin') -Force | Out-Null
foreach ($fileName in @('ffmpeg.exe', 'ffprobe.exe')) {
    Copy-Item -LiteralPath (Join-Path $extractedPath "bin/$fileName") -Destination (Join-Path $destinationPath "bin/$fileName")
}
Copy-Item -LiteralPath (Join-Path $extractedPath 'LICENSE') -Destination $destinationPath
Write-Host 'FFmpeg/ffprobe 9.0.2 preparados localmente. No se modificó PATH ni se versionarán los binarios.'
