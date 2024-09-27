// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using log4net;
using log4net.Core;
using zxeltor.StoCombat.Lib.Classes;
using zxeltor.StoCombat.Realtime.Classes;
using zxeltor.Types.Lib.Collections;
using zxeltor.Types.Lib.Helpers;
using zxeltor.Types.Lib.Logging;
using zxeltor.Types.Lib.Result;

namespace zxeltor.StoCombat.Realtime.Controls;

/// <summary>
///     Interaction logic for LogControl.xaml
/// </summary>
public partial class LogControl : UserControl
{
    #region Private Fields

    private readonly ILog _log = LogManager.GetLogger(typeof(LogControl));

    private readonly LoggingEventAppender? _loggingEventAppender;

    #endregion

    #region Constructors

    public LogControl()
    {
        this.InitializeComponent();
        this.DataContext = this.MyContext = new LogControlDataContext(this.Dispatcher);

        // Attach our custom log4net appender, so we can handle log messages as notifications in the application.
        if (LoggingHelper.TryAddingLoggingEventAppender("ui_event_appender", out var appender) && appender != null)
        {
            this._loggingEventAppender = appender;
            this._loggingEventAppender.LoggingEvent += this.AppenderOnLoggingEvent;
            this._log.Debug($"Created {nameof(LoggingEventAppender)}");
        }

        // Set the minimum result level from the main config.
        this.MyContext.SelectedResultLevel = new ResultLevelWrapper(StoCombatRealtimeSettings.Instance.MinResultLevel);

        // Handle other application notifications.
        //AppCommunicationsManager.Instance.Notification += this.InstanceOnNotification;
    }

    #endregion

    #region Public Properties

    /// <summary>
    ///     The main data context for this class
    /// </summary>
    public LogControlDataContext MyContext { get; }

    #endregion

    #region Other Members

    /// <summary>
    ///     Process log messages from log4net forwarded by our custom appender.
    /// </summary>
    /// <param name="sender">The sender</param>
    /// <param name="loggingEvent">The log4net message object</param>
    private void AppenderOnLoggingEvent(object? sender, LoggingEvent loggingEvent)
    {
        var resultLevel = ResultLevel.Halt;

        if (loggingEvent.Level == Level.Debug || loggingEvent.Level == Level.All)
            resultLevel = ResultLevel.Debug;
        else if (loggingEvent.Level == Level.Info)
            resultLevel = ResultLevel.Info;
        else if (loggingEvent.Level == Level.Warn)
            resultLevel = ResultLevel.Warning;
        else if (loggingEvent.Level == Level.Error)
            resultLevel = ResultLevel.Error;

        if (loggingEvent.ExceptionObject is ParserHaltException)
            resultLevel = ResultLevel.Halt;

        if (resultLevel >= this.MyContext.SelectedResultLevel.ResultLevel)
            this.MyContext.AddLogGridRow(new DataGridRowContext(loggingEvent.TimeStamp, loggingEvent.RenderedMessage,
                loggingEvent.ExceptionObject, resultLevel));
    }

    /// <summary>
    ///     Handle misc notification messages sent my the application.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void InstanceOnNotification(object? sender, DataGridRowContext e)
    {
        this.MyContext.AddLogGridRow(e);
    }

    /// <summary>
    ///     Clear the notification message data grid.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void UiButtonClearLog_OnClick(object sender, RoutedEventArgs e)
    {
        this.MyContext.Clear();
    }
    
    #endregion
}

/// <summary>
///     A data context class for <see cref="LogControl" />
/// </summary>
public class LogControlDataContext : INotifyPropertyChanged
{
    #region Private Fields

    private ResultLevel? _highestResultLevel;

    private ResultLevelWrapper _selectedResultLevel = new(ResultLevel.Error);

    #endregion

    #region Constructors

    /// <summary>
    ///     A constructor used to initialize this class using the dispatcher thread
    ///     from the parent object.
    /// </summary>
    /// <param name="parentDispatcher">The parent dispatcher thread.</param>
    public LogControlDataContext(Dispatcher parentDispatcher)
    {
        this.ParentDispatcher = parentDispatcher;
    }

    #endregion

    #region Public Properties

    /// <summary>
    ///     The parent dispatcher thread.
    /// </summary>
    private Dispatcher ParentDispatcher { get; }

    /// <summary>
    ///     A list of available result levels for the UI.
    /// </summary>
    public ObservableCollection<ResultLevelWrapper> ResultLevels { get; } = new()
    {
        new ResultLevelWrapper(ResultLevel.Debug),
        new ResultLevelWrapper(ResultLevel.Info),
        new ResultLevelWrapper(ResultLevel.Warning),
        new ResultLevelWrapper(ResultLevel.Error),
        new ResultLevelWrapper(ResultLevel.Halt)
    };

    /// <summary>
    ///     The highest result level we currently have for messages in our data grid.
    /// </summary>
    public ResultLevel? HighestResultLevel
    {
        get => this._highestResultLevel;
        set => this.SetField(ref this._highestResultLevel, value);
    }

    /// <summary>
    ///     The currently selected minimum result level.
    /// </summary>
    public ResultLevelWrapper SelectedResultLevel
    {
        get => this._selectedResultLevel;
        set
        {
            this.SetField(ref this._selectedResultLevel, value);
            StoCombatRealtimeSettings.Instance.MinResultLevel = value.ResultLevel;
        }
    }

    /// <summary>
    ///     The list of messages for the UI.
    /// </summary>
    public SyncNotifyCollection<DataGridRowContext> LogGridRows { get; } = [];

    #endregion

    #region Public Members

    /// <summary>
    ///     Used to add a message to the main list.
    ///     <para>Note: This method uses the parent dispatcher thread when adding messages to the list.</para>
    /// </summary>
    /// <param name="context">The message to add to the list.</param>
    public void AddLogGridRow(DataGridRowContext context)
    {
        this.ParentDispatcher.Invoke(() => this.LogGridRows.Add(context));

        if (this.HighestResultLevel == null || context.ResultLevel > this.HighestResultLevel)
            this.HighestResultLevel = context.ResultLevel;
    }

    /// <summary>
    ///     Used to clear messages from the main list.
    ///     <para>Note: This method uses the parent dispatcher thread when clearing messages from the list.</para>
    /// </summary>
    public void Clear()
    {
        this.ParentDispatcher.Invoke(() => this.LogGridRows.Clear());

        this.HighestResultLevel = null;
    }

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
}

/// <summary>
///     Used to wrap result levels for the <see cref="LogControl" /> dropdown control.
/// </summary>
public class ResultLevelWrapper : IEquatable<ResultLevelWrapper>
{
    #region Constructors

    /// <summary>
    ///     Initialize a new instance using a <see cref="ResultLevel" />
    /// </summary>
    /// <param name="resultLevel">The result level.</param>
    public ResultLevelWrapper(ResultLevel resultLevel)
    {
        this.Id = (int)resultLevel;
        this.ResultLevel = resultLevel;
    }

    #endregion

    #region Public Properties

    /// <summary>
    ///     The enumeration ID for the result level
    /// </summary>
    public int Id { get; }

    /// <summary>
    ///     The result level.
    /// </summary>
    public ResultLevel ResultLevel { get; }

    #endregion

    #region Equality members

    /// <inheritdoc />
    public bool Equals(ResultLevelWrapper? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return this.Id == other.Id && this.ResultLevel == other.ResultLevel;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return this.Equals((ResultLevelWrapper)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(this.Id, (int)this.ResultLevel);
    }

    #region Overrides of Object

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{this.Id} - {this.ResultLevel}";
    }

    #endregion

    #endregion
}