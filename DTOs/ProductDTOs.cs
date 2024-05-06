using System.ComponentModel.DataAnnotations;

namespace APBD7.DTOs;

public record AddProductToWarehouseDTO(
    [Required] int IdProduct, 
    [Required] int IdWarehouse,
    [Required][Range(1, Int32.MaxValue)] int Amount, 
    [Required] DateTime CreatedAt);