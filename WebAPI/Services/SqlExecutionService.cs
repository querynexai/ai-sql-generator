using System.Data;
using AiSqlGenerator.Api.Models;
using Npgsql;

namespace AiSqlGenerator.Api.Services;

public class SqlExecutionService
{
    private readonly string _connectionString;

    public SqlExecutionService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not configured.");
    }

    public async Task<ExecuteSqlResponse> ExecuteAsync(string sql)
    {
        // Security: block any non-SELECT statements
        var normalized = sql.TrimStart().ToUpperInvariant();
        var forbidden = new[] { "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE", "GRANT", "REVOKE", "EXEC", "EXECUTE" };

        if (!normalized.StartsWith("SELECT") && !normalized.StartsWith("WITH"))
        {
            return new ExecuteSqlResponse(false, new(), new(), 0,
                "Only SELECT (and WITH ... SELECT) queries are allowed.");
        }

        foreach (var word in forbidden)
        {
            if (normalized.Contains(word + " ") || normalized.Contains(word + "("))
            {
                return new ExecuteSqlResponse(false, new(), new(), 0,
                    $"Query contains forbidden keyword: {word}. Only read-only queries are allowed.");
            }
        }

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 30;

            await using var reader = await cmd.ExecuteReaderAsync();

            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
                columns.Add(reader.GetName(i));

            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }

            return new ExecuteSqlResponse(true, rows, columns, rows.Count);
        }
        catch (Exception ex)
        {
            return new ExecuteSqlResponse(false, new(), new(), 0, ex.Message);
        }
    }
}