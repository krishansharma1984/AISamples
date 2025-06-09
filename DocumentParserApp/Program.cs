using Azure;
using Azure.AI.OpenAI;
using DocumentParserApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);
string[] allowedOrigins = new[]
{
    "http://localhost:62579",
    "https://another-client-app.com"
};


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IParserService, ParserService>();

builder.Services.AddSingleton<OpenAIClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = new Uri(config["AzureOpenAI:Endpoint"]);
    var key = new AzureKeyCredential(config["AzureOpenAI:Key"]);
    return new OpenAIClient(endpoint, key);
});
builder.Services.AddScoped<IIntentClassifier, IntentClassifier>();
builder.Services.AddSingleton<IRouteMappingService, RouteMappingService>();

builder.Services.AddSingleton<StepRouteMapService>();

var app = builder.Build();

app.UseCors("AllowAll"); 
app.UseAuthorization();
app.MapControllers();

app.Run();