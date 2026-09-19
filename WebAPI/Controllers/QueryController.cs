using AiSqlGenerator.Api.Models;
using AiSqlGenerator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiSqlGenerator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly QueryGeneratorService _generator;
    private readonly SqlExecutionService _executor;
    private readonly ILogger<QueryController> _logger;

    public QueryController(
        QueryGeneratorService generator,
        SqlExecutionService executor,
        ILogger<QueryController> logger)
    {
        _generator = generator;
        _executor = executor;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<GenerateSqlResponse>> Generate([FromBody] GenerateSqlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest(new GenerateSqlResponse("", "Query cannot be empty."));

        try
        {
            var sql = await _generator.GenerateSqlAsync(request.Query);
            _logger.LogInformation("Generated SQL: {Sql}", sql);
            return Ok(new GenerateSqlResponse(sql));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SQL");
            return StatusCode(500, new GenerateSqlResponse("", $"AI generation failed: {ex.Message}"));
        }
    }

    [HttpPost("execute")]
    public async Task<ActionResult<ExecuteSqlResponse>> Execute([FromBody] ExecuteSqlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sql))
            return BadRequest(new ExecuteSqlResponse(false, new(), new(), 0, "SQL cannot be empty."));

        var result = await _executor.ExecuteAsync(request.Sql);
        return Ok(result);
    }
}