param([string]$Server = '.\SQLEXPRESS')
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$project = Join-Path $PSScriptRoot 'FormaDrive'
$local = Join-Path $project '.local'
New-Item -ItemType Directory -Force $local | Out-Null

# sqlcmd and SSMS execute the same idempotent SQL script.
& sqlcmd -S $Server -E -C -b -f 65001 -i (Join-Path $PSScriptRoot 'database/Create-FormaDrive.sql')
if ($LASTEXITCODE -ne 0) { throw 'Could not create or update FormaDrive. Check SQL Server and Windows permissions.' }

$settingsPath = Join-Path $local 'settings.json'
$connection = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
$connection.set_DataSource($Server)
$connection.set_InitialCatalog('FormaDrive')
$connection.set_IntegratedSecurity($true)
$connection.set_Encrypt($true)
$connection.set_TrustServerCertificate($true)
if (Test-Path -LiteralPath $settingsPath) {
    $config = Get-Content -Raw -Encoding utf8 $settingsPath | ConvertFrom-Json
    if ($config.ConnectionStrings.Fleet -match '(?:^|;)Port=3347(?:;|$)') {
        throw 'Existing MySQL settings found. Transfer the data before switching this installation to SQL Server.'
    }
    $config.ConnectionStrings.Fleet = $connection.ConnectionString
} else {
    $config = @{
        ConnectionStrings = @{ Fleet = $connection.ConnectionString }
        Staff = @{ Email = 'staff@forma.local'; Password = [Guid]::NewGuid().ToString('N') }
    }
}
$config | ConvertTo-Json | Set-Content -Encoding utf8 $settingsPath

function IsListening([int]$port) {
    $client = New-Object Net.Sockets.TcpClient
    try { $client.Connect('127.0.0.1', $port); return $true }
    catch { return $false }
    finally { $client.Dispose() }
}
if (!(IsListening 5187)) {
    dotnet build (Join-Path $project 'FormaDrive.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $program = Join-Path $project 'bin/Release/net10.0/FormaDrive.exe'
    $web = Start-Process -FilePath $program -ArgumentList '--urls http://127.0.0.1:5187' -WorkingDirectory $project -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $local 'web.log') -RedirectStandardError (Join-Path $local 'web-error.log')
    $web.Id | Set-Content (Join-Path $local 'web.pid')
    for ($i=0; $i -lt 90 -and !(IsListening 5187); $i++) {
        if ($web.HasExited) { throw "Check $local/web-error.log" }
        Start-Sleep -Milliseconds 500
    }
}
$response = Invoke-WebRequest -UseBasicParsing 'http://127.0.0.1:5187/'
if ($response.Content -notmatch 'Forma Drive') { throw 'Another application is listening on port 5187.' }
Write-Host 'Forma Drive: http://127.0.0.1:5187/'
Write-Host "SSMS: $Server / FormaDrive (Windows Authentication)"
Write-Host "Staff credentials: $settingsPath (Staff)"