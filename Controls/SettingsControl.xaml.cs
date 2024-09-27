// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using log4net;
using Microsoft.Win32;
using zxeltor.StoCombat.Lib.Helpers;
using zxeltor.StoCombat.Lib.Parser;
using zxeltor.StoCombat.Realtime.Classes;
using zxeltor.StoCombat.Realtime.Properties;
using zxeltor.Types.Lib.Result;

namespace zxeltor.StoCombat.Realtime.Controls;

/// <summary>
///     Interaction logic for SettingsControl.xaml
/// </summary>
public partial class SettingsControl : UserControl
{
    #region Private Fields

    private readonly ILog _log = LogManager.GetLogger(typeof(SettingsControl));

    private AchievementOverlayWindow? _testAchievementOverlayWindow;
    private MetricsOverlayWindow? _testMetricsOverlayWindow;

    #endregion

    #region Constructors

    public SettingsControl()
    {
        this.MetricsOverlayWindowContext = StoCombatRealtimeSettings.Instance.MetricsOverlayWindowContext;
        this.AchievementOverlayWindowContext = StoCombatRealtimeSettings.Instance.AchievementOverlayWindowContext;

        this.InitializeComponent();

        this.Unloaded += this.OnUnloaded;
    }

    #endregion

    #region Public Properties

    public AchievementOverlayWindowContext? AchievementOverlayWindowContext { get; private set; }
    public MetricsOverlayWindowContext? MetricsOverlayWindowContext { get; private set; }

    #endregion

    #region Other Members

    private void ColorButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button buttonResult) return;

        var colorPicker = new ColorPickerWindow();
        colorPicker.Owner = Application.Current.MainWindow;
        colorPicker.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        if (buttonResult.Background is SolidColorBrush brushResult)
            colorPicker.SelectedColor = brushResult.Color;

        var dialogResult = colorPicker.ShowDialog();
        if (dialogResult.HasValue && dialogResult.Value)
            buttonResult.Background = new SolidColorBrush(colorPicker.SelectedColor);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        this._testAchievementOverlayWindow?.Close();
        this._testAchievementOverlayWindow = null;
        this._testMetricsOverlayWindow?.Close();
        this._testMetricsOverlayWindow = null;
    }

    private void UiButtonOpenLogFile_OnClick(object sender, RoutedEventArgs e)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoCombatRealtime\\logs\\StoCombatRealtime.log");

        if (!File.Exists(logPath))
        {
            MessageBox.Show($"Log file not found: {logPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            using (var openLogProcess = new Process())
            {
                openLogProcess.StartInfo = new ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                };

                openLogProcess.Start();
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to load log file: {logPath}";
            this._log.Error(errorMessage, ex);
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    ///     Opens a folder dialog letting the user select a logging folder.
    /// </summary>
    private void UiButtonBoxCombatLogPath_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select STO combat log folder",
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(StoCombatRealtimeSettings.Instance.ParseSettings.CombatLogPath))
            if (Directory.Exists(StoCombatRealtimeSettings.Instance.ParseSettings.CombatLogPath))
                dialog.InitialDirectory = StoCombatRealtimeSettings.Instance.ParseSettings.CombatLogPath;

        var dialogResult = dialog.ShowDialog(Application.Current.MainWindow);

        if (dialogResult.HasValue && dialogResult.Value)
            StoCombatRealtimeSettings.Instance.ParseSettings.CombatLogPath = dialog.FolderName;
    }

    /// <summary>
    ///     Uses a helper to try and find the STO log folder. If successful, we set the CombatLogPath setting.
    ///     <para>
    ///         We make an attempt to find a window registry key for the STO application base folder. We then append the log
    ///         folder sub path to it.
    ///     </para>
    /// </summary>
    private void UiButtonBoxCombatLogPathDetect_OnClick(object sender, RoutedEventArgs e)
    {
        if (LibHelper.TryGetStoBaseFolder(out var stoBaseFolder))
        {
            var stoLogFolderPath = Path.Combine(stoBaseFolder, LibHelper.StoCombatLogSubFolder);
            if (Directory.Exists(stoLogFolderPath))
            {
                StoCombatRealtimeSettings.Instance.ParseSettings.CombatLogPath = stoLogFolderPath;
                MessageBox.Show(Application.Current.MainWindow!,
                    "The STO log folder was found. Setting CombatLogPath with the folder path.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StoCombatRealtimeSettings.Instance.ParseSettings.CombatLogPath = stoBaseFolder;
                MessageBox.Show(Application.Current.MainWindow!,
                    "The STO base folder was found, but not the combat log sub folder. Setting CombatLogPath to the base STO folder as a starting point.",
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        else
        {
            MessageBox.Show(Application.Current.MainWindow!,
                "Failed to find the STO base folder in the Windows registry.",
                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UiButtonPlayDeadAudio_OnClick(object sender, RoutedEventArgs e)
    {
        AppCommunicationsManager.Instance.SendAchievementAudio(sender, AchievementPlaybackEnum.DEAD);
    }

    private void UiButtonPlayKillingSpreeAudio_OnClick(object sender, RoutedEventArgs e)
    {
        AppCommunicationsManager.Instance.SendAchievementAudio(sender, AchievementPlaybackEnum.KILLSPREE);
    }

    private void UiButtonPlayMultiKillAudio_OnClick(object sender, RoutedEventArgs e)
    {
        AppCommunicationsManager.Instance.SendAchievementAudio(sender, AchievementPlaybackEnum.MONSTER);
    }

    private void UiButtonToggleAchievementOverlayMode_OnClick(object sender, RoutedEventArgs e)
    {
        if (this.AchievementOverlayWindowContext == null)
            this.AchievementOverlayWindowContext = StoCombatRealtimeSettings.Instance.AchievementOverlayWindowContext;

        this._testAchievementOverlayWindow?.Close();
        this._testAchievementOverlayWindow =
            new AchievementOverlayWindow(this.AchievementOverlayWindowContext, "ULTRA MEGA KILL", true);
        this._testAchievementOverlayWindow.ShowOverlay();
    }

    private void UiButtonToggleOverlayMode_OnClick(object sender, RoutedEventArgs e)
    {
        if (this.MetricsOverlayWindowContext == null)
            this.MetricsOverlayWindowContext = StoCombatRealtimeSettings.Instance.MetricsOverlayWindowContext;

        this._testMetricsOverlayWindow?.Close();
        this._testMetricsOverlayWindow = new MetricsOverlayWindow(this.MetricsOverlayWindowContext, true);
        this._testMetricsOverlayWindow.ShowOverlay();
    }
    
    #endregion
}