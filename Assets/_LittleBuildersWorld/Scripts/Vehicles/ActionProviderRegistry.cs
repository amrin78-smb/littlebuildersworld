using System.Collections.Generic;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>World-placed action providers (dig piles, loading bays, delivery spots). Vehicle tools are asked directly.</summary>
    public static class ActionProviderRegistry
    {
        private static readonly List<IActionProvider> s_Providers = new List<IActionProvider>();
        public static IReadOnlyList<IActionProvider> Providers => s_Providers;

        public static void Register(IActionProvider p)
        {
            if (!s_Providers.Contains(p)) s_Providers.Add(p);
        }

        public static void Unregister(IActionProvider p) => s_Providers.Remove(p);
    }
}
