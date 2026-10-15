using Microsoft.EntityFrameworkCore;
using RaceDay.API_Part2.Data;
using RaceDay.API_Part2.Helper;

namespace RaceDay.API_Part2
{
    public class Program
    {
        public static void Main(string[] args) => CreateApp(args).Run();

        /// <summary>
        /// Builds the application without starting it. <paramref name="configureServices"/> runs after the API's own
        /// registrations, so a host (for example an integration-test harness) can replace the database provider.
        /// </summary>
        public static WebApplication CreateApp(string[] args, Action<IServiceCollection>? configureServices = null)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = "RaceDay API",
                    Version = "v1",
                    Description = "REST API for the RaceDay event management system. Log in with POST /api/auth/login " +
                                  "first; the session cookie is then sent automatically by Swagger UI for later calls.",
                });

                // Endpoint and model descriptions come from the XML file produced by GenerateDocumentationFile.
                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in configuration.");

            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.Cookie.Name = SessionKeys.CookieName;
                options.IdleTimeout = SessionKeys.IdleTimeout;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

            configureServices?.Invoke(builder.Services);

            var app = builder.Build();

            // Integration tests run with the "Testing" environment and create their own schema.
            if (!app.Environment.IsEnvironment("Testing"))
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.Initialize(db, app.Configuration.GetValue<bool>("Seed:Demo"));
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            if (!app.Environment.IsEnvironment("Testing"))
            {
                app.UseHttpsRedirection();
            }

            app.UseSession();   // Must come before MapControllers so controllers can read the session.
            app.MapControllers();

            return app;
        }
    }
}
