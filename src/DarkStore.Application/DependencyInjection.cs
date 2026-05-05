using DarkStore.Application.Common.Behaviors;
using FluentValidation;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DarkStore.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Application layer:
    ///   MediatR (handlers + pipeline behaviors) →
    ///   FluentValidation validators →
    ///   Mapster type adapter
    ///
    /// Call from Program.cs: builder.Services.AddApplication();
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // ── MediatR ────────────────────────────────────────────────────────────
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline order (innermost first):
            //   1. LoggingBehavior  — timing + structured log for every request
            //   2. ValidationBehavior — FluentValidation before any handler runs
            //   3. AuditBehavior — records "who did what" for commands
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
        });

        // ── FluentValidation — scan all validators in this assembly ───────────
        services.AddValidatorsFromAssembly(assembly);

        // ── Mapster ────────────────────────────────────────────────────────────
        // Scans for IRegister implementations in this assembly (TypeAdapterConfig mappings).
        TypeAdapterConfig mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(assembly);
        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}

