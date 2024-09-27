// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using zxeltor.StoCombat.Lib.Parser;
using zxeltor.StoCombat.Realtime.Properties;
using zxeltor.Types.Lib.Helpers;
using zxeltor.Types.Lib.Result;

namespace zxeltor.StoCombat.Realtime.Classes;

public sealed class StoCombatRealtimeSettings : INotifyPropertyChanged, IDisposable
{
    #region Static Fields and Constants

    private static StoCombatRealtimeSettings? _instance;
    private static readonly object Padlock = new();

    #endregion

    #region Private Fields

    private AchievementOverlayWindowContext _achievementOverlayWindowContext = new();
    private MetricsOverlayWindowContext _metricsOverlayWindowContext = new();
    private RealtimeCombatLogParseSettings _parseSettings = new();

    private bool _isEnableDebugLogging = false;

    public bool IsEnableDebugLogging
    {
        get => _isEnableDebugLogging;
        set => SetField(ref this._isEnableDebugLogging, value);
    }

    private ResultLevel _minResultLevel = ResultLevel.Error;
    public ResultLevel MinResultLevel
    {
        get => _minResultLevel;
        set => SetField(ref this._minResultLevel, value);
    }

    private string _stoAppName = "GameClient.exe";
    public string StoAppName
    {
        get => this._stoAppName;
        set => SetField(ref this._stoAppName, value);
    }
    
    private bool _isDisplayToolsTab;
    public bool IsDisplayToolsTab
    {
        get => this._isDisplayToolsTab;
        set => SetField(ref this._isDisplayToolsTab, value);
    }

    #endregion

    #region Constructors

    /// <summary>
    ///     A private constructor to block creating instances outside the singleton pattern.
    /// </summary>
    private StoCombatRealtimeSettings()
    {
        this._parseSettings.PropertyChanged += this.ParseSettingsOnPropertyChanged;
        this._metricsOverlayWindowContext.PropertyChanged += this.MetricsOverlayWindowContextOnPropertyChanged;
        this._achievementOverlayWindowContext.PropertyChanged += this.AchievementOverlayWindowContextOnPropertyChanged;
    }

    #endregion

    #region Public Properties

    [JsonPropertyOrder(3)]
    public AchievementOverlayWindowContext AchievementOverlayWindowContext
    {
        get => this._achievementOverlayWindowContext;
        set => this.SetField(ref this._achievementOverlayWindowContext, value);
    }

    [JsonPropertyOrder(2)]
    public MetricsOverlayWindowContext MetricsOverlayWindowContext
    {
        get => this._metricsOverlayWindowContext;
        set => this.SetField(ref this._metricsOverlayWindowContext, value);
    }

    [JsonPropertyOrder(1)]
    public RealtimeCombatLogParseSettings ParseSettings
    {
        get => this._parseSettings;
        set => this.SetField(ref this._parseSettings, value);
    }

    [JsonIgnore]
    public static StoCombatRealtimeSettings Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ??= FromAppConfig();
            }
        }
    }

    #endregion

    #region Public Members

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        this._parseSettings.PropertyChanged -= this.ParseSettingsOnPropertyChanged;
        this._metricsOverlayWindowContext.PropertyChanged -= this.MetricsOverlayWindowContextOnPropertyChanged;
        this._achievementOverlayWindowContext.PropertyChanged -= this.AchievementOverlayWindowContextOnPropertyChanged;
    }

    #endregion

    public static StoCombatRealtimeSettings FromAppConfig()
    {
        if (string.IsNullOrWhiteSpace(Settings.Default.StoCombatRealtimeSettings) ||
            !SerializationHelper.TryDeserializeString<StoCombatRealtimeSettings>(
                Settings.Default.StoCombatRealtimeSettings, out var settings) || settings == null)
        {
            var context = new StoCombatRealtimeSettings();
            context.SaveToAppConfig();
            return context;
        }

        return settings;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SaveToAppConfig()
    {
        var thisAsString = SerializationHelper.Serialize(this);
        Settings.Default.StoCombatRealtimeSettings = thisAsString;
        Settings.Default.Save();
    }

    #endregion

    #region Other Members

    private void AchievementOverlayWindowContextOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        this.SaveToAppConfig();
    }

    private void MetricsOverlayWindowContextOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        this.SaveToAppConfig();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        this.SaveToAppConfig();
    }

    private void ParseSettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        this.SaveToAppConfig();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        this.OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}