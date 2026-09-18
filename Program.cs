using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CampusPulse.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// =========================
// Database (SQLite)
// =========================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=campuspulse.db"));   // FORCE single DB file

// =========================
// JWT Authentication
// =========================
// NOTE: the fallback literal key below only exists so the API still boots
// in a fresh dev checkout. For anything beyond local dev, set Jwt:Key in
// appsettings/user-secrets/environment and remove this fallback.
var jwtKey = builder.Configuration["Jwt:Key"] ?? "by6oJCi9HSd5CvNuZET4tjEg0VTHSHTfdOy4ELPDeng=";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CampusPulse";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    // .NET's JwtSecurityTokenHandler automatically renames certain
    // well-known inbound claim types by default - notably "role" gets
    // silently rewritten to the long ClaimTypes.Role URI before
    // RoleClaimType below is ever consulted. That meant RoleClaimType =
    // "role" was looking for a claim type that no longer existed under that
    // literal name, so [Authorize(Roles = "Admin")] failed for every
    // request regardless of the caller's actual role - the token validated
    // fine (401 never fired), but the role check silently always failed
    // (403). Turning this off keeps claim types exactly as issued.
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

        // Our token issues a plain "role" claim (see AuthController).
        // Mapping it here is what makes [Authorize(Roles = "Admin")] work -
        // without this, ASP.NET looks for ClaimTypes.Role and never finds it,
        // so every [Authorize(Roles=...)] check silently fails open/closed
        // incorrectly. Likewise NameClaimType so User.Identity.Name / the
        // GetUserId() helper line up with the "sub" claim.
        RoleClaimType = "role",
        NameClaimType = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub
    };
});

builder.Services.AddAuthorization();

// =========================
// Controllers / Swagger
// =========================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Post.User<->User.Posts, Post.Category<->Category.Posts,
        // Post.Comments<->Comment.Post, Event.Registrations<->
        // EventRegistration.Event, etc. - every one of these navigation
        // properties added for moderation/ownership loops back on itself.
        // Without this, System.Text.Json throws once it hits 32 levels of
        // nesting trying to walk the cycle, and the endpoint returns a 500
        // instead of data. IgnoreCycles just omits the repeated reference
        // (sets it to null) instead of erroring - the caller still gets
        // everything else on the object, just not an infinitely-nested copy
        // of itself.
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Migrate + seed BEFORE the host starts accepting requests. This used to run
// inside app.Lifetime.ApplicationStarted.Register(async () => {...}) - but
// Register expects a plain Action, so that async lambda was actually
// "async void": any exception inside it would become an unobserved
// exception instead of a normal, visible startup failure, and it also ran
// AFTER Kestrel was already listening (you can see this in the log -
// "Now listening" prints before the seeding queries), so a client request
// arriving in that window could hit an unseeded database. Doing it here,
// synchronously, before app.Run(), means it's guaranteed done first and any
// failure surfaces as a normal startup crash with a real stack trace.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.SeedAsync(db).GetAwaiter().GetResult();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
