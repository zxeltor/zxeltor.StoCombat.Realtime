// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using log4net;
using zxeltor.StoCombat.Lib.Classes;
using zxeltor.StoCombat.Realtime.Classes;
using zxeltor.StoCombat.Realtime.Controls;
using zxeltor.Types.Lib.Helpers;
using zxeltor.Types.Lib.Result;

namespace zxeltor.StoCombat.Realtime;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    #region Private Fields

    //private AchievementOverlayWindowContext _achievementOverlayWindowContext;

    private readonly AchievementPlaybackManager? _achievementPlaybackManager;

    private readonly ILog _log = LogManager.GetLogger(typeof(MainWindow));

    //private readonly RealtimeCombatLogParseSettings _parserSettings;
    private readonly RealtimeCombatLogMonitor _realtimeCombatLogMonitor;
    private AchievementOverlayWindow? _testAchievementOverlayWindow;
    private MetricsOverlayWindow? _testMetricsOverlayWindow;

    #endregion

    #region Constructors

    public MainWindow()
    {
        if (LoggingHelper.TryConfigureLog4NetLogging(out var isUsingDevelopmentConfig))
            AppCommunicationsManager.Instance.SendNotification(this,
                isUsingDevelopmentConfig
                    ? "Logging has been configured using Log4Net.Development.config."
                    : "Logging has been configured.",
                ResultLevel.Debug);
        else
            AppCommunicationsManager.Instance.SendNotification(this, "Logging configuration failed.",
                ResultLevel.Error);

        this.InitializeComponent();

        this.Title = $"STO Realtime Combat Log Analyzer {this.ApplicationVersionInfoString}";

        this.DataContext = this._realtimeCombatLogMonitor = new RealtimeCombatLogMonitor(this.Dispatcher);

        this._achievementPlaybackManager = new AchievementPlaybackManager(this.Dispatcher);
        StoCombatRealtimeSettings.Instance.MetricsOverlayWindowContext.CurrentRealtimeCombat =
            this._realtimeCombatLogMonitor.CurrentRealtimeCombat;

        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;

        this._log.Info("Application Started.");
    }

    #endregion

    #region Public Properties

    //private MetricsOverlayWindowContext _metricsOverlayWindowContext;
    public MetricsOverlayWindow? MetricsOverlayWindow { get; private set; }

    //public AchievementOverlayWindowContext AchievementOverlayWindowContext
    //{
    //    get => this._achievementOverlayWindowContext;
    //    private set => this.SetField(ref this._achievementOverlayWindowContext, value);
    //}

    //public MetricsOverlayWindowContext MetricsOverlayWindowContext
    //{
    //    get => this._metricsMetricsOverlayWindowContext;
    //    private set => this.SetField(ref this._metricsMetricsOverlayWindowContext, value);
    //}

    private string ApplicationVersionInfoString
    {
        get
        {
            var version = AssemblyInfoHelper.GetApplicationVersionFromAssembly();
            return $"{version.Major}.{version.Minor}.{version.Revision}";
        }
    }

    #endregion

    #region Public Members

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Other Members

    private void ColorButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button buttonResult) return;

        var colorPicker = new ColorPickerWindow();
        colorPicker.Owner = this;
        colorPicker.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        if (buttonResult.Background is SolidColorBrush brushResult)
            colorPicker.SelectedColor = brushResult.Color;

        var dialogResult = colorPicker.ShowDialog();
        if (dialogResult.HasValue && dialogResult.Value)
            buttonResult.Background = new SolidColorBrush(colorPicker.SelectedColor);
    }

    private void InstanceOnNotification(object? sender, DataGridRowContext e)
    {
        if (e.ResultLevel == ResultLevel.Halt)
            if (this.MetricsOverlayWindow != null)
                this.Dispatcher.Invoke(() =>
                {
                    this.MetricsOverlayWindow?.Close();
                    this.MetricsOverlayWindow = null;
                });
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.Loaded -= this.OnLoaded;

        this.SetLoggingLevel();

        StoCombatRealtimeSettings.Instance.PropertyChanged += this.Instance_PropertyChanged;
        
        this._realtimeCombatLogMonitor.PropertyChanged += this.RealtimeCombatLogMonitorOnPropertyChanged;
        AppCommunicationsManager.Instance.Notification += this.InstanceOnNotification;

        if (StoCombatRealtimeSettings.Instance.ParseSettings.IsUnrealAnnouncementsEnabled)
            this._achievementPlaybackManager?.Start();
    }

    private void Instance_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != null && e.PropertyName.Equals(nameof(StoCombatRealtimeSettings.IsEnableDebugLogging)))
        {
            this.SetLoggingLevel();
        }
    }

    private void SetLoggingLevel()
    {
        LoggingHelper.TrySettingLog4NetLogLevel(StoCombatRealtimeSettings.Instance.IsEnableDebugLogging);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        this.Unloaded -= this.OnUnloaded;

        this._testAchievementOverlayWindow?.Close();
        this._testAchievementOverlayWindow = null;

        this._testMetricsOverlayWindow?.Close();
        this._testMetricsOverlayWindow = null;

        this.MetricsOverlayWindow?.Close();
        this.MetricsOverlayWindow = null;

        this._testAchievementOverlayWindow?.Close();
        this._testAchievementOverlayWindow = null;

        //this._parserSettings.PropertyChanged -= this.ParserSettingsOnPropertyChanged;

        this._realtimeCombatLogMonitor?.Dispose();
        this._achievementPlaybackManager?.Dispose();
    }

    //private void ParserSettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    //{
    //    if (e.PropertyName != null && e.PropertyName.Equals(nameof(this._parserSettings.IsUnrealAnnouncementsEnabled)))
    //        if (this._parserSettings.IsUnrealAnnouncementsEnabled) this._achievementPlaybackManager?.Start();
    //        else this._achievementPlaybackManager?.Dispose();
    //}

    private void RealtimeCombatLogMonitorOnAccountPlayerEvents(object? sender, AchievementEvent e)
    {
        this.Dispatcher.Invoke(() => this._achievementPlaybackManager?.ProcessEvent(e));
    }

    private void RealtimeCombatLogMonitorOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != null &&
            e.PropertyName.Equals(nameof(this._realtimeCombatLogMonitor.CurrentRealtimeCombat)))
            StoCombatRealtimeSettings.Instance.MetricsOverlayWindowContext.CurrentRealtimeCombat =
                this._realtimeCombatLogMonitor.CurrentRealtimeCombat;
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        this.OnPropertyChanged(propertyName);
        return true;
    }

    private void UiButtonDisplayOverlay_OnClick(object sender, RoutedEventArgs e)
    {
        if (this.MetricsOverlayWindow == null)
        {
            this._testMetricsOverlayWindow?.Close();
            this._testMetricsOverlayWindow = null;

            this.MetricsOverlayWindow =
                new MetricsOverlayWindow(StoCombatRealtimeSettings.Instance.MetricsOverlayWindowContext);
            this.MetricsOverlayWindow.ShowOverlay();
        }
        else
        {
            this._testMetricsOverlayWindow?.Close();
            this._testMetricsOverlayWindow = null;

            this.MetricsOverlayWindow.Close();
            this.MetricsOverlayWindow = null;
        }
    }

    private void UiButtonEnd_OnClick(object sender, RoutedEventArgs e)
    {
        this._realtimeCombatLogMonitor?.Dispose();
    }

    //private void UiButtonOpenLog_OnClick(object sender, RoutedEventArgs e)
    //{
    //    this.uiTabItemLogging.IsSelected = true;
    //}

    private void UiButtonPlayAudio_OnClick(object sender, RoutedEventArgs e)
    {
        this._log.Error("Playing audio file");
        this._achievementPlaybackManager?.PlayAudio(AchievementPlaybackEnum.MONSTER);
    }

    private void UiButtonRefreshCombat_OnClick(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftAlt))
        {
            StoCombatRealtimeSettings.Instance.IsDisplayToolsTab =
                !StoCombatRealtimeSettings.Instance.IsDisplayToolsTab;
            return;
        }

        try
        {
            this._realtimeCombatLogMonitor.Stop();
            this._realtimeCombatLogMonitor.CurrentRealtimeCombat = null;
        }
        catch (Exception exception)
        {
            this._log.Error("Failed to stop and clear combat.", exception);
        }
    }

    private void UiButtonStart_OnClick(object sender, RoutedEventArgs e)
    {
        this._realtimeCombatLogMonitor?.Dispose();
        this._realtimeCombatLogMonitor?.Start(this.Dispatcher);
    }

    private void UiButtonToggleAchievementOverlayMode_OnClick(object sender, RoutedEventArgs e)
    {
        this._testAchievementOverlayWindow?.Close();
        this._testAchievementOverlayWindow = null;

        this._testAchievementOverlayWindow =
            new AchievementOverlayWindow(StoCombatRealtimeSettings.Instance.AchievementOverlayWindowContext,
                "ULTRA MEGA KILL", true);
        this._testAchievementOverlayWindow.ShowOverlay();
    }

    private void UiButtonToggleOverlayMode_OnClick(object sender, RoutedEventArgs e)
    {
        this.MetricsOverlayWindow?.Close();
        this.MetricsOverlayWindow = null;

        this._testMetricsOverlayWindow?.Close();
        this._testMetricsOverlayWindow =
            new MetricsOverlayWindow(StoCombatRealtimeSettings.Instance.MetricsOverlayWindowContext, true);
        this._testMetricsOverlayWindow.ShowOverlay();
    }

    #endregion
}