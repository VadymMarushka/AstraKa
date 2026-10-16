using AstraKa.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Controllers stay thin: they only forward requests to MediatR handlers.
builder.Services.AddControllers();

// OpenAPI document; Scalar is added as the UI on top of it later.
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Default")!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
