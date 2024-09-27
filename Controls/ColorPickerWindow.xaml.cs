// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.Windows;
using System.Windows.Media;

namespace zxeltor.StoCombat.Realtime.Controls;

/// <summary>
///     Interaction logic for ColorPickerWindow.xaml
/// </summary>
public partial class ColorPickerWindow : Window
{
    #region Constructors

    public ColorPickerWindow()
    {
        this.InitializeComponent();
    }

    #endregion

    #region Public Properties

    public Color SelectedColor
    {
        get => this.uiColorPicker.SelectedColor; 
        set => this.uiColorPicker.SelectedColor = value;
    }

    #endregion

    #region Other Members

    private void UiButtonCancel_OnClick(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
    }

    private void UiButtonOk_OnClick(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
    }

    #endregion
}