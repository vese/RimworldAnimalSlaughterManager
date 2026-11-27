using System;
using System.Collections.Generic;
using System.Linq;

namespace ASM;

/// <summary>
/// The mod's service registry — a small DI-like mechanism (in the spirit of the ASP.NET
/// service collection, sized for a mod): a feature or an integration registers its
/// implementation of one or more service contracts, the consumers resolve every
/// implementation of a contract and never know the concrete types. Registrations come from
/// [StaticConstructorOnStartup] classes — the core's own and each integration's (or a foreign
/// mod hosting an integration).
/// </summary>
public static class ServiceRegistry
{
    private static readonly Dictionary<Type, List<object>> services = [];

    /// <summary>Registers the implementation under the contract; several implementations of
    /// one contract coexist and are resolved together.</summary>
    public static void Add<TContract>(TContract implementation) where TContract : class
    {
        var type = typeof(TContract);

        if (services.TryGetValue(type, out var list))
        {
            if (!list.Contains(implementation))
            {
                list.Add(implementation);
            }
        }
        else
        {
            services.Add(type, [implementation]);
        }
    }

    /// <summary>Every registered implementation of the contract, in registration order.</summary>
    public static IReadOnlyList<TContract> GetServices<TContract>() where TContract : class
    {
        return [.. GetServices(typeof(TContract)).Cast<TContract>()];
    }

    /// <summary>Every registered implementation of a contract type (the closed generic of a
    /// preset model, for one).</summary>
    public static IReadOnlyList<object> GetServices(Type contract)
    {
        return services.TryGetValue(contract, out var list) ? [.. list] : [];
    }
}
