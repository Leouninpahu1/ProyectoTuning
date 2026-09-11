using Microsoft.EntityFrameworkCore;
using Serilog;
using Turning.API.Extensions;
using Turning.API.Middleware;
using Turning.Application.DependencyInjection;
using Turning.Infrastructure.DependencyInjection;
using Turning.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Agregar servicios
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

builder.Services.AddCorsConfiguration();

var startupLogger = new Serilog.Extensions.Logging.SerilogLoggerFactory(Log.Logger).CreateLogger("Turning.API.Jwt");
builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment, startupLogger);

builder.Services.AddAuthorization();

// Agregar controllers y Swagger
// El filtro de propietario va global: las rutas anidadas bajo
// /api/sessions/{sessionId}/... nacieron sin validarlo, y hacerlo opt-in
// repetiria el problema con el proximo controller que se agregue.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<Turning.API.Filters.SessionOwnershipFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TurningDbContext>();
    dbContext.Database.Migrate();
    await Turning.Infrastructure.Persistence.TurningDbSeeder.SeedAsync(dbContext);
}

// Configurar middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Turning API v1");
    });
}

app.UseExceptionHandling();
app.UseHttpsRedirection();
app.UseCors("AllowSpecific");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
