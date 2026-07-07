using System;
using System.IO;
using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = "Host=db.mxvlfswcxofnhrfmfedl.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=Iloveyou1207@1207;Include Error Detail=true;";
        
        // Go up to the root folder to find database/schema-v1.sql
        // Current directory will be apps/backend-dotnet/scratch/DbInit
        var sqlFilePath = "../../../../database/schema-v1.sql";
        
        if (!File.Exists(sqlFilePath))
        {
            Console.WriteLine($"Cannot find SQL file at {sqlFilePath}");
            return;
        }

        Console.WriteLine("Reading SQL file...");
        var sql = await File.ReadAllTextAsync(sqlFilePath);

        Console.WriteLine("Connecting to database...");
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            Console.WriteLine("Executing schema setup...");
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();

            Console.WriteLine("SUCCESS: Database schema created successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED: {ex.Message}");
        }
    }
}
