using Api.Extensions;
using Application;
using Infrastructure;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddControllers();
builder.Services.AddApiFoundation();
builder.Services.AddJwtAuthentication();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Request logging fica fora do exception handler: registra o status final (ex.: 500) sem duplicar a excecao.
app.UseSerilogRequestLogging(options =>
    options.GetLevel = static (httpContext, _, exception) =>
        exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError
            ? LogEventLevel.Error
            : httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information);

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

// Autenticacao antes da autorizacao: o ClaimsPrincipal precisa existir quando as politicas forem avaliadas.
// Seguro por padrao: todo endpoint de controller exige usuario autenticado (RequireAuthorization em MapControllers);
// os publicos declaram [AllowAnonymous]. Nao se usa FallbackPolicy porque ela tambem barra requisicoes sem
// endpoint correspondente (rota inexistente retornaria 401 em vez de 404).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();
app.MapHealthEndpoints();

await app.ApplyMigrationsIfConfiguredAsync();
await app.RunAsync();
