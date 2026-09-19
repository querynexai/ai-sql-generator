using Npgsql;

namespace AiSqlGenerator.Api.Services;

public class SchemaProvider
{
    private readonly string _connectionString;

    public SchemaProvider(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not configured.");
    }

    public async Task<string> GetSchemaDescriptionAsync()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Tables and columns:");

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Get all tables
        var tables = new List<string>();
        await using (var cmd = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_type = 'BASE TABLE' " +
            "ORDER BY table_name", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));
        }

        // For each table, get its columns
        foreach (var table in tables)
        {
            sb.AppendLine($"\n{table} (");

            await using var cmd = new NpgsqlCommand(
                "SELECT column_name, data_type, is_nullable " +
                "FROM information_schema.columns " +
                "WHERE table_schema = 'public' AND table_name = @t " +
                "ORDER BY ordinal_position", conn);

            cmd.Parameters.AddWithValue("t", table);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var colName = reader.GetString(0);
                var dataType = reader.GetString(1);
                var nullable = reader.GetString(2) == "YES" ? " NULL" : " NOT NULL";
                sb.AppendLine($"  {colName} {dataType}{nullable},");
            }

            sb.AppendLine(")");
        }

        return sb.ToString();
    }
}