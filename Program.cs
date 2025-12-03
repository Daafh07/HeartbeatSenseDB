using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Supabase;
using System.Text;
 
// Load env (.env for local dev; on Render the usual env vars still work)

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

string RequireEnv(string key)
{
    var value = Environment.GetEnvironmentVariable(key);
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"{key} environment variable is not set.");
    return value;
}
 
// --- JWT & Supabase setup ---

var jwtSecret = RequireEnv("JWT_SECRET");
var supabaseUrl = RequireEnv("SUPABASE_URL");
var supabaseKey = RequireEnv("SUPABASE_KEY");
 
var key = Encoding.ASCII.GetBytes(jwtSecret);
 
// --- CORS origins ---

var rawOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");

string[] allowedOrigins = Array.Empty<string>();
 
if (!string.IsNullOrWhiteSpace(rawOrigins))

{

    // Support both ";" and "," as separators

    allowedOrigins = rawOrigins

        .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

}
 
// Debug logging (useful on Render)

Console.WriteLine("CORS allowed origins:");

if (allowedOrigins.Length == 0)

{

    Console.WriteLine("- <any origin> (AllowAnyOrigin)");

}

else

{

    foreach (var origin in allowedOrigins)

    {

        Console.WriteLine($"- {origin}");

    }

}
 
// --- Services ---

builder.Services
    .AddSingleton<Client>(_ =>
    {
        var supabaseOptions = new SupabaseOptions
        {
            AutoRefreshToken = false,
            AutoConnectRealtime = false
        };

        return new Client(supabaseUrl, supabaseKey, supabaseOptions);
    })
    // Register controllers that are used as services (UsersController depends on AuthController)
    .AddScoped<HeartbeatBackend.Controllers.AuthController>()
    .AddControllers();
 
builder.Services.AddAuthentication(options =>

{

    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

})

.AddJwtBearer(options =>

{

    options.TokenValidationParameters = new TokenValidationParameters

    {

        ValidateIssuer = false,

        ValidateAudience = false,

        ValidateIssuerSigningKey = true,

        IssuerSigningKey = new SymmetricSecurityKey(key)

    };

});
 
builder.Services.AddCors(options =>

{

    options.AddPolicy("Frontend", policy =>

    {

        if (allowedOrigins.Length == 0)

        {

            // Dev / fallback: allow any origin (no credentials!)

            policy

                .AllowAnyOrigin()

                .AllowAnyHeader()

                .AllowAnyMethod();

        }

        else

        {

            policy

                .WithOrigins(allowedOrigins) // exact match with browser Origin

                .AllowAnyHeader()

                .AllowAnyMethod();

        }

    });

});
 
// --- Pipeline ---

var app = builder.Build();
 
app.UseRouting();
 
app.UseCors("Frontend");
 
app.UseAuthentication();

app.UseAuthorization();
 
app.MapControllers();
 
app.Run();

 
