using Microsoft.Data.SqlClient;
using WebApplication1.DTOs;
using WebApplication1.Exceptions;

namespace WebApplication1.Services;

public interface IDBService
{
    public Task<ClientGetDTO> CreateClientAsync(ClientCreateDTO client);
    public Task<IEnumerable<TripGetDTO>> GetTripsByClientIdAsync(int id);
    public Task<IEnumerable<TripGetDTO>> GetTripsAsync();
    public Task RegisterClientToTripAsync(int clientId, int tripId);
    public Task RemoveClientFromTripAsync(int clientId, int tripId);
}


public class DbService(IConfiguration config) : IDBService
{
    public async Task<IEnumerable<TripGetDTO>> GetTripsByClientIdAsync(int id)
    {
        var conStr = config.GetConnectionString("Default");
        await using var con = new SqlConnection(conStr);
        var sqlCommand = "select 1 from Client where IdClient = @id"; //sprawdzanie czy klient o takim id istnieje
        await using var com = new SqlCommand(sqlCommand, con);
        com.Parameters.AddWithValue("@id", id);
        await con.OpenAsync();
        
        var clientExists = await com.ExecuteScalarAsync();
        
        if (clientExists == null)
        {
            throw new NotFoundException($"Client with id {id} not found");
        }
        
        var result = new List<TripGetDTO>();
        
        //znalezenie wycieczek do ktorych jest przypisany klient o danym id
        var sqlCommand2 = "select t.IdTrip, t.Name,t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.RegisteredAt, ct.PaymentDate from Trip t Inner join Client_Trip ct on t.IdTrip = ct.IdTrip Inner join Client c on ct.IdClient = c.IdClient where c.IdClient = @id";
        await using var com2 = new SqlCommand(sqlCommand2, con);
        com2.Parameters.AddWithValue("@id", id);
        await using var reader = await com2.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO()
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                RegisteredAt = reader.GetInt32(6),
                PaymentDate = reader.IsDBNull(7) ? null : reader.GetInt32(7)
            });
        }

        return result;
    }
    

    public async Task<ClientGetDTO> CreateClientAsync(ClientCreateDTO client)
    {
        //sprawdzenie czy nie istnieje juz klient o takim numerze PESEL
        if (await ClientWithPeselExistsAsync(client.Pesel))
        {
            throw new Exception($"Client with: {client.Pesel} pesel already exists");
        }
        
        var conStr = config.GetConnectionString("Default");
        await using var con = new SqlConnection(conStr);
        //wstawienie do bazy nowego klienta
        var sqlCommand = "insert into Client (FirstName, LastName, Email, Telephone, Pesel) values (@firstname, @lastname, @email, @telephone, @pesel); select scope_identity()";
        await using var com = new SqlCommand(sqlCommand, con);
        
        
        com.Parameters.AddWithValue("@firstname",client.FirstName);
        com.Parameters.AddWithValue("@lastname",client.LastName);
        com.Parameters.AddWithValue("@email",client.Email);
        com.Parameters.AddWithValue("@telephone",client.Telephone);
        com.Parameters.AddWithValue("@pesel",client.Pesel);
        await con.OpenAsync();
        var nextId = Convert.ToInt32(await com.ExecuteScalarAsync());
        return new ClientGetDTO()
        {
            IdClient = nextId,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Telephone = client.Telephone,
            Pesel = client.Pesel
        };
    }
    
    private async Task<bool> ClientWithPeselExistsAsync(string pesel)
    {
        var conStr = config.GetConnectionString("Default");
        await using var con = new SqlConnection(conStr);
        var sqlCommand = "SELECT 1 FROM Client WHERE Pesel = @pesel";
        await using var com = new SqlCommand(sqlCommand, con);
        com.Parameters.AddWithValue("@pesel", pesel);
        await con.OpenAsync();
        var result = await com.ExecuteScalarAsync();
        return result != null;
    }
    
    public async Task<IEnumerable<TripGetDTO>> GetTripsAsync()
    {
        var result = new List<TripGetDTO>();
        var conStr = config.GetConnectionString("Default");
        await using var con = new SqlConnection(conStr);
        //znalezienie danych wycieczek wraz z odpowiednim dla nich krajem
        var sqlCommand = @"select t.IdTrip,
                         t.Name,
                         t.Description,
                         t.DateFrom,
                         t.DateTo,
                         t.MaxPeople,
                         c.Name AS CountryName
                         from Trip t
                         Inner join Country_Trip ct on t.IdTrip = ct.IdTrip
                         Inner join Country c on ct.IdCountry = c.IdCountry";
                         
                            
        await using var com = new SqlCommand(sqlCommand, con);
        
        await con.OpenAsync();
        await using var reader = await com.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO
            {   
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                Country = reader.GetString(6)
            });
        }
        
        return result;
    }
    
    
