// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.Windows;
using System.Windows.Controls;
using log4net;
using zxeltor.StoCombat.Lib.Model.CombatLog;
using zxeltor.StoCombat.Lib.Model.Realtime;
using zxeltor.StoCombat.Realtime.Classes;
using zxeltor.Types.Lib.Result;

namespace zxeltor.StoCombat.Realtime.Controls;

/// <summary>
///     Interaction logic for TestControl.xaml
/// </summary>
public partial class TestControl : UserControl
{
    #region Private Fields

    private readonly ILog _log = LogManager.GetLogger(typeof(TestControl));

    #endregion

    #region Constructors

    public TestControl()
    {
        this.InitializeComponent();

        this.MyRealtimeCombatLogMonitor = this.DataContext as RealtimeCombatLogMonitor;
    }

    #endregion

    #region Public Properties

    public RealtimeCombatLogMonitor? MyRealtimeCombatLogMonitor { get; private set; }

    #endregion

    #region Other Members

    private void ButtonBase_SendTestNotification_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.Source is not Button buttonResult) return;
        if (buttonResult.CommandParameter is not string resultLevelFromButton) return;

        var resultLevel = ResultLevel.Debug;

        switch (resultLevelFromButton)
        {
            case "ResultLevel.Info":
                resultLevel = ResultLevel.Info;
                break;
            case "ResultLevel.Warning":
                resultLevel = ResultLevel.Warning;
                break;
            case "ResultLevel.Error":
                resultLevel = ResultLevel.Error;
                break;
            case "ResultLevel.Halt":
                resultLevel = ResultLevel.Halt;
                break;
        }

        AppCommunicationsManager.Instance.SendNotification(this, "Test Message", resultLevel);
    }

    private void PopulateCombat()
    {
        if (!int.TryParse(this.uiTextBoxEventsCount.Text, out var eventCount)) return;
        if (this.MyRealtimeCombatLogMonitor == null) return;

        if (this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat == null)
            this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat = new RealtimeCombat();

        this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat.PlayerEntities.Clear();

        var benzoate = new RealtimeCombatEntity("benzoate") { IsPlayer = true };
        var horak = new RealtimeCombatEntity("horak") { IsPlayer = true };
        var red = new RealtimeCombatEntity("red shirt") { IsPlayer = true };
        var scotty = new RealtimeCombatEntity("scotty") { IsPlayer = true };
        var zxeltor = new RealtimeCombatEntity("zxeltor") { IsPlayer = true };

        this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat.PlayerEntities.Add(benzoate);
        this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat.PlayerEntities.Add(horak);
        this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat.PlayerEntities.Add(red);
        this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat.PlayerEntities.Add(scotty);
        this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat.PlayerEntities.Add(zxeltor);

        var dateTimeNow = DateTime.Now;
        var startDateTime = DateTime.Now.AddSeconds(-(eventCount / 4d));
        var currentDateTime = startDateTime;

        var randomGen = new Random();

        while (currentDateTime <= dateTimeNow)
        {
            currentDateTime = currentDateTime.AddMilliseconds(250);

            benzoate.CombatEventsList.Add(new CombatEvent
            {
                Type = "Phaser", Flags = "Kill", EventDisplay = "phaser", Magnitude = randomGen.Next(0, 1000),
                MagnitudeBase = randomGen.Next(0, 1000), Timestamp = currentDateTime
            });
            horak.CombatEventsList.Add(new CombatEvent
            {
                Type = "Phaser", Flags = "Kill", EventDisplay = "phaser", Magnitude = randomGen.Next(0, 1000),
                MagnitudeBase = randomGen.Next(0, 1000), Timestamp = currentDateTime
            });
            red.CombatEventsList.Add(new CombatEvent
            {
                Type = "Phaser", Flags = "Kill", EventDisplay = "phaser", Magnitude = randomGen.Next(0, 1000),
                MagnitudeBase = randomGen.Next(0, 1000), Timestamp = currentDateTime
            });
            scotty.CombatEventsList.Add(new CombatEvent
            {
                Type = "Phaser", Flags = "Kill", EventDisplay = "phaser", Magnitude = randomGen.Next(0, 1000),
                MagnitudeBase = randomGen.Next(0, 1000), Timestamp = currentDateTime
            });
            zxeltor.CombatEventsList.Add(new CombatEvent
            {
                Type = "Phaser", Flags = "Kill", EventDisplay = "phaser", Magnitude = randomGen.Next(0, 1000),
                MagnitudeBase = randomGen.Next(0, 1000), Timestamp = currentDateTime
            });
        }

        this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat.Refresh();
    }

    private void UiButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            this.MyRealtimeCombatLogMonitor = this.DataContext as RealtimeCombatLogMonitor;

            if (this.MyRealtimeCombatLogMonitor == null) return;
            this.PopulateCombat();
        }
        catch (Exception exception)
        {
            this._log.Error(exception);
        }
    }

    private void UiButtonClear_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (this.MyRealtimeCombatLogMonitor == null) return;
            if (this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat == null) return;

            this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat.PlayerEntities.Clear();
            this.MyRealtimeCombatLogMonitor.CurrentRealtimeCombat.Refresh();
        }
        catch (Exception exception)
        {
            this._log.Error(exception);
        }
    }

    #endregion
}