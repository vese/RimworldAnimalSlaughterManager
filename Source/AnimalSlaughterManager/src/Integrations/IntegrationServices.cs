using System;
using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>
/// The service registry of the integrations — a small DI-like mechanism (in the spirit of the
/// ASP.NET service collection, sized for a mod): an integration registers its adapter as an
/// implementation of one or more service contracts, the core resolves every implementation of
/// a contract and never knows the concrete types. Registrations come from each integration's
/// own [StaticConstructorOnStartup] class (or from a foreign mod hosting an integration).
/// </summary>
public static class IntegrationServices
{
    private static readonly Dictionary<Type, List<object>> services = new();

    /// <summary>Registers the implementation under the contract; several implementations of
    /// one contract coexist and are resolved together.</summary>
    public static void Add<TContract>(TContract implementation)
        where TContract : class
    {
        if (!services.TryGetValue(typeof(TContract), out var list))
        {
            list = [];
            services[typeof(TContract)] = list;
        }

        if (!list.Contains(implementation))
        {
            list.Add(implementation);
        }
    }

    /// <summary>Every registered implementation of the contract, in registration order.</summary>
    public static IReadOnlyList<TContract> GetServices<TContract>()
        where TContract : class
    {
        return services.TryGetValue(typeof(TContract), out var list) ? list.Cast<TContract>().ToList() : [];
    }
}
