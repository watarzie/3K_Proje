# Sunucuya dağıtım

`Invoke-Dagitim.ps1` API'yi publish eder, ön yüzü derler ve sunucuya **yalnız değişen dosyaları** gönderir.
`dotnet publish` + `scp -r publish` + `scp -r dist/3k_onyuz` + `ssh … systemctl restart` sırasının yerini alır.

## Bir kez

Sunucu adresi depoya yazılmaz; kullanıcı ortam değişkeninde tutulur:

```powershell
[Environment]::SetEnvironmentVariable('THREEK_DAGITIM_SUNUCU', 'root@<sunucu-adresi>', 'User')
```

Sonra yeni bir PowerShell penceresi açın. (Alternatif: her seferinde `-Sunucu root@<sunucu-adresi>` verin.)

## Her dağıtımda

```powershell
./scripts/dagitim/Invoke-Dagitim.ps1
```

| Seçenek | Ne yapar |
|---|---|
| `-Hedef Api` / `-Hedef OnYuz` | Yalnız birini derler ve gönderir (varsayılan `Hepsi`). |
| `-Kuru` | Derler ve neyin gideceğini listeler; sunucuda hiçbir şey değişmez. |
| `-DerlemeyiAtla` | Mevcut `publish` / `dist` çıktısını gönderir. |
| `-YenidenBaslatma` | API dosyalarından sonra `myapi` servisini yeniden başlatmaz. |
| `-Tamami` | Karşılaştırmayı atlayıp bütün çıktıyı gönderir (eski `scp -r` davranışı). |

## Nasıl çalışır

1. Yerel çıktıdaki her dosyanın SHA-256 özeti alınır.
2. Tek `ssh` çağrısıyla sunucudaki **aynı yollardaki** dosyaların özeti okunur (Uploads gibi sunucu klasörleri okunmaz).
3. Farklı ya da sunucuda olmayan dosyalar tek tar paketinde `scp` ile `/tmp`'ye gider.
4. Paket açılmadan önce bütün olarak, açıldıktan sonra dosya dosya özetiyle doğrulanır; dosyalar hedef klasörün sahibine verilir.
   Doğrulama tutmazsa servis yeniden başlatılmaz.
5. API'de dosya gittiyse `systemctl restart myapi`; servis 3 sn sonra çalışmıyorsa son loglar gösterilir ve betik hata verir.

Sonuç: kod değişince yalnız proje DLL/PDB'leri (~10 MB) gider; `Assets` (teknik resimler), `LatoFont`, `runtimes`
ve ön yüz `media` dosyaları değişmedikçe gitmez, değiştiklerinde kendiliğinden gider. Sunucuda kaybolan ya da bozulan
dosya da bir sonraki dağıtımda yeniden gönderilir.

- Dosya düzeni değişmez; uygulama dosyaları eski yollarında bulur.
- Sunucuda olup yerelde olmayan hiçbir dosya silinmez (`appsettings*.json`, `Uploads`, eski ön yüz paketleri).
- `appsettings*.json`, `Uploads/`, `Logs/` yerel çıktıda oluşmuş olsa bile gönderilmez.
- Ön yüzde `index.html` paketin en sonunda yazılır; yeni paketler ondan önce sunucuda olur.

Varsayılan yollar: API `/opt/myapi/publish`, ön yüz `/var/www/angular/3k_onyuz`, servis `myapi`,
ön yüz deposu `%USERPROFILE%\Desktop\3k_onyuz`. Hepsi parametreyle değiştirilebilir (`Get-Help ./scripts/dagitim/Invoke-Dagitim.ps1 -Full`).
