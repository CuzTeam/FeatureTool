namespace FeatureTool.Services
{
    public sealed class AppSettings
    {
        public static AppSettings Instance { get; } = new();

        public bool ShowReadOnlyFeatures { get; set; } = true;
    }
}
