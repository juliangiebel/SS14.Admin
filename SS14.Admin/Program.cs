using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Content.Server.Database;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using SS14.Admin;
using SS14.Admin.Components;
using SS14.Admin.Data;
using SS14.Admin.Helpers;
using SS14.Admin.Services;
using SS14.Admin.SignIn;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
builder.Configuration.AddYamlFile("appsettings.yml", false, true);
builder.Configuration.AddYamlFile($"appsettings.{env.EnvironmentName}.yml", true, true);
builder.Configuration.AddYamlFile("appsettings.Secret.yml", true, true);

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration);
});

builder.Host.UseSystemd();

builder.Services.AddScoped<SignInManager>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<BanHelper>();
builder.Services.AddScoped<PlayerLocator>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddQuickGridEntityFrameworkAdapter();
builder.Services.AddScoped<ClientPreferencesService>();

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (connStr == null)
    throw new InvalidOperationException("Need to specify DefaultConnection connection string");

builder.Services.AddDbContext<PostgresServerDbContext>(options => options.UseNpgsql(connStr));

// dummy implementation
// Configure this for actually use
builder.Services.AddDbContext<PreferencesDbContext>(options =>
    options.UseSqlite("Data Source=preferences.db"));

// dummy implementation
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();

builder.Services.AddControllers();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies", options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.SignInScheme = "Cookies";

        options.Authority = builder.Configuration["Auth:Authority"];
        options.ClientId = builder.Configuration["Auth:ClientId"];
        options.ClientSecret = builder.Configuration["Auth:ClientSecret"];
        options.SaveTokens = true;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.GetClaimsFromUserInfoEndpoint = true;
        options.TokenValidationParameters.NameClaimType = "name";

        options.Events.OnTokenValidated = async ctx =>
        {
            var handler = ctx.HttpContext.RequestServices.GetRequiredService<LoginHandler>();
            await handler.HandleTokenValidated(ctx);
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};

foreach (var ip in app.Configuration.GetSection("ForwardProxies").Get<string[]>() ?? Array.Empty<string>())
{
    forwardedHeadersOptions.KnownProxies.Add(IPAddress.Parse(ip));
}

app.UseForwardedHeaders(forwardedHeadersOptions);

var pathBase = app.Configuration.GetValue<string>("PathBase");
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseAuthentication();
app.UseHttpsRedirection();
app.MapStaticAssets();

app.UseRouting();

app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapControllers();

app.Run();
