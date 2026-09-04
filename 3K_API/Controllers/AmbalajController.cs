using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K_API.Extensions;

namespace _3K_API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AmbalajController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AmbalajController(IMediator mediator) => _mediator = mediator;

        [HttpGet("projeler")]
        public async Task<ActionResult> Projeler(
            [FromQuery] GetAmbalajPlanlamaProjeleriQuery query,
            CancellationToken cancellationToken) =>
            (await _mediator.Send(query, cancellationToken)).ToActionResult();

        [HttpGet("projeler/{projeId:int}/plan")]
        public async Task<ActionResult> Plan(
            int projeId,
            [FromQuery] int? kaynakProjeTipiId,
            [FromQuery] int? grup) =>
            (await _mediator.Send(new GetAmbalajPlanlamaPlanQuery
            {
                ProjeId = projeId,
                KaynakProjeTipiId = kaynakProjeTipiId,
                Grup = grup
            })).ToActionResult();

        [HttpPut("projeler/{projeId:int}/plan")]
        public async Task<ActionResult> PlanKaydet(
            int projeId,
            [FromQuery] int? kaynakProjeTipiId,
            [FromBody] AmbalajPlanKaydetCommand command)
        {
            command.ProjeId = projeId;
            command.KaynakProjeTipiId = kaynakProjeTipiId;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpPut("sandiklar/{sandikId:int}/ambalaj-karari")]
        public async Task<ActionResult> AmbalajKarariKaydet(
            int sandikId,
            [FromBody] AmbalajKarariKaydetCommand command)
        {
            command.SandikId = sandikId;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpPost("projeler/{projeId:int}/kalemler")]
        public async Task<ActionResult> KalemEkle(
            int projeId,
            [FromBody] AmbalajPlanKalemKaydetCommand command)
        {
            command.ProjeId = projeId;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpPut("kalemler/{kalemId:int}")]
        public async Task<ActionResult> KalemGuncelle(
            int kalemId,
            [FromBody] AmbalajPlanKalemKaydetCommand command)
        {
            command.KalemId = kalemId;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpDelete("kalemler/{kalemId:int}")]
        public async Task<ActionResult> KalemSil(int kalemId) =>
            (await _mediator.Send(new AmbalajPlanKalemSilCommand { KalemId = kalemId })).ToActionResult();

        [HttpGet("ic-sandik-sablonlari")]
        public async Task<ActionResult> IcSandikSablonlari() =>
            (await _mediator.Send(new GetAmbalajIcSandikSablonlariQuery())).ToActionResult();

        [HttpPost("ic-sandik-sablonlari")]
        public async Task<ActionResult> IcSandikSablonuEkle(
            [FromBody] AmbalajIcSandikSablonuEkleCommand command) =>
            (await _mediator.Send(command)).ToActionResult();

        [HttpDelete("ic-sandik-sablonlari/{sablonId:int}")]
        public async Task<ActionResult> IcSandikSablonuSil(int sablonId) =>
            (await _mediator.Send(new AmbalajIcSandikSablonuSilCommand { SablonId = sablonId })).ToActionResult();

        [HttpGet("talep-edenler")]
        public async Task<ActionResult> TalepEdenler() =>
            (await _mediator.Send(new GetAmbalajTalepEdenlerQuery())).ToActionResult();

        [HttpPost("talep-edenler")]
        public async Task<ActionResult> TalepEdenEkle([FromBody] AmbalajTalepEdenEkleCommand command) =>
            (await _mediator.Send(command)).ToActionResult();

        [HttpGet("talep-eden-kullanicilar")]
        public async Task<ActionResult> TalepEdenKullanicilar(CancellationToken cancellationToken) =>
            (await _mediator.Send(
                new GetAmbalajTalepEdenKullanicilarQuery(),
                cancellationToken)).ToActionResult();

        [HttpGet("bagimsiz-sandiklar")]
        public async Task<ActionResult> BagimsizSandiklar(
            [FromQuery] GetAmbalajBagimsizSandiklarQuery query,
            CancellationToken cancellationToken) =>
            (await _mediator.Send(query, cancellationToken)).ToActionResult();

        [HttpPost("bagimsiz-sandiklar")]
        public async Task<ActionResult> BagimsizSandikEkle(
            [FromBody] AmbalajBagimsizSandikKaydetCommand command) =>
            (await _mediator.Send(command)).ToActionResult();

        [HttpPut("bagimsiz-sandiklar/{sandikId:int}")]
        public async Task<ActionResult> BagimsizSandikGuncelle(
            int sandikId,
            [FromBody] AmbalajBagimsizSandikKaydetCommand command)
        {
            command.SandikId = sandikId;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpDelete("bagimsiz-sandiklar/{sandikId:int}")]
        public async Task<ActionResult> BagimsizSandikSil(int sandikId) =>
            (await _mediator.Send(new AmbalajBagimsizSandikSilCommand { SandikId = sandikId })).ToActionResult();

        [HttpGet("projeler/{projeId:int}/ilave-sandik-adaylari")]
        public async Task<ActionResult> IlaveSandikAdaylari(
            int projeId,
            [FromQuery] int? mevcutKayitId) =>
            (await _mediator.Send(new GetAmbalajIlaveSandikAdaylariQuery
            {
                ProjeId = projeId,
                MevcutKayitId = mevcutKayitId
            })).ToActionResult();

        [HttpGet("projeler/{projeId:int}/sandik-secenekleri")]
        public async Task<ActionResult> ProjeSandikSecenekleri(
            int projeId,
            CancellationToken cancellationToken) =>
            (await _mediator.Send(new GetAmbalajProjeSandikSecenekleriQuery
            {
                ProjeId = projeId
            }, cancellationToken)).ToActionResult();

        [HttpPost("projeler/{projeId:int}/kaynaklari-senkronize-et")]
        public async Task<ActionResult> KaynaklariSenkronizeEt(int projeId) =>
            (await _mediator.Send(new AmbalajKaynaklariSenkronizeEtCommand { ProjeId = projeId })).ToActionResult();

        [HttpGet("kayitlar")]
        public async Task<ActionResult> Kayitlar([FromQuery] GetAmbalajUretimKayitlariQuery query) =>
            (await _mediator.Send(query)).ToActionResult();

        [HttpGet("kayitlar/sayfali")]
        public async Task<ActionResult> KayitlarSayfali([FromQuery] GetAmbalajUretimSayfasiQuery query) =>
            (await _mediator.Send(query)).ToActionResult();

        [HttpGet("manuel-proje-secenekleri")]
        public async Task<ActionResult> ManuelProjeSecenekleri(
            [FromQuery] GetAmbalajManuelProjeSecenekleriQuery query) =>
            (await _mediator.Send(query)).ToActionResult();

        [HttpGet("kayitlar/{id:int}")]
        public async Task<ActionResult> KayitDetay(int id) =>
            (await _mediator.Send(new GetAmbalajUretimKaydiDetayQuery { Id = id })).ToActionResult();

        [HttpPost("kayitlar")]
        public async Task<ActionResult> KayitOlustur([FromBody] AmbalajUretimKaydiOlusturCommand command) =>
            (await _mediator.Send(command)).ToActionResult();

        [HttpPut("kayitlar/{id:int}")]
        public async Task<ActionResult> KayitGuncelle(int id, [FromBody] AmbalajUretimKaydiGuncelleCommand command)
        {
            command.Id = id;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpPut("kayitlar/{id:int}/secim")]
        public async Task<ActionResult> SecimGuncelle(int id, [FromBody] AmbalajUretimSecimGuncelleCommand command)
        {
            command.Id = id;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpPut("kayitlar/{id:int}/durum")]
        public async Task<ActionResult> DurumGuncelle(int id, [FromBody] AmbalajUretimDurumuGuncelleCommand command)
        {
            command.Id = id;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpPut("kayitlar/{id:int}/m3-override")]
        public async Task<ActionResult> M3OverrideGuncelle(int id, [FromBody] AmbalajM3OverrideGuncelleCommand command)
        {
            command.Id = id;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpPut("kayitlar/{id:int}/sarf-orani")]
        public async Task<ActionResult> SarfOraniGuncelle(int id, [FromBody] AmbalajSarfOraniGuncelleCommand command)
        {
            command.Id = id;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpPost("kayitlar/{id:int}/iptal")]
        public async Task<ActionResult> IptalEt(int id, [FromBody] AmbalajUretimKaydiIptalEtCommand command)
        {
            command.Id = id;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpPost("kayitlar/{id:int}/aktiflestir")]
        public async Task<ActionResult> Aktiflestir(int id, [FromBody] AmbalajUretimKaydiAktiflestirCommand command)
        {
            command.Id = id;
            return (await _mediator.Send(command)).ToActionResult();
        }

        [HttpGet("rapor")]
        public async Task<ActionResult> Rapor([FromQuery] GetAmbalajRaporQuery query) =>
            (await _mediator.Send(query)).ToActionResult();

        [HttpGet("rapor/dosya")]
        public async Task<ActionResult> RaporDosyasi([FromQuery] GetAmbalajRaporDosyasiQuery query) =>
            DosyayaDonustur(await _mediator.Send(query));

        [HttpGet("kayitlar/{id:int}/uretim-formu")]
        public async Task<ActionResult> KayitUretimFormu(int id) =>
            (await _mediator.Send(new GetAmbalajUretimFormuQuery { KayitId = id })).ToActionResult();

        [HttpGet("kayitlar/{id:int}/uretim-formu/dosya")]
        public async Task<ActionResult> KayitUretimFormuDosyasi(int id, [FromQuery] string format = "pdf") =>
            DosyayaDonustur(await _mediator.Send(new GetAmbalajUretimFormuDosyasiQuery
            {
                KayitId = id,
                Format = format
            }));

        [HttpGet("projeler/{projeId:int}/uretim-formu")]
        public async Task<ActionResult> ProjeUretimFormu(int projeId) =>
            (await _mediator.Send(new GetAmbalajUretimFormuQuery { ProjeId = projeId })).ToActionResult();

        [HttpGet("projeler/{projeId:int}/uretim-formu/dosya")]
        public async Task<ActionResult> ProjeUretimFormuDosyasi(int projeId, [FromQuery] string format = "pdf") =>
            DosyayaDonustur(await _mediator.Send(new GetAmbalajUretimFormuDosyasiQuery
            {
                ProjeId = projeId,
                Format = format
            }));

        [HttpGet("manuel-proje/uretim-formu")]
        public async Task<ActionResult> ManuelProjeUretimFormu([FromQuery] string manuelProjeNo) =>
            (await _mediator.Send(new GetAmbalajUretimFormuQuery
            {
                ManuelProjeNo = manuelProjeNo
            })).ToActionResult();

        [HttpGet("manuel-proje/uretim-formu/dosya")]
        public async Task<ActionResult> ManuelProjeUretimFormuDosyasi(
            [FromQuery] string manuelProjeNo,
            [FromQuery] string format = "pdf") =>
            DosyayaDonustur(await _mediator.Send(new GetAmbalajUretimFormuDosyasiQuery
            {
                ManuelProjeNo = manuelProjeNo,
                Format = format
            }));

        /// <summary>
        /// Aynı proje altında kullanıcı tarafından seçilen sandıklar için tek bir üretim formu üretir.
        /// POST kullanılması, kayıt listesinin URL/query string boyut sınırına takılmasını önler.
        /// </summary>
        [HttpPost("uretim-formu/dosya")]
        public async Task<ActionResult> SeciliKayitlarUretimFormuDosyasi(
            [FromBody] AmbalajSeciliUretimFormuDosyasiRequest request,
            [FromQuery] string format = "pdf")
        {
            return DosyayaDonustur(await _mediator.Send(new GetAmbalajUretimFormuDosyasiQuery
            {
                KayitIdleri = request.KayitIdleri,
                Format = format
            }));
        }

        private ActionResult DosyayaDonustur(Result<AmbalajDosyaDto> result) =>
            result.IsSuccess
                ? File(result.Value!.Icerik, result.Value.IcerikTuru, result.Value.DosyaAdi)
                : result.ToActionResult();
    }
}
