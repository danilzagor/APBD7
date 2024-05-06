using APBD7.DTOs;
using APBD7.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD7.Controllers;


[ApiController]
[Route("api/[controller]")]
public class WarehouseController(IWarehouseService warehouseService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> AddProductToWarehouse(AddProductToWarehouseDTO productToWarehouse)
    {
        try
        {
            var result = await warehouseService.AddProductToWarehouseAsync(productToWarehouse);
            return Ok(result);
        }
        catch (ArgumentException e)
        {
            return NotFound(e.Message);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
    }
}