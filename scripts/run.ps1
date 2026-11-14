param(
    [ValidateSet('stack', 'demos', 'api', 'infra', 'debug', 'down')]
    [string] $Mode,
    [switch] $Seed
)
$ErrorActionPreference = 'Stop'
if (-not $PSBoundParameters.ContainsKey('Mode')) {
    if ([Console]::IsInputRedirected) {
        Write-Error 'Informe -Mode ao executar sem terminal interativo.'
    }
    Write-Host 'Qual ambiente deseja executar?'
    Write-Host '1) Stack completa (UI, API e infraestrutura)'
    Write-Host '2) Somente API e infraestrutura'
    Write-Host '3) Somente infraestrutura (PostgreSQL, Redis e Keycloak)'
    Write-Host '4) Infraestrutura para depuração no VS Code'
    Write-Host '5) Encerrar os serviços'
    Write-Host '6) Stack completa com demos de Fraude e Cartões'
    $modes = @{ '1' = 'stack'; '2' = 'api'; '3' = 'infra'; '4' = 'debug'; '5' = 'down'; '6' = 'demos' }
    do {
        $choice = Read-Host 'Escolha [1-6] ou q para cancelar'
        if ($choice -eq 'q') { return }
        if (-not $modes.ContainsKey($choice)) { Write-Host 'Opção inválida.' }
    } until ($modes.ContainsKey($choice))
    $Mode = $modes[$choice]
    if ($Mode -in @('stack', 'demos', 'api') -and -not $PSBoundParameters.ContainsKey('Seed')) {
        do {
            $answer = Read-Host 'Carregar contas fictícias? [s/N]'
            if ($answer -notin @('', 's', 'n')) { Write-Host 'Responda s ou n.' }
        } until ($answer -in @('', 's', 'n'))
        $Seed = $answer -eq 's'
    }
}
$compose = Join-Path (Split-Path $PSScriptRoot -Parent) 'infra/compose.yaml'
$debugCompose = Join-Path (Split-Path $PSScriptRoot -Parent) 'infra/compose.debug.yaml'
$previous = $env:ACCOUNTS_SEED
$env:ACCOUNTS_SEED = ([bool]$Seed).ToString().ToLowerInvariant()
function Invoke-Compose {
    & docker compose -f $compose @args
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
try {
    if ($Mode -in @('stack', 'api', 'infra', 'debug')) {
        Invoke-Compose --profile demos stop fraud-demo cards-demo
    }
    switch ($Mode) {
        demos { Invoke-Compose --profile api -f $debugCompose stop api-gateway debug-gateway; Invoke-Compose --profile demos up --build -d nginx fraud-demo cards-demo }
        stack { Invoke-Compose --profile api -f $debugCompose stop api-gateway debug-gateway; Invoke-Compose up --build -d nginx }
        api { Invoke-Compose -f $debugCompose stop nginx web debug-gateway; Invoke-Compose --profile api up --build -d api-gateway }
        infra { Invoke-Compose up -d postgres redis keycloak }
        debug { Invoke-Compose --profile api stop nginx api-gateway api web; Invoke-Compose -f $debugCompose up -d postgres redis keycloak debug-gateway }
        down { Invoke-Compose --profile api --profile demos down --remove-orphans }
    }
}
finally {
    $env:ACCOUNTS_SEED = $previous
}
