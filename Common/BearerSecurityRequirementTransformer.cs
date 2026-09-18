using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FollowUpApi.Common;

public sealed class BearerSecurityRequirementTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;

        var allowsAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();

        var requiresAuthorization = endpointMetadata.OfType<IAuthorizeData>().Any();

        if(allowsAnonymous || !requiresAuthorization)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document)] = []
        });

        return Task.CompletedTask;
    }
}
