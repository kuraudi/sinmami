using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RentGen.Api.Middleware;
using RentGen.Api.Services;
using RentGen.Application.Common.Interfaces;
using RentGen.Infrastructure.DependencyInjection;
using RentGen.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        var origins = new List<string> { "http://127.0.0.1:3007", "http://localhost:3007", "http://127.0.0.1:3000", "http://localhost:3000" };
        var extraOrigin = builder.Configuration["Cors:AllowedOrigin"];
        if (!string.IsNullOrWhiteSpace(extraOrigin))
            origins.Add(extraOrigin);

        policy
            .WithOrigins(origins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }
        else
        {
            dbContext.Database.EnsureCreated();
        }
    }
    catch (Exception exception)
    {
        Log.Warning(exception, "Database migration was skipped. Check PostgreSQL connectivity.");
    }
}

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendDev");
app.UseSerilogRequestLogging();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
