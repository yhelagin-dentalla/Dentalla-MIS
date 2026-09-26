using Dentalla.Api;
using Dentalla.Api.Endpoints;
using Dentalla.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options => options.ServiceName = "Dentalla MIS Server");
builder.Services.AddDentallaInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services
    .AddAuthentication(DentallaSessionDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, DentallaSessionAuthenticationHandler>(DentallaSessionDefaults.Scheme, _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseAuthentication();
app.UseAuthorization();

app.MapServerEndpoints();
app.MapAuthEndpoints();
app.MapLoginDirectoryEndpoints();
app.MapRoleContextEndpoints();
app.MapWorkspaceEndpoints();
app.MapHub<UpdatesHub>("/hubs/updates");

app.Run();

public sealed class UpdatesHub : Microsoft.AspNetCore.SignalR.Hub;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;
        logger.LogError(exception, "Unhandled server exception. TraceId={TraceId}", traceId);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal server error",
            detail: $"TraceId: {traceId}").ExecuteAsync(httpContext);
        return true;
    }
}
