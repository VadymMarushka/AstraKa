using System.Text.Json.Serialization;
using AstraKa.Api.Auth;
using AstraKa.Api.ExceptionHandling;
using AstraKa.Application;
using AstraKa.Application.Common;
using AstraKa.Domain.Enums;
using AstraKa.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers stay thin: they only forward requests to MediatR handlers. Enums go over the wire
// as their names ("Seconds"), not their numeric values.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// OpenAPI document; Scalar is the UI on top of it below. The Bearer security scheme lets a
// pasted Auth0 access token be sent on every "Try it" request from Scalar - see the matching
// MapScalarApiReference call below for the UI side of this.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = $"Paste an Auth0 access token (Audience: {builder.Configuration["Authentication:Audience"]}).",
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
        });

        return Task.CompletedTask;
    });
});

builder.Services.AddApplication(builder.Configuration["MediatR:LicenseKey"]);
builder.Services.AddInfrastructure(builder.Configuration);

// Auth0 issues and signs the tokens; the API only validates them. Authority/Audience are not
// secrets - see docs/domain-model.md's "Authentication & authorization" section.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.MapInboundClaims = false; // keep raw JWT claim names (`sub`, not a legacy URI)
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Wanderer, policy => policy.RequireClaim(AstraKaClaimTypes.Role, nameof(UserRole.Wanderer)))
    .AddPolicy(Policies.Starshepherd, policy => policy.RequireClaim(AstraKaClaimTypes.Role, nameof(UserRole.Starshepherd)))
    .AddPolicy(Policies.Oracle, policy => policy.RequireClaim(AstraKaClaimTypes.Role, nameof(UserRole.Oracle)))
    .AddPolicy(Policies.AuthorOrOracle, policy => policy.AddRequirements(new AuthorOrOracleRequirement()));

builder.Services.AddSingleton<IAuthorizationHandler, AuthorOrOracleAuthorizationHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClaimsTransformation, DatabaseClaimsTransformation>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// The one place exceptions become ProblemDetails responses, per CLAUDE.md.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Bearer field in Scalar's UI - paste a real Auth0 access token here and it's sent as
    // Authorization: Bearer <token> on every "Try it" request.
    app.MapScalarApiReference(options => options
        .AddPreferredSecuritySchemes(["Bearer"])
        .AddHttpAuthentication("Bearer", auth => auth.Token = string.Empty));
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
