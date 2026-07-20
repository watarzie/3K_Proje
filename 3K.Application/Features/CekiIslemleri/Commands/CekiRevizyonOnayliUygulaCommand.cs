using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    /// <summary>
    /// Yalnızca mevcut onay motoru tarafından saklanıp çalıştırılan iç komuttur.
    /// Dosya içeriği yerine değişmez revizyon talebinin kimliğini taşır.
    /// Bu komut için dışarı açık bir HTTP endpoint'i bulunmaz.
    /// </summary>
    public sealed class CekiRevizyonOnayliUygulaCommand
        : IRequest<Result<CekiRevizyonSonuc>>,
          IConfigurableApproval,
          IApprovalReference
    {
        public int TalepId { get; set; }
        public int ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;

        public string GetApprovalDescription()
        {
            var proje = string.IsNullOrWhiteSpace(ProjeNo)
                ? $"#{ProjeId}"
                : ProjeNo.Trim();

            return $"{proje} projesinin çeki revizyonu onay bekliyor.";
        }

        public string GetApprovalOperationCode() => OnayIslemKodlari.CekiRevizyonuUygula;

        public ApprovalReference GetApprovalReference() => new(
            OnayReferansTipleri.CekiRevizyonTalebi,
            TalepId,
            ProjeId,
            "/onay-merkezi");
    }
}
