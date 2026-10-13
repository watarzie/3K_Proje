using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _3K.Application.Features.AmbalajIslemleri.Commands;
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

        [HttpPost("uretim-formlari")]
        public async Task<ActionResult> FormOlustur([FromBody] AmbalajFormOlusturCommand command, CancellationToken ct) =>
            (await _mediator.Send(command, ct)).ToActionResult();

        [HttpGet("uretim-formlari")]
        public async Task<ActionResult> FormSurumleri([FromQuery] GetAmbalajFormSurumleriQuery query, CancellationToken ct) =>
            (await _mediator.Send(query, ct)).ToActionResult();

        [HttpGet("uretim-formlari/{id:int}/dosya")]
        public async Task<ActionResult> FormSurumuDosyasi(int id, [FromQuery] string format = "pdf", CancellationToken ct = default) =>
            (await _mediator.Send(new GetAmbalajFormSurumuDosyasiQuery { Id = id, Format = format }, ct)).ToFileResult();

        [HttpGet("gerceklesen-uretim-raporu")]
        public async Task<ActionResult> GerceklesenRapor([FromQuery] GetAmbalajGerceklesenRaporQuery query, CancellationToken ct) =>
            (await _mediator.Send(query, ct)).ToActionResult();

        [HttpGet("gerceklesen-uretim-raporu/dosya")]
        public async Task<ActionResult> GerceklesenRaporDosyasi([FromQuery] GetAmbalajGerceklesenRaporDosyasiQuery query, CancellationToken ct) =>
            (await _mediator.Send(query, ct)).ToFileResult();

        [HttpPut("gerceklesmeler/{id:int}")]
        public async Task<ActionResult> GerceklesmeDuzelt(int id, [FromBody] AmbalajGerceklesmeDuzeltCommand command, CancellationToken ct)
        {
            command.Id = id;
            return (await _mediator.Send(command, ct)).ToActionResult();
        }

        [HttpPut("kayitlar/{id:int}/durum")]
        public async Task<ActionResult> DurumGuncelle(int id, [FromBody] AmbalajUretimDurumuGuncelleCommand command, CancellationToken ct)
        {
            command.Id = id;
            return (await _mediator.Send(command, ct)).ToActionResult();
        }

        [HttpPut("projeler/{projeId:int}/uretim-durumu")]
        public async Task<ActionResult> ProjeUretimDurumu(int projeId, [FromBody] AmbalajProjeUretimDurumuCommand command, CancellationToken ct)
        {
            command.ProjeId = projeId;
            return (await _mediator.Send(command, ct)).ToActionResult();
        }

        [HttpGet("projeler")]
        public async Task<ActionResult> Projeler([FromQuery] GetAmbalajPlanlamaProjeleriQuery query, CancellationToken ct) =>
            (await _mediator.Send(query, ct)).ToActionResult();

        [HttpGet("projeler/{projeId:int}/plan")]
        public async Task<ActionResult> Plan(int projeId, [FromQuery] int? kaynakProjeTipiId, [FromQuery] int? grup, CancellationToken ct) =>
            (await _mediator.Send(new GetAmbalajPlanlamaPlanQuery
            {
                ProjeId = projeId,
                KaynakProjeTipiId = kaynakProjeTipiId,
                Grup = grup
            }, ct)).ToActionResult();

        [HttpPut("projeler/{projeId:int}/plan")]
        public async Task<ActionResult> PlanKaydet(
            int projeId,
            [FromQuery] int? kaynakProjeTipiId,
            [FromBody] AmbalajPlanKaydetCommand command,
            CancellationToken ct)
        {
            command.ProjeId = projeId;
            command.KaynakProjeTipiId = kaynakProjeTipiId;
            return (await _mediator.Send(command, ct)).ToActionResult();
        }

        [HttpPut("sandiklar/{sandikId:int}/ambalaj-karari")]
        public async Task<ActionResult> AmbalajKarariKaydet(int sandikId, [FromBody] AmbalajKarariKaydetCommand command, CancellationToken ct)
        {
            command.SandikId = sandikId;
            return (await _mediator.Send(command, ct)).ToActionResult();
        }

        [HttpPost("projeler/{projeId:int}/kalemler")]
        public async Task<ActionResult> KalemEkle(int projeId, [FromBody] AmbalajPlanKalemKaydetCommand command, CancellationToken ct)
        {
            command.ProjeId = projeId;
            return (await _mediator.Send(command, ct)).ToActionResult();
        }

        [HttpPut("kalemler/{kalemId:int}")]
        public async Task<ActionResult> KalemGuncelle(int kalemId, [FromBody] AmbalajPlanKalemKaydetCommand command, CancellationToken ct)
        {
            command.KalemId = kalemId;
            return (await _mediator.Send(command, ct)).ToActionResult();
        }

        [HttpDelete("kalemler/{kalemId:int}")]
        public async Task<ActionResult> KalemSil(int kalemId, [FromQuery] string? gerekce, CancellationToken ct) =>
            (await _mediator.Send(new AmbalajPlanKalemSilCommand { KalemId = kalemId, Gerekce = gerekce }, ct)).ToActionResult();

        [HttpGet("ic-sandik-sablonlari")]
        public async Task<ActionResult> IcSandikSablonlari(CancellationToken ct) =>
            (await _mediator.Send(new GetAmbalajIcSandikSablonlariQuery(), ct)).ToActionResult();

        [HttpPost("ic-sandik-sablonlari")]
        public async Task<ActionResult> IcSandikSablonuEkle([FromBody] AmbalajIcSandikSablonuEkleCommand command, CancellationToken ct) =>
            (await _mediator.Send(command, ct)).ToActionResult();

        [HttpDelete("ic-sandik-sablonlari/{sablonId:int}")]
        public async Task<ActionResult> IcSandikSablonuSil(int sablonId, CancellationToken ct) =>
            (await _mediator.Send(new AmbalajIcSandikSablonuSilCommand { SablonId = sablonId }, ct)).ToActionResult();

        [HttpGet("talep-eden-kullanicilar")]
        public async Task<ActionResult> TalepEdenKullanicilar(CancellationToken ct) =>
            (await _mediator.Send(new GetAmbalajTalepEdenKullanicilarQuery(), ct)).ToActionResult();

        [HttpGet("bagimsiz-sandiklar")]
        public async Task<ActionResult> BagimsizSandiklar([FromQuery] GetAmbalajBagimsizSandiklarQuery query, CancellationToken ct) =>
            (await _mediator.Send(query, ct)).ToActionResult();

        [HttpPost("bagimsiz-sandiklar")]
        public async Task<ActionResult> BagimsizSandikEkle([FromBody] AmbalajBagimsizSandikKaydetCommand command, CancellationToken ct) =>
            (await _mediator.Send(command, ct)).ToActionResult();

        [HttpPut("bagimsiz-sandiklar/{sandikId:int}")]
        public async Task<ActionResult> BagimsizSandikGuncelle(
            int sandikId,
            [FromBody] AmbalajBagimsizSandikKaydetCommand command,
            CancellationToken ct)
        {
            command.SandikId = sandikId;
            return (await _mediator.Send(command, ct)).ToActionResult();
        }

        /// <summary>Özel sandığı finansa aktarır: Finans iş kaydı açılır (üretim formu gerekmez).</summary>
        [HttpPost("bagimsiz-sandiklar/{sandikId:int}/finansa-aktar")]
        public async Task<ActionResult> OzelSandikFinansaAktar(
            int sandikId,
            [FromBody] AmbalajOzelSandikFinansCommand command,
            CancellationToken ct)
        {
            command.SandikId = sandikId;
            command.Aktar = true;
            return (await _mediator.Send(command, ct)).ToActionResult();
        }

        /// <summary>Özel sandığı finanstan geri alır; finans kaydı PO'ya bağlıysa 409.</summary>
        [HttpPost("bagimsiz-sandiklar/{sandikId:int}/finanstan-geri-al")]
        public async Task<ActionResult> OzelSandikFinanstanGeriAl(
            int sandikId,
            [FromBody] AmbalajOzelSandikFinansCommand command,
            CancellationToken ct)
        {
            command.SandikId = sandikId;
            command.Aktar = false;
            return (await _mediator.Send(command, ct)).ToActionResult();
        }

        [HttpDelete("bagimsiz-sandiklar/{sandikId:int}")]
        public async Task<ActionResult> BagimsizSandikSil(int sandikId, [FromQuery] string? gerekce, CancellationToken ct) =>
            (await _mediator.Send(new AmbalajBagimsizSandikSilCommand { SandikId = sandikId, Gerekce = gerekce }, ct)).ToActionResult();

        [HttpGet("projeler/{projeId:int}/ilave-sandik-adaylari")]
        public async Task<ActionResult> IlaveSandikAdaylari(int projeId, [FromQuery] int? mevcutKayitId, CancellationToken ct) =>
            (await _mediator.Send(new GetAmbalajIlaveSandikAdaylariQuery
            {
                ProjeId = projeId,
                MevcutKayitId = mevcutKayitId
            }, ct)).ToActionResult();

        [HttpGet("projeler/{projeId:int}/sandik-secenekleri")]
        public async Task<ActionResult> ProjeSandikSecenekleri(int projeId, CancellationToken ct) =>
            (await _mediator.Send(new GetAmbalajProjeSandikSecenekleriQuery { ProjeId = projeId }, ct)).ToActionResult();
    }
}
