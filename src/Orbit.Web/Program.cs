using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Orbit.Web.Authorization;
using Orbit.Web.Data;
using Orbit.Web.Features;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-Orbit";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthorizationHandler, OrganizationReadHandler>();
builder.Services.AddScoped<IProjectService, ProjectService>();
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
var connectionString = builder.Configuration.GetConnectionString("Orbit")
    ?? $"Data Source={Path.Combine(dataDirectory, "orbit.db")}";
builder.Services.AddDbContext<OrbitDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddHealthChecks().AddDbReadiness();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorPages();
app.MapProjectEndpoints();
app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready");
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    Directory.CreateDirectory(dataDirectory);
    await using var scope = app.Services.CreateAsyncScope();
    await DemoData.InitializeAsync(scope.ServiceProvider.GetRequiredService<OrbitDbContext>());
}

await app.RunAsync();

public partial class Program;
