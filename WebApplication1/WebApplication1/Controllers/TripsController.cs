using Microsoft.AspNetCore.Mvc;
using WebApplication1.Exceptions;
using WebApplication1.Services;

namespace WebApplication1.Collectors;

[ApiController]
    [Route("api/[controller]")]

public class TripsController(IDBService service) : ControllerBase
{
    [HttpGet] //zwraca wszystkie wycieczki wraz z odpowiednim dla nich krajem
    public async Task<IActionResult> GetTripsAsync()
    {
        try
        {
            return Ok(await service.GetTripsAsync());
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        
    }
    
    
    
}