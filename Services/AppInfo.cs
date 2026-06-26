using System.Reflection;

namespace FeatureTool.Services
{
    public static class AppInfo
    {
        public static string Version { get; } = GetVersion();

        private static string GetVersion()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var attr = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attr != null && !string.IsNullOrWhiteSpace(attr.InformationalVersion))
            {
                var v = attr.InformationalVersion.Split('+')[0].Trim();
                if (v.Length > 0) return v;
            }
            return asm.GetName().Version?.ToString() ?? "0.0.0";
        }
    }
}
