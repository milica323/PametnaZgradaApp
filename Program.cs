using MongoDB.Driver;
using SmartBuildingServer.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --- 1. MONGODB KONFIGURACIJA ---
// Povezivanje na tvoj Docker kontejner "mongo-zgrada"
var mongoClient = new MongoClient("mongodb://localhost:27017");
var database = mongoClient.GetDatabase("SmartBuildingDB");

// Registrujemo bazu kao Singleton kako bi Servis mogao da je koristi
builder.Services.AddSingleton(database);

// --- 2. REGISTRACIJA SERVISA ---
// Registrujemo ZgradaService koji sadrži svu logiku i agregacije
builder.Services.AddScoped<ZgradaService>();

// --- 3. KONTROLERI I JSON PODEŠAVANJA ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ignoriše null vrednosti u odgovorima kako bi JSON bio čist
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// --- 4. SWAGGER KONFIGURACIJA ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 5. CORS (OBAVEZNO ZA FRONTEND) ---
// Dozvoljava JavaScript-u (index.html) da pristupi API-ju
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- 6. MIDDLEWARE I SWAGGER UI ---
// Omogućavamo Swagger uvek radi lakšeg testiranja projekta
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Precizna putanja do JSON fajla da bi se izbegla "Fetch error"
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Building API v1");

    // Postavlja Swagger na OSNOVNU putanju (http://localhost:5006/)
    c.RoutePrefix = string.Empty;
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();