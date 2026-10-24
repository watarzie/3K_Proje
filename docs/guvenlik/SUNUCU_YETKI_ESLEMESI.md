# Sunucu işlem / menü yetkisi eşlemesi

19 Eylül 2026 değişikliği: `AuthorizationBehavior`, `X-Menu-Kod` değerini yetki kararında kullanmaz. İzin kayıtları hâlâ veritabanındaki `RolYetkileri` üzerinden `IRolService` ile doğrulanır; yeni rol ID'si veya Admin bypass eklenmedi. İstemci menü başlığını değiştirmek, kaldırmak veya başka ekranda W sahibi olmak farklı bir operasyonu açmaz.

Çalıştırılabilir katalog `3K.Application/Common/RequestMenuPermissionResolver.cs` dosyasındadır. Tanımlanmamış `ISecuredRequest` 403 ile kapanır; `ISecuredRequest` eklemeyi unutmak da yeni bir operasyonu public yapmaz. `ButunKorumaliOperasyonlar_SunucudaTanimlidir` testi assembly'deki **bütün somut MediatR request tiplerini** tarar; sunucu tanımı veya açık istisna kategorisi olmayan yeni request testte başarısız olur.

## Sabit işlemler

| Operasyon | Gerekli izin |
|---|---|
| Rol oluştur / güncelle / sil | rol-yonetimi W |
| Rol detay | rol-yonetimi R |
| Rol seçim listesi | rol-yonetimi **veya** kullanicilar **veya** onay-kurallari-yonet R |
| Dashboard sorguları | dashboard R |
| Grid / 3K iş listeleri | grid-is-listesi / 3k-is-listesi R |
| Grid Sahası: Grid'e gelen ürünler ve mal kabul kayıtları (`GET api/grid/saha/urunler`, `GET api/grid/saha/mal-kabul`) | grid-saha-gorunumu R (proje tipinden bağımsız, salt okuma; ekrandaki "Grid'e git" satırın tipinin Grid modülü R iznini ayrıca ister) |
| Barkodla ürün arama ve raporu (`GET api/barkod-arama`, `GET api/barkod-arama/rapor/{format}`) | barkod-urun-arama R (Id 5260, proje tipinden bağımsız, salt okuma, kenar menüde yok: ekran genel aramadan açılır; satırdaki proje no bağlantısı satırın tipinin Grid modülü R iznini ayrıca ister) |
| Üst çubuktaki genel arama (`GET api/arama?q=`) | Gruplardan birinin okuma izni (Any). Her grup ayrıca kendi izniyle süzülür: Projeler tipin ProjectRead, Sandıklar tipin CrateRead politikası (`RequestMenuPermissionResolver.ProjectPolicy`, proje tipi veritabanından), Ürünler barkod-urun-arama R. İzni olmayan grup boş ve `…Aranabilir = false` döner |
| Sandık açılış/kapanış istatistiği ve Excel'i (`GET api/sandik-istatistik`, `GET api/sandik-istatistik/excel`) | sandik-acilis-istatistigi R (Id 5261, proje tipinden bağımsız, salt okuma, kenar menüde yok: ekran Projeler ve 3K sandık listesinin "Diğer" menüsünden açılır). Sandığın kendi zaman çizelgesi (`GET api/sandik/{id}/durum-gecmisi`) sandık okuma iznidir (CrateRead, sandık detayıyla aynı). Kapalı sandığı geri alma (`PUT api/proje/sandik-kapat`, `Kapali=false`) mevcut CrateWrite iznidir; sebep zorunludur, yeni izin istemez |
| Çeki ana veri düzenle / satır sil | ceki-verisi-duzenle / ceki-verisi-sil W |
| Kalite / süreç değişikliği | kalite-modulu / surec-modulu W |
| Stok listesi ve PDF / stok yazma / stok silme | stok R / stok W / stok-sil W |
| Sevkiyatlar sayfası: sefer listesi, sefer detayı, istatistik, dönem raporu (PDF/Excel) ve sefer PDF'i | sevkiyatlar R |
| Depo raporları | depo-durumu R |
| Depo lokasyon tanımı | depo-durumu W (sandığa lokasyon atama ayrı işlemdir) |
| Hareket geçmişi | hareket-gecmisi R |
| Proje fotoğraf / gerçekleşen çeki bağlantısı ekle / değiştir / sil (`POST api/proje/{id}/fotograf-baglantilari`, `PUT`/`DELETE api/proje/{id}/fotograf-baglantilari/{baglantiId}`) | proje-fotograf-baglantisi W (proje tipinden ve sevk durumundan bağımsız tek kod; Projeler altında, Projeler tavanıdır) |
| Proje listesinde fotoğraf bağlantılarının dönmesi | proje-fotograf-baglantisi R; izin yoksa liste satırında bağlantı listesi boş döner |
| Kullanıcı, onay, bildirim aboneliği, ambalaj, finans | Request üzerindeki mevcut sabit veya çoklu işlem izinleri; artık header fallback yok |
| **Finans (13 Ekim 2026'dan itibaren yalnız sekme / alt sekme izni)** | Her istek `Sekmeler` listesini bildirir (`FinansCommand`/`FinansQuery`); sorgu listelenen sekmelerden **herhangi birinin** R'sini, işlem W'sini ister. Alan maskelemesi yoktur: sekme R'si tutar, fiyat, gelir, gider, kârlılık, ölçü ve m³ dahil bütün alanları gösterir. İş Takibi, Tanımlar ve PO/Fatura işlemleri **alt sekmeye** bağlıdır (`20261013_03_Finans_Alt_Sekme_Yetkileri.sql`); sekme alt sekmenin tavanıdır, sekme W'si alt sekme W'sini kendiliğinden vermez. Kümeler: `3K.Core/Constants/FinansSekmeleri.cs` |
| Finans kontrol paneli | finans-sekme-panel R |
| Finans aylık iş listesi, proje sandık kırılımı, aylık değer, tek seferlik iş ekleme | finans-sekme-is-takibi-aylik R / W |
| Finans iş kaydı / PO / fatura detayı, seçim, hareketler, genel arama | Kayıt sekmelerinden biri R: is-takibi, panel, po-fatura, munferit-isler, raporlar |
| Finans iş fiyatlandırma, hacim, finans tarihi, iptal/geri alma | is-takibi veya munferit-isler W |
| Finans PO açma, PO'ya kalem ekleme, PO grup seçici | po-fatura-po, is-takibi veya munferit-isler W (seçici R) |
| Finans PO öncesi iş onayına gönderme (`POST api/finans/po-onay-talepleri`) | po-fatura-po, is-takibi veya munferit-isler W. Talebi onaylama/reddetme Finans izni değildir: `islem-onay-merkezi` W ve `OnayIslemYetkileri`'nde `FINANS_PO_IS_ONAYI` rolü (Admin her zaman; Admin dışında kimse kendi talebini onaylayamaz). Onay detayını (kalemler, ölçü, m³, tutar; kalemlerin şimdiki net tutarı) Finans sekme izni olmadan şunlar görür: talep eden, karar veren ve `islem-onay-merkezi` R ile rolünde `FINANS_PO_IS_ONAYI` bulunanlar (onay geçmişinin genel erişim kuralı). Detay yalnız onay kaydındaki talep eden ve projeyle eşleşen talepten okunur. İç komut `FinansPoOnayUygulaCommand` HTTP ucu olmayan sunucu içi komuttur. |
| Finans PO listesi (fatura girişinde PO seçimi dahil) | finans-sekme-po-fatura-po **veya** finans-sekme-po-fatura-fatura R |
| Finans PO düzeltme / iptal | finans-sekme-po-fatura-po W |
| Finans fatura listesi; fatura girişi/düzeltme/iptal, ödeme | finans-sekme-po-fatura-fatura R / W |
| Finans vade uyarısı (İş Takibi üstü) | **finans-vade-uyarisi R** (sekme izninden bağımsız) |
| Finans gider listesi ve gider işlemleri | finans-sekme-giderler R / W |
| Finans gider kategori/kalem listesi (formlar, rapor filtreleri) | giderler, gider-kategorileri veya raporlar R |
| Finans gider kütüphanesi yönetimi | finans-sekme-gider-kategorileri R / W |
| Finans düzenli iş listesi ve işlemleri (dönem üretme dahil) | finans-sekme-tanimlar-duzenli R / W |
| Finans tarife (ürün) listesi, yeniden fiyatlama, tarife düzeltme (önizleme W) | finans-sekme-tanimlar-tarifeler R / W |
| Finans para birimi ve vade parametresi kaydetme | finans-sekme-tanimlar-para-birimi W |
| Finans parametreleri (vade günleri) okuma | tanimlar-para-birimi **veya** po-fatura-fatura R (fatura girişinde vade önerisi) |
| Finans münferit iş listesi / oluşturma (tarifeden fiyatlanır), tarife seçenekleri | finans-sekme-munferit-isler R / W |
| Finans iş (siparişi açılan / açılmayan / birleşik), özet, alacak ve finansal durum raporu (`GET api/finans/raporlar/finansal-durum/{format}`; Kontrol Paneliyle aynı tutarlar, Raporlar'ın başka raporlarla zaten verdiği alanlardan oluşur) ve önizleme | finans-sekme-raporlar R (panel izni bu raporu açmaz, Raporlar izni de paneli açmaz) |
| Finans gider raporu (düz / kategoriye / projeye göre) | giderler veya raporlar R |
| Finans aylık / sipariş durumu raporu | is-takibi-aylik veya raporlar R |
| Finans bekleyen işler (yaşlandırma) | panel, is-takibi-bekleyen veya raporlar R |
| Finans belge listeleme/indirme, değişiklik geçmişi | Kayıt sekmelerinden biri veya giderler R |
| Finans belge yükleme, kalıcı silme (önizleme dahil) | Hedef kaydın sekmesi W: iş kaydı → is-takibi/munferit-isler, PO → po-fatura-po, fatura → po-fatura-fatura, gider → giderler |
| Finans "Tüm dönemlerde ara" alt sekmesi | finans-sekme-is-takibi-arama yalnız ekranı açar (ön yüz); arama sorgusu kayıt sekmelerinden biriyle açılır |
| Finans proje seçenekleri, para birimleri | finans-yonetimi R |
| Otomatik sahaya aktarım önizlemesi (`GET api/proje/{id}/otomatik-saha-aktarim/onizle`; veri değiştirmez) | sahaya-aktar R |
| Otomatik sahaya aktarım uygulaması (`POST api/proje/{id}/otomatik-saha-aktarim`; plan özeti önizlemeyle aynı değilse 409); elle eksiklerden / sandıklardan saha oluşturma | sahaya-aktar W (onay merkezine düşmez; elle yolla aynı) |
| Bir saha aktarımının tamamını geri alma (`POST api/proje/saha-aktarimi-geri-al`; sahadan sahaya taşıma başlığı toplu geri alınmaz, 409); satır ve saha sandığı geri alma | saha-aktarim-geri-al W |
| Sahadan sahaya taşıma önizlemesi (`POST api/proje/sahadan-sahaya-tasima/onizle`; veri değiştirmez) | sahaya-aktar R |
| Sahadan sahaya taşıma (`POST api/proje/sahadan-sahaya-tasima`; satır ya da saha sandığının bütün içeriği; işlem anahtarıyla tekrar koruması) | sahaya-aktar W (ayrı kod yok; onay merkezine düşmez) |
| Özel sandık listesi (finansa aktarılmayı bekleyen / aktarılan süzgeci, ekleyen ve aktaran bilgisi) | ambalaj-uretim-listesi R (mevcut liste izni) |
| Ambalaj kaydı değişiklik geçmişi (`GET api/ambalaj/kayitlar/{id}/gecmis`) | ambalaj-uretim-listesi R; kayda erişim yoksa 404. Ölçü, m³, sarf ve kaynak alanları ilgili görüntüleme izni yoksa gösterilmez |

## Gerçek proje tipine bağlı işlemler

Normal / Saha / Yedek türü, mevcut kaydın proje FK'si üzerinden okunur. Gövdede başka projenin ID'si verilerek bir satır/sandık yetkilendirilemez. Her seçili kayıt var olmalıdır; karışık toplu seçimde her projenin gereksinimi sağlanır.

| Operasyon | Normal | Saha | Yedek |
|---|---|---|---|
| Grid okuma/yazma | grid-modulu | saha-grid-modulu | yedek-grid-modulu |
| 3K okuma/yazma | 3k-modulu | saha-3k-modulu | yedek-3k-modulu |
| Projeden karşılama kaynak listesi (`GET api/uck/{hedefProjeId}/projeden-karsilama-kaynaklari`); tür **hedef** projeden okunur | 3k-modulu W | saha-3k-modulu W | yedek-3k-modulu W |
| Sandık yazma / tekil-toplu taşıma / sandık sevk-düzeltme | sandik-yonetimi **veya** 3k-modulu W | saha-sandiklar **veya** saha-3k-modulu W | yedek-sandiklar **veya** yedek-3k-modulu W |
| Sevk sonrası düzeltme: sevkiyat kaydı korunarak kilit açma (sandık `SandikKilidiAcCommand`, proje `ProjeKilidiAcCommand`); geri alınarak açma bu izni istemez | tipin mevcut izni (sandık: sandik-yonetimi **veya** 3k-modulu W; proje kilidi: yönetim **veya** sevk-edilen W) **ve** sevk-sonrasi-duzeltme W | saha izni **ve** sevk-sonrasi-duzeltme W | yedek izni **ve** sevk-sonrasi-duzeltme W |
| Sandık satırının ürün bilgisi (seri no, SKT, lot; `PUT api/sandik/icerik-bilgisi-guncelle`, Grid ve 3K ekranları); Grid W yalnız bu işlemi açar | sandik-yonetimi **veya** 3k-modulu **veya** grid-modulu W | saha-sandiklar **veya** saha-3k-modulu **veya** saha-grid-modulu W | yedek-sandiklar **veya** yedek-3k-modulu **veya** yedek-grid-modulu W |
| Proje sevki; toplu sevkte (tır seferi) her sandığın projesinin gerçek türüne göre, bütün projeler için birlikte | proje-sevk-et W | saha-sevk-et W | yedek-sevk-et W |
| Yeni proje | aktif-projeler **veya** sandik-yonetimi W | saha-yonetimi W | yedek-yonetimi W |
| Proje silme | proje-sil W | saha-proje-sil W | yedek-proje-sil W |
| Planlanan tarih | planlanan-sevk-tarihi W | planlanan-sevk-tarihi W | yedek-planlanan-sevk-tarihi W |
| Gerçekleşen çeki raporu | gerceklesen-ceki-raporu R | saha-gerceklesen-ceki-raporu R | yedek-gerceklesen-ceki-raporu R |
| Müşteri gerçekleşen çeki listesi (`GET api/pdf/gerceklesen-ceki-listesi/{projeId}/musteri/{format}`) | gerceklesen-ceki-raporu R | saha-gerceklesen-ceki-raporu R | yedek-gerceklesen-ceki-raporu R |
| 3K sandık durum raporu | 3k-sandik-durum-raporu R | saha-3k-sandik-durum-raporu R | yedek-3k-sandik-durum-raporu R |
| Genel sandık raporu | sandik-yonetimi **veya** aktif-projeler R | saha-raporu R | yedek-raporu R |
| Tekil / toplu eksik raporu | eksik-raporu R | saha-sevk-sonrasi-eksik-raporu R | yedek-eksik-raporu R |

`sevk-sonrasi-duzeltme` (Id 5259, Sandık Yönetimi altında işlem izni, ekran değil) proje tipinden bağımsız tek koddur ve tipin iznine AND grubu olarak eklenir (`RequestMenuPermissionResolver`, `ProjectAccess.SevkSonrasiDuzeltme`). Betik yalnız Admin'e W verir. Düzeltme durumu okuması (`GET api/sandik/{id}/sevk-sonrasi-duzeltme`) sandık okuma iznidir; projedeki bütün açık düzeltmeleri tamamlama (`POST api/sandik/sevkiyat-duzeltme-toplu-tamamla`) tekil tamamlama gibi tipin sandık yazma iznidir (`ServerAuthorizationRegressionTests.TopluDuzeltmeTamamlama_ProjeninSandikYazmaIzniniIster`). Düzeltmedeki işlemler (ürün ekleme/silme, taşıma, Grid/3K sıfırlama) kendi mevcut izinleriyle yapılır; düzeltme sandık eklemeyi/silmeyi açmaz. Testler: `ServerAuthorizationRegressionTests.SevkSonrasiDuzeltme_AyriDuzeltmeIzniIster`, `SevkiyatGeriAlinarakAcma_DuzeltmeIzniIstemez`.

Ortak proje/sandık okumaları ilgili türün yönetim, Grid, 3K ekranlarından kullanılabilir. Depo görünümü, genel sevk listesi ve kaynak/hedef seçen sahaya aktar ekranı kendi okuma izinleriyle ortak okumaları kullanır. Proje dropdown'u yalnız seçim bilgisi veren ortak lookup'tır; erişimi çağıran kullanım senaryolarının açık OR listesine bağlıdır, yazma izni kazandırmaz. Normal eksik kaynak seçimi, saha/yedek sandık ve sahaya aktar ekranlarının ortak okumasıdır.

Projeden karşılama kaynak listesi (14 Ekim 2026): 3K paneli kaynak projenin ürünlerini eskiden Grid ekranının listesinden (`GET api/grid/{kaynakProjeId}/urunler`, kaynak projede Grid R) okuyordu; Grid izni olmayan 3K personeli 403 alıyor ve ekranda "eşleşen ürün yok" görüyordu. Liste artık karşılamayı kaydeden `UcKDurumGuncelleCommand` ile aynı izne, hedef projenin 3K W iznine bağlıdır; kaynak projenin Grid izni sorulmaz ve Grid izni tek başına bu listeyi açmaz. Yalnız eşleştirme alanları döner (sıra, barkod, poz, açıklama, 3K gelen, kullanılabilir). Testler: `ServerAuthorizationRegressionTests.ProjedenKarsilamaKaynaklari_*`, `ProjedenKarsilamaKaynaklariTests`.

Revizyon geçmişi/liste/detay/dosya için gerçek türün yönetim izni (`sandik-yonetimi`, `saha-yonetimi`, `yedek-yonetimi`) **veya** `ceki-revizyon-yukle` **veya** `sevk-edilen` R gerekir. Proje kilit açma talebinde gerçek türün yönetim izni **veya** `sevk-edilen` W gerekir; onay süreci ayrıca korunur.

`IRequiresMenuPermissions` varsayılan **All**; açık `PermissionMatch.Any` alternatif izin grubudur. Gruplar kendi aralarında **AND**'dir. Bir boş/geçersiz grup izin kazandıramaz. Sabit menü sözleşmesinin mevcut Query=R / Command=W kuralı korunur; merkezi katalog rapor oluşturan salt-okuma komutlarını açık R olarak sınıflar.

## Korunan istisnalar ve ilişkiler

- Login ve 2FA challenge akışları menü yetkisine tabi değildir; kendi kimlik doğrulama kontrolleri korunur.
- `MenuAuthorizationExceptions` public (login/2FA ve yalnız `LookupBase` türlerini kabul eden lookup), kimliği zorunlu kişisel okuma/yazma ve yalnız sunucu içi komutları adlarıyla listeler. Kategoriye girmeyen işaretsiz bir request otomatik geçmez.
- Kullanıcının sidebar menüsü ayrı `GetKullaniciMenuQuery` ile yalnız oturum sahibinin **güncel DB rolünden** okunur. İstemci rol/kullanıcı ID'si seçemez; rol yönetimi detay endpoint'i bunun üzerinden açılmaz.
- Bildirimlerin kişisel okumaları ve kişisel onay geçmişi mevcut sahiplik/erişim kontrolleriyle çalışır. Dahili finans aktarım ve background akışları dış HTTP operasyonu olarak eklenmedi.
- Onay handler'ının doğrulanmış karar/payload sonrası açtığı sunucu `IApprovalExecutionContext` bağlamında yalnız `IApprovalOperation` çalıştırma izni devralır. Aynı bağlam rol silme gibi ilgisiz request'leri bypass etmez. Handler iş kuralları tekrar çalışır.
- Saha eksik tamamlamasında kaynak normal satırın hedef saha projesiyle aynı projede olması istenmez; hedefin gerçek yetkisi ve mevcut handler'ın kaynak uygunluğu/kapasite kontrolleri birlikte korunur.
- Tam tekil taşımada kaynak içerik silinse bile aynı kullanıcı + işlem anahtarı + aynı payload, değişmez transfer defterinden kanıtlanırsa tekrar isteği gerçek proje yetkisiyle değerlendirilebilir. Defter yoksa veya kullanıcı/içerik farklıysa bu istisna açılmaz.

## Kurulum ve doğrulama sınırı

Yeni tablo, kolon veya izin seed'i gerekmiyor. Mevcut menü/işlem izinleri artık sunucu tarafından gerçekten uygulanır; canlıya geçerken kullanılan rollerin gerekli R/W kayıtlarını yönetim ekranından kontrol edin. UI gizleme veya header gönderme eksik DB izninin yerine geçmez.

`ServerAuthorizationRegressionTests` ve `OrtakYetkiOnayIsKurallariTests` header spoof, readonly, normal/saha/yedek, ilişkiler, açık AND/OR, onay/public giriş, self-menu ve idempotent tekrar senaryolarını doğrular. PostgreSQL SQL çeviri testi bağlantı açmadan LINQ çevirisini kontrol eder. Bunlar gerçek HTTP/JWT middleware, canlı rol verisi veya PostgreSQL bağlantılı entegrasyon testinin yerine geçmez.
