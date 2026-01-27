using Lab360.Application.Publishing;
using Lab360.Application.Schemas;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Infrastructure.Persistence;
using Valora.Api.Infrastructure.Persistence.Interceptors;
using Valora.Api.Infrastructure.Services;
using Valora.Api.Infrastructure.Middleware;
using Valora.Api.Infrastructure.Projections;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

builder.Services.AddScoped<AuditableEntityInterceptor>();
builder.Services.AddDbContext<PlatformDbContext>((sp, options) =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WriteConnection"))
           .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>())
           );

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddSingleton<Valora.Api.Infrastructure.Persistence.MongoDbContext>();
builder.Services.AddScoped<MongoProjectionRepository>();
builder.Services.AddScoped<ProjectionManager>();

builder.Services.AddHostedService<Valora.Api.Infrastructure.BackgroundJobs.OutboxProcessor>();
builder.Services.AddHostedService<Valora.Api.Infrastructure.BackgroundJobs.KafkaConsumer>();

builder.Services.AddSingleton<SchemaContext>();
builder.Services.AddSingleton<ISchemaProvider, SchemaCache>();
builder.Services.AddScoped<ISchemaSyncService, SchemaSyncService>();
builder.Services.AddScoped<ScreenPublishService>();
builder.Services.AddScoped<Valora.Api.Application.Services.SchemaValidator>();

builder.Services.Configure<HostOptions>(opts => opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("DevCors");
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseAuthorization();

app.MapControllers().RequireCors("DevCors");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    try 
    {
        // await dbContext.Database.EnsureCreatedAsync();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        // Log error or ignore if already exists/connection issues, 
        // though typically we want to fail fast if DB is unreachable.
        Console.WriteLine($"Error initializing database: {ex.Message}");
    }
}

try
{
    Console.WriteLine("Starting application...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application terminated unexpectedly: {ex}");
}
finally
{
    Console.WriteLine("Application exiting.");
}

