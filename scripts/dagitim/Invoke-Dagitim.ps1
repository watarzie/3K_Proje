<#
.SYNOPSIS
    3K API ve ön yüzünü derler; sunucuya yalnız değişen dosyaları gönderir.

.DESCRIPTION
    Yerel çıktıdaki her dosyanın SHA-256 özeti sunucudaki aynı yoldaki dosyanınkiyle
    karşılaştırılır. Yalnız farklı olan ya da sunucuda bulunmayan dosyalar tek bir tar
    paketiyle gönderilir. Böylece Assets (teknik resimler), LatoFont, runtimes ve ön yüz
    medya dosyaları değişmedikçe yeniden gitmez; değiştiklerinde kendiliğinden gider.

    Dosya düzeni değişmez: her dosya sunucuda eskisiyle aynı yola yazılır. Sunucuda olup
    yerel çıktıda olmayan dosyalar (Uploads, appsettings, eski paketler) silinmez.

    Sunucuda gereken araçlar: bash, sha256sum, tar, xargs (Ubuntu'da hazır gelir).

.EXAMPLE
    ./scripts/dagitim/Invoke-Dagitim.ps1
    API'yi publish eder, ön yüzü derler, ikisinin de yalnız değişen dosyalarını gönderir
    ve API servisini yeniden başlatır.

.EXAMPLE
    ./scripts/dagitim/Invoke-Dagitim.ps1 -Hedef Api -Kuru
    Yalnız API'yi publish eder ve neyin gideceğini listeler; sunucuda hiçbir şey değişmez.
#>
[CmdletBinding()]
param(
    [ValidateSet('Hepsi', 'Api', 'OnYuz')]
    [string] $Hedef = 'Hepsi',

    # ssh hedefi (kullanici@adres). Verilmezse THREEK_DAGITIM_SUNUCU ortam değişkeni okunur.
    [string] $Sunucu = $env:THREEK_DAGITIM_SUNUCU,

    # Derlemeden mevcut çıktıyı gönderir.
    [switch] $DerlemeyiAtla,

    # Yalnız neyin gideceğini listeler; sunucuya dosya gönderilmez, servis yeniden başlatılmaz.
    [switch] $Kuru,

    # Özet karşılaştırmasını atlayıp bütün çıktıyı gönderir (eski scp -r davranışı).
    [switch] $Tamami,

    # API dosyaları gönderildikten sonra servisi yeniden başlatmaz.
    [switch] $YenidenBaslatma,

    [string] $OnYuzDeposu = (Join-Path $env:USERPROFILE 'Desktop\3k_onyuz'),
    [string] $ApiCikti = '',
    [string] $OnYuzCikti = '',
    [string] $ApiUzakYolu = '/opt/myapi/publish',
    [string] $OnYuzUzakYolu = '/var/www/angular/3k_onyuz',
    [string] $ApiServisi = 'myapi'
)

$ErrorActionPreference = 'Stop'
$depo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (-not $ApiCikti) { $ApiCikti = Join-Path $depo 'publish' }
if (-not $OnYuzCikti) { $OnYuzCikti = Join-Path $OnYuzDeposu 'dist\3k_onyuz' }
if ([string]::IsNullOrWhiteSpace($Sunucu)) {
    throw "Sunucu belirtilmedi. -Sunucu kullanici@adres verin ya da bir kez şunu çalıştırın: " +
        "[Environment]::SetEnvironmentVariable('THREEK_DAGITIM_SUNUCU', 'kullanici@adres', 'User') " +
        "(sonra yeni bir PowerShell penceresi açın)."
}

$tarExe = Join-Path $env:SystemRoot 'System32\tar.exe'
if (-not (Test-Path -LiteralPath $tarExe)) { $tarExe = 'tar' }
$utf8 = New-Object System.Text.UTF8Encoding $false
$sonIsareti = '__3K_DAGITIM_SON__'

# Sunucu yerel çıktıdan gelmemesi gereken çalışma zamanı dosyalarını tutar; yerelde
# oluşmuş olsalar bile gönderilmezler (appsettings publish'e zaten alınmaz).
$apiHaric = @('appsettings*.json', 'Uploads/*', 'Logs/*', 'logs/*')

function Get-KabukMetni([string] $Deger) {
    # Uzak komutta yalnız tek tırnak kullanılır: Windows PowerShell 5.1 yerel programa
    # geçirdiği argümandaki çift tırnakları bozar.
    return "'" + $Deger.Replace("'", "'\''") + "'"
}

function Get-YerelDosyalar([string] $Kok, [string[]] $Haric) {
    $kokTam = (Resolve-Path -LiteralPath $Kok).ProviderPath.TrimEnd('\') + '\'
    Get-ChildItem -LiteralPath $Kok -Recurse -File -Force | ForEach-Object {
        $yol = $_.FullName.Substring($kokTam.Length).Replace('\', '/')
        foreach ($desen in $Haric) { if ($yol -like $desen) { return } }
        [pscustomobject]@{
            Yol   = $yol
            Boyut = $_.Length
            Ozet  = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
}

function Get-UzakOzetler([string] $UzakYol, [string[]] $Yollar) {
    # Yalnız yerel çıktıdaki yollar özetlenir; Uploads gibi sunucu klasörleri okunmaz.
    # Klasör yoksa (ilk dağıtım) boş döner ve her şey gönderilir.
    # Windows PowerShell 5.1 stdin'e UTF-8 BOM ve CRLF yazar; ikisi de uzakta temizlenir.
    $komut = "cd $(Get-KabukMetni $UzakYol) 2>/dev/null && { LC_ALL=C sed -e '1s/^\xef\xbb\xbf//' -e 's/\r//g' | " +
        "xargs -r -d '\n' sha256sum -- 2>/dev/null; }; echo $sonIsareti"
    $cikti = @($Yollar | & ssh $Sunucu $komut)
    if ($LASTEXITCODE -eq 255 -or $cikti -notcontains $sonIsareti) {
        throw "Sunucudaki dosya özetleri okunamadı ($Sunucu). ssh bağlantısını kontrol edin."
    }
    $ozetler = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([StringComparer]::Ordinal)
    foreach ($satir in $cikti) {
        if ($satir -cmatch '^([0-9a-f]{64}) [ *](.+)$') { $ozetler[$Matches[2]] = $Matches[1] }
    }
    return , $ozetler
}

function Get-Bolum([string] $Yol, [int] $Derinlik) {
    $parcalar = $Yol.Split('/')
    $adet = [math]::Min($parcalar.Length - 1, $Derinlik)
    if ($adet -le 0) { return '(kök)' }
    return ($parcalar[0..($adet - 1)] -join '/') + '/'
}

function Write-Ozet([object[]] $Dosyalar, [object[]] $Degisen, [int] $Derinlik) {
    $gidecek = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($dosya in $Degisen) { [void] $gidecek.Add($dosya.Yol) }

    $Dosyalar | Group-Object { Get-Bolum $_.Yol $Derinlik } | Sort-Object Name | ForEach-Object {
        $giden = @($_.Group | Where-Object { $gidecek.Contains($_.Yol) })
        [pscustomobject]@{
            'Bölüm'      = $_.Name
            'Dosya'      = $_.Count
            'Gidecek'    = $giden.Count
            'Gidecek MB' = [math]::Round(($giden | Measure-Object Boyut -Sum).Sum / 1MB, 2)
        }
    } | Format-Table -AutoSize | Out-Host

    $toplamMb = [math]::Round(($Dosyalar | Measure-Object Boyut -Sum).Sum / 1MB, 1)
    $gidenMb = [math]::Round(($Degisen | Measure-Object Boyut -Sum).Sum / 1MB, 2)
    Write-Host "Gidecek: $($Degisen.Count)/$($Dosyalar.Count) dosya, $gidenMb MB / $toplamMb MB"
    $Degisen | Select-Object -First 40 | ForEach-Object { Write-Host "  + $($_.Yol)" }
    if ($Degisen.Count -gt 40) { Write-Host "  ... ve $($Degisen.Count - 40) dosya daha" }
}

function Send-Dosyalar([string] $Kaynak, [string] $UzakYol, [object[]] $Dosyalar, [string] $Sonrasi) {
    $ad = '3k-dagitim-' + [Guid]::NewGuid().ToString('N')
    $yerelKlasor = Join-Path ([IO.Path]::GetTempPath()) $ad
    New-Item -ItemType Directory -Path $yerelKlasor | Out-Null
    try {
        $liste = Join-Path $yerelKlasor 'liste.txt'
        $paket = Join-Path $yerelKlasor "$ad.tar"
        $ozetDosyasi = Join-Path $yerelKlasor "$ad.sha256"
        [IO.File]::WriteAllText($liste, ((@($Dosyalar | ForEach-Object Yol)) -join "`n") + "`n", $utf8)
        [IO.File]::WriteAllText($ozetDosyasi, ((@($Dosyalar | ForEach-Object { "$($_.Ozet)  $($_.Yol)" })) -join "`n") + "`n", $utf8)

        & $tarExe -cf $paket -C $Kaynak -T $liste
        if ($LASTEXITCODE -ne 0) { throw "Paket oluşturulamadı." }
        $paketOzeti = (Get-FileHash -LiteralPath $paket -Algorithm SHA256).Hash.ToLowerInvariant()
        & scp $paket $ozetDosyasi "${Sunucu}:/tmp/"
        if ($LASTEXITCODE -ne 0) { throw "Paket sunucuya kopyalanamadı; sunucuda hiçbir dosya değişmedi." }

        # Paket açılmadan önce bütün olarak doğrulanır; bozuksa sunucudaki dosyalara dokunulmaz.
        # Açılan her dosya ayrıca özetiyle doğrulanır ve hedef klasörün sahibine verilir
        # (servis kullanıcısı okuyabilsin). Doğrulama başarısızsa servis yeniden başlatılmaz.
        $t = Get-KabukMetni "/tmp/$ad.tar"
        $o = Get-KabukMetni "/tmp/$ad.sha256"
        $h = Get-KabukMetni $UzakYol
        $komut = "set -e; echo '$paketOzeti  /tmp/$ad.tar' | sha256sum -c --quiet; mkdir -p $h; cd $h; " +
            "tar -xf $t --no-same-owner --no-same-permissions; " +
            "sha256sum -c --quiet $o; " +
            "tar -tf $t | xargs -r -d '\n' chown --reference=. --; " +
            "tar -tf $t | xargs -r -d '\n' dirname -- | sort -u | xargs -r -d '\n' chown --reference=. --; " +
            "rm -f $t $o" + $Sonrasi
        & ssh $Sunucu $komut
        if ($LASTEXITCODE -ne 0) {
            throw "Sunucuda paket açılırken ya da sonrasında hata oldu (çıkış kodu $LASTEXITCODE). Paket: /tmp/$ad.tar"
        }
    }
    finally {
        Remove-Item -LiteralPath $yerelKlasor -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-Hedef([string] $Baslik, [string] $Kaynak, [string] $UzakYol, [string[]] $Haric, [int] $Derinlik, [string] $Sonrasi, [string] $BasariMesaji) {
    if (-not (Test-Path -LiteralPath $Kaynak -PathType Container)) { throw "$Baslik çıktısı bulunamadı: $Kaynak" }
    Write-Host ""
    Write-Host "== $Baslik -> ${Sunucu}:$UzakYol" -ForegroundColor Cyan

    $dosyalar = @(Get-YerelDosyalar $Kaynak $Haric)
    if ($dosyalar.Count -eq 0) { throw "$Baslik çıktısı boş: $Kaynak" }

    if ($Tamami) {
        $degisen = $dosyalar
    }
    else {
        $uzak = Get-UzakOzetler $UzakYol @($dosyalar | ForEach-Object Yol)
        $degisen = @($dosyalar | Where-Object { -not $uzak.ContainsKey($_.Yol) -or $uzak[$_.Yol] -ne $_.Ozet })
    }
    # HTML en sona: index.html, başvurduğu yeni paketler sunucuya yazıldıktan sonra değişir.
    $degisen = @(@($degisen | Where-Object { $_.Yol -notlike '*.html' }) + @($degisen | Where-Object { $_.Yol -like '*.html' }))

    Write-Ozet $dosyalar $degisen $Derinlik
    if ($degisen.Count -eq 0) {
        Write-Host "Değişen dosya yok; $Baslik için sunucuya bir şey gönderilmedi." -ForegroundColor Green
        return
    }
    if ($Kuru) {
        Write-Host "Kuru çalıştırma: $Baslik dosyaları gönderilmedi." -ForegroundColor Yellow
        return
    }
    Send-Dosyalar $Kaynak $UzakYol $degisen $Sonrasi
    Write-Host $BasariMesaji -ForegroundColor Green
}

function Invoke-Derleme([string] $Klasor, [string] $Komut, [string[]] $Argumanlar) {
    Push-Location -LiteralPath $Klasor
    try {
        & $Komut @Argumanlar
        if ($LASTEXITCODE -ne 0) { throw "$Komut $($Argumanlar -join ' ') başarısız (çıkış kodu $LASTEXITCODE)." }
    }
    finally {
        Pop-Location
    }
}

$api = $Hedef -in 'Hepsi', 'Api'
$onYuz = $Hedef -in 'Hepsi', 'OnYuz'

$OutputEncoding = $utf8
$eskiKonsolKodlamasi = [Console]::OutputEncoding
try {
    [Console]::OutputEncoding = $utf8

    # Önce ikisi de derlenir; derleme hatası sunucuya dokunmadan durdurur.
    if (-not $DerlemeyiAtla) {
        if ($api) { Invoke-Derleme $depo 'dotnet' @('publish', '3K_API', '-c', 'Release', '-o', $ApiCikti, '--force') }
        if ($onYuz) { Invoke-Derleme $OnYuzDeposu 'npx' @('ng', 'build', '--configuration=production') }
    }

    if ($api) {
        $yenidenBaslat = ''
        $apiMesaji = "API gönderildi; servis yeniden başlatılmadı."
        if (-not $YenidenBaslatma) {
            # Yeni dosyalar yalnız gönderim olduysa devreye alınır; servis açılmazsa son loglar gösterilir.
            $s = Get-KabukMetni $ApiServisi
            $yenidenBaslat = "; systemctl restart $s; sleep 3; " +
                "systemctl is-active --quiet $s || { journalctl -u $s -n 40 --no-pager; exit 1; }"
            $apiMesaji = "API gönderildi; $ApiServisi servisi yeniden başlatıldı ve çalışıyor."
        }
        Invoke-Hedef 'API' $ApiCikti $ApiUzakYolu $apiHaric 1 $yenidenBaslat $apiMesaji
    }
    if ($onYuz) {
        Invoke-Hedef 'Ön yüz' $OnYuzCikti $OnYuzUzakYolu @() 2 '' 'Ön yüz gönderildi.'
    }
}
finally {
    [Console]::OutputEncoding = $eskiKonsolKodlamasi
}
