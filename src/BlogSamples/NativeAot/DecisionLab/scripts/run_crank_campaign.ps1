[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $CrankPath,

    [Parameter(Mandatory)]
    [string] $ResultsDirectory,

    [int] $Seed = 20260901
)

$ErrorActionPreference = 'Stop'
$decisionLabDirectory = Split-Path -Parent $PSScriptRoot
$configPath = Join-Path $decisionLabDirectory 'crank/decision-lab.benchmarks.yml'
$publishRoot = '/workspace/src/BlogSamples/NativeAot/DecisionLab/artifacts/publish'
$profiles = 1..5 | ForEach-Object { 'jit-fdd', 'jit-scd', 'r2r-scd', 'native-aot' }
$random = [Random]::new($Seed)
$order = $profiles | Sort-Object { $random.Next() }
$counters = @{}

New-Item -ItemType Directory -Force -Path $ResultsDirectory | Out-Null
$order | ConvertTo-Json | Set-Content -Encoding utf8 (Join-Path $ResultsDirectory 'crank-order.json')

foreach ($profile in $order) {
    $counters[$profile] = 1 + ($counters[$profile] ?? 0)
    $run = $counters[$profile]
    $resultPath = Join-Path $ResultsDirectory ('crank-{0}-{1:d2}.json' -f $profile, $run)

    if ($profile -eq 'jit-fdd') {
        $executable = '/usr/share/dotnet/dotnet'
        $arguments = "$publishRoot/jit-fdd/DecisionLab.Api.dll"
    }
    else {
        $executable = "$publishRoot/$profile/DecisionLab.Api"
        $arguments = ''
    }

    Write-Host "[$profile $run/5] $resultPath"
    & $CrankPath --config $configPath --scenario workload --profile local `
        --variable "executable=$executable" `
        --variable "applicationArguments=$arguments" `
        --json $resultPath

    if ($LASTEXITCODE -ne 0) {
        throw "Crank falhou no perfil $profile, rodada $run, com código $LASTEXITCODE."
    }

    $result = Get-Content -Raw $resultPath | ConvertFrom-Json
    $badResponses = $result.jobResults.jobs.load.results.'http/requests/badresponses'
    if ($result.returnCode -ne 0 -or $badResponses -ne 0) {
        throw "Rodada inválida no perfil $profile, rodada ${run}: returnCode=$($result.returnCode), badResponses=$badResponses."
    }
}