using System.Text.Json;
using System.Threading.Tasks;
using HelloworldApplication.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policyBuilder =>
        {
            policyBuilder.WithOrigins("http://localhost:4040")
                .WithHeaders("Authorization");
        });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{configuration["Auth0:Domain"]}/";
        options.Audience = configuration["Auth0:Audience"];

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.Response.OnStarting(async () =>
                {
                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(new ApiResponse("You are not authorized!"))
                    );
                });

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                c.Type == "permissions" &&
                c.Value == "read:admin-messages" &&
                c.Issuer == $"https://{configuration["Auth0:Domain"]}/")));
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

const string publicMessage = "The API doesn't require an access token to share this message.";
const string protectedMessage = "The API successfully validated your access token.";
const string adminMessage = "The API successfully recognized you as an admin.";

var group = app.MapGroup("api/messages");
group.MapGet("public", () => new ApiResponse(publicMessage));
group.MapGet("protected", () => new ApiResponse(protectedMessage)).RequireAuthorization();
group.MapGet("admin", () => new ApiResponse(adminMessage)).RequireAuthorization("Admin");

app.Run();