



using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Com.Api;

/// <summary>
/// Operation filter to add the requirement of the custom header
/// </summary>
public class MyHeaderFilter : IOperationFilter
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="context"></param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "api_key",
            In = ParameterLocation.Header,
            Required = false
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "api_sign",
            In = ParameterLocation.Header,
            Required = false
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "api_timestamp",
            In = ParameterLocation.Header,
            Required = false
        });
    }
}