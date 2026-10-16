using AstraKa.Api.Auth;
using AstraKa.Api.ExceptionHandling;
using AstraKa.Application;
using AstraKa.Application.Common;
using AstraKa.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers stay thin: they only forward requests to MediatR handlers.
builder.Services.AddControllers();

// OpenAPI document; Scalar is the UI on top of it below.
builder.Services.AddOpenApi();

builder.Services.AddApplication(builder.Configuration["MediatR:LicenseKey"]);
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default")!);

// Temporary: no real authentication yet - replaced by lab 3's OIDC validation.
builder.Services.AddScoped<ICurrentUser, DevelopmentCurrentUser>();

// The one place exceptions become ProblemDetails responses, per CLAUDE.md.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();

app.MapControllers();

app.Run();
