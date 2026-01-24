using Guessnica_backend.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Guessnica_backend.Integration.Test;

public class IntegrationTestGuessnicaFactory 
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("guessnica_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithPortBinding(5432, true) // automatyczny wolny port
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Najważniejsza linia – bez tego WebRootPath jest null w testach
        builder.UseWebRoot("wwwroot");

        builder.ConfigureServices(services =>
        {
            // Usuwamy stary rejestr DbContext (z konfiguracji z Program.cs)
            var descriptor = services
                .SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            // Dodajemy DbContext z połączeniem do kontenera testowego
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });

            // Opcjonalnie – możesz tu wyczyścić inne usługi / dodać mocki
            // services.RemoveAll<IEmailSender>(); itp.
        });
    }

    public async Task InitializeAsync()
    {
        // 1. Uruchamiamy kontener PostgreSQL
        await _postgresContainer.StartAsync();

        // 2. Migracje – wykonujemy je raz na starcie fabryki
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        await dbContext.Database.MigrateAsync();

        // Opcjonalnie – możesz tu wrzucić seedowanie danych testowych
        // await DbSeeder.SeedTestDataAsync(dbContext);
    }

    public new async Task DisposeAsync()
    {
        // Zatrzymujemy kontener i sprzątamy po WebApplicationFactory
        await _postgresContainer.StopAsync()
            .ContinueWith(_ => _postgresContainer.DisposeAsync());

        await base.DisposeAsync();
    }

    // Pomocnicza metoda – przydatna w testach, żeby szybko dostać connection string
    public string GetTestConnectionString() => _postgresContainer.GetConnectionString();
}