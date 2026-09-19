namespace AiSqlGenerator.Api.Models;

public record GenerateSqlRequest(string Query);

public record GenerateSqlResponse(string Sql, string? Error = null);

public record ExecuteSqlRequest(string Sql);

public record ExecuteSqlResponse(
    bool Success,
    List<Dictionary<string, object?>> Data,
    List<string> Columns,
    int RowCount,
    string? Error = null);