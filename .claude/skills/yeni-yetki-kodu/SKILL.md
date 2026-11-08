---
name: yeni-yetki-kodu
description: Yeni bir menü/ekran/işlem/alan izin kodunu uçtan uca ekler — YetkiKodlari.cs, idempotent MenuTanimlari + RolYetkileri SQL betiği, README satırı, sunucu yetki eşlemesi dokümanı, ön yüz yetki-kodlari.ts ve TR/EN MENU çevirileri. Kullanıcı yeni bir izin/yetki kodu, yeni sayfa izni veya yeni sekme düğümü istediğinde kullan.
argument-hint: "<kod> <üst düğüm kodu> [R|W] [açıklama]"
disable-model-invocation: true
---

# Yeni yetki kodu ekleme

Yetki kodları tek kaynaktan gelir; bir parçası eksik kalan kod ya ekranda görünmez ya da sunucuda 403 verir.
Aşağıdaki adımların **hepsi aynı işte** yapılır. Önce `docs/is-kurallari/GRID_3K_IS_KURALLARI.md` içindeki yetki
kurallarını ve `.claude/rules/yetki-modeli.md` dosyasını oku.

## 0. Karar ver (belirsizse kullanıcıya sor)
- Kod (kebab-case, ör. `sevkiyatlar`), sabit adı (PascalCase), üst düğüm (`ParentId` için üst kod).
- Ekran (Route'lu) mı, işlem/alan kodu (Route `NULL`) mu? Sıra (`Sira`).
- Hangi roller hangi seviyeyi (N/R/W) alır? Varsayılan: Admin W; diğer roller için mevcut ilgili izinlerden türet
  (ör. üst ekranı görebilen roller R) — para, yetki veya kalıcı veri açısından belirsizse **inşa etmeden sor**.

## 1. Id seç
`MenuTanimlari` Id'leri betiklerle sabit verilir. Kullanılmış en büyük Id'yi bul ve bir sonrakini al:

```bash
grep -rhoE "\(52[0-9]{2}, '|\"Id\" = 52[0-9]{2}" scripts/database/*.sql | grep -oE "52[0-9]{2}" | sort -u | tail -3
```

**Asla yeniden kullanma:** 5202, 5204, 5205, 5208, 5234 (silinmiş kodlara aitti; eski rol kayıtlarıyla çakışır).

## 2. Arka uç sabiti
- `3K.Core/Constants/YetkiKodlari.cs` içinde doğru iç sınıfa (`Proje`, `Sandik`, `Stok`, `Saha`, `Yedek`, `Onay`,
  `Ambalaj`, `Finans`…) `public const string X = "kod";` ekle; Türkçe `/// <summary>` yaz.
- Proje tipine bağlıysa `ProjeTipiYetkiKodlari` ve `RequestMenuPermissionResolver` (proje tipi DB'den okunur).
- Uçlar isteğin kendisinde bildirir: `ISecuredRequest` + `IRequiresMenuPermission` (`RequiredMenuKod`,
  `RequiredYetkiTipi`) veya `RequestMenuPermissionResolver.GetFixedPolicy` satırı. Kod literali yazma.

## 3. SQL betiği
`scripts/database/YYYYMMDD_NN_<Konu>_Yetkisi.sql` — idempotent, tek `BEGIN … COMMIT`, uygulama açılışında çalışmaz.
Bu dizindeki [sablon.sql](sablon.sql) dosyasını kopyalayıp yer tutucuları doldur. Şablon:
- üst düğüm yoksa veya Id/kod başka kayda aitse **hiçbir şey değiştirmeden** `RAISE EXCEPTION` ile durur,
- düğüm varsa eklemez, verilmiş izni değiştirmez,
- `MenuTanimlari` kimlik dizisini `setval` ile ileri alır,
- sonunda rol × seviye listesini `RAISE NOTICE` ile yazar.

Sonra `scripts/database/README.md` çalıştırma sırası tablosuna satır ekle (sıra numarası, dosya, ne yaptığı).

## 4. Doküman
- `docs/guvenlik/SUNUCU_YETKI_ESLEMESI.md` tablosuna "işlem / gereken izin" satırı.
- Davranış kuralı değiştiyse `GRID_3K_IS_KURALLARI.md` + `coverage/*.md` + `TEST_KAPSAMI.md` ve
  `./scripts/test/Test-IsKuraliEslemesi.ps1` doğrulaması.

## 5. Ön yüz (`C:\Users\Watarzie\Desktop\3k_onyuz`)
- `src/app/core/constants/yetki-kodlari.ts` — arka uçla birebir aynı ad ve değer (ör. `YETKI.PROJE.SEVKIYATLAR`).
- Ekran koduysa `app.routes.ts` rotasında `canActivate: [menuGuard]`, `data: { menuKod: YETKI.… }`. Kenar menüsü
  sunucudaki `MenuTanimlari` ağacından gelir (Route + `LabelKey`).
- `src/assets/i18n/tr.json` ve `en.json` → `MENU.<KOD_BÜYÜK_HARF>` (LabelKey ile aynı anahtar). İki dosyada aynı
  anahtarlar olmalı; JSON'u `JSON.parse` ile doğrula.
- Ön yüz çalışma kopyası LF'dir; arka uç CRLF (hook otomatik düzeltir).

## 6. Testler (yalnız ilgili alt küme)
```powershell
dotnet test 3K.Application.Tests/3K.Application.Tests.csproj -c Release --filter "FullyQualifiedName~YetkiModeliTests|FullyQualifiedName~ServerAuthorizationRegressionTests"
```
- Yeni korumalı uç `ServerAuthorizationRegressionTests.ButunKorumaliOperasyonlar_SunucudaTanimlidir` testini geçmeli.
- Betiğin kendisini izole PostgreSQL'de iki kez çalıştırarak (idempotentlik) sınamak için `/postgres-test`;
  örnek: `TopluSevkEtTests.Postgres_SemaVeYetkiBetikleri_ModelleAyni_TekrarCalisir`.
- Ön yüz: ilgili `*.spec.ts` (`--include`), `npx ng build --configuration development`.

## Teslim
Kodu, Id'yi, betik adını ve README sırasını, hangi rollerin hangi seviyeyi aldığını ve **betiğin yeni API'den önce
çalıştırılması gerektiğini** yaz. Commit için kullanıcıdan onay iste.
