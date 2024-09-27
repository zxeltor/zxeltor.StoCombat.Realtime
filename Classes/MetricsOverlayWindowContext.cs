// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using log4net;
using Newtonsoft.Json;
using zxeltor.StoCombat.Lib.Model.Realtime;

namespace zxeltor.StoCombat.Realtime.Classes;

/// <summary>
///     A context object for the main grid overlay window.
/// </summary>
public class MetricsOverlayWindowContext : INotifyPropertyChanged
{
    #region Static Fields and Constants

    private static readonly ILog Log = LogManager.GetLogger(typeof(MetricsOverlayWindowContext));

    #endregion

    #region Private Fields

    private SolidColorBrush? _backgroundColorBrush = new() { Color = Colors.Black, Opacity = 0.40 };
    private RealtimeCombat? _currentRealtimeCombat;
    private bool _displayAttacks = true;
    private bool _displayDamage = true;
    private bool _displayDps = true;
    private bool _displayDuration;
    private bool _displayEnd;
    private bool _displayEventsCount;
    private bool _displayInActive;
    private bool _displayKills = true;
    private bool _displayMaxDamage;
    private bool _displayStart;
    private SolidColorBrush? _headerTextColorBrush = new(Colors.Yellow);
    private SolidColorBrush? _outOfCombatTextColorBrush = new(Colors.Red);
    private double _overlayWindowLeft = -1;
    private double _overlayWindowTop = -1;
    private RealtimeCombat? _testRealtimeCombat;
    private SolidColorBrush? _textColorBrush = new(Colors.LawnGreen);
    private int _cellPadding = 2;

    #endregion

    #region Public Properties

    public bool DisplayAttacks
    {
        get => this._displayAttacks;
        set => this.SetField(ref this._displayAttacks, value);
    }

    public bool DisplayDamage
    {
        get => this._displayDamage;
        set => this.SetField(ref this._displayDamage, value);
    }

    public bool DisplayDps
    {
        get => this._displayDps;
        set => this.SetField(ref this._displayDps, value);
    }

    public bool DisplayDuration
    {
        get => this._displayDuration;
        set => this.SetField(ref this._displayDuration, value);
    }

    public bool DisplayEnd
    {
        get => this._displayEnd;
        set => this.SetField(ref this._displayEnd, value);
    }

    public bool DisplayEventsCount
    {
        get => this._displayEventsCount;
        set => this.SetField(ref this._displayEventsCount, value);
    }

    public bool DisplayInActive
    {
        get => this._displayInActive;
        set => this.SetField(ref this._displayInActive, value);
    }

    public bool DisplayKills
    {
        get => this._displayKills;
        set => this.SetField(ref this._displayKills, value);
    }

    public bool DisplayMaxDamage
    {
        get => this._displayMaxDamage;
        set => this.SetField(ref this._displayMaxDamage, value);
    }

    public bool DisplayStart
    {
        get => this._displayStart;
        set => this.SetField(ref this._displayStart, value);
    }

    public double OverlayWindowLeft
    {
        get => this._overlayWindowLeft;
        set => this.SetField(ref this._overlayWindowLeft, value);
    }

    public double OverlayWindowTop
    {
        get => this._overlayWindowTop;
        set => this.SetField(ref this._overlayWindowTop, value);
    }

    [JsonIgnore]
    public RealtimeCombat? CurrentRealtimeCombat
    {
        get => this._currentRealtimeCombat;
        set => this.SetField(ref this._currentRealtimeCombat, value);
    }

    [JsonIgnore]
    public RealtimeCombat? TestRealtimeCombat
    {
        get => this._testRealtimeCombat;
        set => this.SetField(ref this._testRealtimeCombat, value);
    }

    public SolidColorBrush? BackgroundColorBrush
    {
        get => this._backgroundColorBrush;
        set => this.SetField(ref this._backgroundColorBrush, value);
    }

    public SolidColorBrush? HeaderTextColorBrush
    {
        get => this._headerTextColorBrush;
        set => this.SetField(ref this._headerTextColorBrush, value);
    }

    public SolidColorBrush? OutOfCombatTextColorBrush
    {
        get => this._outOfCombatTextColorBrush;
        set => this.SetField(ref this._outOfCombatTextColorBrush, value);
    }

    public SolidColorBrush? TextColorBrush
    {
        get => this._textColorBrush;
        set => this.SetField(ref this._textColorBrush, value);
    }

    #endregion

    #region Public Members

    ///// <summary>
    /////     Create a new instance by pulling existing settings from application config.
    ///// </summary>
    ///// <returns>Settings from application config.</returns>
    //public static MetricsOverlayWindowContext? FromSettings()
    //{
    //    try
    //    {
    //        if (string.IsNullOrWhiteSpace(Settings.Default.OverlayWindowContextString))
    //        {
    //            var context = new MetricsOverlayWindowContext();
    //            context.SaveToConfig();
    //            return context;
    //        }

    //        return SerializationHelper.Deserialize<MetricsOverlayWindowContext>(Settings.Default
    //            .OverlayWindowContextString);
    //    }
    //    catch (Exception e)
    //    {
    //        Log.Error($"Failed to get {nameof(MetricsOverlayWindowContext)} from config.", e);
    //    }

    //    return null;
    //}

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Other Members

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        this.OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    ///// <summary>
    /////     Save settings to application config.
    ///// </summary>
    //public void SaveToConfig()
    //{
    //    try
    //    {
    //        var thisAsString = SerializationHelper.Serialize(this);
    //        Settings.Default.OverlayWindowContextString = thisAsString;
    //        Settings.Default.Save();
    //    }
    //    catch (Exception e)
    //    {
    //        Log.Error($"Failed to save {nameof(MetricsOverlayWindowContext)}.", e);
    //    }
    //}
}