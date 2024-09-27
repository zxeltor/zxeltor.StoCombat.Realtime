// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.Windows;
using System.Windows.Interop;
using zxeltor.StoCombat.Lib.Helpers;
using zxeltor.StoCombat.Realtime.Classes;

namespace zxeltor.StoCombat.Realtime.Controls;

/// <summary>
///     Interaction logic for AchievementOverlayWindow.xaml
/// </summary>
public partial class AchievementOverlayWindow : Window
{
    #region Private Fields

    public AchievementOverlayWindowContext MyContext;

    #endregion

    #region Constructors

    public AchievementOverlayWindow(AchievementOverlayWindowContext context, string achievementMessage,
        bool testMode = false)
    {
        this.InitializeComponent();

        this.DataContext = this;

        this.DataContext = this.MyContext = context;

        if (testMode)
        {
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.uiTextBlockAchievementMessage.Text = $"{AchievementPlaybackEnum.LUDICROUS} KILL!";
        }
        else
        {
            this.AllowsTransparency = true;
            this.WindowStyle = WindowStyle.None;
            this.uiTextBlockAchievementMessage.Text = achievementMessage;
        }
    }

    #endregion

    #region Public Members

    public void SetText(string strAchievementMessage)
    {
        this.uiTextBlockAchievementMessage.Text = strAchievementMessage;
    }

    public void ShowOverlay()
    {
        var workArea = SystemParameters.WorkArea;

        if (this.MyContext.OverlayWindowLeft < 0
            || this.MyContext.OverlayWindowLeft > workArea.Right
            || this.MyContext.OverlayWindowTop < 0
            || this.MyContext.OverlayWindowTop > workArea.Bottom)
        {
            this.MyContext!.OverlayWindowLeft = workArea.Right / 2d;
            this.MyContext.OverlayWindowTop = workArea.Bottom / 2d;
        }

        this.Show();
    }

    #endregion

    #region Other Members

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        WindowHelper.SetWindowExTransparent(hwnd);
    }

    #endregion
}