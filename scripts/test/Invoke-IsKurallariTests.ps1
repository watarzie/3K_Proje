[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [switch] $Coverage,
    [switch] $NoRestore
)

$ErrorActionPreference = 'Stop'
$taskRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskProject = Join-Path $taskRepository '3K.Application.Tests/3K.Application.Tests.csproj'
$taskRunId = '{0}-{1}' -f (Get-Date -Format 'yyyyMMdd-HHmmss'), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
$taskResults = Join-Path $taskRepository "artifacts/test-results/$taskRunId"

& (Join-Path $PSScriptRoot 'Test-IsKuraliEslemesi.ps1')

# Yalnız test projesi çalışır; üretim Program/worker başlatılmaz, uygulama bağlantı ayarları okunmaz.
# HTTP güvenlik testleri yalnız localhost'ta sentetik bağımlılıklarla izole test host'u açar.
# Sonuçlar ayrı klasöre yazılır; önceki test raporları silinmez veya ezilmez.
$taskArguments = @(
    'test', $taskProject,
    '--configuration', $Configuration,
    '--verbosity', 'minimal',
    '--logger', 'trx;LogFileName=is-kurallari.trx',
    '--results-directory', $taskResults
)
if ($NoRestore) { $taskArguments += '--no-restore' }
if ($Coverage) { $taskArguments += @('--collect', 'XPlat Code Coverage') }

& dotnet @taskArguments
if ($LASTEXITCODE -ne 0) {
    throw "Is kurali testleri basarisiz. Rapor: $taskResults"
}
$taskTrxPath = Join-Path $taskResults 'is-kurallari.trx'
if (-not (Test-Path -LiteralPath $taskTrxPath)) {
    throw "Test sonucu raporu bulunamadi: $taskTrxPath"
}
[xml] $taskTrx = Get-Content -LiteralPath $taskTrxPath -Raw -Encoding UTF8
$taskCounters = $taskTrx.TestRun.ResultSummary.Counters
if ($null -eq $taskCounters -or [int]$taskCounters.total -le 0 -or
    [int]$taskCounters.executed -ne [int]$taskCounters.total -or
    [int]$taskCounters.passed -ne [int]$taskCounters.total) {
    throw "Tum testler calisip basarili olmali; sifir veya atlanmis test kabul edilmez. Rapor: $taskTrxPath"
}
Write-Output "Dogrulanan test: $($taskCounters.passed)/$($taskCounters.total); atlanan: 0."
& (Join-Path $PSScriptRoot 'Test-IsKuraliEslemesi.ps1') -TrxPath $taskTrxPath
Write-Output "Is kurali testleri basarili. Rapor: $taskResults"
