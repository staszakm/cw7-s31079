using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication1.DTOs;
using WebApplication1.Exceptions;
using WebApplication1.Services;

namespace WebApplication1.Collectors;

[ApiController]
    [Route("api/[controller]")]

public class ClientsController(IDBService service) : ControllerBase
{
    
    [HttpPost] //Tworzy nowego klienta
    public async Task<ActionResult> CreateClient([FromBody] ClientCreateDTO body)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var client = await service.CreateClientAsync(body);
            return Created($"/Clients/{client.IdClient}", client);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    
    [HttpGet("{id}/trips")] //zwraca wszystkie wycieczki klienta o podanym ID
    public async Task<IActionResult> GetTripsByClientIdAsync(int id)
    {
        try
        {
            return Ok(await service.GetTripsByClientIdAsync(id));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{id}/trips/{tripId}")] //Zapisuje klienta o podanym id na wycieczke o podanym tripId
    public async Task<IActionResult> RegisterClientToTripAsync(int id, int tripId)
    {
        try
        {
            await service.RegisterClientToTripAsync(id, tripId);
            return Ok("Client registered to trip");

        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{id}/trips/{tripId}")] //Usuwa klienta o id z wycieczki o podanym tripId
    public async Task<IActionResult> RemoveClientFromTripAsync(int id, int tripId)
    {
        try
        {
            await service.RemoveClientFromTripAsync(id, tripId);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
      
}