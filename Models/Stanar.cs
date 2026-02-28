using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Stanar
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Ime { get; set; } = null!;
    public int BrojStana { get; set; }
}