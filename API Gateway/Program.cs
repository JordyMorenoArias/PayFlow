using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var jwtKey = builder.Configuration["JWT:KEY"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT:KEY is not configured");
}

var jwtIssuer = builder.Configuration["JWT:ISSUER"];

if (string.IsNullOrEmpty(jwtIssuer))
{
    throw new Exception("JWT:ISSUER is not configured");
}

var jwtAudience = builder.Configuration["JWT:AUDIENCE"];

if (string.IsNullOrEmpty(jwtAudience))
{
    throw new Exception("JWT:AUDIENCE is not configured");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero, // Elimina el tiempo de gracia para la expiración del token

        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Read the token from the cookie instead of the Authorization header
            if (context.Request.Cookies.ContainsKey("access_token"))
            {
                context.Token = context.Request.Cookies["access_token"];
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();

            logger.LogError($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Token validated successfully.");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(transformContext =>
        {
            var user = transformContext.HttpContext.User;

            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == "sub");
                if (userIdClaim != null)
                {
                    transformContext.ProxyRequest.Headers.Add("X-User-Id", userIdClaim.Value);
                }
            }

            return ValueTask.CompletedTask;
        });
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.Run();
