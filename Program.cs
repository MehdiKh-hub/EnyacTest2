using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

// Swashbuckle.AspNetCore v10+ exposes its OpenAPI model types under the
// "Microsoft.OpenApi" namespace (NOT "Microsoft.OpenApi.Models", which was
// used by v9 and earlier, and which is what "Microsoft.AspNetCore.OpenApi"
// also touches). We reference ONLY this namespace and never install the
// Microsoft.AspNetCore.OpenApi package, so there is no ambiguity/conflict.
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// 1) JWT configuration
//    Values come from appsettings.json by default. For real deployments,
//    override "Jwt:SigningKey" via `dotnet user-secrets` (development) or an
//    environment variable such as Jwt__SigningKey (production). Never commit
//    a real signing key to source control.
// =============================================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"] ?? "MinimalApiJwtDemo";
var jwtAudience = jwtSection["Audience"] ?? "MinimalApiJwtDemoClients";
var jwtSigningKey = jwtSection["SigningKey"]
    ?? throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. Set it in appsettings.json, " +
        "via `dotnet user-secrets set \"Jwt:SigningKey\" \"...\"`, or the " +
        "Jwt__SigningKey environment variable.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));

// =============================================================================
// 2) Authentication / Authorization
// =============================================================================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// =============================================================================
// 3) IHttpClientFactory - a named client used by the "send data" endpoint to
//    call an external/third-party API (e.g. a partner web service).
//    JSONPlaceholder is a free public sandbox API, used here only so the demo
//    endpoint works out of the box without any real credentials.
// =============================================================================
builder.Services.AddHttpClient("ExternalApiClient", client =>
{
    client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// =============================================================================
// 4) Swagger / OpenAPI - Swashbuckle ONLY.
//    AddEndpointsApiExplorer() ships with the ASP.NET Core shared framework
//    (it is what lets Minimal API endpoints be discovered by SwaggerGen) and
//    is unrelated to, and does not require, the Microsoft.AspNetCore.OpenApi
//    package.
// =============================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API JWT Demo",
        Version = "v1",
        Description = "Minimal API sample: JWT auth, Swashbuckle Swagger UI, record DTOs, IHttpClientFactory."
    });

    // --- Bearer scheme definition: adds the "Authorize" button in Swagger UI ---
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste ONLY the raw JWT returned by POST /api/auth/login " +
                      "(Swagger UI adds the \"Bearer \" prefix automatically)."
    });

    // --- Apply the scheme to every operation.
    //     Swashbuckle v10+ requires a Func<OpenApiDocument, OpenApiSecurityRequirement>
    //     here instead of a plain OpenApiSecurityRequirement instance. ---
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

// =============================================================================
// Middleware pipeline
// =============================================================================
// NOTE: Swagger is exposed in every environment (not just Development) so that
// `dotnet run` works out of the box even if launchSettings.json / ASPNETCORE_ENVIRONMENT
// isn't picked up. For a real production API, you may want to guard this behind
// app.Environment.IsDevelopment() again, or protect /swagger with authentication.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Minimal API JWT Demo v1");
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// =============================================================================
// In-memory "demo" data. Replace with a real user store (e.g. ASP.NET Core
// Identity + a database, with hashed passwords) and a real persistence layer
// before using any of this in production.
// =============================================================================
var demoUsers = new Dictionary<string, (string Password, string[] Roles)>(StringComparer.OrdinalIgnoreCase)
{
    ["admin"] = ("P@ssw0rd!", new[] { "Admin", "User" }),
    ["ali"] = ("12345678", new[] { "User" })
};

var receivedItems = new List<SendDataResponseItem>();

// =============================================================================
// Endpoint 1 — POST /api/auth/login
// Authenticates a demo user and issues a JWT access token.
// =============================================================================
app.MapPost("/api/auth/login", (LoginRequest request) =>
{
    if (!demoUsers.TryGetValue(request.Username, out var user) || user.Password != request.Password)
    {
        return Results.Json(
            new { message = "نام کاربری یا رمز عبور نامعتبر است." },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var expiresAtUtc = DateTime.UtcNow.AddHours(1);

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, request.Username),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new(ClaimTypes.Name, request.Username)
    };
    claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: expiresAtUtc,
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new LoginResponse(tokenString, expiresAtUtc));
})
.WithName("Login")
.WithTags("Authentication")
.WithSummary("Authenticates a demo user and issues a JWT access token.")
.Produces<LoginResponse>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized)
.AllowAnonymous();

// =============================================================================
// Endpoint 2 — GET /api/users/me   (protected)
// Returns the profile of the currently authenticated user, read from the
// claims embedded in the JWT.
// =============================================================================
app.MapGet("/api/users/me", (ClaimsPrincipal user) =>
{
    var username = user.Identity?.Name ?? "unknown";
    var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();

    return Results.Ok(new UserProfileResponse(username, roles));
})
.WithName("GetCurrentUser")
.WithTags("Users")
.WithSummary("Returns the profile of the currently authenticated user.")
.Produces<UserProfileResponse>(StatusCodes.Status200OK)
.RequireAuthorization();

// =============================================================================
// Endpoint 3 — POST /api/data/send   (protected)
// Stores an item and forwards it to an external API via IHttpClientFactory.
// This is the endpoint that demonstrates calling a third-party/operator-style
// web service, similar to how a "buy/charge" service would call an external
// provider before returning a confirmation to the caller.
// =============================================================================
app.MapPost("/api/data/send", async (SendDataRequest request, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("ExternalApiClient");

    var externalPayload = new { title = request.Title, body = request.Payload, userId = 1 };
    using var response = await client.PostAsJsonAsync("posts", externalPayload);

    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem(
            title: "خطا در ارتباط با سرویس خارجی",
            statusCode: (int)response.StatusCode);
    }

    var externalResult = await response.Content.ReadFromJsonAsync<ExternalServiceResult>();

    var item = new SendDataResponseItem(
        Id: receivedItems.Count + 1,
        Title: request.Title,
        Payload: request.Payload,
        ExternalReferenceId: externalResult?.Id,
        CreatedAtUtc: DateTime.UtcNow);

    receivedItems.Add(item);

    return Results.Created($"/api/data/{item.Id}", item);
})
.WithName("SendData")
.WithTags("Data")
.WithSummary("Stores a data item and forwards it to an external API (demonstrates IHttpClientFactory).")
.Produces<SendDataResponseItem>(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status502BadGateway)
.RequireAuthorization();

app.Run();

// =============================================================================
// DTOs — using records for concise, immutable request/response models.
// =============================================================================
record LoginRequest(string Username, string Password);

record LoginResponse(string Token, DateTime ExpiresAtUtc);

record UserProfileResponse(string Username, string[] Roles);

record SendDataRequest(string Title, string Payload);

record SendDataResponseItem(int Id, string Title, string Payload, int? ExternalReferenceId, DateTime CreatedAtUtc);

record ExternalServiceResult(int Id, int UserId, string Title, string Body);