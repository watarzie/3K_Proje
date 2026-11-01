# Giriş kilidi, hız sınırı ve istemci IP'si

25 Ekim 2026. Giriş ve API kötüye kullanım koruması; kod: `GirisKilidiKurali` (Core), `AuthService.ValidateCredentialsAsync`,
`HizSiniriBolumleri` ve `AddApiRateLimiting` / `AddApiForwardedHeaders` (`3K_API/Extensions`).

## Hesap bazlı giriş kilidi

- Art arda 5 hatalı parola → hesap 15 dakika kilitlenir (`Kullanicilar.BasarisizGirisSayisi`, `GirisKilitBitisTarihi`,
  Türkiye saati; betik `scripts/database/20261025_01_Giris_Kilidi.sql`).
- Kilitliyken doğru parola da reddedilir. Süre dolunca sonraki hatalı deneme sayacı 1'den başlatır.
- Başarılı giriş ve yöneticinin parola değiştirmesi (`KullaniciSifreDegistirCommand`) sayacı ve kilidi sıfırlar.
- Hatalı parola, bilinmeyen e-posta ve kilitli hesap aynı 401 ve aynı mesajı alır; bilinmeyen e-postada da BCrypt çalışır.
  Yanıt ve süresi hesabın var olup olmadığını ya da kilitli olduğunu göstermez.
- Sayaç ve sıfırlama yalnız hesap kilitsizken çalışan tek koşullu `UPDATE`'tir. Farklı IP'lerden paralel denemeler artışı
  kaybetmez. Okuma ile karar arasında başka istek kilidi kurduysa doğru parola kilidi silemez (kilitli sayılır), hatalı
  parola sayılmaz ve kilidi uzatmaz.
- Loglar: kilit kurulunca `Warning`, kilitli hesaba deneme `Information` (yalnız `KullaniciId`); her başarısız giriş ayrıca
  `IsHatasiLoglamaBehavior` satırıdır.

## Hız sınırları (dakikalık sabit pencere, kuyruk yok)

| Kapsam | Bölümleme | Sınır |
|---|---|---|
| Bütün API (global) | oturumlu: kullanıcı Id; oturumsuz: IP | 600 / 120 |
| PDF/Excel/etiket üretimi ve dosya indirme (MediatR hattı) | kullanıcı Id | 20 |
| Giriş (`auth-login`) | IP | 10 |
| 2FA adımları (`two-factor`) | IP | 20 |

- 600/dk: bir ekran açılışı ~10–20 istek atar; yoğun kullanım bile bunun çok altında kalır. Sınır kaçak döngü ve betikli
  kazımayı keser.
- IP kovası IPv4'te tam adres, IPv6'da /64 önekidir (istemci kendi bloğunda adres değiştirerek yeni kova açamaz).
- `UseRateLimiter`, `UseAuthentication`'dan sonra (kullanıcı Id'si okunabilsin), `UseAuthorization`'dan önce çalışır.
- 429 gövdesi `{ message }` (Türkçe) ve `Retry-After` başlığıdır (CORS'ta okunabilir); ön yüz mesajı olduğu gibi gösterir.
- Dışa aktarım sınırı attribute değil, MediatR hattındaki `DisaAktarimHizSiniriBehavior`'dır: yanıtı `Result<DosyaIcerigi>` olan
  her istek (bütün dosya uçları `ToFileResult()` ile bu tipi döndürür) kendiliğinden kapsanır; yeni rapor ucu için iş yoktur.
  Yetki ve doğrulamadan sonra çalışır (reddedilen istek kotadan düşmez); kullanıcısız sunucu içi çağrıyı sınırlamaz.
  Sınırlayıcı `IDisaAktarimHizSiniri` (Core) → `DisaAktarimHizSiniri` (API, tek örnek). Bu yoldan dönen 429'da `Retry-After`
  başlığı yoktur; 429 `IsHatasiLoglamaBehavior` ile loglanır. Testler: `DisaAktarimHizSiniriTests`.
- Uç politikası (`[EnableRateLimiting]`) yalnız giriş ve 2FA uçlarındadır; `HizSiniriTests` başka politika adı kalmadığını denetler.

## Proxy arkasında istemci IP'si

Canlı: istemci → Cloudflare → nginx (API ile aynı Linux makine) → Kestrel. `appsettings.json` → `ForwardedHeaders`:

- `KnownNetworks`: Cloudflare aralıkları (<https://www.cloudflare.com/ips/>; 25.10.2026'da alındı). Cloudflare listeyi
  değiştirirse güncellenir. Loopback (nginx) varsayılan olarak güvenilirdir.
- `ForwardLimit: 2` (nginx + Cloudflare). Zincirdeki ilk güvenilmeyen adres istemci sayılır. Cloudflare'i atlayıp sunucuya
  doğrudan gelen isteğin sahte `X-Forwarded-For` girdisi bu yüzden etkisizdir.
- Hatalı adres yazılırsa API açılışta durur.
- .NET yapılandırması dizileri sıra numarasıyla birleştirir: listeyi `appsettings.Secrets.json` ya da ortam değişkeniyle
  değiştirirken daha kısa bir liste temel dosyadaki fazla girdileri silmez; listenin tamamı tek yerde tutulur.
- nginx Kestrel'e loopback üzerinden bağlanmalıdır (`proxy_pass http://127.0.0.1:<port>`). Başka bir adresten bağlanıyorsa
  (LAN IP, Docker köprüsü) o adres `KnownProxies`'e eklenir; yoksa başlık yok sayılır ve bütün istemciler tek IP paylaşır.
- nginx `X-Forwarded-For`'a ekleme yapmalıdır: `proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;`. Yalnız
  `$remote_addr` yazılırsa API bütün istekleri Cloudflare IP'sinden gelmiş görür.
- Önerilir: sunucu güvenlik duvarında 80/443 yalnız Cloudflare aralıklarına açık olsun.

## CAPTCHA kararı

Şimdilik eklenmedi. Uygulama dahili/B2B kullanımdadır, herkese açık kayıt yoktur ve TOTP 2FA vardır; hesap kilidi ve hız
sınırı yeterlidir. İnternete açık kayıt ya da bot trafiği olursa birkaç hatalı denemeden sonra çıkan Cloudflare Turnstile
önerilir: anahtarlar `appsettings.Secrets.json`'da tutulur, token sunucuda Turnstile `siteverify` ile doğrulanır.
