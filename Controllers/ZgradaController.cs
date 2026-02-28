using Microsoft.AspNetCore.Mvc;
using SmartBuildingServer.Models;
using SmartBuildingServer.Services;
using SmartBuildingServer.DTOs;
using MongoDB.Bson;

[ApiController]
[Route("api/[controller]")]
public class ZgradaController : ControllerBase
{
    private readonly ZgradaService _service;
    public ZgradaController(ZgradaService s) => _service = s;

    [HttpGet]
    public async Task<List<Zgrada>> Get() => await _service.GetAll();

    [HttpPost]
    public async Task<IActionResult> Create(CreateZgradaDto dto)
    {
        
        if (string.IsNullOrWhiteSpace(dto.Adresa))
        {
            return BadRequest("Adresa zgrade ne sme biti prazna.");
        }

        if (dto.BrojStanova <= 0)
        {
            return BadRequest("Zgrada mora imati barem jedan stan.");
        }

        var z = new Zgrada
        {
            Adresa = dto.Adresa,
            BrojStanova = dto.BrojStanova,
            BudzetZgrade = dto.BudzetZgrade
        };

        await _service.Create(z);
        return Ok(z);
    }

    [HttpPost("{id}/kvar")]
    public async Task<IActionResult> Kvar(string id, PrijaviKvarDto dto)
    {
        var k = new Kvar { Opis = dto.Opis, Trosak = dto.Trosak, Hitno = dto.Hitno };
        await _service.DodajKvar(id, k);
        return Ok("Kvar prijavljen.");
    }

    [HttpPatch("{zId}/kvar/{kId}/resi")]
    public async Task<IActionResult> Resi(string zId, string kId)
    {
        await _service.ResiKvar(zId, kId);
        return Ok("Kvar je rešen.");
    }

    [HttpGet("{id}/statistika")]
    public async Task<IActionResult> Statistika(string id)
    {
        var trosak = await _service.GetUkupniTroskovi(id);
        return Ok(new { UkupnoTroskova = trosak });
    }


    [HttpPatch("{id}/resi-sve-hitne")]
    public async Task<IActionResult> ResiHitne(string id) 
    {
        await _service.ResiSveHitneKvarove(id);
        return Ok(new { poruka = "Uspešno rešeno" });
    }

    [HttpGet("{id}/izvestaj")]
    public async Task<IActionResult> GetIzvestaj(string id)
    {
        var stats = await _service.GetMesecnaStatistika(id);

        
        var rezultat = stats.Select(s => new
        {
            _id = s.Contains("_id") ? s["_id"].ToString() : "Nepoznato",
            brojKvarova = s.Contains("brojKvarova") ? s["brojKvarova"].ToInt32() : 0,

           
            ukupniTrosak = s.Contains("ukupniTrosak") ? s["ukupniTrosak"].ToDouble() : 0,
            prosecanTrosak = s.Contains("prosecanTrosak") ? s["prosecanTrosak"].ToDouble() : 0
        }).ToList();

        return Ok(rezultat);
    }

}