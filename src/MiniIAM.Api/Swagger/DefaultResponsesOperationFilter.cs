using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MiniIAM.Swagger;

/// <summary>
/// Adds consistent default responses to all operations.
/// </summary>
public class DefaultResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!operation.Responses.ContainsKey("401"))
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
        if (!operation.Responses.ContainsKey("403"))
            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
        if (!operation.Responses.ContainsKey("500"))
            operation.Responses.Add("500", new OpenApiResponse { Description = "Server error" });
    }
}
