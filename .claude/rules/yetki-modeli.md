# Yetki modeli

Bu dosya her oturumda yüklenir (yol sınırı yok). İsteğin izni nasıl bildirdiği `.claude/rules/ozellik-yazimi.md`,
Finans sekme izni `.claude/rules/finans.md` içindedir. Uçtan uca yeni izin kodu için `yeni-yetki-kodu` skill'ini kullan.

- Bütün kodlar tek kaynaktan gelir: `3K.Core/Constants/YetkiKodlari.cs` (`YetkiKodlari.Finans.*`, `YetkiKodlari.Ambalaj.*`, `YetkiKodlari.Proje.*`…). Kod literali yazma. Proje tipine bağlı kodlar `ProjeTipiYetkiKodlari.Getir(projeTipiId)`.
- Ekran, işlem ve alan kodlarının hepsi `MenuTanimlari` ağacında N/R/W olarak `YetkiAgaci` ile değerlendirilir: R görüntüleme, W işlem. Üst düğüm alt düğümün tavanıdır; üst W alt izni kendiliğinden vermez. Yetki yalnız rolden gelir: kullanıcıya özel izin/ret yoktur (`KullaniciYetkileri` tablosu `20261003_01_Kisisel_Yetkileri_Kaldir.sql` ile kaldırıldı). İstisna gerekiyorsa ayrı, dar bir rol tanımlanır.
- Admin rolü yetki bypass'ı değildir (`SistemRolleri.AdminId` = 1, kendi `RolYetkileri` ile değerlendirilir). `X-Menu-Kod` başlığı, route veya gövde yetki kaynağı değildir.
- Kullanıcıya rol atama denetimi `IRolAtamaService` (`RolAtamaService`): `kullanicilar:W` ister; kullanıcı kendi rolünü değiştiremez, sahip olmadığı izinleri içeren rolü başkasına atayamaz, Admin rolünü yalnız mevcut Admin atar.
- Yeni kod eklerken birlikte güncelle: `YetkiKodlari` + `scripts/database/` altında `MenuTanimlari` satırı (`LabelKey` = `MENU.<KOD>`) + ön yüz `src/app/core/constants/yetki-kodlari.ts` + TR/EN `MENU.*` çevirileri + `docs/guvenlik/SUNUCU_YETKI_ESLEMESI.md`.
- Finans'ta yalnız 8 sekme izni ve `finans-vade-uyarisi` vardır; alan maskesi yoktur (ayrıntı `.claude/rules/finans.md`).
- Ambalaj tarafında maskeleme açıktır: `AmbalajAlanMaskesi` ve `AmbalajYetkilendirmeYardimcisi.DtoyuMaskele` (`3K.Application/Features/AmbalajIslemleri`); yeni Ambalaj DTO'sunda maskeyi ve `AmbalajAlanMaskelemeTests`'i güncelle. Kayda bağlı ek izinler handler içinde `AmbalajIslemKurallari` + `AmbalajKritikIslemDenetimi` ile kontrol edilir.
