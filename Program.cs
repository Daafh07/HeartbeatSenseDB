using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.IdentityModel.Tokens;

using System.Text;
 
var builder = WebApplication.CreateBuilder(args);
 
// Load env (.env for local dev; on Render the usual env vars still work)

DotNetEnv.Env.Load();
 
// --- JWT setup ---

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

if (string.IsNullOrWhiteSpace(jwtSecret))

{

    throw new InvalidOperationException("JWT_SECRET environment variable is not set.");

}
 
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

builder.Services.AddControllers();
 
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

 