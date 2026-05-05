using DarkStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DarkStore.API;

/// <summary>
/// Applies pending EF Core migrations at startup with retry logic.
///
/// Fault-tolerance role:
///   • Retries DB connection up to <see cref="_maxRetries"/> times with exponential back-off.
///   • This handles the race condition where the app container starts before the DB is ready
///     (common in Docker Compose / Kubernetes deployments).
///   • On permanent failure the app logs the error and throws, preventing silent data-loss bugs
///     from running against a schema that has not been migrated.
/// </summary>
internal static class DatabaseMigrationHelper
{
    private const int _maxRetries      = 10;
    private const int _delayBaseMs     = 2_000; // doubles on each attempt (exponential back-off)

    public static async Task MigrateAsync(IServiceProvider services, ILogger logger)
    {
        using IServiceScope scope = services.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        await MigrateContextAsync<AppDbContext>(sp, logger);
        await MigrateContextAsync<PersonalDataDbContext>(sp, logger);
    }

    private static async Task MigrateContextAsync<TContext>(
        IServiceProvider sp,
        ILogger logger) where TContext : DbContext
    {
        string contextName = typeof(TContext).Name;
        int attempt     = 0;

        while (true)
        {
            attempt++;
            try
            {
                logger.LogInformation(
                    "Applying migrations for {Context} (attempt {Attempt}/{MaxRetries})…",
                    contextName, attempt, _maxRetries);

                TContext db = sp.GetRequiredService<TContext>();
                await db.Database.MigrateAsync();

                logger.LogInformation("Migrations for {Context} applied successfully", contextName);
                return;
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(_delayBaseMs * Math.Pow(2, attempt - 1));
                logger.LogWarning(
                    ex,
                    "Migration for {Context} failed on attempt {Attempt}. Retrying in {Delay}…",
                    contextName, attempt, delay);

                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    ex,
                    "Migration for {Context} failed after {MaxRetries} attempts. Aborting startup",
                    contextName, _maxRetries);
                throw; // rethrow to fail fast — don't start the app with an un-migrated DB
            }
        }
    }
}

