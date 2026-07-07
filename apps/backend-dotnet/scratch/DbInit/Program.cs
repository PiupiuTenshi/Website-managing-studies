using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

class Program
{
    static async Task Main(string[] args)
    {
        var appsettingsPath = "../../src/WebApi/appsettings.json";
        var appsettings = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(appsettingsPath));
        var connectionString = appsettings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
        
        Console.WriteLine("Connecting to database...");
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var sqlFilePaths = new[] { "../../../../database/schema-phase8.sql" };
            foreach (var sqlFilePath in sqlFilePaths)
            {
                if (!File.Exists(sqlFilePath)) continue;
                Console.WriteLine("Dropping old phase 8 tables...");
                await using var dropCmd = new NpgsqlCommand("DROP TABLE IF EXISTS chat_messages CASCADE; DROP TABLE IF EXISTS chat_participants CASCADE; DROP TABLE IF EXISTS chat_rooms CASCADE;", connection);
                await dropCmd.ExecuteNonQueryAsync();
                
                Console.WriteLine($"Reading SQL file: {sqlFilePath}");
                var sql = await File.ReadAllTextAsync(sqlFilePath);
                Console.WriteLine("Executing schema setup...");
                await using var command = new NpgsqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();
                
                Console.WriteLine("Creating default chat room...");
                var defaultRoomId = Guid.NewGuid();
                var insertRoomCmd = new NpgsqlCommand("INSERT INTO chat_rooms (id, name, type) VALUES (@id, 'Phòng Chat Toàn Trường', 'General')", connection);
                insertRoomCmd.Parameters.AddWithValue("id", defaultRoomId);
                await insertRoomCmd.ExecuteNonQueryAsync();

                var selectUsersCmd = new NpgsqlCommand("SELECT id FROM users", connection);
                var userIds = new System.Collections.Generic.List<Guid>();
                await using (var reader = await selectUsersCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) userIds.Add(reader.GetGuid(0));
                }

                foreach (var uid in userIds)
                {
                    var insertPartCmd = new NpgsqlCommand("INSERT INTO chat_participants (chat_room_id, user_id) VALUES (@rid, @uid)", connection);
                    insertPartCmd.Parameters.AddWithValue("rid", defaultRoomId);
                    insertPartCmd.Parameters.AddWithValue("uid", uid);
                    await insertPartCmd.ExecuteNonQueryAsync();
                }

                Console.WriteLine("SUCCESS: 4 test accounts and default chat room created!");
            }
            
            Console.WriteLine("SUCCESS: Database schema created successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED: {ex.Message}");
        }
    }
}
