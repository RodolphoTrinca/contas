param(
    [ValidateSet('fast', 'integration', 'e2e', 'all')]
    [string] $Mode = 'fast'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$names = @('POSTGRES_PORT', 'REDIS_PORT', 'KEYCLOAK_PORT', 'ACCOUNTS_PORT',
           'ACCOUNTS_SEED', 'ACCOUNTS_E2E_URL', 'CONTAS_TEST_POSTGRES', 'CONTAS_TEST_REDIS')
$old = @{}
foreach ($name in $names) { $old[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
function Invoke-Native {
    param([string] $Executable, [string[]] $Arguments)
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Executable terminou com código $LASTEXITCODE." }
}
function Invoke-Fast {
    Invoke-Native dotnet @('test', 'tests/Domain')
    Invoke-Native dotnet @('test', 'tests/Architecture')
}
function Invoke-Isolated {
    param([string] $Suite)
    $project = "contas-$Suite-" + [Guid]::NewGuid().ToString('N').Substring(0, 12)
    $compose = @('compose', '-p', $project, '-f', (Join-Path $root 'infra/compose.yaml'))
    $env:POSTGRES_PORT = if ($env:TEST_POSTGRES_PORT) { $env:TEST_POSTGRES_PORT } else { '25432' }
    $env:REDIS_PORT = if ($env:TEST_REDIS_PORT) { $env:TEST_REDIS_PORT } else { '26379' }
    try {
        if ($Suite -eq 'integration') {
            Invoke-Native docker ($compose + @('up', '-d', '--wait', 'postgres', 'redis'))
            $env:CONTAS_TEST_POSTGRES = "Host=127.0.0.1;Port=$env:POSTGRES_PORT;Database=contas;Username=contas;Password=contas-local"
            $env:CONTAS_TEST_REDIS = "127.0.0.1:$env:REDIS_PORT"
            Invoke-Native dotnet @('test', 'tests/Integration')
        } else {
            $env:KEYCLOAK_PORT = if ($env:TEST_KEYCLOAK_PORT) { $env:TEST_KEYCLOAK_PORT } else { '28090' }
            $env:ACCOUNTS_PORT = if ($old['ACCOUNTS_PORT']) { $old['ACCOUNTS_PORT'] } else { '18080' }
            $env:ACCOUNTS_SEED = 'false'
            $env:ACCOUNTS_E2E_URL = "http://localhost:$env:ACCOUNTS_PORT"
            Invoke-Native docker ($compose + @('up', '--build', '-d', 'nginx'))
            $ready = $false
            for ($i = 0; $i -lt 90; $i++) {
                try {
                    foreach ($route in @('/health', '/auth/realms/contas/.well-known/openid-configuration', '/')) {
                        Invoke-WebRequest -UseBasicParsing -TimeoutSec 2 -Uri ($env:ACCOUNTS_E2E_URL + $route) | Out-Null
                    }
                    $ready = $true
                    break
                } catch { Start-Sleep -Seconds 1 }
            }
            if (-not $ready) {
                & docker @compose logs --tail 100
                throw 'O ambiente E2E não ficou pronto.'
            }
            Push-Location 'tests/e2e'
            try { Invoke-Native npm @('test') } finally { Pop-Location }
        }
    } finally { & docker @compose down -v }
}
Push-Location $root
try {
    switch ($Mode) {
        fast { Invoke-Fast }
        integration { Invoke-Isolated integration }
        e2e { Invoke-Isolated e2e }
        all { Invoke-Fast; Invoke-Isolated integration; Invoke-Isolated e2e }
    }
}
finally {
    Pop-Location
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $old[$name], 'Process') }
}
