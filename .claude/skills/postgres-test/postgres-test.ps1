<#
.SYNOPSIS
  İzole yerel PostgreSQL test sunucusunu başlatır, filtreli Release testlerini koşar ve sunucuyu durdurur.

.DESCRIPTION
  - Veri klasörü yoksa initdb ile kurulur (yalnız 127.0.0.1, gerçek 3K_Db'ye hiç bağlanmaz).
  - Testler THREEK_TEST_POSTGRES ile 127.0.0.1:55439 / Database=postgres bekler.
  - 55439 bağlanamazsa (Windows'ta ara ara "Permission denied") sunucu 55440'ta açılır ve 55439 yerel bir
    yönlendiriciyle ona bağlanır.
  - Test bitince (başarılı veya değil) sunucu ve yönlendirici kapatılır.

.EXAMPLE
  ./postgres-test.ps1 -Filter "FullyQualifiedName~Finans|FullyQualifiedName~Ambalaj"
#>
param(
    [Parameter(Mandatory = $true)][string]$Filter,
    [string]$DataDir = (Join-Path $env:LOCALAPPDATA '3K\pg-test'),
    [switch]$NoBuild,
    [switch]$KeepRunning
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$parola = if ($env:THREEK_TEST_PG_PASSWORD) { $env:THREEK_TEST_PG_PASSWORD } else { 'yerel-test' }
$bin = Get-ChildItem 'C:\Program Files\PostgreSQL\*\bin\pg_ctl.exe' -ErrorAction SilentlyContinue |
    Sort-Object { [int]($_.Directory.Parent.Name -replace '\D', '') } -Descending | Select-Object -First 1
if (-not $bin) { throw 'PostgreSQL kurulumu bulunamadı (C:\Program Files\PostgreSQL\<sürüm>\bin).' }
$pgCtl = $bin.FullName
$initdb = Join-Path $bin.DirectoryName 'initdb.exe'
$log = Join-Path (Split-Path $DataDir -Parent) 'pg-test.log'

if (-not (Test-Path (Join-Path $DataDir 'PG_VERSION'))) {
    New-Item -ItemType Directory -Force (Split-Path $DataDir -Parent) | Out-Null
    $pw = New-TemporaryFile
    Set-Content -Path $pw -Value $parola -Encoding ascii -NoNewline
    try {
        & $initdb -D $DataDir -U postgres --auth=scram-sha-256 --pwfile=$pw -E UTF8 --locale=C | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'initdb başarısız.' }
    } finally { Remove-Item $pw -Force }
    Add-Content (Join-Path $DataDir 'postgresql.conf') "`nlisten_addresses = '127.0.0.1'`n"
    Write-Host "Test kümesi kuruldu: $DataDir"
}

function Baslat([int]$port) {
    # pg_ctl'in başlattığı postgres süreci konsol/çıktı tutamaçlarını devralırsa çağıran kabuk hiç dönmez. Ayrı
    # (gizli) pencerede başlatılıp yalnız pg_ctl'in kendisi beklenir; postgres arka planda çalışmaya devam eder.
    $p = Start-Process -FilePath $pgCtl -ArgumentList @('-D', "`"$DataDir`"", '-l', "`"$log`"", '-o', "`"-p $port`"", '-w', 'start') `
        -WindowStyle Hidden -PassThru
    $p.WaitForExit()
    return $p.ExitCode -eq 0
}

function PortAcik([int]$port) {
    $c = New-Object System.Net.Sockets.TcpClient
    try { $c.Connect('127.0.0.1', $port); return $true } catch { return $false } finally { $c.Dispose() }
}

$yonlendirici = $null
# -KeepRunning ile açık bırakılmış sunucu yeniden başlatılmaz; yalnız 55439 erişilemiyorsa yönlendirici açılır.
$durum = Start-Process -FilePath $pgCtl -ArgumentList @('-D', "`"$DataDir`"", 'status') -WindowStyle Hidden -PassThru
$durum.WaitForExit()
if ($durum.ExitCode -eq 0) {
    Write-Host 'Test sunucusu zaten çalışıyor.'
    if (-not (PortAcik 55439)) {
        $yonlendirici = Start-Process node -ArgumentList "`"$(Join-Path $PSScriptRoot 'pg-yonlendir.js')`"" -PassThru -WindowStyle Hidden
        Start-Sleep -Milliseconds 500
    }
}
elseif (-not (Baslat 55439)) {
    Write-Host '55439 bağlanamadı; 55440 + yerel yönlendirme deneniyor.'
    if (-not (Baslat 55440)) { throw "Test sunucusu başlatılamadı; günlük: $log" }
    $yonlendirici = Start-Process node -ArgumentList "`"$(Join-Path $PSScriptRoot 'pg-yonlendir.js')`"" -PassThru -WindowStyle Hidden
    Start-Sleep -Milliseconds 500
}

try {
    $env:THREEK_TEST_POSTGRES = "Host=127.0.0.1;Port=55439;Database=postgres;Username=postgres;Password=$parola"
    Push-Location $repo
    if (-not $NoBuild) {
        dotnet build 3K_Proje.slnx -c Release -v q -nologo
        if ($LASTEXITCODE -ne 0) { throw 'Derleme başarısız.' }
    }
    dotnet test 3K.Application.Tests/3K.Application.Tests.csproj -c Release --no-build --filter $Filter
    $testKodu = $LASTEXITCODE
} finally {
    Pop-Location
    if (-not $KeepRunning) {
        if ($yonlendirici -and -not $yonlendirici.HasExited) { Stop-Process -Id $yonlendirici.Id -Force }
        $p = Start-Process -FilePath $pgCtl -ArgumentList @('-D', "`"$DataDir`"", '-m', 'fast', '-w', 'stop') -WindowStyle Hidden -PassThru
        $p.WaitForExit()
        Write-Host 'Test sunucusu durduruldu.'
    }
}
exit $testKodu
