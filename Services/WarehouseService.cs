using System.Data;
using System.Data.SqlClient;
using APBD7.DTOs;

namespace APBD7.Services;

public interface IWarehouseService
{
    Task<int> AddProductToWarehouseAsync(AddProductToWarehouseDTO productToWarehouse);
}

public class WarehouseService(IConfiguration configuration) : IWarehouseService
{
    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    public async Task<int> AddProductToWarehouseAsync(AddProductToWarehouseDTO productToWarehouse)
    {
        await using var connection = await GetConnection();
        var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = "SELECT IdProduct FROM Product WHERE IdProduct=@IdProduct";
        command.Parameters.AddWithValue("@IdProduct", productToWarehouse.IdProduct);
        var reader = await command.ExecuteReaderAsync();

        if (!reader.HasRows) throw new ArgumentException($"Product with the id {productToWarehouse.IdProduct} doesn't exist!");

        command.CommandText = "SELECT IdWarehouse FROM Warehouse WHERE IdWarehouse=@IdWarehouse";
        command.Parameters.AddWithValue("@IdWarehouse", productToWarehouse.IdWarehouse);
        await reader.DisposeAsync();
        reader = await command.ExecuteReaderAsync();

        if (!reader.HasRows) throw new ArgumentException($"Warehouse with the id {productToWarehouse.IdWarehouse} doesn't exist!");

        command.CommandText = "SELECT * FROM [Order] WHERE IdProduct=@IdProduct1 AND Amount=@Amount";
        command.Parameters.AddWithValue("@IdProduct1", productToWarehouse.IdProduct);
        command.Parameters.AddWithValue("@Amount", productToWarehouse.Amount);
        await reader.DisposeAsync();
        reader = await command.ExecuteReaderAsync();
        
        if (!reader.HasRows) {throw new ArgumentException("No order with this id and amount!");}
        await reader.ReadAsync();
        var result = new OrderDTO(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetDateTime(3),
            !await reader.IsDBNullAsync(4) ? reader.GetDateTime(4) :  DateTime.MinValue
        );
        
        if (result.CreatedAt > productToWarehouse.CreatedAt) throw new InvalidOperationException("Order was created later than in request!");
        

        if (result.FulfilledAt != DateTime.MinValue) throw new InvalidOperationException("Order has been already fulfilled!");
        
        
        await reader.DisposeAsync();
        command.CommandText = "SELECT IdProductWarehouse FROM Product_Warehouse WHERE IdProduct=@IdProduct2";
        command.Parameters.AddWithValue("@IdProduct2", productToWarehouse.IdProduct);
        await reader.DisposeAsync();
        reader = await command.ExecuteReaderAsync();

        if (reader.HasRows) throw new InvalidOperationException("Order has been already fulfilled and added to warehouse!");
         
        await reader.DisposeAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var c = new SqlCommand("",connection, (SqlTransaction)transaction);
        try
        {
            c.CommandText =
                "UPDATE [Order] SET FulFilledAt = @FulFilledAt WHERE IdProduct=@IdProduct3 AND Amount=@Amount1";
            c.Parameters.AddWithValue("@FulFilledAt", DateTime.Now);
            c.Parameters.AddWithValue("@IdProduct3", productToWarehouse.IdProduct);
            c.Parameters.AddWithValue("@Amount1", productToWarehouse.Amount);
            await c.ExecuteNonQueryAsync();
            await reader.DisposeAsync();
            c.CommandText = "SELECT Price FROM Product WHERE IdProduct=@IdProduct4";
            c.Parameters.AddWithValue("@IdProduct4", productToWarehouse.IdProduct);
            await reader.DisposeAsync();
            reader = await c.ExecuteReaderAsync();
            await reader.ReadAsync();
            c.CommandText =
                "INSERT INTO Product_Warehouse VALUES(@IdWarehouse1, @IdProduct6, @IdOrder, @Amount3, @Price, @Date) select cast(scope_identity() as int)";
            c.Parameters.AddWithValue("@IdWarehouse1", productToWarehouse.IdWarehouse);
            c.Parameters.AddWithValue("@IdProduct6", productToWarehouse.IdProduct);
            c.Parameters.AddWithValue("@IdOrder", result.IdOrder);
            c.Parameters.AddWithValue("@Amount3", productToWarehouse.Amount);
            c.Parameters.AddWithValue("@Price", reader.GetDecimal(0) * productToWarehouse.Amount);
            c.Parameters.AddWithValue("@Date", DateTime.Now);
            await reader.DisposeAsync();
            await transaction.CommitAsync();
            var id = (int)(await c.ExecuteScalarAsync())!;
            return id;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}