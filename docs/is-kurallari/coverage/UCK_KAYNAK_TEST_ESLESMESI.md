# 3K karşılama ve kaynak kuralları test eşlemesi

Bu matris, `GRID_3K_IS_KURALLARI.md` belgesinin 8. bölümündeki tüm `U-*`
kuralları ile 9.A-9.D bölümlerindeki `K-001`-`K-029` kurallarını çalışan
testlerle eşleştirir. Her kural kimliği ayrı satırdadır. `Birim` doğrudan
üretim davranışı assertini, `Kısmi` yalnız kuralın bir bölümünün kanıtlandığını,
`Entegrasyon` gerçek HTTP/veritabanı sınırına ihtiyaç duyulduğunu, `UI` önyüz
kapsamını ve `Açık` henüz güvenle otomatik teste dönüştürülmemiş boşluk ya da
bilinen uyumsuzluğu gösterir.

Bir test adının bulunması tek başına kuralın tüm kombinasyonlarını kanıtlamaz;
satırdaki kapsam ve not birlikte okunmalıdır. Belgede `[UYUMSUZLUK]` olarak
tanımlanan mevcut açıklar, istenen davranış kabul edilmeden regresyon testiyle
sabitlenmemiştir.

| Kural | Kapsam | Test | Not |
|---|---|---|---|
| U-001 | Birim | `GridUcKSatirMiktarSenkronizasyonTests.AnaMiktarUcEskiTekTahsisBir_IstenenEksikVeKalanGuncelMiktariKullanir` | Güncel ana ihtiyaç ile tahsis ayrımı doğrudan doğrulanır. |
| U-002 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_CokluTahsisleriAyriDtoYapar_AktifPartiKalaniniChildBazindaBolmedenKorur` | Çoklu tahsislerin ayrı DTO ve ayrı sandık miktarı üretmesi doğrulanır. |
| U-003 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_CokluTahsisleriAyriDtoYapar_AktifPartiKalaniniChildBazindaBolmedenKorur` | Çoklu tahsislerin ayrı DTO ve ayrı sandık miktarı üretmesi doğrulanır. |
| U-004 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_TahsisYoksaFiiliSandiklaTekFallbackDtoUretirVeAnaMiktariKullanir` | Tahsis yokken tek fallback DTO ve ana miktar doğrulanır. |
| U-005 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Kümülatif teslim, aktif parti miktarı, aktif sayaç ve kalan ayrı ayrı doğrulanır. |
| U-006 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Kümülatif teslim, aktif parti miktarı, aktif sayaç ve kalan ayrı ayrı doğrulanır. |
| U-007 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Kümülatif teslim, aktif parti miktarı, aktif sayaç ve kalan ayrı ayrı doğrulanır. |
| U-008 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Kümülatif teslim, aktif parti miktarı, aktif sayaç ve kalan ayrı ayrı doğrulanır. |
| U-009 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StoktanOndalikKarsilama_AdiNormalizeEder_BakiyeHareketTahsisBorcVeLokasyonuSenkronizeEder` | Alternatif toplam ve stok kırılımı birlikte artar. |
| U-010 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Donör proje çıkışı ve net kalan etkisi doğrulanır. |
| U-011 | Birim | `UcKKaynakTransferStokIadeKatalogTests.EksikGeldi_OndalikAktifPartiyiBirKezSonuclandirir_ChildVeBorcSenkronKalir` | Yeniden sevk borcu decimal miktarla ve tek kez oluşur. |
| U-012 | Birim | `GridUcKSevkPartisiYasamDongusuTests.ErkenSonuclananEksikParti_AlternatifKaynakBorcuKapatsaDaTeslimeKapanir_IadeyeAcikKalir` | Erken sonuç bayrağının teslim açıklığından farklı anlamı doğrulanır. |
| U-013 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.CokluTahsis_IkinciPartiSayaclariniSandikBazindaTutarVeTekrarIstegiKaydirmaz` | Parent ve child aktif sayaç uyumu doğrulanır. |
| U-014 | Birim | `UcKKaynakTransferStokIadeKatalogTests.MiktarValidatoru_DortOndaligiKabulEder_BesOndaligiReddeder` | Dört ondalık kabulü ve beşinci basamak reddi sınanır. |
| U-015 | Birim | `UcKKaynakTransferStokIadeKatalogTests.FazlaGeldi_NormalKalanIleStokFazlasiniAyirirVeMenseiHareketiniKorur` | Genel durum ile son karşılama tipinin Fazla Geldi işleminde ayrışması doğrulanır. |
| U-016 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.TedarikcidenOndalikKarsilama_BorcuKapatir_StokHareketiUretmezVeLokasyonuAtar` | Handler sonunda merkezi durum/kalan servisinin sonucu doğrulanır; servis tüm formülleri ortak matriste kapsanır. |
| U-017 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_CokluTahsisleriAyriDtoYapar_AktifPartiKalaniniChildBazindaBolmedenKorur` | Ana, sandık ve aktif parti alanlarının ayrı DTO değerleri doğrulanır. |
| U-018 | Birim | `GridUcKSatirMiktarSenkronizasyonTests.SaklananOrijinalMiktar_IkiListedeDeAyricaDoner_GuncelHesaplamalaraKatilmaz` | Orijinal miktarın yalnız tarihsel gösterim olduğu doğrulanır. |
| U-019 | Entegrasyon | — | HTTP rota, model binding ve yetki zinciri API entegrasyon testi gerektirir. |
| U-020 | Entegrasyon | — | HTTP rota, model binding ve yetki zinciri API entegrasyon testi gerektirir. |
| U-021 | Entegrasyon | — | HTTP rota, model binding ve yetki zinciri API entegrasyon testi gerektirir. |
| U-022 | Entegrasyon | — | HTTP rota, model binding ve yetki zinciri API entegrasyon testi gerektirir. |
| U-023 | Entegrasyon | — | HTTP rota, model binding ve yetki zinciri API entegrasyon testi gerektirir. |
| U-024 | Kısmi | `UcKSozlesmeListeVeDtoKatalogTests.TekilKomut_DinamikOnayVeReferansSozlesmesiniTasirkenTopluVeResetKomutlariYalnizGuvenlidir` | Komut sözleşmesi reflection ile doğrulanır; HTTP serileştirme entegrasyon kapsamındadır. |
| U-025 | Kısmi | `UcKSozlesmeListeVeDtoKatalogTests.TekilKomut_DinamikOnayVeReferansSozlesmesiniTasirkenTopluVeResetKomutlariYalnizGuvenlidir` | Komut sözleşmesi reflection ile doğrulanır; HTTP serileştirme entegrasyon kapsamındadır. |
| U-026 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.TekilKomut_DinamikOnayVeReferansSozlesmesiniTasirkenTopluVeResetKomutlariYalnizGuvenlidir` | Tekil ve toplu komutların güvenlik/onay marker ayrımı doğrudan doğrulanır. |
| U-026A | Birim | `UcKSozlesmeListeVeDtoKatalogTests.TekilKomut_DinamikOnayVeReferansSozlesmesiniTasirkenTopluVeResetKomutlariYalnizGuvenlidir` | Tekil ve toplu komutların güvenlik/onay marker ayrımı doğrudan doğrulanır. |
| U-027 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.TekilOnayAciklamasi_KaynakVeMiktarBilgisiniTasir` | Üç alternatif kaynak için kaynak ve kültüre uygun miktar açıklaması doğrulanır. |
| U-028 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TopluSecimHelper_AcikSecimleriOnceliklendirirVeBilesikAnahtarlaTekillestirir` | Legacy ID fallback, açık seçim önceliği ve bileşik anahtar tekilleştirmesi doğrulanır. |
| U-029 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TopluSecimHelper_AcikSecimleriOnceliklendirirVeBilesikAnahtarlaTekillestirir` | Legacy ID fallback, açık seçim önceliği ve bileşik anahtar tekilleştirmesi doğrulanır. |
| U-030 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.HataliUrun_GuncelKomutTipiDegildir_ReddedilirVeDegisiklikYapilmaz` | Desteklenmeyen tip reddi var; kabul kümesindeki her tip bu tek testte yürütülmez. |
| U-031 | Birim | `UcKKaynakTransferStokIadeKatalogTests.HataliUrun_GuncelKomutTipiDegildir_ReddedilirVeDegisiklikYapilmaz` | Küme dışı tip mutasyonsuz reddedilir. |
| U-032 | Birim | `UcKKaynakTransferStokIadeKatalogTests.OlmayanSatirVeyaBaskaSatiraAitSandikSecimi_ReddedilirVeYazmaYapilmaz` | Olmayan parent ve yabancı child varyantları Save olmadan reddedilir. |
| U-033 | Birim | `UcKKaynakTransferStokIadeKatalogTests.OlmayanSatirVeyaBaskaSatiraAitSandikSecimi_ReddedilirVeYazmaYapilmaz` | Olmayan parent ve yabancı child varyantları Save olmadan reddedilir. |
| U-034 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.CokluSandiktaGeriGonderim_SandikSecilmedenFizikselDagilimiTahminEtmez` | Çoklu tahsiste sandıksız iade mutasyonsuz reddedilir. |
| U-035 | Birim | `UcKKaynakTransferStokIadeKatalogTests.AktifSahaKaynagi_TekilUcKIsleminiMutasyonsuzReddeder` | Aktif saha ilişkisi Save ve hareket üretmeden işlemi keser. |
| U-036 | Birim | `UcKKaynakTransferStokIadeKatalogTests.SevkEdilmisSandik_TekilUcKIsleminiMutasyonsuzReddeder` | Sevk kilidi mutasyonsuz reddedilir. |
| U-037 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TerminalGridDurumu_TekilUcKIsleminiMutasyonsuzReddeder` | İptal ve Grid Kapandı theory varyantları mutasyonsuz reddedilir. |
| U-038 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TerminalGridDurumu_TekilUcKIsleminiMutasyonsuzReddeder` | İptal ve Grid Kapandı theory varyantları mutasyonsuz reddedilir. |
| U-039 | Birim | `UcKKaynakTransferStokIadeKatalogTests.KaliteTadilatta_TekilUcKIsleminiMutasyonsuzReddeder` | Tadilatta satırı Save olmadan reddedilir. |
| U-040 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TrafoSevkAktifPartisizFizikselIslemVeGridGelmediKaynakDisiIslemReddedilir` | Trafo aktif-parti ve Grid Gelmedi tip kapıları ayrı varyantlarda sınanır. |
| U-041 | Kısmi | `GridUcKParcaliSevkPartisiRegresyonTests.GeriGonderim_TeslimiSurmekteOlanAktifPartiyiBozamaz` | Özel geri gönderim kararı sınanır; Trafo varyantı ayrıca adlandırılmış değildir. |
| U-042 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TrafoSevkAktifPartisizFizikselIslemVeGridGelmediKaynakDisiIslemReddedilir` | Trafo aktif-parti ve Grid Gelmedi tip kapıları ayrı varyantlarda sınanır. |
| U-043 | Birim | `GridUcKSevkPartisiYasamDongusuTests.SayacVeSonuclanmaBayragiNullableUyusmuyorsa_LegacyAktifPartiBelirsizdir` | Belirsiz legacy sayaç sözleşmesi güvenli biçimde kapalıdır. |
| U-044 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.PA702TahsisUyumsuzlugu_KapasiteyiAsanUcKKarsilamasiniEngeller` | Kaynak miktarının tahsis kapasitesini aşması reddedilir. |
| U-045 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.StokAdiVeyaBakiyeGecersizse_HedefStokVeHareketlerDegismez` | Üst sınır/ret sonrası mutasyon olmaması kaynak yolunda doğrulanır; tüm bileşikler ortak formül testlerindedir. |
| U-046 | Entegrasyon | — | Gerçek veritabanı transaction atomikliği unit-of-work bellek doubles ile kanıtlanamaz. |
| U-047 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TrafoSevkAktifPartisizFizikselIslemVeGridGelmediKaynakDisiIslemReddedilir` | Aktif sevk yokken fiziksel işlem reddedilir. |
| U-048 | Birim | `GridUcKSevkPartisiYasamDongusuTests.ErkenSonuclananEksikParti_AlternatifKaynakBorcuKapatsaDaTeslimeKapanir_IadeyeAcikKalir` | Alternatif kaynakla kapanan erken parti kendiliğinden yeniden açılmaz. |
| U-049 | Birim | `GridUcKSevkPartisiYasamDongusuTests.KismiYenidenSevkPartisi_TeslimeAcikKalirVeAyniPartiBitmedenYeniPartiUretmez` | SevkEdildi/YenidenSevkGerekli açık parti semantiği helper düzeyinde doğrulanır. |
| U-050 | Birim | `GridUcKSevkPartisiYasamDongusuTests.KismiYenidenSevkPartisi_TeslimeAcikKalirVeAyniPartiBitmedenYeniPartiUretmez` | SevkEdildi/YenidenSevkGerekli açık parti semantiği helper düzeyinde doğrulanır. |
| U-051 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_CokluTahsisleriAyriDtoYapar_AktifPartiKalaniniChildBazindaBolmedenKorur` | DTO teslim açıklığı güvenli parti ve pozitif kalanla doğrulanır. |
| U-052 | Birim | `GridUcKSevkPartisiYasamDongusuTests.AlternatifKaynaklaIhtiyacKapanmisAktifParti_FazlaGeldiIslemineAcikKalir` | Normal kalan sıfırken güvenli aktif partide fazla kararı açık kalır. |
| U-053 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GridEksikSatirinTamamlamaSevki_EskiKalanSemantiginiTekliVeTopludaKorur` | Yeni/devam partisinin mutlak miktar, sayaç, durum ve borç etkileri tekil/toplu akışta doğrulanır. |
| U-054 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GridEksikSatirinTamamlamaSevki_EskiKalanSemantiginiTekliVeTopludaKorur` | Yeni/devam partisinin mutlak miktar, sayaç, durum ve borç etkileri tekil/toplu akışta doğrulanır. |
| U-055 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GridEksikSatirinTamamlamaSevki_EskiKalanSemantiginiTekliVeTopludaKorur` | Yeni/devam partisinin mutlak miktar, sayaç, durum ve borç etkileri tekil/toplu akışta doğrulanır. |
| U-056 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GridEksikSatirinTamamlamaSevki_EskiKalanSemantiginiTekliVeTopludaKorur` | Yeni/devam partisinin mutlak miktar, sayaç, durum ve borç etkileri tekil/toplu akışta doğrulanır. |
| U-057 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GridEksikSatirinTamamlamaSevki_EskiKalanSemantiginiTekliVeTopludaKorur` | Yeni/devam partisinin mutlak miktar, sayaç, durum ve borç etkileri tekil/toplu akışta doğrulanır. |
| U-058 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GridEksikSatirinTamamlamaSevki_EskiKalanSemantiginiTekliVeTopludaKorur` | Yeni/devam partisinin mutlak miktar, sayaç, durum ve borç etkileri tekil/toplu akışta doğrulanır. |
| U-059 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.YenidenSevkKarari_BorcuMevcutDecimalKalanaGoreSinirlar` | Yeni parti üst sınırı decimal borç ve kalanla min formülünü izler. |
| U-060 | Birim | `UcKProjeTransferTelafiTeslimKuralTests.Satir86_KismiTransferSonrasiYeniSekizAdedinTamaminiTeslimAlir` | Proje çıkışı telafi paketinin miktarı ve teslimi doğrulanır. |
| U-061 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GridEksikSatirinTamamlamaSevki_EskiKalanSemantiginiTekliVeTopludaKorur` | Tamamlanmış Grid Eksik satırında kalan ihtiyaca devam partisi açılır. |
| U-062 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TamTeslimdenSonraCekiIkiUcDuzenlenir_KalanBirTekliVeTopluAkistaTamamlanir` | Grid Tam miktar artışında devam üst sınırı yalnız yeni farktır. |
| U-063 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyPartiSayaciBosken_TamDurumdaGelenAktifSevkeEsitDegilseOtomatikDevamAcmaz` | Belirsiz legacy kayıtta otomatik devam üretilmez. |
| U-064 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.CokluTahsis_SeciliSandikParentAktifPartisininTamaminiTekBasinaTuketemez` | Parent, satır, sandık ve child aktif pay min sınırı doğrulanır. |
| U-065 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.CokluTahsis_SeciliSandikParentAktifPartisininTamaminiTekBasinaTuketemez` | Parent, satır, sandık ve child aktif pay min sınırı doğrulanır. |
| U-066 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.CokluTahsis_SecimsizTeslimAktifPartiyiChildPaylariniAsmayanSekildeDagitir` | Seçimsiz fiziksel teslim child hedeflerini aşmadan dağıtılır. |
| U-067 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.EskiTekliTeslimEndpointi_AktifPartiUstSiniriniAsamazVeTekrarCiftSaymaz` | Parent aktif sayaç parti üst sınırını aşamaz. |
| U-068 | Birim | `GridUcKSevkPartisiYasamDongusuTests.SayacVeSonuclanmaBayragiNullableUyusmuyorsa_LegacyAktifPartiBelirsizdir` | Nullable sayaç/bayrak tutarsızlığı güvenli ret üretir. |
| U-069 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.CokluTahsis_SecimsizTeslimAktifPartiyiChildPaylariniAsmayanSekildeDagitir` | Child hedefleri mevcut açıklar üzerinden kurulur ve dolu child aşılmaz. |
| U-070 | Birim | `GridUcKSevkPartisiYasamDongusuTests.YeniPartiBaslatmaBayragiFalseYapar_TakipTemizlemeParentVeChildAlanlariniNullYapar` | Aktif sevk yokken takip temizliği null sözleşmesini korur. |
| U-071 | Birim | `GridUcKSevkPartisiYasamDongusuTests.SayacVeSonuclanmaBayragiNullableUyusmuyorsa_LegacyAktifPartiBelirsizdir` | Yarım nullable takip sözleşmesi belirsizdir. |
| U-072 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyPartiSayaciBosken_EksikDurumMiktarlarEsitOlsaDaBelirsizKalir` | Legacy Eksik kaydı tahmin edilmez. |
| U-073 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyPartiSayaciBosken_TamDurumdaGelenAktifSevkeEsitDegilseOtomatikDevamAcmaz` | Legacy Tam eşitlik/eşitsizlik ayrımı doğrudan sınanır. |
| U-074 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyPartiSayaciBosken_TamDurumdaGelenAktifSevkeEsitDegilseOtomatikDevamAcmaz` | Legacy Tam eşitlik/eşitsizlik ayrımı doğrudan sınanır. |
| U-075 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyAcikPartideIlkKismiTeslim_YasamDongusunuMaterializeEderVeKalanTeslimiEngellemez` | Güvenli Bekliyor kaydı sayaç 0 ve false bayrakla materialize edilir. |
| U-076 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyAcikPartideIlkKismiTeslim_YasamDongusunuMaterializeEderVeKalanTeslimiEngellemez` | Güvenli Bekliyor kaydı sayaç 0 ve false bayrakla materialize edilir. |
| U-077 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyGelmediPartisi_AlternatifKaynakKismiKapatilmadanOnceSonuclanmisOlarakMaterializeEdilir` | Güvenli Gelmedi kaydı erken sonuçlanmış olarak materialize edilir. |
| U-078 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyGelmediPartisi_TopluTedarikciSeciliSandikKadarKapatirVeKalanDevamSevkiniKorur` | Kaynak işlemi öncesi legacy materializasyonu toplu tedarikçide doğrulanır. |
| U-079 | Birim | `GridUcKSevkPartisiYasamDongusuTests.SayacVeSonuclanmaBayragiNullableUyusmuyorsa_LegacyAktifPartiBelirsizdir` | Belirsiz geçmiş tahminle ilerlemez. |
| U-080 | Birim | `GridUcKSevkPartisiYasamDongusuTests.YeniPartiBaslatmaBayragiFalseYapar_TakipTemizlemeParentVeChildAlanlariniNullYapar` | Aktif sevksiz tam reset parent/child alanlarını null yapar. |
| U-081 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Tam Geldi yalnız açık parti kalanını bir kez parent/child ve kümülatif sayaca işler. |
| U-082 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Tam Geldi yalnız açık parti kalanını bir kez parent/child ve kümülatif sayaca işler. |
| U-083 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Tam Geldi yalnız açık parti kalanını bir kez parent/child ve kümülatif sayaca işler. |
| U-084 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Tam Geldi yalnız açık parti kalanını bir kez parent/child ve kümülatif sayaca işler. |
| U-085 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Tam Geldi yalnız açık parti kalanını bir kez parent/child ve kümülatif sayaca işler. |
| U-086 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Tam Geldi yalnız açık parti kalanını bir kez parent/child ve kümülatif sayaca işler. |
| U-087 | Birim | `UcKProjeTransferTelafiTeslimKuralTests.TelafiPaketindeSandikDoluGorunseBile_TeslimHesabiAtlanmaz` | Tek sandıklı telafi paketinde dolu child yeni paketi engellemez. |
| U-088 | Birim | `UcKProjeTransferTelafiTeslimKuralTests.TelafiSevkiHamKalaniAsarsa_Islem409IleReddedilir` | Telafi paketi ham kalanı aşarsa 409 davranışı doğrulanır. |
| U-089 | Birim | `UcKKaynakTransferStokIadeKatalogTests.EksikGeldi_OndalikAktifPartiyiBirKezSonuclandirir_ChildVeBorcSenkronKalir` | Decimal Eksik hareketi üst sınır, sayaç, borç ve erken sonuç etkileriyle tek kez işlenir. |
| U-090 | Birim | `UcKKaynakTransferStokIadeKatalogTests.EksikGeldi_TahsisKadarGirildigindeReddedilirVeSayaclarDegismez` | Tahsis kadar Eksik girişi reddedilir ve sayaçlar korunur. |
| U-091 | Birim | `UcKKaynakTransferStokIadeKatalogTests.EksikGeldi_OndalikAktifPartiyiBirKezSonuclandirir_ChildVeBorcSenkronKalir` | Decimal Eksik hareketi üst sınır, sayaç, borç ve erken sonuç etkileriyle tek kez işlenir. |
| U-092 | Birim | `UcKKaynakTransferStokIadeKatalogTests.EksikGeldi_OndalikAktifPartiyiBirKezSonuclandirir_ChildVeBorcSenkronKalir` | Decimal Eksik hareketi üst sınır, sayaç, borç ve erken sonuç etkileriyle tek kez işlenir. |
| U-093 | Birim | `UcKKaynakTransferStokIadeKatalogTests.EksikGeldi_OndalikAktifPartiyiBirKezSonuclandirir_ChildVeBorcSenkronKalir` | Decimal Eksik hareketi üst sınır, sayaç, borç ve erken sonuç etkileriyle tek kez işlenir. |
| U-094 | Birim | `UcKKaynakTransferStokIadeKatalogTests.EksikGeldi_OndalikAktifPartiyiBirKezSonuclandirir_ChildVeBorcSenkronKalir` | Decimal Eksik hareketi üst sınır, sayaç, borç ve erken sonuç etkileriyle tek kez işlenir. |
| U-095 | Birim | `UcKKaynakTransferStokIadeKatalogTests.EksikGeldi_OndalikAktifPartiyiBirKezSonuclandirir_ChildVeBorcSenkronKalir` | Decimal Eksik hareketi üst sınır, sayaç, borç ve erken sonuç etkileriyle tek kez işlenir. |
| U-096 | Birim | `UcKKaynakTransferStokIadeKatalogTests.Gelmedi_AktifPartininTamaminiBorcaCevirirVeDepoLokasyonunuDegistirmez` | Gelmedi açık partiyi sıfır karşılananla sonuçlandırır, tümünü borca çevirir ve lokasyonu korur. |
| U-097 | Birim | `UcKKaynakTransferStokIadeKatalogTests.Gelmedi_AktifPartideKismiTeslimVarsaReddedilirVeDegisiklikYazilmaz` | Kısmi teslimli aktif partide Gelmedi mutasyonsuz reddedilir. |
| U-098 | Birim | `UcKKaynakTransferStokIadeKatalogTests.Gelmedi_AktifPartininTamaminiBorcaCevirirVeDepoLokasyonunuDegistirmez` | Gelmedi açık partiyi sıfır karşılananla sonuçlandırır, tümünü borca çevirir ve lokasyonu korur. |
| U-099 | Birim | `UcKKaynakTransferStokIadeKatalogTests.Gelmedi_AktifPartininTamaminiBorcaCevirirVeDepoLokasyonunuDegistirmez` | Gelmedi açık partiyi sıfır karşılananla sonuçlandırır, tümünü borca çevirir ve lokasyonu korur. |
| U-100 | Birim | `UcKKaynakTransferStokIadeKatalogTests.Gelmedi_AktifPartininTamaminiBorcaCevirirVeDepoLokasyonunuDegistirmez` | Gelmedi açık partiyi sıfır karşılananla sonuçlandırır, tümünü borca çevirir ve lokasyonu korur. |
| U-101 | Birim | `UcKKaynakTransferStokIadeKatalogTests.Gelmedi_AktifPartininTamaminiBorcaCevirirVeDepoLokasyonunuDegistirmez` | Gelmedi açık partiyi sıfır karşılananla sonuçlandırır, tümünü borca çevirir ve lokasyonu korur. |
| U-102 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.TedarikcidenOndalikKarsilama_BorcuKapatir_StokHareketiUretmezVeLokasyonuAtar` | Alternatif tip ve uygun Grid durumunda pozitif akış doğrulanır; üç durumun tüm çapraz matrisi tek testte değildir. |
| U-103 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.TedarikcidenOndalikKarsilama_BorcuKapatir_StokHareketiUretmezVeLokasyonuAtar` | Alternatif tip ve uygun Grid durumunda pozitif akış doğrulanır; üç durumun tüm çapraz matrisi tek testte değildir. |
| U-104 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TedarikcidenOndalikKarsilama_BorcuKapatir_StokHareketiUretmezVeLokasyonuAtar` | Pozitif yeniden-sevk borcu kaynak kapısını açar ve miktar kadar kapanır. |
| U-105 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TedarikcidenOndalikKarsilama_BorcuKapatir_StokHareketiUretmezVeLokasyonuAtar` | Pozitif yeniden-sevk borcu kaynak kapısını açar ve miktar kadar kapanır. |
| U-106 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Proje çıkışı telafi istisnası gerçek proje transferiyle doğrulanır. |
| U-107 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GridTam_OtuzBesHedeftenOtuzIkiTeslimde_AlternatifKaynakKapaliKalirVeYazmaYapilmaz` | 35 ihtiyaç / 32 Grid teslim / 3 kalan örneğinde üç kaynak da mutasyonsuz kapalıdır. |
| U-108 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GridTam_OtuzBesHedeftenOtuzIkiTeslimde_AlternatifKaynakKapaliKalirVeYazmaYapilmaz` | 35 ihtiyaç / 32 Grid teslim / 3 kalan örneğinde üç kaynak da mutasyonsuz kapalıdır. |
| U-109 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StokAdiVeyaBakiyeGecersizse_HedefStokVeHareketlerDegismez` | Geçersiz kaynak miktarı/bakiye hedefi değiştirmez. |
| U-110 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TedarikcidenOndalikKarsilama_BorcuKapatir_StokHareketiUretmezVeLokasyonuAtar` | Kaynak miktarı borcu azaltır; sıfırlanan borç Grid sevk durumunu geri çeker. |
| U-111 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TedarikcidenOndalikKarsilama_BorcuKapatir_StokHareketiUretmezVeLokasyonuAtar` | Kaynak miktarı borcu azaltır; sıfırlanan borç Grid sevk durumunu geri çeker. |
| U-112 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.EksikPartiAlternatifKaynaktanKapanincaEskiPartiYenidenTeslimeAcilmaz` | Kaynak işlemi erken sonuçlanmış eski partiyi yeniden açmaz. |
| U-113 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.ProjedenKarsilamada_KendiProjeMetniReddedilirVeKaliciDegisiklikOlusmaz` | Kaynak proje sözleşmesinin ret yolu doğrulanır; üç zorunlu alan ayrı theory değildir. |
| U-114 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenKarsilamada_KendiProjeMetniReddedilirVeKaliciDegisiklikOlusmaz` | Mevcut metin-temelli kendi proje kontrolü mutasyonsuz retle sabitlenir. |
| U-115 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.AktifSahaKaynagi_TekilUcKIsleminiMutasyonsuzReddeder` | Hedef aktif saha blokajı doğrudan sınanır; donör aktif saha varyantı ayrı adlandırılmamıştır. |
| U-116 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Decimal proje aktarımı donör kullanılabilirini, iki taraf sayaçlarını, telafi borcunu ve transfer defterini doğrular. |
| U-117 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Decimal proje aktarımı donör kullanılabilirini, iki taraf sayaçlarını, telafi borcunu ve transfer defterini doğrular. |
| U-118 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Decimal proje aktarımı donör kullanılabilirini, iki taraf sayaçlarını, telafi borcunu ve transfer defterini doğrular. |
| U-119 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Decimal proje aktarımı donör kullanılabilirini, iki taraf sayaçlarını, telafi borcunu ve transfer defterini doğrular. |
| U-120 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Decimal proje aktarımı donör kullanılabilirini, iki taraf sayaçlarını, telafi borcunu ve transfer defterini doğrular. |
| U-121 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Decimal proje aktarımı donör kullanılabilirini, iki taraf sayaçlarını, telafi borcunu ve transfer defterini doğrular. |
| U-122 | UI | — | Kaynak proje/ürün filtreleri frontend bileşen testidir; backend unit kapsamı değildir. |
| U-123 | UI | — | Kaynak proje/ürün filtreleri frontend bileşen testidir; backend unit kapsamı değildir. |
| U-124 | UI | — | Kaynak proje/ürün filtreleri frontend bileşen testidir; backend unit kapsamı değildir. |
| U-125 | Açık | — | Bilinen backend yeniden-doğrulama boşluğu istenen davranış diye regresyon testine çevrilmedi. |
| U-126 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StokAdiVeyaBakiyeGecersizse_HedefStokVeHareketlerDegismez` | ID/ad/bakiye ret varyantları hedef, stok ve hareket mutasyonu olmadan doğrulanır. |
| U-127 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StokAdiVeyaBakiyeGecersizse_HedefStokVeHareketlerDegismez` | ID/ad/bakiye ret varyantları hedef, stok ve hareket mutasyonu olmadan doğrulanır. |
| U-128 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StokAdiVeyaBakiyeGecersizse_HedefStokVeHareketlerDegismez` | ID/ad/bakiye ret varyantları hedef, stok ve hareket mutasyonu olmadan doğrulanır. |
| U-129 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StoktanOndalikKarsilama_AdiNormalizeEder_BakiyeHareketTahsisBorcVeLokasyonuSenkronizeEder` | Decimal stok tüketimi, kırılım, tükenme/hareket ve kaynak bağı birlikte doğrulanır. |
| U-130 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StoktanOndalikKarsilama_AdiNormalizeEder_BakiyeHareketTahsisBorcVeLokasyonuSenkronizeEder` | Decimal stok tüketimi, kırılım, tükenme/hareket ve kaynak bağı birlikte doğrulanır. |
| U-131 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StoktanOndalikKarsilama_AdiNormalizeEder_BakiyeHareketTahsisBorcVeLokasyonuSenkronizeEder` | Decimal stok tüketimi, kırılım, tükenme/hareket ve kaynak bağı birlikte doğrulanır. |
| U-132 | UI | — | İlk 500 ve aktif stok filtresi frontend servis/bileşen entegrasyon kapsamındadır. |
| U-133 | Açık | — | 500 kayıt sınırı bilinen uyumsuzluktur; hedef davranış olarak unit teste sabitlenmedi. |
| U-134 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TedarikcidenOndalikKarsilama_BorcuKapatir_StokHareketiUretmezVeLokasyonuAtar` | Decimal tedarikçi miktarı iki hedef sayaca eklenir ve stok hareketi üretmez. |
| U-135 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TedarikcidenOndalikKarsilama_BorcuKapatir_StokHareketiUretmezVeLokasyonuAtar` | Decimal tedarikçi miktarı iki hedef sayaca eklenir ve stok hareketi üretmez. |
| U-136 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TopluTedarikci_AyniSandikSeciminiTekillestirirVeYalnizKalanKadarKarsilar` | Toplu tedarikçi seçili child kalanını tam kapatır ve gate ile ilerler. |
| U-137 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TopluTedarikci_AyniSandikSeciminiTekillestirirVeYalnizKalanKadarKarsilar` | Toplu tedarikçi seçili child kalanını tam kapatır ve gate ile ilerler. |
| U-138 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.TopluTedarikci_AyniSandikSeciminiTekillestirirVeYalnizKalanKadarKarsilar` | Normal toplu tedarikçi akışı doğrulanır; proje-transfer istisnasının özellikle yokluğu ayrı negatif test değildir. |
| U-139 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.TopluTedarikci_AyniSandikSeciminiTekillestirirVeYalnizKalanKadarKarsilar` | Başarılı toplu kayıt sınanır; karışık başarı+hata sonrası Result sözleşmesi ayrıca açık kalır. |
| U-140 | Açık | — | Açık transaction sarmalayıcısı yokluğu mimari uyumsuzluktur; istenen davranış olarak testlenmedi. |
| U-141 | Birim | `UcKKaynakTransferStokIadeKatalogTests.FazlaGeldi_StogaAktarSecilmezseHicbirSayacVeyaStokDegismez` | StogaAktar zorunluluğu ret ve mutasyonsuzlukla doğrulanır. |
| U-142 | Birim | `UcKKaynakTransferStokIadeKatalogTests.FazlaGeldi_NormalKalanIleStokFazlasiniAyirirVeMenseiHareketiniKorur` | Açık aktif parti, normal teslim ve ayrı stok fazlası, durum/tip ve menşei hareketi birlikte doğrulanır. |
| U-143 | Birim | `UcKKaynakTransferStokIadeKatalogTests.FazlaGeldi_NormalKalanIleStokFazlasiniAyirirVeMenseiHareketiniKorur` | Açık aktif parti, normal teslim ve ayrı stok fazlası, durum/tip ve menşei hareketi birlikte doğrulanır. |
| U-144 | Birim | `UcKKaynakTransferStokIadeKatalogTests.FazlaGeldi_NormalKalanIleStokFazlasiniAyirirVeMenseiHareketiniKorur` | Açık aktif parti, normal teslim ve ayrı stok fazlası, durum/tip ve menşei hareketi birlikte doğrulanır. |
| U-145 | Birim | `UcKKaynakTransferStokIadeKatalogTests.FazlaGeldi_NormalKalanIleStokFazlasiniAyirirVeMenseiHareketiniKorur` | Açık aktif parti, normal teslim ve ayrı stok fazlası, durum/tip ve menşei hareketi birlikte doğrulanır. |
| U-146 | Birim | `UcKKaynakTransferStokIadeKatalogTests.FazlaGeldi_NormalKalanIleStokFazlasiniAyirirVeMenseiHareketiniKorur` | Açık aktif parti, normal teslim ve ayrı stok fazlası, durum/tip ve menşei hareketi birlikte doğrulanır. |
| U-147 | UI | — | Fazla input ve checkbox zorunluluğu frontend panel test kapsamıdır. |
| U-148 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar` | Decimal iade yalnız Grid payını azaltır, kaynak paylarını korur ve borç/durum etkilerini doğrular. |
| U-149 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar` | Decimal iade yalnız Grid payını azaltır, kaynak paylarını korur ve borç/durum etkilerini doğrular. |
| U-150 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar` | Decimal iade yalnız Grid payını azaltır, kaynak paylarını korur ve borç/durum etkilerini doğrular. |
| U-151 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GeriGonderim_TeslimiSurmekteOlanAktifPartiyiBozamaz` | Tamamlanmamış aktif partide iade kapalıdır. |
| U-152 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar` | Decimal iade yalnız Grid payını azaltır, kaynak paylarını korur ve borç/durum etkilerini doğrular. |
| U-153 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar` | Decimal iade yalnız Grid payını azaltır, kaynak paylarını korur ve borç/durum etkilerini doğrular. |
| U-154 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar` | Decimal iade yalnız Grid payını azaltır, kaynak paylarını korur ve borç/durum etkilerini doğrular. |
| U-155 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar` | Decimal iade yalnız Grid payını azaltır, kaynak paylarını korur ve borç/durum etkilerini doğrular. |
| U-156 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar` | Decimal iade yalnız Grid payını azaltır, kaynak paylarını korur ve borç/durum etkilerini doğrular. |
| U-157 | Birim | `UcKKaynakTransferStokIadeKatalogTests.HataliUrun_GuncelKomutTipiDegildir_ReddedilirVeDegisiklikYapilmaz` | Hatalı Ürün güncel tip olarak mutasyonsuz reddedilir. |
| U-158 | Birim | `UcKKaynakTransferStokIadeKatalogTests.HataliUrun_LegacyDurumEnumDegeriniKorur` | Legacy Hatalı Ürün enum değerinin 13 sayısal kontratı doğrulanır. |
| U-159 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar` | Geçerli iade sebebiyle işlem yürür; tüm sebep enum değerleri ayrı kontrat testi değildir. |
| U-160 | Açık | — | Erişilemeyen frontend validasyon dalı bilinen uyumsuzluktur. |
| U-161 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StoktanOndalikKarsilama_AdiNormalizeEder_BakiyeHareketTahsisBorcVeLokasyonuSenkronizeEder` | Başarılı işlem sonrası child Konulan ve eksik miktarı merkezi kaynak toplamından senkronize edilir. |
| U-162 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StoktanOndalikKarsilama_AdiNormalizeEder_BakiyeHareketTahsisBorcVeLokasyonuSenkronizeEder` | Başarılı işlem sonrası child Konulan ve eksik miktarı merkezi kaynak toplamından senkronize edilir. |
| U-163 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.PA702TahsisUyumsuzlugu_KapasiteyiAsanUcKKarsilamasiniEngeller` | Fiziksel hedef tahsis kapasitesini aşarsa kalıcı mutasyon oluşmaz. |
| U-164 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.CokluTahsis_SecimsizTeslimAktifPartiyiChildPaylariniAsmayanSekildeDagitir` | Seçimsiz dağıtım deterministik child sırasını ve kapasiteyi korur. |
| U-165 | Birim | `GridUcKSatirMiktarSenkronizasyonTests.CokluTahsis_SandikFiltresiTekSatirBiraksaDaDagilimDegismez` | Etkin tahsis ve child eksik hesabı filtreyle değişmez. |
| U-166 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.StoktanOndalikKarsilama_AdiNormalizeEder_BakiyeHareketTahsisBorcVeLokasyonuSenkronizeEder` | Stok kaynağı Belirsiz lokasyonu 3K yapar; diğer pozitif tipler farklı testlerde dolaylıdır. |
| U-167 | Birim | `UcKKaynakTransferStokIadeKatalogTests.Gelmedi_AktifPartininTamaminiBorcaCevirirVeDepoLokasyonunuDegistirmez` | Gelmedi lokasyonu değiştirmez; iade koruması iade testinde de kontrol edilir. |
| U-168 | Entegrasyon | — | Gerçek transaction commit/rollback sınırı bellek unit testiyle kanıtlanamaz. |
| U-169 | Birim | `UcKKaynakTransferStokIadeKatalogTests.OlmayanSatirVeyaBaskaSatiraAitSandikSecimi_ReddedilirVeYazmaYapilmaz` | Yabancı child seçimi Save olmadan reddedilir. |
| U-170 | Kısmi | `UcKSevkKaynakSonrasiSifirlamaTests.EksikTeslimSonrasiTedarikciVeSandikSifirlama_YenidenTeslimdeSahteSevkBorcuBirakmaz` | Seçili child geri alma yolu sınanır; stok/proje kaynaklı seçili child negatif matrisi ayrı değildir. |
| U-171 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TekilReset_AktifSahaSevkKilidiVeyaGridIptaldeMutasyonsuzReddedilir` | Saha, sevk kilidi ve Grid İptal varyantları Save olmadan ve değerleri koruyarak reddedilir. |
| U-172 | Birim | `UcKKaynakTransferStokIadeKatalogTests.AktifGidenTransferVarkenDonorReset_ReddedilirVeTumDegerlerKorunur` | Aktif giden transfer donör resetini tüm değerleri koruyarak reddeder. |
| U-173 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StokHareketGeriAl_OndalikTuketimVeFazlaStoguKorunumluTersler` | Tam reset stok tüketimini iade eder ve fazla stoğu güvenli biçimde geri alır. |
| U-174 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TamReset_GelenProjeTransferiniTersler_DonoruIadeEderVeKapaliSandigiAcar` | Tam reset sayaçları, kaynak izlerini ve teslim metadata alanlarını başlangıca döndürür. |
| U-175 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TamReset_GelenProjeTransferiniTersler_DonoruIadeEderVeKapaliSandigiAcar` | Tam reset sayaçları, kaynak izlerini ve teslim metadata alanlarını başlangıca döndürür. |
| U-176 | Birim | `UcKSevkKaynakSonrasiSifirlamaTests.EksikTeslimSonrasiTedarikciVeSandikSifirlama_YenidenTeslimdeSahteSevkBorcuBirakmaz` | Seçili child fiziksel/tedarikçi payı ve durum/borç etkileri tekil-toplu varyantlarla doğrulanır. |
| U-177 | Birim | `UcKSevkKaynakSonrasiSifirlamaTests.EksikTeslimSonrasiTedarikciVeSandikSifirlama_YenidenTeslimdeSahteSevkBorcuBirakmaz` | Seçili child fiziksel/tedarikçi payı ve durum/borç etkileri tekil-toplu varyantlarla doğrulanır. |
| U-178 | Kısmi | `GridUcKParcaliSevkPartisiRegresyonTests.EksikPartiSandikBazliSifirlaninca_YalnizPartiEksigiBorctanDusulurVeKalanBorcKorunur`; `UcKKaynakTransferStokIadeKatalogTests.SandikBazliReset_DigerUcKPayiKalirkenKaliteVeSureciKorur` | Katalog mevcut aktif-parti istisnasıyla düzeltildi; üretim davranışı değiştirilmedi. Aktif Grid payını geri almanın partiyi Bekliyor açması, diğer teslimleri/borcu ve kalite/süreç bilgisini koruması testli. Aktif Grid payı olmayan tüm legacy/karşılamalı dağılımların etiket matrisi burada bütünüyle doğrulanmaz. |
| U-179 | Birim | `UcKSevkKaynakSonrasiSifirlamaTests.EksikTeslimSonrasiTedarikciVeSandikSifirlama_YenidenTeslimdeSahteSevkBorcuBirakmaz` | Seçili child fiziksel/tedarikçi payı ve durum/borç etkileri tekil-toplu varyantlarla doğrulanır. |
| U-180 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TamReset_GelenProjeTransferiniTersler_DonoruIadeEderVeKapaliSandigiAcar` | Gelen aktif transfer pasifleşir ve donör çıkışı iade edilir. |
| U-181 | Birim | `UcKKaynakTransferStokIadeKatalogTests.SandikBazliReset_DigerUcKPayiKalirkenKaliteVeSureciKorur` | Başka child üzerinde 3K payı kalırken sandık bazlı reset Kalite/Süreç alanlarını korur; tam reset temizliği ayrı testtedir. |
| U-182 | Entegrasyon | — | Toplu reset özel exception ile gerçek transaction rollback davranışı entegrasyon testi gerektirir. |
| U-183 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TopluReset_SecimlerdenBiriAktifSahadaysaUygunSatiraDaDokunmadanTumIstegiReddeder` | Tek aktif saha seçimi bütün toplu isteği, uygun satıra ve Save'e dokunmadan reddeder. |
| U-184 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TopluReset_YalnizAtlananSecimlerVarsaBasarisizDonerVeKaliciMutasyonYapmaz` | Sevk kilidi, Grid İptal, başlangıç ve geçersiz child varyantları atlanır; sıfır başarıda mutasyon oluşmaz. |
| U-185 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TopluReset_StokVeyaProjeKaynakliChildSeciminiMutasyonsuzReddeder` | Kaynaklı child seçimi Save ve sayaç mutasyonu olmadan reddedilir. |
| U-186 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.AktifGidenTransferVarkenDonorReset_ReddedilirVeTumDegerlerKorunur` | Aktif giden transfer tekil reset için doğrulanır; toplu atlama varyantı ayrı değildir. |
| U-187 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TopluReset_YalnizAtlananSecimlerVarsaBasarisizDonerVeKaliciMutasyonYapmaz` | Hiçbir seçim geri alınamazsa handler başarısız döner ve Save çağırmaz. |
| U-188 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TopluReset_BirSecimBasariliDigeriAtlanirsaBasariliDonerVeYalnizUygunSatiriSifirlar` | Karışık seçimde endpoint başarı döner; yalnız uygun satır sıfırlanır ve atlanan satır korunur. |
| U-189 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TamReset_GelenProjeTransferiniTersler_DonoruIadeEderVeKapaliSandigiAcar` | Geri alınan kapalı sandık yeniden açılır. |
| U-190 | Entegrasyon | — | Toplu Tam Geldi gerçek transaction sınırı entegrasyon düzeyindedir. |
| U-191 | Kısmi | `GridUcKParcaliSevkPartisiRegresyonTests.TopluUcKTamGeldiYollari_KaliteTadilattaSatiriTeslimAlamaz` | Toplu Tadilatta blokajı testlidir; saha/kilit/terminal kombinasyonlarının tamamı bu testte değildir. |
| U-192 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Toplu Tam yalnız açık parti kalanı ile aynı merkezi parent/child sayaçlarını kullanır. |
| U-193 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Toplu Tam yalnız açık parti kalanı ile aynı merkezi parent/child sayaçlarını kullanır. |
| U-194 | Birim | `UcKProjeTransferTelafiTeslimKuralTests.TelafiPaketindeSandikDoluGorunseBile_TeslimHesabiAtlanmaz` | Dolu child istisnası yalnız doğrulanmış telafi paketinde uygulanır. |
| U-195 | Açık | — | Kısmi başarı kaydedip hata Result dönme uyumsuzluğu hedef davranış olarak regresyon testine çevrilmedi. |
| U-196 | Açık | — | Kısmi başarı kaydedip hata Result dönme uyumsuzluğu hedef davranış olarak regresyon testine çevrilmedi. |
| U-197 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.IsListesi_SayfalamayiSatirDegilProjeBazindaYaparVeFiltreOncesiOzetSayaclariniKorur` | Page sınırları, filtre öncesi özet, öncelik ve proje bazlı pagination doğrulanır. |
| U-198 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.IsListesi_SevkEdilmisProjeyiYalnizSandikDuzeltmesiAcikkenGosterir` | Sevk edilmiş proje kapalıyken gizlenir, sandık düzeltme bayrağı açılınca iş listesine gelir. |
| U-199 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.IsListesi_SayfalamayiSatirDegilProjeBazindaYaparVeFiltreOncesiOzetSayaclariniKorur` | Page sınırları, filtre öncesi özet, öncelik ve proje bazlı pagination doğrulanır. |
| U-200 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.IsListesi_SayfalamayiSatirDegilProjeBazindaYaparVeFiltreOncesiOzetSayaclariniKorur` | Page sınırları, filtre öncesi özet, öncelik ve proje bazlı pagination doğrulanır. |
| U-201 | Kısmi | `UcKSozlesmeListeVeDtoKatalogTests.IsListesi_SayfalamayiSatirDegilProjeBazindaYaparVeFiltreOncesiOzetSayaclariniKorur` | Açık teslim ve yeniden sevk örnekleri vardır; terminal/formül kombinasyonlarının tamamı tek testte değildir. |
| U-202 | Kısmi | `UcKSozlesmeListeVeDtoKatalogTests.IsListesi_SayfalamayiSatirDegilProjeBazindaYaparVeFiltreOncesiOzetSayaclariniKorur` | Açık teslim ve yeniden sevk örnekleri vardır; terminal/formül kombinasyonlarının tamamı tek testte değildir. |
| U-203 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.IsListesi_SayfalamayiSatirDegilProjeBazindaYaparVeFiltreOncesiOzetSayaclariniKorur` | Page sınırları, filtre öncesi özet, öncelik ve proje bazlı pagination doğrulanır. |
| U-204 | Kısmi | `UcKSozlesmeListeVeDtoKatalogTests.IsListesi_SayfalamayiSatirDegilProjeBazindaYaparVeFiltreOncesiOzetSayaclariniKorur` | Bellek içi filtre/grup/pagination sonucu doğrulanır; sorgu performansı entegrasyon ölçümüdür. |
| U-205 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_CokluTahsisleriAyriDtoYapar_AktifPartiKalaniniChildBazindaBolmedenKorur` | Backend karar alanları ve child aktif parti kalanı ayrı DTO satırlarında doğrulanır. |
| U-206 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_CokluTahsisleriAyriDtoYapar_AktifPartiKalaniniChildBazindaBolmedenKorur` | Backend karar alanları ve child aktif parti kalanı ayrı DTO satırlarında doğrulanır. |
| U-207 | Kısmi | `UcKProjeTransferTelafiTeslimKuralTests.TelafiPaketindeSandikDoluGorunseBile_TeslimHesabiAtlanmaz` | Telafi merkezi kalan semantiği domain testinde vardır; DTO özel varyantı ayrıca adlandırılmamıştır. |
| U-208 | UI | `uck-urunler-sevk-partisi.spec.ts — backend açık alanını tek otorite kabul eder` | Frontend aktif parti gate testi. |
| U-209 | UI | `uck-urunler-sevk-partisi.spec.ts — backend alanları yoksa eski enum tabanlı davranışı korur` | Frontend legacy fallback testi. |
| U-210 | UI | `uck-urunler-sevk-partisi.spec.ts — geri gönderim sözleşmesi yarımsa güvenli kapanır, tam legacy payload fallback kullanır` | Frontend geri gönderim kontrat testi. |
| U-211 | UI | `uck-urunler-sevk-partisi.spec.ts — Tam Geldi miktarını aktif partinin sandık bazlı kalanından alır` | Panel otomatik miktar ve input kilidi testi. |
| U-212 | UI | `uck-urunler-sevk-partisi.spec.ts — Eksik Geldi alanını kümülatif gelenle doldurmaz` | Yeni hareket miktarı kümülatif değerden başlamaz. |
| U-213 | UI | `uck-urunler-miktar.spec.ts — güncel kalanı DTO üzerinden gösterir` | Frontend gösterim sorumluluğu. |
| U-214 | UI | — | Kaydetme sonrası event ve API reload davranışı UI servis entegrasyon testi gerektirir. |
| U-215 | UI | `uck-urunler-sevk-partisi.spec.ts — çoklu sandık seçimlerini child anahtarıyla ayırır` | Frontend satır anahtarı davranışı. |
| U-216 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.TopluSecimHelper_AcikSecimleriOnceliklendirirVeBilesikAnahtarlaTekillestirir` | Backend iki seçim biçimini kabul eder; frontend payload şekli UI testidir. |
| U-217 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_ManuelSahaIceriginiTamamlanmisNegatifSentetikKimlikleMapler` | Manuel saha/yedek içerik sentetik tamamlanmış ve işleme kapalı DTO olur. |
| U-218 | UI | — | Tek çağrı, istemci filtresi ve pagination yokluğu UI/API performans entegrasyon kapsamındadır. |
| U-219 | UI | — | Aktif servis endpoint seçimi frontend servis kontratıdır. |
| U-220 | Entegrasyon | — | Legacy controller rotalarının açık olması API rota entegrasyon testi gerektirir. |
| U-221 | Kısmi | `GridUcKParcaliSevkPartisiRegresyonTests.EskiTeslimEndpointleri_GridKapandiSatiriDegistiremez` | Legacy endpoint terminal blokajı testlidir; saha/kilit/Tadilatta matrisi aynı testte tam değildir. |
| U-222 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.EskiTekliTeslimEndpointi_AktifPartiUstSiniriniAsamazVeTekrarCiftSaymaz` | Legacy tekil aşan miktarı reddeder ve tekrar çift saymaz. |
| U-223 | Açık | — | Legacy durum etiketi formülü bilinen uyumsuzluktur; hedef diye sabitlenmedi. |
| U-224 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.EskiTopluTeslimEndpointi_FazlaIstegiAktifPartiyeSinirlarVeSeciliChildSayaciniKorur` | Legacy toplu istek aktif parti sınırına kırpılır. |
| U-225 | Açık | — | Legacy sessiz kırpma/durum etiketi uyumsuzlukları istenen davranış diye sabitlenmedi. |
| U-226 | Açık | — | Legacy sessiz kırpma/durum etiketi uyumsuzlukları istenen davranış diye sabitlenmedi. |
| U-227 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.EskiTopluTeslimEndpointi_FazlaIstegiAktifPartiyeSinirlarVeSeciliChildSayaciniKorur` | Legacy toplu helper parent/child sayacı ve çift saymama sınırını korur. |
| U-228 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TopluGridIkinciParti_TekliUcKKarsilama_KalaniTamamlarVeTahsisDegismez` | Katalogda anılan ikinci parti tekil/toplu regresyon paketi mevcuttur. |
| U-229 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyGelmediPartisi_AlternatifKaynakKismiKapatilmadanOnceSonuclanmisOlarakMaterializeEdilir` | Katalogda anılan legacy materializasyon paketi mevcuttur. |
| U-230 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.YenidenSevkPartisi_EksikKarsilanincaPartiEksigiMevcutIhtiyacaBirKezEklenir` | Eksik/Gelmedi borçlarının tek kez eklenmesi testlidir. |
| U-231 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.EksikFinalizasyonuSonrasi_ParcaliGeriGonderimTekrarliCalisir` | Parçalı iade, karışık kaynak ve açık parti varyantları regresyon paketindedir. |
| U-232 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.CokluTahsis_IkinciPartiSayaclariniSandikBazindaTutarVeTekrarIstegiKaydirmaz` | Çoklu tahsis parent/child aktif sayaç regresyonu mevcuttur. |
| U-233 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.EskiTekliTeslimEndpointi_AktifPartiUstSiniriniAsamazVeTekrarCiftSaymaz` | Legacy tekil/toplu aktif parti üst sınır testleri mevcuttur. |
| U-234 | Birim | `UcKProjeTransferTelafiTeslimKuralTests.Satir86_KismiTransferSonrasiYeniSekizAdedinTamaminiTeslimAlir` | Telafi adaylığı, tek child ve miktar sınırı test paketi mevcuttur. |
| U-235 | Birim | `UcKSevkKaynakSonrasiSifirlamaTests.EksikTeslimSonrasiTedarikciVeSandikSifirlama_YenidenTeslimdeSahteSevkBorcuBirakmaz` | Kaynak sonrası tekil/toplu reset borç regresyonu mevcuttur. |
| U-236 | UI | `uck-urunler-sevk-partisi.spec.ts — aktif parti gate, Tam, Eksik, Fazla ve Geri sözleşmesi` | Katalogda anılan frontend test paketi. |
| U-237 | UI | `uck-urunler-miktar.spec.ts — güncel ana miktar, orijinal miktar ve çoklu tahsis ayrımı` | Katalogda anılan miktar gösterim test paketi. |
| U-238 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GridTam_OtuzBesHedeftenOtuzIkiTeslimde_AlternatifKaynakKapaliKalirVeYazmaYapilmaz` | Belgedeki eski test boşluğu bu hedefli üç kaynak theory testiyle kapatıldı; katalog etiketi güncellenmelidir. |
| U-239 | Açık | — | Backend ürün/proje kimliği yeniden doğrulama boşluğu korunmadı; iş kararı gerekir. |
| U-240 | Açık | — | Toplu Tam kısmi kayıt+hata Result uyumsuzluğu korunmadı; iş kararı gerekir. |
| U-241 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Aktif parti ve kümülatif teslim ayrımı ile yeni parti sayacının tek ilerlemesi doğrulanır. |
| U-242 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TekliGridIkinciParti_TopluUcKKarsilama_KalaniTamamlarVeTekrarCagriCiftSaymaz` | Aktif parti ve kümülatif teslim ayrımı ile yeni parti sayacının tek ilerlemesi doğrulanır. |
| U-243 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.CokluTahsis_SecimsizTeslimAktifPartiyiChildPaylariniAsmayanSekildeDagitir` | Parent/child sayaçları tahsis paylarını aşmadan ilerler. |
| U-244 | Birim | `GridUcKSevkPartisiYasamDongusuTests.SayacVeSonuclanmaBayragiNullableUyusmuyorsa_LegacyAktifPartiBelirsizdir` | Belirsiz legacy geçmiş güvenli biçimde kapalıdır. |
| U-245 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GridTam_OtuzBesHedeftenOtuzIkiTeslimde_AlternatifKaynakKapaliKalirVeYazmaYapilmaz` | Kabul edilen 35/32/3 kaynak kapısı doğrudan test edilir. |
| U-246 | UI | `uck-urunler-sevk-partisi.spec.ts — Tam Geldi miktarını backend aktif parti kalanından alır` | Frontend fiziksel miktarı backend kararından tüketir. |
| U-247 | Birim | `GridUcKSatirMiktarSenkronizasyonTests.SaklananOrijinalMiktar_IkiListedeDeAyricaDoner_GuncelHesaplamalaraKatilmaz` | Orijinal miktar güncel hesaplara katılmaz. |
| U-248 | Açık | — | Meta yönetişim kuralıdır; uyumsuzlukları otomatik üretim davranışına çevirmemek code review sürecidir. |
| K-001 | Birim | `GridUcKSatirMiktarSenkronizasyonTests.AnaMiktarUcEskiTekTahsisBir_IstenenEksikVeKalanGuncelMiktariKullanir` | Ana talep, tahsis ve fiziksel teslim ayrı kalır. |
| K-002 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TerminalGridDurumu_TekilUcKIsleminiMutasyonsuzReddeder` | İptal/Kapandı kaynak işlemini mutasyonsuz kapatır. |
| K-003 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GridTam_OtuzBesHedeftenOtuzIkiTeslimde_AlternatifKaynakKapaliKalirVeYazmaYapilmaz` | Alternatif kaynak kapısının kabul edilen negatif örneği üç tipte doğrulanır. |
| K-004 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.StokKarsilamasi_TahsisKapasitesiYetersizseStoguTekBasinaDusurmez` | Ana talep/tahsis kapasitesi aşımında stok dahil hiçbir yan etki kalmaz. |
| K-005 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyGelmediPartisi_AlternatifKaynakKismiKapatilmadanOnceSonuclanmisOlarakMaterializeEdilir` | Kaynak işlemi önce güvenli legacy parti izini materialize eder. |
| K-006 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.ProjedenKarsilamada_KendiProjeMetniReddedilirVeKaliciDegisiklikOlusmaz` | Kaynak ID/metin sözleşmesinin kendi proje ret yolu testlidir; tüm eksik alanlar ayrı theory değildir. |
| K-007 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Donör kullanılabilir formülü, iki taraf sayaçları, telafi borcu ve transfer zinciri doğrulanır. |
| K-008 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.AktifSahaKaynagi_TekilUcKIsleminiMutasyonsuzReddeder` | Hedef normal kaynak saha blokajı doğrudan testlidir; donör varyantı ayrıca adlandırılmamıştır. |
| K-009 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Donör kullanılabilir formülü, iki taraf sayaçları, telafi borcu ve transfer zinciri doğrulanır. |
| K-010 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Donör kullanılabilir formülü, iki taraf sayaçları, telafi borcu ve transfer zinciri doğrulanır. |
| K-011 | Birim | `UcKKaynakTransferStokIadeKatalogTests.ProjedenOndalikKarsilama_HedefDonorTransferDefteriVeTelafiBorcunuKorunumluGunceller` | Donör kullanılabilir formülü, iki taraf sayaçları, telafi borcu ve transfer zinciri doğrulanır. |
| K-012 | Birim | `UcKProjeTransferTelafiTeslimKuralTests.Satir86_KismiTransferSonrasiYeniSekizAdedinTamaminiTeslimAlir` | Tek sandıklı telafi teslim hesabı ve yeni paket miktarı doğrulanır. |
| K-013 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StoktanOndalikKarsilama_AdiNormalizeEder_BakiyeHareketTahsisBorcVeLokasyonuSenkronizeEder` | Türkçe ad normalizasyonuyla stok eşleşmesi doğrulanır. |
| K-014 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StokHareketGeriAl_OndalikTuketimVeFazlaStoguKorunumluTersler` | Stok tüketim ve hareket defteri decimal değerlerle ileri/geri korunur. |
| K-015 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.StokAdiVeyaBakiyeGecersizse_HedefStokVeHareketlerDegismez` | Kaydetme yolu ID/ad/bakiyeyi yeniden doğrular; uygun stok liste filtresi entegrasyon dışındadır. |
| K-016 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TedarikcidenOndalikKarsilama_BorcuKapatir_StokHareketiUretmezVeLokasyonuAtar` | Tedarikçi manuel miktarı kaynak sayaçlarını artırır, stok kartına dokunmaz. |
| K-017 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TopluTedarikci_AyniSandikSeciminiTekillestirirVeYalnizKalanKadarKarsilar` | Toplu tedarikçi child kalanını ve bileşik seçim tekilleştirmesini doğrular. |
| K-018 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.TopluTedarikci_AyniSandikSeciminiTekillestirirVeYalnizKalanKadarKarsilar` | Uygun satırın işlendiği kanıtlıdır; karışık başarı+hata kısmi kalıcılık sözleşmesi açık kalır. |
| K-019 | Birim | `UcKKaynakTransferStokIadeKatalogTests.FazlaGeldi_NormalKalanIleStokFazlasiniAyirirVeMenseiHareketiniKorur` | Normal ihtiyaç ve stok fazlası ayrılır; menşei stok/hareket kaydı korunur. |
| K-020 | Birim | `UcKKaynakTransferStokIadeKatalogTests.FazlaGeldi_NormalKalanIleStokFazlasiniAyirirVeMenseiHareketiniKorur` | Normal ihtiyaç ve stok fazlası ayrılır; menşei stok/hareket kaydı korunur. |
| K-021 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.StoktanOndalikKarsilama_AdiNormalizeEder_BakiyeHareketTahsisBorcVeLokasyonuSenkronizeEder` | Belirsiz lokasyon pozitif stok kaynağında 3K olur; diğer tipler ayrı testlerde dolaylıdır. |
| K-022 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar` | İade yalnız Grid fiziksel payı, güvenli durum, sebep ve decimal miktar sınırında ilerler. |
| K-023 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GeriGonderildi_OndalikGridPayiniAzaltir_KaynakPaylariniKorurVeBorcAcar` | İade yalnız Grid fiziksel payı, güvenli durum, sebep ve decimal miktar sınırında ilerler. |
| K-024 | Birim | `UcKKaynakTransferStokIadeKatalogTests.AktifGidenTransferVarkenDonorReset_ReddedilirVeTumDegerlerKorunur` | Donör aktif dış transfer bitmeden resetlenemez. |
| K-025 | Birim | `UcKKaynakTransferStokIadeKatalogTests.TamReset_GelenProjeTransferiniTersler_DonoruIadeEderVeKapaliSandigiAcar` | Hedef tam reset gelen transferi pasifleştirip donör çıkışını iade eder. |
| K-026 | Birim | `UcKKaynakTransferStokIadeKatalogTests.StokHareketGeriAl_OndalikTuketimVeFazlaStoguKorunumluTersler` | Tüketim iadesi ve fazla stok düşümü tüm hareket gruplarıyla korunur. |
| K-027 | Birim | `UcKKaynakTransferStokIadeKatalogTests.FazlaStokBaskaHareketteKullanildiysa_OnceTumGruplarDogrulanirVeHicbiriDegismez` | Tüketilmiş fazla stok kök reseti bütün grupları mutasyonsuz reddeder. |
| K-028 | Birim | `UcKSevkKaynakSonrasiSifirlamaTests.EksikTeslimSonrasiTedarikciVeSandikSifirlama_YenidenTeslimdeSahteSevkBorcuBirakmaz` | Seçili child reseti diğer payları korur ve sahte sevk borcu bırakmaz. |
| K-029 | Kısmi | `UcKKaynakTransferStokIadeKatalogTests.TamReset_GelenProjeTransferiniTersler_DonoruIadeEderVeKapaliSandigiAcar` | Tam reset alan ve sandık etkileri testlidir; toplu mixed-result/gerçek rollback entegrasyon boşluğu sürer. |
