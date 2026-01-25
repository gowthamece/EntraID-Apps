using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen; // Add this using directive
using Swashbuckle.AspNetCore.SwaggerUI; // Add this using directive
using Swashbuckle.AspNetCore.Swagger; // Add this using directive


string appConfigurationFile = string.Empty;
var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Environment:{builder.Environment.EnvironmentName}");

if (builder.Environment.EnvironmentName.Equals("Development"))
{
    appConfigurationFile = $"appsettings.{builder.Environment.EnvironmentName}.json";
}
else if (builder.Environment.EnvironmentName.Equals("Production"))
{
    appConfigurationFile = $"appsettings.json";
}
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddHttpClient();
builder.Services.AddTransient<EntraID_MI_ShareFile.Services.AzureService>();
//builder.Host.UseSerilog((context, lc) => lc
//    .ReadFrom.Configuration(context.Configuration)
//    .Enrich.WithCorrelationId());

var app = builder.Build();

app.MapGet("/", () => $"API ping utc time: {DateTime.UtcNow}, Environment: {app.Environment.EnvironmentName}");

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

app.UseExceptionHandler();
app.UseHttpsRedirection();
//Add support to logging request with SERILOG
//app.UseSerilogRequestLogging();
app.UseAuthorization();
app.MapControllers();

app.Run();
