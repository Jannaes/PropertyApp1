using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Localization;  //..tähän
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using PropertyApp.Config;
using PropertyApp.Data;
using System.Composition;
using System.Configuration;
using System.Globalization;  //Lisätty tästä..
using System.Runtime.Intrinsics.Arm;
using System.Threading.Channels;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

// configure console logging (optional — Console provider is present by default in typical templates)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

// If there is Azure Database settings in appsettings.Development.json, grab them here
// and use them to connect to the database.
var keyVaultUrl = builder.Configuration["KeyVault:VaultUrl"];

var keyVaultSecretName = builder.Configuration["KeyVault:VaultSecretName"];

string logthis;

// Database connection string from Azure Key Vault (if configured) will work
// both in Development and Production environments. 
// If in production and deployed to Azure App Service there is no need to
// change code here as long as the Key Vault settings are in appsettings.Production.json
// or in App Service Application Settings as environment variables.
//
// App Service Key Vault reference (no code change, easiest)
// •	Enable a System-assigned Managed Identity for the Web App: Azure Portal -> App Service -> Identity -> turn on System-assigned.
// •	Give that identity permission to read secrets in Key Vault: Key Vault -> Access policies (or RBAC) -> Add -> Secret GET for the Web App principal.
// •	Add an App Setting that references the Key Vault secret. In Azure Portal -> App Service -> Configuration -> Application settings add a new setting:
// •	Name: ConnectionStrings: Dbproperty
// •	Value: @Microsoft.KeyVault(SecretUri = https://<your-vault-name>.vault.azure.net/secrets/<secret-name>/<optional-version>)
// •	Save and restart the app.
// •	.NET configuration will expose this under Configuration["ConnectionStrings:Dbproperty"] and the built -in ConnectionStrings mapping works.No code changes required.

if (!string.IsNullOrEmpty(keyVaultUrl) && !string.IsNullOrEmpty(keyVaultSecretName))
{
    var secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
    var secretResponse = await secretClient.GetSecretAsync(keyVaultSecretName);
    var connectionString = secretResponse.Value.Value;

    // set configuration value (so calls later that read configuration see it.
    // Later call will be done in PropertyApp\Data\PropertyContext.cs with name="Dbproperty")
    builder.Configuration["ConnectionStrings:Dbproperty"] = connectionString;

    // use the connection string directly to register DbContext (or read it later)
    builder.Services.AddDbContext<PropertyContext>(options =>
        options.UseSqlServer(connectionString));
   
    logthis = "Azure connection string: " + connectionString;
}
else
{
    logthis = "Using local appsettings.json to connect to the database.";
    // Use the connection string from appsettings.json to connect to local database
    builder.Services.AddDbContext<PropertyContext>(options =>
         options.UseSqlServer(builder.Configuration.GetConnectionString("Dbproperty")));
}

// bind DevelopmentVariables section so it can be injected via IOptions<T>
builder.Services.Configure<DevelopmentVariables>(builder.Configuration.GetSection("DevelopmentVariables"));

var app = builder.Build();

// Simple request logging middleware to help debug why Login POST may not reach the handler
//app.Use(async (context, next) =>
//{
//    app.Logger.LogInformation("Incoming request: {Method} {Path}", context.Request.Method, context.Request.Path);
//    await next();
//    app.Logger.LogInformation("Outgoing response: {StatusCode} for {Method} {Path}", context.Response.StatusCode, context.Request.Method, context.Request.Path);
//});

//var defaultCulture = "fi-FI";                       //Lisätty tästä...
//var ci = new CultureInfo(defaultCulture);

//ci.NumberFormat.NumberDecimalSeparator = ",";
//ci.NumberFormat.CurrencyDecimalSeparator = ",";

//var supportedCultures = new[] { ci };

//app.UseRequestLocalization(new RequestLocalizationOptions
//{
//    DefaultRequestCulture = new RequestCulture(defaultCulture),
//    SupportedCultures = supportedCultures,
//    SupportedUICultures = supportedCultures
//});                                                  //..tähän

// log the values using the app logger (avoid logging secrets in production)
app.Logger.LogInformation("KeyVault: VaultUrl = {KeyVaultUrl}, SecretName = {SecretName}",
    keyVaultUrl ?? "(null)", keyVaultSecretName ?? "(null)");
app.Logger.LogInformation("{logthis}", logthis);

// rest of pipeline...
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession(); // Load session first
app.UseAuthentication(); // Then authenticate user
app.UseAuthorization(); // Then check user authorization


app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();