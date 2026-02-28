using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace SmartBuildingServer.Models;

public class Zgrada
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Adresa { get; set; } = null!;
    public int BrojStanova { get; set; }
    public decimal BudzetZgrade { get; set; }

    // Ugnježdeni dokumenti - NoSQL moć
    public List<Kvar> Kvarovi { get; set; } = new();

    // Referenca na ID-eve stanara (Linking)
    public List<string> StanariIds { get; set; } = new();
}

public class Kvar
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string Opis { get; set; } = null!;
    public decimal Trosak { get; set; }
    [BsonElement("Hitno")] 
    public bool Hitno { get; set; }
    public string Status { get; set; } = "Prijavljen"; // Prijavljen, U toku, Resen
    public DateTime Datum { get; set; } = DateTime.Now;
}

