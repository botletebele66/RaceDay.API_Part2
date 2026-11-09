using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;

namespace RaceDay.MVC;

public partial class Program
{
    public static void Main(string[] args) => CreateApp(args).Run();

    /// <summary>
    /// Builds the web application without starting it, so that tests can host it.
    /// <paramref name="configureServices"/> runs after the standard registrations.
    /// </summary>
    public static WebApplication CreateApp(string[] args, Action<IServiceCollection>? configureServices = null)
    {
        // Forms post numbers with a dot and dates in ISO format, so model binding always parses invariantly.
        // Display formatting uses South African English explicitly (see UiFormat).
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews(options =>
        {
            // Every POST must carry the anti-forgery token that the form tag helpers add.
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = MvcSessionKeys.CookieName;
            options.IdleTimeout = TimeSpan.FromMinutes(30); // Matches the API's idle timeout.
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });
        builder.Services.AddScoped<CurrentUser>();

        var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
        builder.Services.AddHttpClient<ApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        configureServices?.Invoke(builder.Services);

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        // Containers serve plain HTTP behind the host's port mapping, so this can be switched off (App:HttpsRedirect).
        if (app.Configuration.GetValue("App:HttpsRedirect", true))
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }
}
