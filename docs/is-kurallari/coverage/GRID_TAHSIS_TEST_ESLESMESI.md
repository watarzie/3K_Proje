# Grid ve tahsis kurallari test eslesmesi

Bu matris, `GRID_3K_IS_KURALLARI.md` belgesinin 7. bolumundeki Grid kurallari ile
9.E-9.H bolumlerindeki tahsis/revizyon kurallarini calisan testlerle eslestirir.
Bir satirin burada bulunmasi, tek basina tam kapsama iddiasi degildir. `Kısmi`,
`Entegrasyon`, `UI` ve `Açık` etiketleri kalan siniri bilerek gorunur tutar.
`Açık` satirlarin bir bolumu gercek test boslugudur; notunda `[FARK]` denilenler
ise mevcut farki istenen davranis olarak regresyon testine dondurmemek icin acik
birakilmistir.

| Kural | Kapsam | Test | Not |
|---|---|---|---|
| G-001 | Kısmi | `GridKomutKatalogKapsamTests.TekilGridKapandi_MiktarlariVeAktifPartiyiKorur_SandigiGridLokasyonunaAlir` | Grid durumu ile sevk alanlarinin ayri degisebildigi alan etkisiyle kanitlanir; enum/API sozlesmesinin kendisi derleme zamani kapsamindadir. |
| G-002 | Birim | `GridSozlesmeVeSorguIsKurallariTests.GridDurumKimlikleri_VeritabaniVeApiSozlesmesiniKorur` | Grid durum kümesinin 14 sayısal kimliği ve küme büyüklüğü doğrulanır; gerçek lookup tablosu içeriği bu birim testin dışındadır. |
| G-003 | Açık | — | Enum yorumundaki sayi farki is davranisi degildir; regresyon testiyle sabitlenmez. |
| G-004 | Birim | `GridSozlesmeVeSorguIsKurallariTests.GridSevkKimlikleri_GridKabulDurumundanBagimsizSozlesmeyiKorur`; `GridKomutKatalogKapsamTests.TekilBekliyor_YalnizDurumVeKullaniciIziniDegistirir_MiktarlariKorur` | Dört sevk kimliği ve Grid sonucu değişirken bağımsız sevk ekseninin korunması doğrulanır. |
| G-005 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TamTeslimSonrasiGeriGonderim_OncekiPartidenGelenMiktariDaraltmadanMevcutAkisiKorur` | Aktif/son parti miktari ile kumulatif teslimin ayri kaldigi dogrudan assert edilir. |
| G-006 | Birim | `GridUcKSevkPartisiYasamDongusuTests.YeniPartiBaslatmaBayragiFalseYapar_TakipTemizlemeParentVeChildAlanlariniNullYapar` | Yeni partide false, takip temizliginde null semantigi dogrudan sinanir. |
| G-007 | Birim | `GridKomutKatalogKapsamTests.TopluSevk_UcKIslemliSatiriAtlar_UygunSatiriSevkEderVeAtlamayiHareketeYazar` | Gercek 3K islemi olan satirin atlanmasi ve uygun satirin korunmasi handler uzerinden sinanir. |
| G-008 | Entegrasyon | — | Controller rota/HTTP baglama kontrati unit test degil API entegrasyon testi gerektirir. |
| G-009 | Entegrasyon | — | Yazma endpointlerinin rota ve yetkilendirme yuzeyi API entegrasyon testi gerektirir. |
| G-010 | Kısmi | `GridKomutKatalogKapsamTests.TekilBekliyor_YalnizDurumVeKullaniciIziniDegistirir_MiktarlariKorur` | Komut alanlarinin handlera tasinmasi sinanir; serilestirme/opsiyonellik API seviyesinde degildir. |
| G-011 | Birim | `GridKomutKatalogKapsamTests.TekilGridValidatoru_OnDortTamDortOndaligiKabulEder_BesinciOndaligiVeSevkUyumsuzlugunuReddeder` | Kimlik, 14+4 hassasiyet ve sevk durumu uyumu sinir degerlerle kapsanir. |
| G-012 | Kısmi | `GridKomutKatalogKapsamTests.TekilGridKomutu_DesteklenmeyenDurumuMutasyonsuzReddeder` | Desteklenmeyen hedefin mutasyonsuz reddi var; kabul edilen sekiz degerin her biri tek theory'de degildir. |
| G-013 | Birim | `GridKomutKatalogKapsamTests.TekilGridKomutu_AktifSahaVeyaSevkEdilmisSandiktaMutasyonsuzReddedilir` | Aktif saha ve sevk edilmis sandik varyantlari, Save/hareket olmadan ret ile kapsanir. |
| G-014 | Birim | `GridKomutKatalogKapsamTests.TekilGridKomutu_TadilattaIkenHerHedefiMutasyonsuzReddeder` | Tum destekli hedeflerde Tadilatta blokaji theory olarak sinanir. |
| G-015 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GercekUcKTeslimiBasladiktanSonra_AktifSevkMiktariEzilemez` | Gercek 3K etkinliginden sonra temel kilit ve mutasyon olmamasi sinanir. |
| G-016 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GridEksikSatirinTamamlamaSevki_EskiKalanSemantiginiTekliVeTopludaKorur` | Devam karariyla izin verilen tekil/toplu ikinci parti yolu sinanir. |
| G-017 | Birim | `GridKomutKatalogKapsamTests.TekilBekliyor_YalnizDurumVeKullaniciIziniDegistirir_MiktarlariKorur` | Basarili tekil denemede personel/aciklama izi dogrudan assert edilir. |
| G-018 | Birim | `GridKomutKatalogKapsamTests.TekilTamEksikTrafo_DurumMiktarSozlesmesiniUygular` | Tam Geldi icin Grid gelen=istenen ve Trafo=0 alan etkisi theory icinde sinanir. |
| G-019 | Birim | `GridKomutKatalogKapsamTests.TekilTamEksikTrafo_DurumMiktarSozlesmesiniUygular` | Eksik icin pozitif ve istenenden kucuk gelen ile Trafo temizligi sinanir. |
| G-020 | Birim | `GridKomutKatalogKapsamTests.TekilTemizleyenDurumlar_SevkMiktarlariniVeParentChildAktifTakibiniTemizler` | Gelmedi parent/child aktif takibi ve sevk alanlarini temizler. |
| G-021 | Birim | `GridKomutKatalogKapsamTests.TekilTamEksikTrafo_DurumMiktarSozlesmesiniUygular` | Trafo/Grid toplami ve alan etkisi pozitif senaryoda sinanir; tum gecersiz kombinasyonlar ayri theory degildir. |
| G-022 | Birim | `GridKomutKatalogKapsamTests.TekilTemizleyenDurumlar_SevkMiktarlariniVeParentChildAktifTakibiniTemizler` | Iptal sayaç/sevk temizligi theory icinde sinanir. |
| G-023 | Birim | `GridKomutKatalogKapsamTests.TekilTemizleyenDurumlar_SevkMiktarlariniVeParentChildAktifTakibiniTemizler` | Sipariste sayaç/sevk temizligi theory icinde sinanir. |
| G-024 | Birim | `GridKomutKatalogKapsamTests.TekilGridKapandi_MiktarlariVeAktifPartiyiKorur_SandigiGridLokasyonunaAlir` | Miktar/takip korunumu ve Grid lokasyonu birlikte sinanir. |
| G-025 | Birim | `GridKomutKatalogKapsamTests.TekilBekliyor_YalnizDurumVeKullaniciIziniDegistirir_MiktarlariKorur` | Bekliyor hedefinde miktar/sevk alanlarinin korunmasi sinanir. |
| G-026 | Kısmi | `GridKomutKatalogKapsamTests.TekilSevk_GridGelenUstSinirinaEsitOndalikPartiyiBaslatir` | Uyumlu Tam/Sevk Edildi yolu sinanir; tum uyumsuz Grid hedefleri ayrica kapsanmamistir. |
| G-027 | Birim | `GridKomutKatalogKapsamTests.TekilSevk_GridGelenUstSinirinaEsitOndalikPartiyiBaslatir` | Pozitif ve Grid gelen ust sinirina esit ondalik sevk dogrudan sinanir. |
| G-028 | Birim | `GridSevkTahsisKapasitesiTests.TekliIlkSevk_RevizyonSonrasiEskiTahsisYetersizsePartiBaslatmaz` | Yetersiz tahsiste yeni parti baslamamasi ve mutasyon korunumu sinanir. |
| G-029 | Kısmi | `GridSahaIsTamamlamaSenkronizasyonTests.TekliIptalVeGeriAlma_KayitSonrasiKaynakProjeyiSenkronizeEder`; `GridKomutKatalogKapsamTests.SurecOtomatikTamamlama_IptaliHaricEksigiKalmayaniTamamlar_NihaiDurumuGeriAcmaz` | Kaynak saha senkronu ve otomatik surec ayri testlerde; genel durum helper cagrisi tam durum matrisiyle kapsanmaz. |
| G-030 | Birim | `GridKomutKatalogKapsamTests.TrafoSevk_SandiktakiDigerUrunlerTamamsaSandigiOtomatikKapatir` | Ayni sandik tamamlik kosulu ve otomatik kapanma sinanir. |
| G-031 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.AktifPartiTeslimBeklerken_HenuzUcKIslemiYoksaSevkMiktariMutlakToplamOlarakGuncellenir` | Tekil/toplu 2->3 mutlak overwrite kapsanir. |
| G-032 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyAktifPartiTeslimBeklerken_HenuzUcKIslemiYoksaYeniTakipleMutlakToplamaGuncellenir` | Nullable legacy takip yeni semantige materialize edilir. |
| G-033 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.SevkMiktariAzaltilipAyniDegerTekrarKaydedildigindeToplanmaz_MutlakDegerKorunur` | 3->2->2 idempotent mutlak davranis tekil/toplu sinanir. |
| G-034 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.DecimalSevkMiktari_UcKIslemiOncesiMutlakToplamOlarakGuncellenir` | Dort basamakli ondalik overwrite tekil/toplu korunur. |
| G-035 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.CokluSandiktaSevkMiktariEzildiktenSonra_UcKTeslimiYeniMutlakToplamiDagitir` | Child sayaç sifirlama ve yeni mutlak toplamdan 3K teslim dagitimi sinanir. |
| G-036 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GercekUcKTeslimiBasladiktanSonra_AktifSevkMiktariEzilemez` | Gercek teslimden sonra overwrite ret ve mutasyon korunumu sinanir. |
| G-037 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.LegacyPartiSayaciBosken_EksikDurumMiktarlarEsitOlsaDaBelirsizKalir` | Belirsiz legacy kaydin otomatik devam karari uretmemesi sinanir. |
| G-038 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.EksikPartiAlternatifKaynaktanKapanincaEskiPartiYenidenTeslimeAcilmaz` | Erken sonuc ve fiili teslim edilebilirligin ayri kararlari sinanir. |
| G-039 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.YenidenSevkKarari_BorcuMevcutDecimalKalanaGoreSinirlar` | Borc onceligi ve min(borc,kalan) ondalik ust siniri sinanir. |
| G-040 | Birim | `UcKProjeTransferTelafiTeslimKuralTests.Satir86_KismiTransferSonrasiYeniSekizAdedinTamaminiTeslimAlir` | Proje transfer telafisinin ayri devam dalinda sinirlandigi test edilir. |
| G-041 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GridEksikSatirinTamamlamaSevki_EskiKalanSemantiginiTekliVeTopludaKorur` | Eksik Grid tamamlama devam partisi tekil/toplu kapsanir. |
| G-042 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TamTeslimdenSonraCekiIkiUcDuzenlenir_KalanBirTekliVeTopluAkistaTamamlanir` | Tam Geldi sonrasi talep artisi ile olusan parçali devam ust siniri sinanir. |
| G-043 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.YenidenSevkKarari_BorcuMevcutDecimalKalanaGoreSinirlar`; `GridUcKParcaliSevkPartisiRegresyonTests.YenidenSevkBorcuBulunsaDaKalanSifirsa_DevamPartisiUretilmez` | Ondalik kalan siniri ve kalan=0 ret birlikte kapsanir. |
| G-044 | Birim | `GridUcKSevkPartisiYasamDongusuTests.YeniPartiBaslatmaBayragiFalseYapar_TakipTemizlemeParentVeChildAlanlariniNullYapar` | Parent/child sayaç ve erken-sonuç bayragi yeni parti baslangicinda atomik assert edilir. |
| G-045 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.YenidenSevkPartisi_EksikKarsilanincaPartiEksigiMevcutIhtiyacaBirKezEklenir` | Devam partisinin 3K kararini yeniden acmasi ve borcu tek sefer guncellemesi sinanir. |
| G-046 | Birim | `UcKKaynakTransferStokIadeKatalogTests.GridTam_OtuzBesHedeftenOtuzIkiTeslimde_AlternatifKaynakKapaliKalirVeYazmaYapilmaz` | Kabul edilen mevcut davranis: yalniz kalan varligi alternatif kaynak kapisini acmaz. |
| G-047 | Kısmi | `GridSevkTahsisKapasitesiTests.TopluTrafoSevk_TrafoPayiniKorumayaDevamEder`; `GridUcKParcaliSevkPartisiRegresyonTests.GridEksikSatirinTamamlamaSevki_EskiKalanSemantiginiTekliVeTopludaKorur` | Trafo ve devam dallari var; normal ilk akisin miktar secimi G-048 ile kapsanir. |
| G-048 | Birim | `GridKomutKatalogKapsamTests.TopluSevk_UcKIslemliSatiriAtlar_UygunSatiriSevkEderVeAtlamayiHareketeYazar` | Uygun normal satir Tam Geldi yapilir ve aktif parti baslatilir. |
| G-049 | Birim | `GridKomutKatalogKapsamTests.TopluSevk_SecimdeTekKilitliSatirVarsaUygunSatiraDaDokunmaz` | Sevk/saha/tadilat tum-secim kilitleri theory ile mutasyonsuz sinanir. |
| G-050 | Birim | `GridKomutKatalogKapsamTests.TopluSevk_UcKIslemliSatiriAtlar_UygunSatiriSevkEderVeAtlamayiHareketeYazar`; `GridSevkTahsisKapasitesiTests.TopluSevk_YetersizSatiriAtlarYeterliSatiriSevkEderVeAtlamayiKaydeder` | 3K ve kapasite engellerinin satir bazli atlanmasi ayri testlerde. |
| G-051 | Birim | `GridSevkTahsisKapasitesiTests.TopluSevk_TumSatirlarinTahsisKapasitesiYetersizse409Doner` | Kapasite durumunda 409; genel uygun satir yok 400 dali dolayli ama ayri isimli test degildir. |
| G-052 | Kısmi | `GridKomutKatalogKapsamTests.TopluSevk_UcKIslemliSatiriAtlar_UygunSatiriSevkEderVeAtlamayiHareketeYazar` | Backend hareket aciklamasi assert edilir; frontend genel bildirim davranisi UI testiyle kapsanmamistir. |
| G-053 | Birim | `GridKomutKatalogKapsamTests.TopluTerminal_DesteklenmeyenHedefiMutasyonsuzReddeder` | Desteklenmeyen hedef mutasyonsuz reddedilir. |
| G-054 | Birim | `GridKomutKatalogKapsamTests.TopluTerminal_SecimdeSevkVeyaSahaKilidiVarsaButunSecimiMutasyonsuzReddeder` | Her iki global kilit varyanti whole-selection olarak sinanir. |
| G-055 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TopluTamGeldi_GercekUcKIslemiVarkenTerminalMuafiyetiniKullanamaz`; `GridUcKParcaliSevkPartisiRegresyonTests.TopluIptal_UcKIslemiSonrasiKaynakMiktarlariniKorurVeAktifPartiTakibiniTemizler`; `GridUcKParcaliSevkPartisiRegresyonTests.TopluGridKapandi_UcKIslemiSonrasiMiktarlariVeAktifPartiyiKorur_SandigiGrideAlir` | Tam blokaji ile Iptal/GridKapandi muafiyeti karsilikli sinanir. |
| G-056 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TopluIptal_UcKIslemiSonrasiKaynakMiktarlariniKorurVeAktifPartiTakibiniTemizler` | Fiziksel kaynak sayaclari korunur; Grid/Trafo/aktif takip temizlenir. |
| G-057 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.TopluGridKapandi_UcKIslemiSonrasiMiktarlariVeAktifPartiyiKorur_SandigiGrideAlir` | Miktar/takip/lokasyon ve Kalan=0 sozlesmesi kapsanir. |
| G-058 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.GridKapandi_TekliVeTopluMevcutSevkMiktarlariniKorur`; `GridUcKParcaliSevkPartisiRegresyonTests.TopluGridKapandi_UcKIslemiSonrasiMiktarlariVeAktifPartiyiKorur_SandigiGrideAlir` | Tekil/toplu terminal farki ve gercek 3K muafiyeti birlikte gorunur. |
| G-059 | Açık | — | Belgelenmis tekil/toplu alan farki istenen kontrat diye dondurulmez. |
| G-060 | Açık | — | Toplu terminaldeki Tadilatta farki istenen kontrat diye dondurulmez. |
| G-061 | Kısmi | `GridSahaIsTamamlamaSenkronizasyonTests.TopluTamGeldi_UcKBlokajiNedeniyleAtlananKaynakSenkronizeEdilmez` | Atlanan ve basarili satirlarin kismi sonucu/senkronu sinanir; API'nin atlama listesini dondurmemesi ayri kontrat testi degildir. |
| G-062 | Birim | `GridKomutKatalogKapsamTests.TekilSifirla_TumGridKaliteSurecTakibiniTemizler_KapaliSandigiYenidenAcar` | Tekil guclu resetin tum alan etkileri dogrudan assert edilir. |
| G-063 | Birim | `GridKomutKatalogKapsamTests.TekilSifirla_UcKIslemiVarsaTumAlanlariMutasyonsuzKorur`; `GridKomutKatalogKapsamTests.TopluSifirla_SecimdeSevkVeyaSahaKilidiVarsaButunSecimiMutasyonsuzReddeder` | Gercek 3K ve global kilitlerin ret/atlama semantigi kapsanir. |
| G-064 | Birim | `GridKomutKatalogKapsamTests.TekilSifirla_TumGridKaliteSurecTakibiniTemizler_KapaliSandigiYenidenAcar` | Kapali sandigin yeniden acilmasi sinanir. |
| G-065 | Birim | `GridKomutKatalogKapsamTests.TopluSifirla_UcKIslemliSatiriAtlar_DigerSatiriVeSandiginiSifirlar` | Satir bazli 3K atlamasi, diger satirin basarisi ve genel basari sonucu sinanir. |
| G-066 | Kısmi | `GridKomutKatalogKapsamTests.TekilSifirla_TumGridKaliteSurecTakibiniTemizler_KapaliSandigiYenidenAcar` | Kaydedilen Gelmedi degeri sinanir; hareket etiketindeki Bekliyor farki ayri assert edilmemistir. |
| G-067 | Açık | — | Eksik “zaten sifir” predicate'i supheli mevcut farktir; istenen kontrat olarak sabitlenmez. |
| G-068 | UI | — | Reset butonu gorunurlugu frontend component testi gerektirir; mevcut backend unitleri bunu kapsamaz. |
| G-069 | Birim | `GridKomutKatalogKapsamTests.ManuelGridUrunu_EnYeniCekiyeYeniSandikVeTamIcerikleOndalikEklenir` | En yeni cekinin secilmesi ve basarili ekleme sinanir; ceki-yok 404 ayri test degildir. |
| G-070 | Birim | `GridKomutKatalogKapsamTests.ManuelGridUrunu_EnYeniCekiyeYeniSandikVeTamIcerikleOndalikEklenir`; `GridKomutKatalogKapsamTests.ManuelGridUrunu_SevkEdilmisSandiktaMutasyonsuzReddedilir` | Yeni sandik varsayilanlari ve sevk kilitli mevcut sandik reddi kapsanir. |
| G-071 | Birim | `GridKomutKatalogKapsamTests.ManuelGridUrunu_EnYeniCekiyeYeniSandikVeTamIcerikleOndalikEklenir` | Manuel satirin sira/bayrak/Grid/3K/surec baslangic alanlari assert edilir. |
| G-072 | Birim | `GridKomutKatalogKapsamTests.ManuelGridUrunu_EnYeniCekiyeYeniSandikVeTamIcerikleOndalikEklenir` | Ondalik tahsis=konulan ve eksik=0 dogrudan sinanir. |
| G-073 | Açık | — | Frontend/backend dogrulama farki supheli davranistir; zayif backend dogrulamasi istenen kontrat olarak sabitlenmez. |
| G-074 | Birim | `GridKomutKatalogKapsamTests.Kalite_GecerliSeciminTamaminiProjeBagiylaGuncellerVeHareketlendirir`; `GridKomutKatalogKapsamTests.Kalite_GecersizLookupDegeriniMutasyonsuzReddeder`; `GridKomutKatalogKapsamTests.Kalite_SecimdeSevkVeyaSahaKilidiVarsaDigerSatiriDaMutasyonsuzKorur` | Pozitif, lookup ret ve iki global kilit kapsanir. |
| G-075 | Birim | `GridKomutKatalogKapsamTests.SurecValidatoru_KimlikListeVeEnumSinirlariniDogrular` | Proje/satir kimligi, bos liste ve enum sinirlari validator seviyesinde kapsanir; lookup ret yolu dolayli kapsamdadir. |
| G-076 | Birim | `GridKomutKatalogKapsamTests.Surec_TamamlanmisTekSatirVarsaButunSecimi409IleMutasyonsuzReddeder` | Tamamlanmis tek satirin tum secimi 409 ile durdurmasi ve mutasyon olmamasi sinanir. |
| G-077 | Birim | `GridKomutKatalogKapsamTests.SurecOtomatikTamamlama_IptaliHaricEksigiKalmayaniTamamlar_NihaiDurumuGeriAcmaz` | Eksik=0 otomatik tamam, Iptal istisnasi ve nihai durumun geri acilmamasi theory ile sinanir. |
| G-078 | UI | — | Persist edilmemis fakat hesaplanmis Tamamlandi gorunumu frontend testi gerektirir. |
| G-079 | Kısmi | `GridSozlesmeVeSorguIsKurallariTests.ProjeVeyaUrunYoksa_NotFoundDonerVeKayitYapilmaz`; `GridSozlesmeVeSorguIsKurallariTests.UrunListesi_ProjeIzolasyonunuVeSiraNumarasiniKorur_YalnizOkur`; `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_CokluTahsisleriAyriDtoYapar_AktifPartiKalaniniChildBazindaBolmedenKorur` | Gerçek Grid sorgusunda proje/ürün yokken 404, proje izolasyonu, sıra ve salt okuma doğrulanır. HTTP endpoint/pagination sözleşmesi API entegrasyonu gerektirir. |
| G-080 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_CokluTahsisleriAyriDtoYapar_AktifPartiKalaniniChildBazindaBolmedenKorur`; `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_TahsisYoksaFiiliSandiklaTekFallbackDtoUretirVeAnaMiktariKullanir` | Coklu child ve tahsis-yok fallback gorunumleri ayri ayri kapsanir. |
| G-081 | Birim | `GridUcKSatirMiktarSenkronizasyonTests.CokluTahsis_OrijinalAnaMiktariPaylaraBolmez_SatirMiktarlariTahsisOlarakKalir`; `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_CokluTahsisleriAyriDtoYapar_AktifPartiKalaniniChildBazindaBolmedenKorur` | Ana toplam ile sandik miktari ayrimi dogrudan assert edilir. |
| G-082 | Birim | `GridUcKSatirMiktarSenkronizasyonTests.SaklananOrijinalMiktar_IkiListedeDeAyricaDoner_GuncelHesaplamalaraKatilmaz`; `GridUcKSatirMiktarSenkronizasyonTests.OrijinalMiktarYoksa_TahsisFarkindanTarihselMiktarUretilmez` | Snapshotin sadece gecmis alani oldugu ve sahte gecmis uretilmedigi sinanir. |
| G-083 | UI | — | Frontendte miktar hiyerarsisi `grid-urunler-miktar.spec.ts` testleriyle kapsanir; .NET `Class.Method` karsiligi yoktur. |
| G-084 | Birim | `GridUcKSatirMiktarSenkronizasyonTests.OndalikMiktarlar_EksikVeSevkPaylarindaKesirKaybetmez`; `GridUcKSatirMiktarSenkronizasyonTests.CokluTahsis_SandikFiltresiTekSatirBiraksaDaDagilimDegismez` | Onluk hassasiyet, oranli dagitim ve filtrede gercek tahsis sayisinin korunmasi sinanir. |
| G-085 | Birim | `GridUcKSatirMiktarSenkronizasyonTests.IptalVeyaGridKapandi_YeniAnaMiktarEksikVeyaKalanUretmez`; `GridUcKSatirMiktarSenkronizasyonTests.FazlaTeslim_EskiTahsisleKirpilmazAmaNegatifEksikUretmez` | Terminal sifir ve negatif eksik sinirlama davranislari kapsanir. |
| G-086 | Kısmi | `GridUcKSatirMiktarSenkronizasyonTests.IptalVeyaGridKapandi_YeniAnaMiktarEksikVeyaKalanUretmez`; `OrtakMiktarDurumIsKurallariTests.HataliUyumsuzluk_FizikselEksikOlmasaDaIsiAcikTutar` | Terminal kalan=0 ve hatali urun minimum kalan dallari kapsanir; formuldaki tum kaynak kombinasyonlari tek testte degildir. |
| G-087 | UI | — | Tablodaki turetilmis Grid Sevk gorunumu frontend-only hesaplamadir; dogrudan UI unit testi yoktur. |
| G-088 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_ManuelSahaIceriginiTamamlanmisNegatifSentetikKimlikleMapler` | Manuel saha iceriginin negatif sentetik kimlikli, tamamlanmis DTO gorunumu sinanir. |
| G-089 | Birim | `GridIsListesiTests.EksikGeldi_IlkVeyaParcaliSevkAsamasinda_EksikOlur`; `GridIsListesiTests.ExplicitYenidenSevk_StateVeMiktarBirlikteyse_YenidenOlur` | Eksik/Yeniden siniflari ve gerekli kombinasyonlari kapsanir. |
| G-090 | Birim | `GridKomutKatalogKapsamTests.GridIsListesi_AktifPartideTeslimSuruyorsaGercekUcKIslemiSirasindaGeciciIsUretmez` | Aktif partide teslim surerken gecici is uretilmemesi sinanir. |
| G-091 | Birim | `GridIsListesiTests.FiiliSandikNoBosluksa_CekidekiSandikNoIleKilitKontroluYapilir` | Fiili sandik fallback'i ve sevk kilidi siniflandirmasi kapsanir. |
| G-092 | Birim | `UcKSozlesmeListeVeDtoKatalogTests.IsListesi_SayfalamayiSatirDegilProjeBazindaYaparVeFiltreOncesiOzetSayaclariniKorur` | Ozet sayaclarinin filtre/sayfa oncesi kapsami sinanir. |
| G-093 | Birim | `GridIsListesiTests.Sayfalama_SatirDegilProjeBazindaYapilir` | Proje bazli sayfalama dogrudan sinanir. |
| G-094 | Açık | — | SQL-level pagination bulunmamasi performans gozlemidir; davranis unit testiyle istenen kontrata dondurulmez. |
| G-095 | UI | — | Frontend proje gruplayarak flatten etme davranisi icin .NET testi yoktur. |
| G-096 | UI | — | Arama/filtre degisiminde sayfanin sifirlanmasi frontend component testidir. |
| G-097 | UI | — | Sayfa bilgisi/disabled dugme davranisi frontend component testidir. |
| G-098 | UI | — | Yukleniyor/yenileme UI durumu frontend component testidir. |
| G-099 | Açık | — | Backend secim indeksi ile UI modal gorunumu farki istenen kontrat diye sabitlenmez. |
| G-100 | Açık | — | Tekil islemde parent kimligi kullanma farki belgelenen mevcut UI davranisidir; entegrasyon testi gerektirir. |
| G-101 | Açık | — | GridSevkDurumuId enum dogrulama farki supheli mevcut davranistir; kontrata dondurulmez. |
| G-102 | Açık | — | UI secenek kumesinin backendden dar olmasi mevcut farktir; UI kontrati olarak sabitlenmez. |
| G-103 | Kısmi | `GridKomutKatalogKapsamTests.TopluSevk_UcKIslemliSatiriAtlar_UygunSatiriSevkEderVeAtlamayiHareketeYazar` | Backendin merkezi 3K islemi tespiti kapsanir; UI fallback paritesi ayri test edilmemistir. |
| G-104 | Açık | — | Checkbox secilebilirliginin backend kilitlerinden dar olmasi belgelenen UI farkidir. |
| G-105 | UI | — | Talep Formu secim/surec/sevk kilidi frontend component testi gerektirir. |
| G-106 | UI | — | PDF kalemlerinin Grid gorunumunden olusmasi ve kullanici duzenlemesi frontend-only akistir. |
| G-107 | UI | — | Talep kaynagi sabit secenekleri frontend/PDF sozlesmesidir; .NET unit testi yoktur. |
| G-108 | UI | — | Istemcide PDF uretme/indirme tarayici testi gerektirir. |
| G-109 | Açık | — | PDF indirme ile surec guncellemesinin atomik olmamasi mevcut UI farkidir; istenen davranis olarak sabitlenmez. |

