using MongoDB.Driver;
using MongoDB.Bson;
using SmartBuildingServer.Models;

namespace SmartBuildingServer.Services;

public class ZgradaService
{
    private readonly IMongoCollection<Zgrada> _zgrade;

    public ZgradaService(IMongoDatabase db) => _zgrade = db.GetCollection<Zgrada>("Zgrade");

    public async Task<List<Zgrada>> GetAll() => await _zgrade.Find(_ => true).ToListAsync();

    public async Task Create(Zgrada z) => await _zgrade.InsertOneAsync(z);

    // 1. Atomska operacija: Dodaj u niz ($push)
    public async Task DodajKvar(string id, Kvar k)
    {
        var filter = Builders<Zgrada>.Filter.Eq(z => z.Id, id);
        var update = Builders<Zgrada>.Update.Push(z => z.Kvarovi, k);
        await _zgrade.UpdateOneAsync(filter, update);
    }

    // 2. Napredna operacija: Update unutar niza ($ positional operator)
    public async Task ResiKvar(string zId, string kId)
    {
        // 1. Pronađi zgradu
        var zgrada = await _zgrade.Find(z => z.Id == zId).FirstOrDefaultAsync();
        if (zgrada == null) return;

        // 2. Pronađi kvar
        var kvar = zgrada.Kvarovi.FirstOrDefault(k => k.Id == kId);

        // 3. Ako kvar postoji i nije već rešen
        if (kvar != null && kvar.Status != "Resen")
        {
            
            decimal noviBudzet = zgrada.BudzetZgrade - kvar.Trosak;

            var filter = Builders<Zgrada>.Filter.Eq(z => z.Id, zId);

           
            var update = Builders<Zgrada>.Update
                .Set("Kvarovi.$[k].Status", "Resen")
                .Set(z => z.BudzetZgrade, noviBudzet); 

            var options = new UpdateOptions
            {
                ArrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument("k._id", new ObjectId(kId))
                )
            }
            };

            await _zgrade.UpdateOneAsync(filter, update, options);
        }
    }

    // 3. Agregacija: Izračunaj ukupne troškove svih kvarova u zgradi
    public async Task<decimal> GetUkupniTroskovi(string zId)
    {
        var pipeline = new BsonDocument[]
        {
            new BsonDocument("$match", new BsonDocument("_id", new ObjectId(zId))),
            new BsonDocument("$unwind", "$Kvarovi"),
            new BsonDocument("$group", new BsonDocument {
                { "_id", "$_id" },
                { "total", new BsonDocument("$sum", "$Kvarovi.Trosak") }
            })
        };

        var result = await _zgrade.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        return result != null ? (decimal)result["total"] : 0;
    }

    // 1. NAPREDNI UPDATE: Koristi arrayFilters da odjednom rešiš SVE kvarove koji su hitni
  
   public async Task ResiSveHitneKvarove(string zId)
{
    var zgrada = await _zgrade.Find(z => z.Id == zId).FirstOrDefaultAsync();
    if (zgrada == null) return;

    // 1. Nađemo sve koji su hitni i NISU rešeni
    var hitni = zgrada.Kvarovi.Where(k => k.Hitno && k.Status != "Resen").ToList();
    
    if (hitni.Any())
    {
       
        foreach (var k in hitni)
        {
            k.Status = "Resen";
        }

        // 3. Izračunamo novi budžet (odužmemo zbir svih hitnih)
        decimal ukupniTrosak = hitni.Sum(k => k.Trosak);
        decimal noviBudzet = zgrada.BudzetZgrade - ukupniTrosak;

        
        var filter = Builders<Zgrada>.Filter.Eq(z => z.Id, zId);
        var update = Builders<Zgrada>.Update
            .Set(z => z.Kvarovi, zgrada.Kvarovi) 
            .Set(z => z.BudzetZgrade, noviBudzet);

        await _zgrade.UpdateOneAsync(filter, update);
    }
}

    // 2. KOMPLEKSNA AGREGACIJA: Izveštaj za upravnika
    // Match (zgrada) -> Unwind (razbij niz) -> Match (filtriraj) -> Group (statistika)
    public async Task<List<BsonDocument>> GetMesecnaStatistika(string zId)
    {
        var pipeline = new BsonDocument[]
        {
        new BsonDocument("$match", new BsonDocument("_id", new ObjectId(zId))),
        new BsonDocument("$unwind", "$Kvarovi"),
        new BsonDocument("$group", new BsonDocument
        {
            { "_id", "$Kvarovi.Status" },
            { "brojKvarova", new BsonDocument("$sum", 1) },
            // Unutar $group faze u ZgradaService.cs
            { "ukupniTrosak", new BsonDocument("$sum", new BsonDocument("$toDouble", "$Kvarovi.Trosak")) },
            { "prosecanTrosak", new BsonDocument("$avg", "$Kvarovi.Trosak") }
        }),
        new BsonDocument("$sort", new BsonDocument("ukupniTrosak", -1)) // Sortiraj od najskupljeg statusa
        };

        return await _zgrade.Aggregate<BsonDocument>(pipeline).ToListAsync();
    }
}