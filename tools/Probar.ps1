#Requires -Version 7.2
param([switch]$Integracion)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    if ($Integracion) {
        if (-not (Test-Path '.env')) { throw 'Primero ejecutar tools/Preparar-Entorno.ps1.' }
        $settings = Get-Content '.env' | ConvertFrom-StringData
        $env:TRAZO_TEST_CONNECTION = "Server=127.0.0.1;Port=3307;Database=trazo_tests;User ID=trazo;Password=$($settings.TRAZO_DB_PASSWORD);SslMode=Disabled;AllowPublicKeyRetrieval=True;CharSet=utf8mb4"
    }
    dotnet test Trazo.sln --logger 'trx;LogFileName=pruebas.trx'
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas.' }
} finally {
    Remove-Item Env:TRAZO_TEST_CONNECTION -ErrorAction SilentlyContinue
    Pop-Location
}
