// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using log4net;

namespace zxeltor.StoCombat.Realtime.Classes;

public class AchievementOverlayWindowContext : INotifyPropertyChanged
{
    #region Static Fields and Constants

    private static readonly ILog Log = LogManager.GetLogger(typeof(MetricsOverlayWindowContext));

    #endregion

    #region Private Fields

    private SolidColorBrush? _dropShadowColorBrush = new(Colors.Red);

    private double _overlayWindowLeft = -1;

    private double _overlayWindowTop = -1;

    private SolidColorBrush? _textColorBrush = new(Colors.GreenYellow);

    #endregion

    #region Public Properties

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

    public SolidColorBrush? DropShadowColorBrush
    {
        get => this._dropShadowColorBrush;
        set => this.SetField(ref this._dropShadowColorBrush, value);
    }

    public SolidColorBrush? TextColorBrush
    {
        get => this._textColorBrush;
        set => this.SetField(ref this._textColorBrush, value);
    }

    #endregion

    #region Public Members

    //public static AchievementOverlayWindowContext? FromSettings()
    //{
    //    try
    //    {
    //        if (string.IsNullOrWhiteSpace(Settings.Default.AchivementOverlayWindowContextString))
    //        {
    //            var context = new AchievementOverlayWindowContext();
    //            context.SaveToConfig();
    //            return context;
    //        }

    //        return SerializationHelper.Deserialize<AchievementOverlayWindowContext>(Settings.Default
    //            .AchivementOverlayWindowContextString);
    //    }
    //    catch (Exception e)
    //    {
    //        Log.Error($"Failed to get {nameof(AchievementOverlayWindowContext)} from config.", e);
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

    //public void SaveToConfig()
    //{
    //    try
    //    {
    //        var thisAsString = SerializationHelper.Serialize(this);
    //        Settings.Default.AchivementOverlayWindowContextString = thisAsString;
    //        Settings.Default.Save();
    //    }
    //    catch (Exception e)
    //    {
    //        Log.Error($"Failed to save {nameof(AchievementOverlayWindowContext)}.", e);
    //    }
    //}
}