public async Task RegisterClientToTripAsync(int clientId, int tripId)
{
    var conStr = config.GetConnectionString("Default");
    using (var con = new SqlConnection(conStr))
    {
        await con.OpenAsync();
        using (var transaction = con.BeginTransaction())
        {
            try
            {
                // sprawdzenie czy istnieje  klint o podanym id
                var checkClient = "select 1 from Client where IdClient = @clientId";
                await using var com = new SqlCommand(checkClient, con, transaction);
                com.Parameters.AddWithValue("@clientId", clientId);
                var clientExistsR = await com.ExecuteScalarAsync();
                var clientExists = clientExistsR != null && clientExistsR != DBNull.Value;

                if (!clientExists)
                {
                    throw new NotFoundException($"Client with id {clientId} not found");
                }
                
                // sprawdzenie czy istnieje wycieczka  o podanym id
                var checkTrip = "select 1 from Trip where IdTrip = @tripId";
                await using var com2 = new SqlCommand(checkTrip, con, transaction);
                com2.Parameters.AddWithValue("@tripId", tripId);
                var tripExistsR = await com2.ExecuteScalarAsync();
                var tripExists = tripExistsR != null && tripExistsR != DBNull.Value;

                if (!tripExists)
                {
                    throw new NotFoundException($"Trip with id {tripId} not found");
                }

                // sprawdzenie czy klient nie jest juz zapisany na dana wycieczke
                var clientReg = "select 1 from Client_Trip where IdClient = @clientId AND IdTrip = @tripId";
                await using var com3 = new SqlCommand(clientReg, con,transaction);
                com3.Parameters.AddWithValue("@clientId", clientId);
                com3.Parameters.AddWithValue("@tripId", tripId);
                var clientRegistered = await com3.ExecuteScalarAsync();
                
                if (clientRegistered != null)
                {
                    throw new NotFoundException($"Client with id {clientId} is already registered for trip {tripId}");
                }

                // sprawdzenie czy limit uczestników nie został osiągniety
                var actualAtt = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @tripId";
                await using var com4 = new SqlCommand(actualAtt, con, transaction);
                com4.Parameters.AddWithValue("@tripId", tripId);
                var count = (int)await com4.ExecuteScalarAsync();
                
                var maxAtt = "SELECT MaxPeople FROM Trip WHERE IdTrip = @tripId";
                await using var com5 = new SqlCommand(maxAtt, con, transaction);
                com5.Parameters.AddWithValue("@tripId", tripId);
                var maxPeople = (int)await com5.ExecuteScalarAsync();

                if (count >= maxPeople)
                {
                    throw new NotFoundException("Max participants reached");
                }
                //zapisanie uczestnika na wycieczke
                var insertAtt = "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@clientId, @tripId, @registeredAt)";
                await using var com6 = new SqlCommand(insertAtt, con, transaction);
                com6.Parameters.AddWithValue("@clientId", clientId);
                com6.Parameters.AddWithValue("@tripId", tripId);
                com6.Parameters.AddWithValue("@registeredAt", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());

                
                await com6.ExecuteNonQueryAsync();
                transaction.Commit();
                return;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}


public async Task RemoveClientFromTripAsync(int clientId, int tripId)
{
    var conStr = config.GetConnectionString("Default");
    using (var con = new SqlConnection(conStr))
    {
        await con.OpenAsync();
        using (var transaction = con.BeginTransaction())
        {
            try
            {
                // sprawdzenie czy klient jest zapisany na dana wycieczke
                var clientReg = "select 1 from Client_Trip where IdClient = @clientId AND IdTrip = @tripId";
                await using var com = new SqlCommand(clientReg, con, transaction);
                com.Parameters.AddWithValue("@clientId", clientId);
                com.Parameters.AddWithValue("@tripId", tripId);
                var clientRegistered = await com.ExecuteScalarAsync();

                if (clientRegistered == null)
                {
                    throw new NotFoundException($"Client with id {clientId} is not registered for trip {tripId}");
                }
                //ususniecie klienta z wycieczki
                var rmvAtt = "delete from Client_Trip where IdClient = @clientId and IdTrip = @tripId";
                await using var com2 = new SqlCommand(rmvAtt, con, transaction);
                com2.Parameters.AddWithValue("@clientId", clientId);
                com2.Parameters.AddWithValue("@tripId", tripId);
                await com2.ExecuteNonQueryAsync();
                transaction.Commit();

            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

    }
}
}