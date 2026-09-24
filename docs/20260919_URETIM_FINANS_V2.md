# Üretim ve Finans V2 — uygulama ve doğrulama kaydı

Kaynak: 19.09.2026 tarihli U1–U6 / F1–F11 / K01–K39 isterleri.
Başlangıç: backend `89d36b7`, frontend `061be65`. Bu belge bir çalışma kaydıdır;
test sonucu yazılmamış maddeler tamamlanmış kabul edilmez.

Güncel depoda V2 EF migration'ı ve K39 migration testi kaldırılmıştır. Aşağıdaki
K39 kanıtı ilk teslimin tarihsel koşumudur; depo dışında yönetilen 02–04 SQL'lerinin
bugünkü halini veya manuel canlı geçişini doğrulamaz.

Sonraki üretim planı tasarımı, plan öncesi çam öngörüsü ve finans görünümü düzeltmesi
[ayrı teslim notunda](20260919_URETIM_FINANS_UI_DUZELTMESI.md) kayıtlıdır.
Güncel sonuç: backend Debug/Release ayrı ayrı 1.259/1.259, frontend 194/194 ve production
build başarılı. Aşağıdaki 1.228/178 sonuçları ilk V2 tesliminin tarihsel koşumlarıdır.

## İş anlamı kararları

| Konu | Uygulanan sözleşme |
|---|---|
| Gerçekleşen üretim | Fiziksel tamamlanma olayının sürümlü snapshot'ı. Başlangıç/form/indirme üretim değildir. |
| İşlenen m³ | Net ahşap tüketimi; sarf ayrıca, sarf dahil toplam ayrıca etiketlenir. Dış geometrik hacim değildir. |
| Kontra/katlanır | Üretimde m³ uygulanmaz; finansın manuel hacmi üretim hesabına dönmez. |
| Finans tarihi | Gün hassasiyetinde ayrı tarih; dönem buradan türetilir. Üretim tarihi değişmez. |
| Fiyat | İşe uygulanan yöntem/tarife/fiyat snapshot'ı; dönem taşıma ve PO oluşturma yeniden fiyatlamaz. |
| Gelir | Aktif faturaların net satır tutarı. Tahsilat anlamına gelmez. |
| Gerçekleşen fark | Faturalanan net gelir eksi kayıtlı net gider; para birimleri ayrı. |
| Tahmini kâr | Uygun işlerin net bedeli eksi kayıtlı gider. Payda sıfırsa oran hesaplanmaz. |
| Fatura kapsamı | Tek PO içinde birden fazla proje/iş. Farklı PO'ları tek faturada birleştirme kapsam dışıdır. |
| Kalıcı silme | Bağımlılık varsa varsayılan ret; önizleme sürümü, ikinci onay, gerekçe, yaşayan audit ve kaynak bastırma izi gerekir. |
| Eski veri | Bilinmeyen üretim tarihi/kararı uydurulmaz; eski mali belge tutarları yeniden hesaplanmaz. |

## Aşamalar ve sahiplik

1. Yetki: bağımsız capability kodları, kullanıcı izin/ret/devral katmanı, sunucu alan koruması ve rol/kullanıcı ekranları.
2. Üretim: merkezi cins/varsayılan dahil kararı, atomik form sürümü, gerçekleşme ve tarihli rapor.
3. Finans: NET/SARF sahipliği, tarih/fiyat snapshot'ı, tutar bazlı PO/fatura ve concurrency.
4. Finans ekranları: panel/arama, belge dağıtımları, dinamik işler, giderler, audit ve belgeler.
5. İzole veritabanında migration/rollback/concurrency ve uygulama regresyon kapıları.

## Veri güvenliği

Uygulama veya canlı veritabanında bu çalışma kapsamında SQL çalıştırılmaz. Migration
ve PostgreSQL testleri yalnız test çalıştırıcısının oluşturduğu izole veritabanlarında
doğrulanır. İlişkili PO/fatura geçmişi, Grid/3K teslim/tahsis/sevk ve saha senkronizasyonu
bu çalışma nedeniyle değiştirilmez. Mevcut iş kuralı test kapıları tekrar çalıştırılır.

## Kabul matrisi — kanıt alanları

