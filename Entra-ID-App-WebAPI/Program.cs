using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration;
// Add services to the container.
string[] scopes =
                config["Swagger:Scopes"]?.Split(' ') ?? new string[0];

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UptecTodoApi",
        Version = "v1",
    });
    c.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
    {
        Name = "OAuth2",
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(config["Swagger:AuthorizationEndpoint"] ?? string.Empty),
                TokenUrl = new Uri(config["Swagger:TokenEndpoint"] ?? string.Empty),
                Scopes = scopes?.ToDictionary(c => c)
            }
        }
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme, Id = "OAuth2"
                            }
                        },
                        scopes
                    }
                });
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(config =>
{
    config.AddPolicy("AuthZPolicy", policyBuilder =>
        policyBuilder.Requirements.Add(new ScopeAuthorizationRequirement() { RequiredScopesConfigurationKey = $"AzureAd:Scopes" }));
});

var app = builder.Build();



// </ms_docref_add_msal>

// <ms_docref_enable_authz_capabilities>


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo Api v1");
        c.OAuthClientId(config["Swagger:ClientId"]);
        c.OAuthUsePkce();
        c.OAuthScopes(scopes);
    });
}
app.UseAuthentication();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