## Tahsis, tasima ve revizyon kurallari (9.E-9.H)

| Kural | Kapsam | Test | Not |
|---|---|---|---|
| K-030 | Birim | `SandikTahsisOkumaKurallariTests.PozitifTahsis_TahsisSayisindanVeAnaMiktardanBagimsizKorunur`; `SandikTahsisOkumaKurallariTests.LegacySifirTahsis_TekKayittaAnaMiktari_CokKayittaFizikselMiktariFallbackAlir`; `SandikTahsisOkumaKurallariTests.TahsisKaydiYoksa_NegatifAnaMiktarSifiraSinirlanir` | Pozitif tahsis, tek/cok legacy fallback ve sifir alt siniri dogrudan helper testleridir. |
| K-031 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.PA702TahsisUyumsuzlugu_KapasiteyiAsanUcKKarsilamasiniEngeller`; `GridUcKParcaliSevkPartisiRegresyonTests.CokluTahsis_SecimsizTeslimAktifPartiyiChildPaylariniAsmayanSekildeDagitir` | Kapasite asimi mutasyonsuz reddedilir; uygun fark tahsisler icinde dagitilir. |
| K-032 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.SandiklarArasiKismiTasima_AktifPartiVeKaynakKiriliminiAtomikTasir`; `UcKKaynakTransferStokIadeKatalogTests.StoktanOndalikKarsilama_AdiNormalizeEder_BakiyeHareketTahsisBorcVeLokasyonuSenkronizeEder` | Kaynak kirilimlarinin fiziksel miktar icinde atomik dagitimi ondalik degerlerle sinanir. |
| K-033 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.SandiklarArasiKismiTasima_AktifPartiVeKaynakKiriliminiAtomikTasir`; `SandikTasimaKatalogKurallariTests.SandikUrunTasi_ProjeCekiVeAyniSandikDogrulamalarindaMutasyonsuzReddedilir` | Pozitif ayni-proje tasimasi ile ayni sandik, sandik/request proje ve ceki proje uyumsuzluklarinin mutasyonsuz retleri kapsanir. |
| K-034 | Birim | `SandikTasimaKatalogKurallariTests.SandikUrunTasi_SevkedilmisKaynakVeyaHedefDuzeltmeAcikOlsaDaMutasyonsuzReddedilir` | Kaynak ve hedef Sevkedildi varyantlari, duzeltme bayragi acik olsa dahi 409 ve sifir yazmayla kapsanir. |
| K-035 | Birim | `SandikTasimaKatalogKurallariTests.SandikUrunTasi_TekAktifSandikKalincaFiiliSandigiGunceller_CokluTahsisVarkenUydurmaz`; `GridUcKParcaliSevkPartisiRegresyonTests.SandiklarArasiKismiTasima_AktifPartiVeKaynakKiriliminiAtomikTasir` | Uc tahsisin yalniz iki fiziksel adetle tasinmasi ve kaynak/hedef alan sonuclari assert edilir. |
| K-036 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.SandiklarArasiKismiTasima_AktifPartiVeKaynakKiriliminiAtomikTasir` | Aktif Grid payi fiziksel/kaynak kirilimlariyla orantili atomik tasinir. |
| K-037 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.SandiklarArasiKismiTasima_AktifPartiVeKaynakKiriliminiAtomikTasir`; `SandikTasimaKatalogKurallariTests.SandikUrunTasi_HedefteAyniSatiraAitBirdenCokIcerikVarsaMutasyonsuzReddedilir` | Var olan tek hedef child ile birlesme ve hedefte coklu eslesmenin mutasyonsuz 409 reddi kapsanir. |
| K-038 | Birim | `SandikTasimaKatalogKurallariTests.SandikUrunTasi_AyniIslemAnahtariniAyniPayloadIcinIdempotent_FarkliPayloadIcin409Yapar` | Ilk basaridan sonra ayni anahtar+ayni payload ikinci yazma yapmaz; farkli miktar 409 olur ve durum degismez. |
| K-039 | Birim | `SandikTasimaKatalogKurallariTests.SandikUrunTasi_TekAktifSandikKalincaFiiliSandigiGunceller_CokluTahsisVarkenUydurmaz` | Tek aktif hedefte `FiiliSandikNo` guncellenir; iki aktif hedef kaldiginda mevcut tekil metin korunur. |
| K-040 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.FiiliSandikDegisikligi_AktifPartiVeKaynakKirilimlariniKorur`; `SandikTasimaKatalogKurallariTests.FiiliSandikDegistir_CokluTahsisliSatiri409IleMutasyonsuzReddeder` | Tek icerikte tum alan korunumuyla pozitif yol ve coklu tahsiste 409 yonlendirmesi birlikte kapsanir. |
| K-041 | Birim | `GridUcKParcaliSevkPartisiRegresyonTests.SandikYonetimi_AktifGridUcKAkisindaKonulanAdediDogrudanDegistiremez`; `GridUcKParcaliSevkPartisiRegresyonTests.SandikYonetimi_GridUcKAkisiBaslamadanMevcutKonulanAdetDuzenlemesiniKorur`; `GridUcKParcaliSevkPartisiRegresyonTests.SandikYonetimi_AktifGridUcKAkisindaYeniTahsisOlusturamaz` | Aktif akista update/add reddi ve akistan onceki mevcut davranis birlikte sinanir. |
| K-042 | Birim | `CekiSatiriAnaVeriTahsisTests.IlkMiktarDegisikligi_OrijinaliAyriSaklar_TahsisGuncelMiktariIzler`; `CekiSatiriAnaVeriTahsisTests.ArdArdaMiktarDegisikligi_IlkOrijinaliEzmez` | Ilk snapshot ve sonraki degisiklikte ezmeme dogrudan kapsanir. |
| K-043 | Birim | `CekiOrijinalMiktarTests.OrijinalMiktar_HicbirAktifMiktarHesabiniDegistirmez`; `GridUcKSatirMiktarSenkronizasyonTests.OrijinalMiktarYoksa_TahsisFarkindanTarihselMiktarUretilmez` | Tarihsel alanin hesaplara katilmamasi ve sahte gecmis uretilmemesi sinanir. |
| K-044 | Birim | `CekiSatiriAnaVeriTahsisTests.TekTamTahsis_MiktarArtincaAnaMiktariTakipEder`; `CekiSatiriAnaVeriTahsisTests.BilincliTekParcaliTahsis_AnaMiktarDegisseDeDagilimVeEksikKorunur`; `CekiSatiriAnaVeriTahsisTests.TekLegacySifirTahsis_MiktarDegisinceAnaMiktaraSenkronizeEdilir` | Tek tam, bilincli kismi ve legacy sifir tahsis dallari ayri sinanir. |
| K-044A | Birim | `CekiSatiriAnaVeriTahsisTests.TamamlanmisTeslimSonrasiArtis_GridKabulDurumuYeniIhtiyaciIzler`; `GridUcKParcaliSevkPartisiRegresyonTests.TamTeslimdenSonraCekiIkiUcDuzenlenir_KalanBirTekliVeTopluAkistaTamamlanir` | 2 teslim sonrasi talep 3: Grid Eksik etiketi, fiziksel sayac korunumu, kalan 1 tekil/toplu sevk+teslim sonuna kadar kapsanir. |
| K-045 | Birim | `CekiSatiriAnaVeriTahsisTests.CokluTahsis_MiktarArtincaSandikDagilimiVeEksikKorunur`; `CekiSatiriAnaVeriTahsisTests.MiktarDegismediyse_EskiTahsisVeyaEksikIcinOnarimVarsayilmaz` | Coklu/kismi tahsis dagilimi tahmin edilmez ve ayni miktarla veri onarimi yapilmaz. |
| K-046 | Birim | `CekiSatiriAnaVeriTahsisTests.TekTamTahsis_KonulanMiktaraKadarAzaltilabilir`; `CekiSatiriAnaVeriTahsisTests.KonulanMiktarinAltinaAzaltma_HicbirAlaniDegistirmedenReddedilir`; `CekiSatiriAnaVeriTahsisTests.IslenmisMiktarinAltinaAzaltma_MevcutAltSinirKorunur`; `CekiSatiriAnaVeriTahsisTests.ParcaliTahsisToplamininAltinaAzaltma_OtomatikDagilimYapmadanReddedilir` | Fiziksel, islenmis ve etkin tahsis alt sinirlari pozitif/ret ve mutasyonsuzlukla kapsanir. |
| K-047 | Birim | `CekiSatiriAnaVeriTahsisTests.MiktarArtisi_FizikselTeslimSevkVeKarsilamaKayitlariniDegistirmez`; `CekiSatiriAnaVeriTahsisTests.AktifSahaKaynakSatiri_MiktarArtisindaDaDegistirilemez`; `CekiSatiriAnaVeriTahsisTests.SevkEdilmisKilitliSandik_MiktarArtisindaDaDegistirilemez` | Teslim gecmisi korunumu ile saha/sevk kilitleri ve reddedilen istekte mutasyon olmamasi kapsanir. |
| K-048 | Birim | `CekiSatiriAnaVeriTahsisTests.MiktarBirimVeSandikBirlikteDegisince_TekTahsisAyniIslemdeTasinarakSenkronizeEdilir`; `CekiSatiriAnaVeriTahsisTests.CokluTahsis_SandikNumarasiDegisinceTekSandigaToplanmaz`; `CekiSatiriAnaVeriTahsisTests.SevkEdilmisKilitliHedefSandigaTasima_AnaMiktariDaDegistirmedenReddedilir` | Plan/fiili izleme, coklu ret ve hedef sevk kilidinde tum guncellemenin korunmasi kapsanir. |
| K-049 | Birim | `CekiOrijinalMiktarTests.RevizyonOncesiOtomatikGeriAl_AktifPartiSayaclariniAnaSatirVeTahsislerdeTemizler`; `CekiOrijinalMiktarTests.RevizyonU_IlkMiktarDegisikligindeOncekiDegeriKorur_SonraEzmez` | U revizyonunun once geri alma, sonra yeni ana veri/snapshot uygulamasi sinanir. |
| K-050 | Kısmi | `CekiRevizyonAktifSahaKaynakKorumaTests.Uygulama_YeniAktifIliskiyiYenidenKontrolEder_KaynakVeTeslimMiktarlariniDegistirmez`; `CekiOrijinalMiktarTests.RevizyonOncesiOtomatikGeriAl_AktifPartiSayaclariniAnaSatirVeTahsislerdeTemizler` | Aktif saha ve reset etkisi kapsanir; sevk kilidi, giden transfer ve tum stok tersleme kombinasyonlari bu test grubunda eksiktir. |
| K-051 | Birim | `CekiOrijinalMiktarTests.PA702Revizyonu_TekTamTahsisAnaMiktarlaBirlikteGuncellenir`; `CekiOrijinalMiktarTests.RevizyonU_CokluTahsisDagiliminiOtomatikYenidenPaylastirmaz` | Tek tam tahsis guncellenir, coklu dagilim uydurulmaz. |
| K-052 | Birim | `CekiRevizyonAktifSahaKaynakKorumaTests.Onizleme_AktifNormalKaynakU_SahaEngeliIleIsaretlenir`; `CekiRevizyonAktifSahaKaynakKorumaTests.Uygulama_YeniAktifIliskiyiYenidenKontrolEder_KaynakVeTeslimMiktarlariniDegistirmez`; `CekiRevizyonAktifSahaKaynakKorumaTests.Uygulama_TekTopluSorgu_TekrarlananKaynaklarTekilleştirilir_TuremisSatirSorgulanmaz` | Preview, uygulama-aninda yeniden kontrol ve sorgu tekillestirme kapsanir. |
| K-053 | Kısmi | `CekiSatiriAnaVeriTahsisTests.MiktarBirimVeSandikBirlikteDegisince_TekTahsisAyniIslemdeTasinarakSenkronizeEdilir`; `CekiOrijinalMiktarTests.RevizyonU_CokluTahsisDagiliminiOtomatikYenidenPaylastirmaz` | Manuel plan/fiili ve revizyon coklu korunumu ayri kapsanir; U revizyonundaki tum tasinabilirlik matrisi dogrudan test degildir. |
| K-054 | Birim | `SandikTahsisOkumaKurallariTests.TekTahsisSatirPayi_EskiTahsisleAnaOperasyonToplaminiKirpmaz`; `SandikTahsisOkumaKurallariTests.CokluTahsisSatirPayi_ToplamTahsisOraniniVeSandikUstSiniriniKorur`; `SandikTahsisOkumaKurallariTests.TekTahsisKalanPayi_MerkeziKalaniDortOndaliklaKorur`; `SandikTahsisOkumaKurallariTests.CokluTahsisKalanPayi_FizikselAcikOranindaDagilirVeYuvarlamaToplaminiKorur`; `GridUcKSatirMiktarSenkronizasyonTests.CokluTahsis_SandikFiltresiTekSatirBiraksaDaDagilimDegismez` | Tek/cok tahsis, merkezi kalan, ondalik yuvarlama ve filtre semantigi helper/query katmaninda kapsanir. |
| K-055 | Açık | — | Eski FB endpoint farki istenen ortak kontrat diye sabitlenmez; route+DB entegrasyon testi gerekir. |
| K-056 | Açık | — | Eski FB hedef sayaç farki belgelenen legacy davranistir; arzu edilen kontrat olarak unit test eklenmemistir. |
| K-057 | Açık | — | Iki eski stok ucunun ana 3K'dan farki belgelenmistir; farki donduren test eklenmemistir. |
| K-058 | Açık | — | Tekli/toplu tedarikci dogrulama farki supheli sinirdir; ortak kontrat diye genellenmez. |
| K-059 | Entegrasyon | — | Gercek transaction/rollback davranisi relational provider ve failure enjeksiyonlu entegrasyon testi gerektirir. |
| K-060 | Kısmi | `UcKSozlesmeListeVeDtoKatalogTests.UrunListesi_ManuelSahaIceriginiTamamlanmisNegatifSentetikKimlikleMapler` | Negatif sentetik okuma gorunumu sinanir; belirsiz eslesme ve okumanin DB onarimi yapmadigi entegrasyon seviyesinde kalir. |

## Calistirilan odak testleri

`dotnet test 3K.Application.Tests/3K.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~GridKomutKatalogKapsamTests|FullyQualifiedName~SandikTahsisOkumaKurallariTests|FullyQualifiedName~SandikTasimaKatalogKurallariTests"`

Sonuc: **67 basarili, 0 basarisiz, 0 atlanan**.
