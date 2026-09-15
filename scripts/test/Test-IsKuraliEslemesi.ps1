[CmdletBinding()]
param([string] $TrxPath)

$ErrorActionPreference = 'Stop'
$taskRepository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskCatalogPath = Join-Path $taskRepository 'docs/is-kurallari/GRID_3K_IS_KURALLARI.md'
$taskMapDirectory = Join-Path $taskRepository 'docs/is-kurallari/coverage'
$taskCatalog = Get-Content -LiteralPath $taskCatalogPath -Raw -Encoding UTF8
$taskRuleIds = @([regex]::Matches($taskCatalog, '(?m)^(?:-\s+)?(?:\*\*|\|\s*)?(?<Id>[A-Z]+-\d{2,3}[A-Z]?)\b') |
    ForEach-Object { $_.Groups['Id'].Value } | Sort-Object -Unique)
if ($taskRuleIds.Count -eq 0) { throw 'Katalogda kural bulunamadi; denetim atlanamaz.' }

$taskRows = @{}
$taskMapFiles = @(Get-ChildItem -LiteralPath $taskMapDirectory -Filter '*_TEST_ESLESMESI.md')
foreach ($taskMapFile in $taskMapFiles) {
    foreach ($taskLine in Get-Content -LiteralPath $taskMapFile.FullName -Encoding UTF8) {
        if ($taskLine -match '^\|\s*(?<Id>[A-Z]+-\d{2,3}[A-Z]?)\s*\|') {
            $taskRuleId = $Matches['Id']
            if ($taskRows.ContainsKey($taskRuleId)) { throw "Birden fazla esleme: $taskRuleId" }
            $taskRows[$taskRuleId] = $taskLine
        }
    }
}

$taskMissing = @($taskRuleIds | Where-Object { -not $taskRows.ContainsKey($_) })
$taskUnknown = @($taskRows.Keys | Where-Object { $_ -notin $taskRuleIds })
if ($taskMissing.Count -gt 0) { throw "Eslemesi eksik kurallar: $($taskMissing -join ', ')" }
if ($taskUnknown.Count -gt 0) { throw "Katalogda bulunmayan kurallar: $($taskUnknown -join ', ')" }

# Bu kontrol sadece dokuman/test referanslarinin eskimesini yakalar.
# Bir metot adinin varligi kuralin dogru veya eksiksiz test edildigini KANITLAMAZ.
$taskTests = @{}
$taskExecutedTests = @{}
if ($TrxPath) {
    [xml] $taskExecutionReport = Get-Content -LiteralPath $TrxPath -Raw -Encoding UTF8
    $taskPassedIds = @{}
    foreach ($taskResult in $taskExecutionReport.TestRun.Results.UnitTestResult) {
        if ($taskResult.outcome -eq 'Passed') { $taskPassedIds[[string]$taskResult.testId] = $true }
    }
    foreach ($taskDefinition in $taskExecutionReport.TestRun.TestDefinitions.UnitTest) {
        if ($taskPassedIds.ContainsKey([string]$taskDefinition.id)) {
            $taskClassName = ([string]$taskDefinition.TestMethod.className).Split('.')[-1]
            $taskExecutedTests[('{0}.{1}' -f $taskClassName, $taskDefinition.TestMethod.name)] = $true
        }
    }
    if ($taskExecutedTests.Count -eq 0) { throw 'TRX raporunda basarili test bulunamadi.' }
}
foreach ($taskTestFile in Get-ChildItem -LiteralPath (Join-Path $taskRepository '3K.Application.Tests') -Filter '*.cs') {
    $taskTestSource = Get-Content -LiteralPath $taskTestFile.FullName -Raw -Encoding UTF8
    $taskTestClass = [regex]::Match($taskTestSource, 'public\s+(?:sealed\s+|partial\s+)?class\s+(?<Name>\w+Tests)\b')
    if (-not $taskTestClass.Success) { continue }
    foreach ($taskMethod in [regex]::Matches($taskTestSource, 'public\s+(?:async\s+)?(?:Task|void)\s+(?<Name>\w+)\s*\(')) {
        $taskTests[('{0}.{1}' -f $taskTestClass.Groups['Name'].Value, $taskMethod.Groups['Name'].Value)] = $true
    }
}
foreach ($taskRow in $taskRows.GetEnumerator()) {
    $taskColumns = $taskRow.Value.Split('|')
    $taskScope = $taskColumns[2].Trim()
    if ($taskScope -notin @('Birim', 'Kısmi', 'Entegrasyon', 'UI', 'Açık', 'Kapsam dışı')) {
        throw "Gecersiz kapsam: $($taskRow.Key) -> $taskScope"
    }
    $taskReferences = [regex]::Matches($taskRow.Value, '`(?<Name>\w+Tests\.\w+)`')
    if ($taskScope -eq 'Birim' -and $taskReferences.Count -eq 0) {
        throw "Birim kapsaminda en az bir test referansi gerekli: $($taskRow.Key)"
    }
    foreach ($taskReference in $taskReferences) {
        $taskName = $taskReference.Groups['Name'].Value
        if (-not $taskTests.ContainsKey($taskName)) { throw "Test metodu bulunamadi: $($taskRow.Key) -> $taskName" }
        if ($TrxPath -and -not $taskExecutedTests.ContainsKey($taskName)) {
            throw "Eslenen test raporda basarili calismamis: $($taskRow.Key) -> $taskName"
        }
    }
}
Write-Output "$($taskRuleIds.Count) kuralin eslemesi ve isimlendirilmis test referanslari dogrulandi."
if ($TrxPath) { Write-Output 'Eslenen test metotlarinin TRX raporunda basarili calistigi dogrulandi.' }
Write-Output 'Bu bir davranis kapsama yuzdesi degildir. Kismi/UI/entegrasyon/acik durumlari esleme tablolarinda ayrica okunmalidir.'