| Kriter | Kapsam | Doğrulama durumu |
|---|---|---|
| K01 | Kontrplak/katlanır için ölçü, adet ve form korunur; üretim net m³/sarf/kesim parçası üretilmez. `AmbalajUretimPolitikasi.M3HesaplanabilirMi`, `AmbalajUretimYardimcilari.M3DegerleriniHesapla`, `AmbalajFormOlusturCommandHandler.Handle`. | Geçti: `AmbalajUretimV2Tests.U1_IlkForm_NonwoodOlcuVeAdediKorur_HacimVeParcaUretmez` (kontrplak/katlanır/diğer); `U1_KaynakCinsi_KontraKorunur_BilinmeyenAhsapOlmaz`. UI `uretim-yasam-dongusu.component.spec.ts`: “nonwood hacmi sıfır yerine hesaplanmaz gösterir”. `U1U4_KarmaForm_CinsVeKesimDetaylariniKorur_MaskeliDosyadaOlcuVeHacimYoktur` gerçek PDF/XLSX üretir; `artifacts/uretim-v2/uretim-karma.pdf/.xlsx` içinde 2 ahşap + 26 kontra + 14 katlanır; m³ yalnız ahşap, nonwood PDF hücresi “—” ve açıklama notu. |
| K02 | Finansın manuel m³ değeri yeniden senkronizasyonda korunur; üretimin hesabına geri yazılmaz. `FinansService.UretimKayitlariniAktarAsync`, `AmbalajFinansSenkronizasyonu.ModelOlustur`, `AmbalajGerceklesenRaporQueryHandler.Handle`. | Geçti: gerçek PostgreSQL `FinansV2PostgresTests.KaynakNetSarf_ManuelFinansSahipligi_SilmeBastirmasi_Duzenli31Catchup`: kaynak adet/hacim değişse ve nonwood olsa da manuel finans 4 m³ / 480 EUR ve tarihi korunur. Üretim tarafı `AmbalajUretimV2Tests.K11_UcCins_185Ahsap26Kontra14Katlanir_FinansManuelM3UretimeKarismaz`: finans hacmi 234→9999 değiştirilirken üretim toplamı 420,50 m³ kalır. |
| K03 | Trafo ve AH/YG bushing kaynakları başlangıçta “Yapılmaz”; listede görünür, gerekli/üretime seçilen adet ve forma katılmaz. `AmbalajUretimPolitikasi.VarsayilanYapilmazMi`, `AmbalajKaynaklariSenkronizeEtCommandHandler.SenkronizeEtAsync`, `AmbalajPlanlamaYardimcisi.PlanDtoOlustur`. | Geçti: `AmbalajUretimV2Tests.U2_Siniflandirma_NumaraSezgisiKullanmaz`; `K03K04_KaynakVarsayilanHaric_YetkiliKararResyncteKorunur_FormaDahilOlur` üç kaynak adıyla gerçek sync/plan/form handler zincirini çalıştırır: satır kalır, dahil=false, gerekli/seçili/proje toplam adedi=0; form başarısız ve snapshot yok. |
| K04 | Yetkili “Yapılır” kararı sonraki kaynak yenilemesinde korunur; satır proje toplamı/formuna girer. `AmbalajUretimSecimGuncelleCommandHandler.Handle`, `AmbalajKaynaklariSenkronizeEtCommandHandler.KaynakAlanlariniUygula`. | Geçti: `AmbalajUretimV2Tests.K03K04_KaynakVarsayilanHaric_YetkiliKararResyncteKorunur_FormaDahilOlur` önce yetkisiz 403/no-change, sonra yetkili dahil kararı, ölçü değişikliğiyle resync, dahil kararının korunması ve başarılı formu doğrular. Audit, onay oturumu 99 yerine başlatan kullanıcı 7 ile kaydedilir. |
| K05 | İlk başarılı form bekleyen kayıtları “Üretimde” yapar; önceden ayrıca üretime alma şartı yoktur. `AmbalajFormOlusturCommandHandler.Handle`. | Geçti: `AmbalajUretimV2Tests.U1_IlkForm_NonwoodOlcuVeAdediKorur_HacimVeParcaUretmez`, `U3_IlkFormRetry_TekSurumVeGecis_SonrakiSurumSnapshotiDegistirmez`; gerçek PG `AmbalajUretimV2PostgresTests.Form_AyniAnahtarParalelRetry_TekSurumVeTekDurumGecisi`. UI “ilk kaynak formu önceden üretime alma gerektirmez” ve “202 form sonucu uygulandı sinyali veya başarılı toast üretmez” testleri geçti. |
| K06 | Çift tıklama/retry/eşzamanlı aynı istek tek form/sürüm/durum hareketi oluşturur; aynı anahtar farklı içerikle 409 döner. `AmbalajFormOlusturCommandHandler.Handle`, `AmbalajFormIslemKilidi.KilitleAsync`. | Geçti: gerçek iki DbContext/paralel işlem içeren `AmbalajUretimV2PostgresTests.Form_AyniAnahtarParalelRetry_TekSurumVeTekDurumGecisi`; birim `U3_IlkFormRetry_TekSurumVeGecis_SonrakiSurumSnapshotiDegistirmez`. UI “form retry aynı anahtarı korur; değişen seçim yeni anahtar kullanır” testi geçti. |
| K07 | GET/PDF/yeniden indirme yeni üretim yaratmaz; yeni form sürümü eski snapshot'ı ve gerçekleşmeyi değiştirmez. `AmbalajFormSurumleriQueryHandler.Handle`, `AmbalajYasamDongusuYardimcisi.FormDtoAsync`; salt-okuma eski form handler'ları. | Geçti: `AmbalajUretimV2Tests.U3_IlkFormRetry_TekSurumVeGecis_SonrakiSurumSnapshotiDegistirmez`; `AmbalajUretimAkisRegresyonTests.GetUretimFormu_SnapshotYoksaYeniFormUretmezVeDurumDegistirmez`, `UretimFormu_M3DegerlerindeKaydedilmisSnapshotiKullanir`, `UretimFormuDosyasi_SeciliKaydiDogruIcerikTuruVeAdlaDondurur`. Gerçek PG `Form_XminEskiYazmayiReddeder_VeSnapshotBaglantisiFizikselSilmeyiEngeller` eski yazmayı ve snapshot bağlı kaydın silinmesini reddeder. PDF/XLSX fixture aynı saklı form projeksiyonundan üretilir. |
| K08 | Hatalı seçim/finans hatasında toplu form, seçim, durum, audit ve snapshot yarım uygulanmaz. `AmbalajFormOlusturCommandHandler.Handle`, `AmbalajYasamDongusuBehavior.Handle`, `UnitOfWork.ExecuteInTransactionAsync`. | Geçti: `AmbalajUretimV2Tests.U3_GecersizTopluSecim_HicbirDurumuDegistirmez`; gerçek PG `AmbalajUretimV2PostgresTests.Form_FinansHatasi_SnapshotSecimAuditAtomikRollback` save sonrası finans hatası enjekte eder, ayrı bağlantıyla bekleyen durum/unselected/unlocked ve boş form/bağlantı/audit tablolarını doğrular. UI 202 pending sonuçlarında yerel “uygulandı” güncellemesi yapılmadığı ayrıca testlidir. |
| K09 | Geri alma/yeniden açma bağımsız izin ve gerekçe gerektirir; aynı fiziksel parti yeniden tamamlanınca ikinci kez sayılmaz. `AmbalajYasamDongusuBehavior.Handle`, `AmbalajYasamDongusuYardimcisi.GerceklesmeyiKaydetAsync`. | Geçti: `AmbalajUretimV2Tests.U6_YenidenAcma_AyriIzinOlmadanHandlerCalismaz` 403 ve handler'ın çalışmamasını; `U4_TamamlaAcTamamla_TekGerceklesme_GuncelKayitDegisimiGecmisiDegistirmez` tek gerçekleşme/eski adedin korunmasını doğrular. UI “durum 202 ve gerekçesiz yeniden açma uygulanmış gibi görünmez” testi geçti. Düzeltme aynı snapshot'ı ezmez; ardıl sürüm oluşturulması K10 testinde doğrulanır. |
| K10 | 1–30 Eylül sınırları gün hassasiyetinde dahildir; günlük/aylık/proje özetleri aynı snapshot kümesinden gelir. `AmbalajGerceklesenRaporQueryHandler.Handle` önce en son gerçekleşme sürümünü seçer, sonra `[başlangıç, bitiş+1)` uygular. | Geçti: `AmbalajUretimV2Tests.U4_U5_TarihSinirlari_GunAyProjeEsit_DuzeltmedeYalnizSonSurum`: 01.09 dahil, 30.09 23:59:59 dahil, 01.10 hariç; adet/net m³ gün/ay/proje mutabakatı, değişmez eski snapshot ve yalnız son düzeltme sürümü. UI servis “takvim aralığını saat dilimi dönüştürmeden gönderir”; component “hızlı tarih değişiminde eski rapor yanıtı yeni raporu ezmez” testleri geçti. |
| K11 | Cins bazında 185 ahşap / 420,50 net m³, 26 kontra ve 14 katlanır ayrı gösterilir; finans m³ üretime eklenmez. `AmbalajGerceklesenRaporQueryHandler.Handle`, `AmbalajYasamDongusuYardimcisi.Maskele`, `AmbalajRaporDosyaService`. | Geçti: `AmbalajUretimV2Tests.K11_UcCins_185Ahsap26Kontra14Katlanir_FinansManuelM3UretimeKarismaz`: 225 adet/420,50 m³, üç cins, nonwood net/sarf=null; finans m³ değişse de sonuç sabit. Ayrı karma çıktı fixture'ı `uretim-karma.pdf/.xlsx` 42 adetlik okunabilir formu; `uretim-maskeli.pdf/.xlsx` ölçü/m³/sarf olmadan adedi korumayı doğrular (fixture, 225 adetlik gerçekleşme örneğinden farklıdır). `AmbalajAlanMaskelemeTests` JSON ve dosya projeksiyonlarının null/alan izni sözleşmesini kapsar. |
| K12 | Proje bütünlüğü, dönem/iş türü filtresi dışında kalan gerekli sarfı da kapsar. | Gerçek PG `FinansV2PostgresTests.CokProjePo_7000_3000Fatura_SarfAcikTamamlanmaz_PoZorunlu_TarihYaslandirmayiDegistirmez`: ana iş tamamen faturalanırken farklı dönemdeki sarf açıktır; proje iki iş/10.500 EUR ve Devam Ediyor kalır. |
| K13 | Kalıcı kaynak kimliği ve NET/SARF bileşeni tekrar aktarımı tekilleştirir. | PG `KaynakNetSarf_ManuelFinansSahipligi_SilmeBastirmasi_Duzenli31Catchup`: çift sync iki kayıt; net 2,5 / sarf 0,6 m³ ayrı. Mevcut `FinansModuluTests` aktarım regresyonları da koşuldu. |
| K14 | Gün hassasiyetinde finans tarihi, üretimden ve manuel sahiplikten ayrıdır. | PG `KaynakNetSarf_ManuelFinansSahipligi_SilmeBastirmasi_Duzenli31Catchup` tarih/sync koruması; `TutarDagitimi_KismiPoFatura_AsimRollback_Tamamlama_ve_GunHassasPanel` panel/dosya tutarlılığı; çok proje testi üretim tarihinin sabit kalması. Finans listeleri `FinansTarihi`/dönemi kullanır. |
| K15 | Genel arama seçilen dönem dışında proje/PO/fatura bulur; sonuç finans dönemini taşır. | PG `K15_GenelArama_SeciliAyDisindakiProjePoVeFaturayiBulur_DigerFiltreleriKorur`: Eylül filtresi dışında Aralık projesi/PO/fatura, büyük-küçük harf ve diğer filtreler; `%`, `_`, `\` gerçek metin eşlemesi, gider araması dahil. Testin yakaladığı tr-TR istemci/DB harf dönüşümü farkı parametreli PostgreSQL ILIKE ile giderildi; joker karakterler kaçırılır. |
| K16 | Yeni tarife geçmiş iş/PO/fatura snapshot'ını değiştirmez; tarih taşıması yeniden fiyatlamaz. | PG `IkiWorkerEszamanliCatchup_TarifeTarihSnapshotlariVeDortFiyatYontemi`: eski 100/200 fiyatları korunur, yeni ay 300 alır. Çok proje testi finans tarihi taşınırken fiyat/tutarı doğrular. |
| K17 | Adet, m³, sabit ve manuel toplam aynı merkezi fiyat politikasından geçer. | PG `IkiWorkerEszamanliCatchup_TarifeTarihSnapshotlariVeDortFiyatYontemi`: 200/250/100/1.200 EUR net, KDV ve kısmi PO bakiyeleri; `FinansModuluTests` kaynak türü/tarife regresyonları. |
| K18 | 10.000 EUR iş 6.000 + 4.000 PO'ya ayrılabilir; tutar fiziksel adet gibi yazılmaz. | PG `TutarDagitimi_KismiPoFatura_AsimRollback_Tamamlama_ve_GunHassasPanel`: kalan 4.000, iki PO ve sonunda Faturalandı; eşzamanlı aşım testi toplam kapasiteyi korur. |
| K19 | Fatura mevcut ve aktif PO kalemine dayanır. | PG çok proje testi eksik/iptal PO için red ve boş fatura tablosu; frontend dağıtım ve servis sözleşme testleri PO kimliği/kalemiyle gönderimi doğrular. Yetki HTTP zinciri doğrudan uç çağrılarını ayrıca kapsar. |
| K20 | PO ve belge mutabakatı üzerinden fatura tavanı aşılamaz. | PG `TutarDagitimi_KismiPoFatura_AsimRollback_Tamamlama_ve_GunHassasPanel` 6.001/6.000 reddi ve audit rollback; `TutarRevizyonu_GerceklestirilenTutarinAltinaInemez_FaturaAsimiRollback` düzenlemede 7.001/7.000 reddi. |
| K21 | Kısmi faturadan sonra gerçek net bakiye kalır; son fatura kapatır. | PG çok proje testi ana 10.000 EUR satıra 7.000 ardından 3.000: önce Kısmi Faturalandı/3.000 kalan, sonra Faturalandı. |
| K22 | Tek PO ve faturada çok proje/iş satırı kendi PO kalemine bağlıdır. | PG çok proje testi iki proje/iki kalem, 8.000 + 4.000 faturalar ve toplam 12.000; `CokProjePo_MixedCurrencyRollback_PdfAuditFailOrphanYok` başlıkta iki proje korunur. |
| K23 | İki bağımsız bağlantı aynı açık kapasiteyi birlikte aşamaz. | PG `EszamanliPo_KapasiteyiAsamaz_ve_HataliBelgeHicKayitBirakmaz`: 10.000 işe paralel 6.000 taleplerinin yalnız biri başarılı; doğrulama bağlantısında toplam 6.000. `TransactionConcurrencyMappingTests` sarmalanmış PostgreSQL yarış hatalarını da doğrular. |
| K24 | PO/fatura düzenleme ve iptal bakiyeleri yeniden hesaplar; kısmi PO'nun bitmesi bütün işi kapatmaz. | PG `TutarRevizyonu_GerceklestirilenTutarinAltinaInemez_FaturaAsimiRollback`: gerçekleşmiş fatura altına PO azaltılamaz, 7.000 faturaya rağmen 3.000 sipariş bakiyesi kalır. Mevcut iptal/geri alma `FinansModuluTests` koşuldu. |
| K25 | Para birimleri ayrı; karışık para birimli belge reddedilir. | PG `CokProjePo_MixedCurrencyRollback_PdfAuditFailOrphanYok`: EUR/USD seçimi rollback ve boş PO; panel/rapor projeksiyonları para birimi bazlıdır, kurla gizli birleştirme yoktur. |
| K26 | Düzenli iş catch-up, tekrar ve iki worker altında tekildir; kısa ay ay sonuna kırpılır. | PG `IkiWorkerEszamanliCatchup_TarifeTarihSnapshotlariVeDortFiyatYontemi`: ayrı bağlantılar/barrier ile iki worker, toplam beş benzersiz dönem; 31 Ocak/28 Şubat/31 Mart/30 Nisan/31 Mayıs; tekrar sıfır. |
| K27 | Dinamik şablon zorunlu/typed alanları ve fiyat bileşenleri sürümlüdür. | PG `SablonSurumu_Bilesenler_BelgeSurumu_ve_MigrationIdempotency`: eksik zorunlu alanda fiyat/sürüm/audit değişmez; 1,2 m³×500 + 0,5 m³×800 + 200 sabit = 1.200 EUR. Frontend `finans-kayit-detay.component.spec.ts` kayıt yeniden açıldığında eski şablon sürümünü/değerlerini/bileşenlerini korur, maskeli fiyatı yanlışlıkla kaydetmez. |
| K28 | Gider kategori/kalem, miktar/KDV/avans ve proje ilişkisi; aynı filtreli PDF/Excel. | PG `TutarDagitimi_KismiPoFatura_AsimRollback_Tamamlama_ve_GunHassasPanel`: 1.000 avans gelir-gidere eklenmez, bağlı 2.500 gerçek gider sayılır; beş rapor PDF/XLSX üretilir. Frontend `finans-kategoriler.component.spec.ts` ve gider testleri kategoriye bağlı kalem, pasif eski seçim ve düzenlemede korunmuş fiyatı doğrular. |
| K29 | Finans tarihi taşınması ilk bekleme/PO yaşını değiştirmez. | PG çok proje testi: ana iş 30 gün, sarf 79 gün; Kasım'a tarih taşındığında bütün yaşlandırma satırları değişmeden kalır. `YaslandirmaAraliklari_Cakismiyor` sınır gruplarını doğrular. |
| K30 | Sabit sunucu capability + modül kökü gerekir; sahte header ve başka menünün W izni yetmez. | `FinansAlanHttpSecurityTests.Http_JwtGerekir_SahteHeaderYetkiVermez_AyniTokenIleIptalAnindaEtkin`, `Http_ModulReddedilirseTumAltIzinlerVeSahteHeaderApiErisimiSaglamaz`; `ServerAuthorizationRegressionTests`. Gerçek controller/JWT/pipeline, izole iş servisiyle çalışır. |
| K31 | Kullanıcı ret → kullanıcı izin → rol → ret; kişisel izin sadece hedef kodu açar, eski token bypass edemez. | `GranularYetkiTests.GercekRolService_KisiselRetIzinVeRolOnceliginiUygular`, `RolIptali_AyniServisVeAyniOturumdaSonrakiIstegiEngeller`, `KisiselAlanIzni_YazmaEylemineVeyaDigerKodaDonusmez`; gerçek PG `GranularYetkiPostgresTests.IzolePostgres_GecisKritikYetkiVermez_KisiselRetAnindaEtkin_RerunIptaliGeriAlmaz`. |
| K32 | Salt-okuma rolü yazma/kritik üretim izinlerini almaz. | `GranularYetkiTests.KatalogBagimsizdir_SaltOkumaSablonlariYazmaIzniIcermez`, `UretimModuluRootRet_AcikFormIzniyleAsilamaz`; `AmbalajUretimV2Tests.U6_YenidenAcma_AyriIzinOlmadanHandlerCalismaz`; frontend directive testleri R/W ayrımı ve canlı izin değişikliğini doğrular. |
| K33 | Alan izni UI'dan ibaret değildir: JSON null, dosya red, audit/dinamik kaplar maskeli. | `FinansAlanHttpSecurityTests` alt alan, panel, audit, rapor verisi/binary ve aynı tokenla indirme iptal testleri; `FinansAlanProjeksiyonuTests`, `AmbalajAlanMaskelemeTests`; frontend `finans-yetki-render.component.spec.ts` ve detay editörü negatif senaryoları. |
| K34 | İptal kullanıcı/zaman/gerekçe audit'i bırakır; aktif mali kapasite yeniden hesaplanır. | PG `TutarRevizyonu_GerceklestirilenTutarinAltinaInemez_FaturaAsimiRollback`: 7.000 EUR fatura iptalinde faturalanan=0, fatura bakiyesi=7.000, sipariş bakiyesi=3.000; eski kalem snapshot'ı korunur; aktör 7, gerçek zaman aralığı ve gerekçe audit'te doğrulanır. Çok proje testinde iptal PO'ya fatura reddedilir. |
| K35 | Kalıcı silmede ayrı izin, güncel önizleme, ikinci onay ve gerekçe gerekir. | PG `KaynakNetSarf_ManuelFinansSahipligi_SilmeBastirmasi_Duzenli31Catchup`: eski sürüm ve eksik ikinci onay reddi; frontend detay testleri stale önizleme/iki adımı, sunucu sabit yetki kodu erişimi korur. |
| K36 | İlişkili/ortak mali belgeler varken silme engellenir; kaynak işi silinince sync yeniden yaratmaz. | PG `CokProjePo_MixedCurrencyRollback_PdfAuditFailOrphanYok`: geçerli sürüm/ikinci onayla zorlanan bağlı iş silme reddedilir; iki projeye ait PO kalemleri/tutarları, işler ve audit aynen kalır. Kaynak/bastırma testinde kalıcı silme sonrası yeniden sync tek SARF bırakır ve bastırma kaydı yaşar. Cascade ile ortak PO silme yolu yoktur. |
| K37 | Belge kimlik bazlı, içerik doğrulamalı ve sürümlüdür; içerik/metadata/audit atomiktir. | PG `SablonSurumu_Bilesenler_BelgeSurumu_ve_MigrationIdempotency`: 1/2 sürümleri, güvenli ad ve eski PDF'nin birebir byte indirilmesi. `CokProjePo_MixedCurrencyRollback_PdfAuditFailOrphanYok`: audit trigger hatasında belge tablosu boş; HTTP dosya izin testleri. PDF/Excel becerilerinin yönlendirdiği görsel kontrolde üretilen örnekler render edilerek incelendi. |
| K38 | Sayfalı liste, global toplam ve tam filtreli export ayrıdır. | PG ilk tutar dağıtımı testi `PageSize=1` iken iş/PO/fatura toplamı 10.000; beş PDF/XLSX raporu aynı filtreyle gerçek servis üzerinden üretilir. Frontend servis testleri sayfa/tarih/filtre sözleşmesini doğrular; eski arama yanıtları iptal edilir. |
| K39 (tarihsel) | İlk teslimdeki 01–04 EF geçişinin snapshot ve tek transaction davranışı sınanmıştı. Bu yol artık kullanılmaz. | O tarihteki `UretimFinansMigrationPostgresTests` üç K39 vakası sentetik V1 şemasında geçmişti. Test sınıfı ve EF V2 migration'ı kaldırıldı; bu sonuç güncel DBA SQL'lerini veya canlı yedek provası yapılmış olduğunu göstermez. |

## Değişen dosya grupları ve sözleşmeler

- Core: üretim form/gerçekleşme varlıkları; finans tarih, tutar dağıtımı, dinamik şablon/bileşen, belge/audit/bastırma modelleri; capability kataloğu ve kullanıcı izin modeli.
- Application: Ambalaj yaşam döngüsü command/query/handler'ları; finans V2 request ve rapor sorguları; sabit sunucu authorization/approval, kullanıcı/rol izin yönetimi. EF/Infrastructure bağımlılığı eklenmedi.
- Infrastructure: mevcut `FinansService` partial dosyaları genişletildi; ayrı yeni finans sistemi kurulmadı. PostgreSQL mapping/transaction, raporlar ve dosya snapshot'ları burada.
- API: ince controller adaptörleri ve finans alan projeksiyon filtresi. Yeni üretim `POST/GET uretim-formlari`, `GET uretim-formlari/{id}/dosya`, `GET gerceklesen-uretim-raporu[/dosya]`, `PUT gerceklesmeler/{id}`. Finans `panel`, `hareketler`, `genel-arama`, `yaslandirma`, gerekçeli `is-kayitlari/{id}/finans-tarihi` / `fiyatlandirma`, `sablonlar`, `belgeler`, `kalici-silme[/onizleme]`, `raporlar/ozet/{tur}/{format}`. Mevcut sipariş/fatura/gider uçları yeni tutar sözleşmesini kullanır.
- Frontend: mevcut iki modül içinde küçük standalone yaşam döngüsü/panel/dağıtım/detay/belge/şablon/kategori/rapor bileşenleri. Aktif servisler `core/services`, DTO'lar `shared/models`; sunucu sayfalama, stale-response koruması, onay bekleyen `202`, null/erişilemez alan sözleşmesi ve TR/EN metinleri.
- Ortak API sarmalayıcısında `202` ve gövde `isSuccess=false` ayrımı korundu ve test eklendi. Model alanlarında `null` yetkisiz veya uygulanmayan değeri temsil eder; sahte sıfır değildir. Fiyat düzenlemesi, gerekli snapshot/alan izinleri eksikse maskeli veriyi sıfırla kaydetmez.

## Geçiş ve rol ataması

Tek dağıtım kılavuzu: [scripts/database/README.md](../scripts/database/README.md).
Önizleme → yedek/staging → bakım penceresi → 01/02/03/04 → backend+frontend → rol ataması → doğrulama sırasını izleyin.
02–04 SQL'leri depo dışında yönetilir; .NET derlemesi veya uygulama paketi bunları gerektirmez.
DBA her SQL'i kendi transaction'ında elle uygular; backend, SQL sırası doğrulandıktan sonra yayımlanır.
Bu sürüm için EF V2 migration'ı çalıştırılmaz ve `__EFMigrationsHistory` elle işaretlenmez.
Eski InitialCreate bütün çalışan şemayı temsil etmediği için boş DB kurulum aracı olarak sunulmaz.

İzin matrisi ve ilk rol eşlemesi: [Granular yetki kılavuzu](guvenlik/GRANULAR_YETKI_20260919.md).
Eski R/W kontrollü eşlenir; bütün kritik izinler eski W'ye otomatik verilmez.
Kullanıcı açık reddi Admin dahil önceliklidir; runtime rol-ID bypass yoktur.
Onay yürütmesi başlatanı tekrar yetkilendirir. Bekleyen onay uygulanmış işlem değildir.

## Doğrulama ve sınırlar

Son Debug/Release, kural eşlemesi, gerçek PostgreSQL ve coverage yolları:
[TEST_KAPSAMI.md](is-kurallari/TEST_KAPSAMI.md). Sayılar yalnız ilgili koşum için geçerlidir;
test adedi bütün olası iş senaryolarının veya sıfır regresyonun garantisi değildir.
Grid/3K/saha iş kuralı kapıları aynı tam koşumda yer alır; bu modüllerin verilerine dokunulmadı.
Son iki backend koşumu **1.228/1.228 başarılı**, başarısız/atlanan sıfır;
501/501 kural–test referansı her iki TRX ile doğrulandı. `dotnet restore 3K_Proje.slnx`
ve Release çözüm derlemesi başarılı. Tam test derlemesinde mevcut SandikService/StokService
nullable uyarıları bulunuyor; bu görev kapsamında ilgili domain kodu değiştirilmedi.
Son kontrollerin ardından yalnız görev için açılan izole PostgreSQL sunucusu kapatıldı.

Frontend son doğrulaması (19.09.2026): temiz `npm ci` başarılı (574 paket),
`npm test -- --watch=false --browsers=ChromeHeadless --progress=false` **178/178 başarılı**,
`npm run build -- --progress=false` başarılı. Bootstrap Sass deprecation ve vendor
`axisIndex` duplicate-key uyarıları sürüyor; görev dışı vendor kodu değiştirilmedi.
Angular animations 21/20 uyumsuzluğu mevcut Angular 20.3.18 ile eşitlendi;
CDK 20.2.14 ile ngx-scrollbar peer uyumu sağlandı. `--force` / `--legacy-peer-deps` kullanılmadı.
Kullanıcının onayıyla kilitleyen eski `ng serve` durdurulup temiz kurulum/testlerden sonra yeniden başlatıldı.

İlk V2 teslimindeki migration sonrası model farkı kontrolü `No changes have been made to the model since the last migration` sonucuyla geçmişti. Bu tarihsel kontrol güncel manuel SQL geçişini doğrulamaz.
Rapor QA sentetik verilerle yapıldı: üretim PDF'sinde nonwood hacim “—” ve not;
finans genel raporunda EUR 10.000 gelir / 2.500 gider / 7.500 fark. XLSX hücre verileri ve
PDF aynı tutarları taşır. Çıktılar `artifacts/uretim-v2` ve `artifacts/finans-v2` altında test artefaktıdır.
XLSX önizleme aracında başlık dolgu rengi render edilmedi; dosya XML'inde başlık metinleri,
beyaz yazı ve renkli dolgu mevcuttur. Native Excel görsel kabulü yapılmış sayılmamalıdır.

Canlıya dağıtım yapılmadı. Gerçek tarihî veritabanı yedeği üzerinde staging SQL geçiş provası
ve giriş yapılmış gerçek kullanıcıyla bütün ekranların uçtan uca kabulü bu testlerin yerine geçmez;
yayından önce yapılmalıdır. HTTP yetki testleri gerçek controller/pipeline, izole iş servisi
kullanır; mali transaction testleri ayrı gerçek PostgreSQL üzerinde çalışır.
Panel/raporlar 20.000 iş/gider güvenlik sınırında anlaşılır hata verir; sessiz veri kesmez.
Yeni V2 yazmalar sonrası veri kaybettiren otomatik Down desteklenmez.
