using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using FeatureTool.ViVe;
using FeatureTool.ViVe.NativeEnums;
using Microsoft.UI.Xaml;

namespace FeatureTool.Models
{
    public sealed class FeatureItem : INotifyPropertyChanged
    {
        public uint FeatureId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Id => FeatureId.ToString();
        public string? Description { get; init; }
        public RTL_FEATURE_CONFIGURATION_PRIORITY Priority { get; init; }

        public bool IsReadOnly => FeatureManager.ImmutablePriorities.Contains(Priority);

        public bool IsEditable => !IsReadOnly;

        private RTL_FEATURE_ENABLED_STATE _state;
        public RTL_FEATURE_ENABLED_STATE State
        {
            get => _state;
            set
            {
                if (_state == value) return;
                _state = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsChecked));
                OnPropertyChanged(nameof(StateLabel));
            }
        }

        public bool? IsChecked
        {
            get => _state switch
            {
                RTL_FEATURE_ENABLED_STATE.Enabled => true,
                RTL_FEATURE_ENABLED_STATE.Disabled => false,
                _ => null,
            };
        }

        public string StateLabel => _state switch
        {
            RTL_FEATURE_ENABLED_STATE.Enabled => "Enabled",
            RTL_FEATURE_ENABLED_STATE.Disabled => "Disabled",
            _ => "Default",
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
