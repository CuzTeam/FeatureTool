using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using FeatureTool.Models;
using FeatureTool.Services;
using FeatureTool.ViVe;
using FeatureTool.ViVe.NativeEnums;
using FeatureTool.ViVe.NativeStructs;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FeatureTool.Views
{
    public sealed partial class FeaturePage : Page
    {
        private readonly List<FeatureItem> _all = new();
        private readonly ObservableCollection<FeatureItem> _filtered = new();
        private readonly FeatureNameProvider _names = new();
        private readonly UserFeatureStore _userStore = new();
        private bool _suppressStateChange;
        private bool _loaded;

        public FeaturePage()
        {
            InitializeComponent();

            FeatureList.ItemsSource = _filtered;

            _names.Load("en-US");
            _userStore.Load();

            Loaded += (_, _) =>
            {
                if (!_loaded)
                {
                    _loaded = true;
                    LoadFeatures();
                    ApplyFilter(string.Empty);
                    CheckAdminStatus();
                }
            };
        }

        private void CheckAdminStatus()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                ShowInfo("未以管理员运行", "写入特性配置需要管理员权限。请以管理员身份重新启动本程序。", InfoBarSeverity.Warning);
            }
        }

        private void LoadFeatures()
        {
            _all.Clear();

            RTL_FEATURE_CONFIGURATION[]? configs = null;
            try
            {
                configs = FeatureManager.QueryAllFeatureConfigurations(RTL_FEATURE_CONFIGURATION_TYPE.Runtime);
            }
            catch (Exception ex)
            {
                ShowInfo("加载失败", $"查询特性配置失败：{ex.Message}", InfoBarSeverity.Error);
                return;
            }

            if (configs == null)
            {
                ShowInfo("无数据", "未能读取到任何特性配置。", InfoBarSeverity.Informational);
                return;
            }

            var effective = configs
                .GroupBy(c => c.FeatureId)
                .Select(g => g.OrderByDescending(c => (uint)c.Priority).First());

            foreach (var cfg in effective)
            {
                var name = _names.GetName(cfg.FeatureId);
                if (uint.TryParse(name, out _) && _userStore.TryGetNote(cfg.FeatureId, out var note))
                {
                    name = note;
                }

                _all.Add(new FeatureItem
                {
                    FeatureId   = cfg.FeatureId,
                    Name        = name,
                    Priority    = cfg.Priority,
                    State       = cfg.EnabledState,
                });
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            {
                return;
            }
            ApplyFilter(sender.Text ?? string.Empty);
        }

        public void RefreshFilter() => ApplyFilter(SearchBox.Text ?? string.Empty);

        private void ApplyFilter(string text)
        {
            text = text.Trim();

            IEnumerable<FeatureItem> source = _all;
            if (!AppSettings.Instance.ShowReadOnlyFeatures)
            {
                source = source.Where(f => !f.IsReadOnly);
            }
            if (!string.IsNullOrEmpty(text))
            {
                source = source.Where(f =>
                    f.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                    f.Id.Contains(text, StringComparison.OrdinalIgnoreCase));
            }

            _filtered.Clear();
            foreach (var item in source)
            {
                _filtered.Add(item);
            }

            EmptyState.Visibility = _filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FeatureCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_suppressStateChange) return;
            if (sender is not CheckBox cb || cb.DataContext is not FeatureItem item) return;
            if (item.IsReadOnly) return;

            var target = cb.IsChecked switch
            {
                true  => RTL_FEATURE_ENABLED_STATE.Enabled,
                false => RTL_FEATURE_ENABLED_STATE.Disabled,
                _     => RTL_FEATURE_ENABLED_STATE.Default,
            };

            var op = target == RTL_FEATURE_ENABLED_STATE.Default
                ? RTL_FEATURE_CONFIGURATION_OPERATION.ResetState
                : RTL_FEATURE_CONFIGURATION_OPERATION.FeatureState;

            if (!TrySetFeature(item, op, target))
            {
                RevertCheckBox(cb, item);
            }
        }

        private async void AddFeature_Click(object sender, RoutedEventArgs e)
        {
            NewFeatureIdBox.Text = string.Empty;
            NewFeatureNoteBox.Text = string.Empty;
            NewFeatureStateBox.SelectedIndex = 0;
            await AddFeatureDialog.ShowAsync();
        }

        private void NewFeatureIdBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var raw = NewFeatureIdBox.Text.Trim();
            AddFeatureDialog.IsPrimaryButtonEnabled = uint.TryParse(raw, out _);
        }

        private void AddFeatureDialog_PrimaryClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;

            var raw = NewFeatureIdBox.Text.Trim();
            if (!uint.TryParse(raw, out var featureId))
            {
                ShowInfo("无效 ID", "请输入数字特性 ID。", InfoBarSeverity.Error);
                return;
            }

            if (_all.Any(f => f.FeatureId == featureId))
            {
                ShowInfo("已存在", $"特性 {featureId} 已在列表中。", InfoBarSeverity.Warning, autoClose: true);
                SearchBox.Text = raw;
                ApplyFilter(raw);
                return;
            }

            var stateIdx = NewFeatureStateBox.SelectedIndex;
            var targetState = stateIdx == 1
                ? RTL_FEATURE_ENABLED_STATE.Disabled
                : RTL_FEATURE_ENABLED_STATE.Enabled;

            var note = NewFeatureNoteBox.Text.Trim();

            var update = new RTL_FEATURE_CONFIGURATION_UPDATE
            {
                FeatureId    = featureId,
                Priority     = RTL_FEATURE_CONFIGURATION_PRIORITY.User,
                EnabledState = targetState,
                Operation    = RTL_FEATURE_CONFIGURATION_OPERATION.FeatureState,
            };
            var updates = new[] { update };

            int hrRt;
            int hrBoot;
            try
            {
                hrRt   = FeatureManager.SetFeatureConfigurations(updates, RTL_FEATURE_CONFIGURATION_TYPE.Runtime);
                hrBoot = FeatureManager.SetFeatureConfigurations(updates, RTL_FEATURE_CONFIGURATION_TYPE.Boot);
            }
            catch (ArgumentException aex)
            {
                ShowInfo("添加失败", aex.Message, InfoBarSeverity.Error);
                return;
            }

            if (hrRt != 0)
            {
                ShowInfo("运行时写入失败", GetHumanError(hrRt), InfoBarSeverity.Error);
                return;
            }
            if (hrBoot != 0)
            {
                ShowInfo("持久化写入失败", GetHumanError(hrBoot), InfoBarSeverity.Error);
                return;
            }

            try
            {
                FeatureManager.SetBootFeatureConfigurationState(BSD_FEATURE_CONFIGURATION_STATE.BootPending);
            }
            catch { }

            if (!string.IsNullOrWhiteSpace(note))
            {
                _userStore.Set(featureId, note);
            }

            var name = !string.IsNullOrWhiteSpace(note)
                ? note
                : _names.GetName(featureId);

            var item = new FeatureItem
            {
                FeatureId = featureId,
                Name      = name,
                Priority  = RTL_FEATURE_CONFIGURATION_PRIORITY.User,
                State     = targetState,
            };

            _all.Add(item);
            ApplyFilter(SearchBox.Text ?? string.Empty);

            sender.Hide();
            ShowInfo("已添加", $"特性 {featureId} 已{DescribeChange(RTL_FEATURE_CONFIGURATION_OPERATION.FeatureState, targetState)}，重启后完全生效。", InfoBarSeverity.Success, autoClose: true);
        }

        private bool TrySetFeature(FeatureItem item, RTL_FEATURE_CONFIGURATION_OPERATION operation, RTL_FEATURE_ENABLED_STATE state)
        {
            var update = new RTL_FEATURE_CONFIGURATION_UPDATE
            {
                FeatureId     = item.FeatureId,
                Priority      = item.Priority,
                EnabledState  = state,
                Operation     = operation,
            };
            var updates = new[] { update };

            int hrRt;
            int hrBoot;
            try
            {
                hrRt   = FeatureManager.SetFeatureConfigurations(updates, RTL_FEATURE_CONFIGURATION_TYPE.Runtime);
                hrBoot = FeatureManager.SetFeatureConfigurations(updates, RTL_FEATURE_CONFIGURATION_TYPE.Boot);
            }
            catch (ArgumentException aex)
            {
                ShowInfo("写入失败", aex.Message, InfoBarSeverity.Error);
                return false;
            }

            if (hrRt != 0)
            {
                ShowInfo("运行时写入失败", GetHumanError(hrRt), InfoBarSeverity.Error);
                return false;
            }
            if (hrBoot != 0)
            {
                ShowInfo("持久化写入失败", GetHumanError(hrBoot), InfoBarSeverity.Error);
                return false;
            }

            try
            {
                if (operation != RTL_FEATURE_CONFIGURATION_OPERATION.ResetState)
                {
                    FeatureManager.SetBootFeatureConfigurationState(BSD_FEATURE_CONFIGURATION_STATE.BootPending);
                }
            }
            catch { }

            _suppressStateChange = true;
            item.State = state;
            _suppressStateChange = false;

            ShowInfo("已更新", $"已{DescribeChange(operation, state)}，重启后完全生效。", InfoBarSeverity.Success, autoClose: true);
            return true;
        }

        private void RevertCheckBox(CheckBox cb, FeatureItem item)
        {
            _suppressStateChange = true;
            cb.IsChecked = item.IsChecked;
            _suppressStateChange = false;
        }

        private static string DescribeChange(RTL_FEATURE_CONFIGURATION_OPERATION op, RTL_FEATURE_ENABLED_STATE state)
        {
            if (op == RTL_FEATURE_CONFIGURATION_OPERATION.ResetState) return "重置为默认";
            return state == RTL_FEATURE_ENABLED_STATE.Enabled ? "启用" : "禁用";
        }

        [DllImport("ntdll.dll")]
        private static extern int RtlNtStatusToDosError(int ntStatus);

        private static string GetHumanError(int ntStatus)
        {
            if (ntStatus == 0) return "成功";
            var dos = RtlNtStatusToDosError(ntStatus);
            return new System.ComponentModel.Win32Exception(dos).Message;
        }

        private void ShowInfo(string title, string message, InfoBarSeverity severity, bool autoClose = false)
        {
            InfoBar.Title = title;
            InfoBar.Message = message;
            InfoBar.Severity = severity;
            InfoBar.IsOpen = true;
            if (autoClose)
            {
                var timer = DispatcherQueue.GetForCurrentThread()!.CreateTimer();
                timer.Interval = TimeSpan.FromSeconds(4);
                timer.IsRepeating = false;
                timer.Tick += (_, _) => { InfoBar.IsOpen = false; timer.Stop(); };
                timer.Start();
            }
        }
    }
}
