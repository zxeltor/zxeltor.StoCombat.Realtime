// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using zxeltor.StoCombat.Lib.Helpers;
using zxeltor.StoCombat.Realtime.Classes;

namespace zxeltor.StoCombat.Realtime.Controls;

/// <summary>
///     Interaction logic for MetricsOverlayWindow.xaml
/// </summary>
public partial class MetricsOverlayWindow : Window
{
    #region Private Fields

    public MetricsOverlayWindowContext MyContext;

    #endregion

    #region Constructors

    public MetricsOverlayWindow(MetricsOverlayWindowContext context, bool testMode = false)
    {
        this.InitializeComponent();

        this.DataContext = this.MyContext = context;

        if (testMode)
        {
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.uiDataGrid.ItemsSource = this.MyContext.TestRealtimeCombat?.PlayerEntitiesOrderByName;
        }
        else
        {
            this.AllowsTransparency = true;
            this.WindowStyle = WindowStyle.None;
        }
    }

    #endregion

    #region Public Members

    public void ShowOverlay()
    {
        this.Background = new SolidColorBrush(Colors.Transparent);
        this.WindowStartupLocation = WindowStartupLocation.Manual;

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