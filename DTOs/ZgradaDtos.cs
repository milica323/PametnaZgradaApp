using System.ComponentModel.DataAnnotations;

namespace SmartBuildingServer.DTOs;

public record CreateZgradaDto(
    [Required(ErrorMessage = "Adresa je obavezna")] 
    string Adresa, 
    
    [Range(1, 1000, ErrorMessage = "Broj stanova mora biti između 1 i 1000")]
    int BrojStanova, 
    
    [Range(0, double.MaxValue, ErrorMessage = "Budžet ne može biti negativan")]
    decimal BudzetZgrade
);

public record PrijaviKvarDto(
    [Required] [StringLength(500, MinimumLength = 5)] string Opis, 
    [Range(1, 1000000)] decimal Trosak, 
    bool Hitno
);