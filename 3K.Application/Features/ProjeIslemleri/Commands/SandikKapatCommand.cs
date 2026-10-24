using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Sandık Kapat/Aç — Sandık Yönetimi yazma yetkisi olan kullanıcılar tarafından yapılabilir. Kapalı sandığı
    /// "Hazırlanıyor"a geri almak (<see cref="Kapali"/> false) sebep ister; not isteğe bağlıdır.
    /// </summary>
    public class SandikKapatCommand : IRequest<Result<bool>>, ISecuredRequest
    {
        public int SandikId { get; set; }
        public bool Kapali { get; set; }
        /// <summary>Geri açma sebebi (<c>LookupSandikAcilmaSebepleri</c>); geri almada zorunlu.</summary>
        public int? AcilmaSebebiId { get; set; }
        /// <summary>Geri açma notu (isteğe bağlı, en fazla 500 karakter).</summary>
        public string? Not { get; set; }
    }
}
