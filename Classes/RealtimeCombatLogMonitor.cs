// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using log4net;
using zxeltor.StoCombat.Lib.Model.CombatLog;
using zxeltor.StoCombat.Lib.Model.Realtime;
using zxeltor.StoCombat.Lib.Parser;
using zxeltor.Types.Lib.Helpers;
using zxeltor.Types.Lib.Result;

namespace zxeltor.StoCombat.Realtime.Classes;

public class RealtimeCombatLogMonitor : IDisposable, INotifyPropertyChanged
{
    #region Static Fields and Constants

    private static readonly ILog Log = LogManager.GetLogger(typeof(RealtimeCombatLogMonitor));

    #endregion

    #region Private Fields

    private RealtimeCombat? _currentRealtimeCombat;
    private long _errorsCountSinceNewCombat;
    private long _eventsLastAddCount;
    private bool _hasHalted;
    private bool _isRunning;

    private bool _isTestMode;
    private DateTime? _lastExceptedParsedCombatLogEventTime;
    private long _lastPosition;
    private TimeSpan _lastTestElapsedTime;
    private DateTime? _lastTimerStartTimeUtc;
    private FileInfo? _latestFileInfo;

    private string? _latestFileName;

    private DateTime? _latestFileUpdated;
    private long _linesLastReadCount;
    private string? _logFileStringResult;
    private readonly RealtimeCombatLogParseSettings _parserSettings;
    private string? _parserUpdate;
    private Timer? _timer;

    private Dispatcher _parentDispatcher;

    #endregion

    #region Constructors

    public RealtimeCombatLogMonitor(Dispatcher parentDispatcher)
    {
        this._parserSettings = StoCombatRealtimeSettings.Instance.ParseSettings;
        this._parentDispatcher = parentDispatcher;
    }

    #endregion

    #region Public Properties

    public bool HasHalted
    {
        get => this._hasHalted;
        set => this.SetField(ref this._hasHalted, value);
    }

    public bool IsRunning
    {
        get => this._isRunning;
        private set => this.SetField(ref this._isRunning, value);
    }

    public bool IsTestMode
    {
        get => this._isTestMode;
        set => this.SetField(ref this._isTestMode, value);
    }

    public DateTime? LastTimerStartTimeUtc
    {
        get => this._lastTimerStartTimeUtc;
        set => this.SetField(ref this._lastTimerStartTimeUtc, value);
    }

    public DateTime? LatestFileUpdated
    {
        get => this._latestFileUpdated;
        set => this.SetField(ref this._latestFileUpdated, value);
    }

    public FileInfo? LatestFileInfo
    {
        get => this._latestFileInfo;
        set
        {
            this.SetField(ref this._latestFileInfo, value);

            this.LatestFileName = value?.Name;
            this.LatestFileUpdated = value?.LastWriteTime;
        }
    }

    public long ErrorsCountSinceNewCombat
    {
        get => this._errorsCountSinceNewCombat;
        set => this.SetField(ref this._errorsCountSinceNewCombat, value);
    }

    public long EventsLastAddCount
    {
        get => this._eventsLastAddCount;
        set => this.SetField(ref this._eventsLastAddCount, value);
    }

    public long LinesLastReadCount
    {
        get => this._linesLastReadCount;
        set => this.SetField(ref this._linesLastReadCount, value);
    }

    public RealtimeCombat? CurrentRealtimeCombat
    {
        get => this._currentRealtimeCombat;
        set => this._parentDispatcher.Invoke(() => this.SetField(ref this._currentRealtimeCombat, value));
    }

    public string? LatestFileName
    {
        get => this._latestFileName;
        set => this.SetField(ref this._latestFileName, value);
    }

    public string? ParserUpdate
    {
        get => this._parserUpdate;
        set => this.SetField(ref this._parserUpdate, value);
    }

    //private TimeSpan HowLongAfterNoCombatBeforeRemoveFromGridInSeconds =>
    //    TimeSpan.FromMinutes(this._parserSettings.HowLongAfterNoCombatBeforeRemoveFromGridInSeconds);

    //private TimeSpan HowLongBeforeNewCombatInSeconds =>
    //    TimeSpan.FromSeconds(this._parserSettings.HowLongBeforeNewCombatInSeconds);

    public TimeSpan LastTestElapsedTime
    {
        get => this._lastTestElapsedTime;
        set => this.SetField(ref this._lastTestElapsedTime, value);
    }

    #endregion

    #region Public Members

    public void Dispose()
    {
        this.Stop();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Start(Dispatcher dispatcher)
    {
        try
        {
            if (this._timer != null)
            {
                this._timer?.Change(Timeout.Infinite, Timeout.Infinite);
                this._timer?.Dispose();
                this._timer = null;
                this.ParserUpdate = null;
            }

            this.ValidateRequiredParserSettings();

            this._timer = new Timer(this.TimerCallBack, null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }
        catch (ParserHaltException pe)
        {
            this.HasHalted = true;
            Log.Error(pe.Message, pe);
            AppCommunicationsManager.Instance.SendNotification(this, pe, ResultLevel.Halt);
        }
        catch (Exception e)
        {
            this.IsRunning = false;
            this.ParserUpdate = "Failed to start parser.";

            Log.Error(this.ParserUpdate, e);
        }
    }

    public void Stop()
    {
        try
        {
            this.IsRunning = false;
            this._timer?.Change(Timeout.Infinite, Timeout.Infinite);
            this._timer?.Dispose();
            this._timer = null;
            this.ParserUpdate = null;
        }
        catch (Exception e)
        {
            this.ParserUpdate = "Failed to dispose of parser.";

            Log.Error(this.ParserUpdate, e);
        }
    }

    #endregion

    #region Other Members

    private void AddTestDataToCombat()
    {
        if (this.CurrentRealtimeCombat == null) return;

        var dateTimeNow = DateTime.Now;
        var startDateTime = dateTimeNow - this._parserSettings.TimeSpanHowOftenParseLogs;

        var randomGen = new Random();

        foreach (var player in this.CurrentRealtimeCombat.PlayerEntities)
        {
            var currentDateTime = startDateTime;

            while (currentDateTime <= dateTimeNow)
            {
                currentDateTime = currentDateTime.AddMilliseconds(100);

                player.AddCombatEvent(new CombatEvent
                {
                    Type = "Phaser", Flags = "Kill", EventDisplay = "phaser", Magnitude = randomGen.Next(0, 1000),
                    MagnitudeBase = randomGen.Next(0, 1000), Timestamp = currentDateTime
                });
            }
        }
    }

    private void DetermineIfCombatEventIsAchievement(CombatEvent combatEvent)
    {
        var isKillEvent = combatEvent.Flags.Contains("kill", StringComparison.CurrentCultureIgnoreCase);
        if (isKillEvent)
        {
            if (string.IsNullOrWhiteSpace(this._parserSettings.MyCharacter)) return;

            if (combatEvent.IsTargetPlayer &&
                combatEvent.TargetInternal.Contains(this._parserSettings.MyCharacter,
                    StringComparison.CurrentCultureIgnoreCase))
                AppCommunicationsManager.Instance.SendAchievementEvent(this, AchievementEvent.Death);
            else if (combatEvent.OwnerInternal.Contains(this._parserSettings.MyCharacter,
                         StringComparison.CurrentCultureIgnoreCase))
                AppCommunicationsManager.Instance.SendAchievementEvent(this, AchievementEvent.Kill);
            else if (this._parserSettings.IsIncludeAssistedKillsInAchievements)
                if (this.CurrentRealtimeCombat != null)
                {
                    var myPlayer = this.CurrentRealtimeCombat.PlayerEntities.FirstOrDefault(ent =>
                        ent.OwnerInternal.Contains(this._parserSettings.MyCharacter,
                            StringComparison.CurrentCultureIgnoreCase));

                    if (myPlayer != null)
                    {
                        var killedEntityFoundInMyPlayerTargetList =
                            myPlayer.CombatEventsList.Any(evt =>
                                evt.TargetInternal.Equals(combatEvent.TargetInternal));

                        if (killedEntityFoundInMyPlayerTargetList)
                            AppCommunicationsManager.Instance.SendAchievementEvent(this, AchievementEvent.Kill);
                    }
                }
        }
    }

    /// <summary>
    ///     Run this on each pass of the parser timer to take players out of combat, if they haven't
    ///     been active for the configured timespan.
    /// </summary>
    private void ExpireInactivePlayersOrCombat()
    {
        if (this.CurrentRealtimeCombat == null || this.CurrentRealtimeCombat.PlayerEntities.Count == 0) return;

        foreach (var player in this.CurrentRealtimeCombat.PlayerEntities.ToList())
        {
            // If a player entity hasn't received an update in the configured timespan, then
            // mark it as not in combat.
            if (DateTime.Now - player.EntityCombatEnd > this._parserSettings.TimeSpanHowLongBeforeNewCombat)
                player.IsInCombat = false;

            if (this._parserSettings.TimeSpanHowLongAfterNoCombatBeforeRemoveFromGrid <= TimeSpan.Zero &&
                !player.IsInCombat)
            {
                if (this.CurrentRealtimeCombat!.PlayerEntities.Count > 1)
                    this.CurrentRealtimeCombat.PlayerEntities.Remove(player);
                else
                    this.CurrentRealtimeCombat = null;
            }
            else if (this._parserSettings.TimeSpanHowLongAfterNoCombatBeforeRemoveFromGrid > TimeSpan.Zero &&
                     !player.IsInCombat
                     && DateTime.Now - player.EntityCombatEnd >
                     this._parserSettings.TimeSpanHowLongAfterNoCombatBeforeRemoveFromGrid)
            {
                if (this.CurrentRealtimeCombat!.PlayerEntities.Count > 1)
                    this.CurrentRealtimeCombat.PlayerEntities.Remove(player);
                else
                    this.CurrentRealtimeCombat = null;
            }
        }
    }

    private bool IsCombatEventValid(CombatEvent combatEvent)
    {
        if (this._lastExceptedParsedCombatLogEventTime == null)
        {
            if (DateTime.Now - combatEvent.Timestamp >
                TimeSpan.FromSeconds(this._parserSettings.HowOftenParseLogsInSeconds * 2)) return false;
        }
        else
        {
            if (combatEvent.Timestamp < this._lastExceptedParsedCombatLogEventTime) return false;
        }

        if (this.CurrentRealtimeCombat != null && this.CurrentRealtimeCombat.AllCombatEvents != null)
        {
            if (combatEvent.IsOwnerPlayer && this.CurrentRealtimeCombat.PlayerEntities.Count > 1 &&
                this.CurrentRealtimeCombat.EventsCount > 1000)
            {
                var player = this.CurrentRealtimeCombat.PlayerEntities.FirstOrDefault(evt =>
                    evt.OwnerInternal.Equals(combatEvent.OwnerInternal));

                if (player != null)
                {
                    if (player.CombatEventsList.Contains(combatEvent)) return false;
                }
                else
                {
                    if (this.CurrentRealtimeCombat.AllCombatEvents.Contains(combatEvent)) return false;
                }
            }
            else
            {
                if (this.CurrentRealtimeCombat.AllCombatEvents.Contains(combatEvent)) return false;
            }
        }

        return true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    ///     We use this to parse a file we previously parsed. This attempts to skip to the latest entries
    ///     in the file, and parse them only. The logic isn't perfect, but we can't lock the file down each time
    ///     we read from it, nor do we want to reread each entry every time we do.
    /// </summary>
    /// <exception cref="Exception">File parsing failed.</exception>
    private void ProcessCurrentFile()
    {
        using (var fs = new FileStream(this.LatestFileInfo!.FullName, FileMode.Open, FileAccess.Read,
                   FileShare.ReadWrite | FileShare.Delete))
        {
            if (!fs.CanRead) throw new Exception($"Don't have read access: {this.LatestFileInfo.Name}");
            if (!fs.CanSeek)
                throw new Exception($"Can't seek position {this._lastPosition} in file : {this.LatestFileInfo.Name}");

            var position = fs.Seek(this._lastPosition, SeekOrigin.Current);
            if (position != this._lastPosition)
                throw new Exception(
                    $"Seek position {this._lastPosition} position in file : {this.LatestFileInfo.Name}");
            this._lastPosition = fs.Length;

            using (var reader = new StreamReader(fs))
            {
                var lineCounter = 0;
                var eventsAdded = 0;
                var firstEventWasExcepted = false;
                while (!reader.EndOfStream)
                {
                    this._logFileStringResult = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(this._logFileStringResult))
                    {
                        //AppCommunicationsManager.Instance.SendNotification(this, "An empty line was returned.",
                        //    ResultLevel.Debug);
                        continue;
                    }

                    try
                    {
                        lineCounter++;

                        // Use our new combat event object to parse the log file line.
                        var combatEvent = new CombatEvent(this.LatestFileInfo.Name, this._logFileStringResult, 0);

                        // A good faith effort to make sure we're not processing the same log file entry again.
                        if (!firstEventWasExcepted && !this.IsCombatEventValid(combatEvent)) continue;

                        firstEventWasExcepted = true;
                        this._lastExceptedParsedCombatLogEventTime = combatEvent.Timestamp;

                        // If we have unreal announcements available, look for kill or death events related to the player account.
                        if (this._parserSettings.IsUnrealAnnouncementsEnabled &&
                            !string.IsNullOrWhiteSpace(this._parserSettings.MyCharacter))
                            this.DetermineIfCombatEventIsAchievement(combatEvent);

                        // If the entry doesn't belong to a player, or it falls outside our timespan, we 
                        // ignore it and move on.
                        if (!combatEvent.IsOwnerPlayer) continue;

                        // Update our current combat instance with the latest player related events for display purposes.
                        if (this.CurrentRealtimeCombat == null)
                        {
                            this.ErrorsCountSinceNewCombat = 0;

                            //this._parentDispatcher.Invoke(() =>
                            //{
                                this.CurrentRealtimeCombat = null;
                                this.CurrentRealtimeCombat = new RealtimeCombat(combatEvent, this._parserSettings);
                            //});

                            AppCommunicationsManager.Instance.SendAchievementEvent(this, AchievementEvent.Reset);
                            eventsAdded++;
                        }
                        else
                        {
                            this.CurrentRealtimeCombat.AddCombatEvent(combatEvent, this._parserSettings);
                            eventsAdded++;
                        }
                    }
                    catch (Exception e)
                    {
                        this.ErrorsCountSinceNewCombat++;
                        Log.Error($"Error parsing line: {e.Message}", e);
                        //AppCommunicationsManager.Instance.SendNotification(this, $"Error parsing line: {e.Message}", e, ResultLevel.Error);
                    }
                }

                this.LinesLastReadCount = lineCounter;
                this.EventsLastAddCount = eventsAdded;
            }
        }
    }

    /// <summary>
    ///     We use this to parse a newly discovered file.
    /// </summary>
    /// <exception cref="Exception">File parsing failed.</exception>
    private void ProcessNewFile()
    {
        using (var fs = new FileStream(this.LatestFileInfo!.FullName, FileMode.Open, FileAccess.Read,
                   FileShare.ReadWrite | FileShare.Delete))
        {
            var length = fs.Length;

            using (var reader = new StreamReader(fs))
            {
                if (!fs.CanRead) throw new Exception($"Don't have read access: {this.LatestFileInfo.Name}");

                this._lastPosition = length;

                var lineCounter = 0;
                var eventsAdded = 0;
                var firstEventWasExcepted = false;
                while (!reader.EndOfStream)
                {
                    this._logFileStringResult = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(this._logFileStringResult))
                    {
                        //AppCommunicationsManager.Instance.SendNotification(this, "An empty line was returned.",
                        //    ResultLevel.Debug);
                        continue;
                    }

                    try
                    {
                        lineCounter++;

                        // Use our new combat event object to parse the log file line.
                        var combatEvent = new CombatEvent(this.LatestFileInfo.Name, this._logFileStringResult, 0);

                        //// If the entry doesn't belong to a player, or it falls outside our timespan, we 
                        //// ignore it and move on.
                        //if (!combatEvent.IsOwnerPlayer) continue;

                        // A good faith effort to make sure we're not processing the same log file entry again.
                        if (!firstEventWasExcepted && !this.IsCombatEventValid(combatEvent)) continue;

                        firstEventWasExcepted = true;
                        this._lastExceptedParsedCombatLogEventTime = combatEvent.Timestamp;

                        // If the entry doesn't belong to a player, or it falls outside our timespan, we 
                        // ignore it and move on.
                        if (!combatEvent.IsOwnerPlayer) continue;

                        // Update our current combat instance with the latest player related events for display purposes.
                        if (this.CurrentRealtimeCombat == null)
                        {
                            this.ErrorsCountSinceNewCombat = 0;

                            //this._parentDispatcher.Invoke(() =>
                            //{
                                this.CurrentRealtimeCombat = null;
                                this.CurrentRealtimeCombat = new RealtimeCombat(combatEvent, this._parserSettings);
                            //});

                            AppCommunicationsManager.Instance.SendAchievementEvent(this, AchievementEvent.Reset);
                            eventsAdded++;
                        }
                        else
                        {
                            this.CurrentRealtimeCombat.AddCombatEvent(combatEvent, this._parserSettings);
                            eventsAdded++;
                        }
                    }
                    catch (Exception e)
                    {
                        this.ErrorsCountSinceNewCombat++;
                        Log.Error($"Error parsing line: {e.Message}", e);
                        //AppCommunicationsManager.Instance.SendNotification(this, $"Error parsing line: {e.Message}", ResultLevel.Error);
                    }
                }

                this.LinesLastReadCount = lineCounter;
                this.EventsLastAddCount = eventsAdded;
            }
        }
    }

    ///// <summary>
    /////     Attempt to send achievement events to <see cref="AchievementPlaybackManager" />
    ///// </summary>
    ///// <param name="achievementEvent"></param>
    //private void SendEvent(AchievementEvent achievementEvent)
    //{
    //    if (this.AccountPlayerEvents != null) Task.Run(() => this.AccountPlayerEvents(this, achievementEvent));
    //}

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        this.OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    ///     A simple validator to confirm the configured STO combat log folder was found, and we have
    ///     a valid file to parse.
    /// </summary>
    /// <returns>True if we have a recent STO combat log to parse. False otherwise.</returns>
    /// <exception cref="ParserHaltException">Can be thrown when folder or file validation fails.</exception>
    private bool StoCombatLogIsAvailableAndHasUpdates()
    {
        // Get the latest STO combat log based on it's last write time.
        if (string.IsNullOrWhiteSpace(this._parserSettings.CombatLogPath) ||
            !Directory.Exists(this._parserSettings.CombatLogPath))
            throw new ParserHaltException("STO combat log folder not found. Check setting: CombatLogPath");

        if (string.IsNullOrWhiteSpace(this._parserSettings.CombatLogPathFilePattern))
            throw new ParserHaltException(
                "STO combat log file pattern not set. Check setting: CombatLogPathFilePattern");

        var fileToParse = Directory
            .GetFiles(this._parserSettings.CombatLogPath, this._parserSettings.CombatLogPathFilePattern)
            .ToList().Select(filepath => new FileInfo(filepath)).MaxBy(fileInfo => fileInfo.LastWriteTime);

        if (fileToParse == null)
        {
            this.LatestFileName = "No combat log file found.";
            return false;
        }

        if (this.LatestFileInfo == null || !this.LatestFileInfo.Name.Equals(fileToParse.Name))
            this._lastPosition = 0;
        else if (this.LatestFileUpdated >= fileToParse.LastWriteTime) return false;

        this.LatestFileInfo = fileToParse;

        return true;
    }

    /// <summary>
    ///     Our timer callback responsible for parsing the STO combat logs.
    /// </summary>
    /// <param name="state">Not used</param>
    private void TimerCallBack(object? state)
    {
        try
        {
            this.IsRunning = true;
            this.HasHalted = false;
            this.LastTimerStartTimeUtc = DateTime.UtcNow;

            this._timer?.Change(Timeout.Infinite, Timeout.Infinite);

            this.ValidateRequiredParserSettings();

            // If STO isn't running, then no point in continuing. Let's halt the parser.
            if (!this.IsTestMode)
            {
                var stoInstanceCount = ProcessHelper.RunningProcessInstanceCount(Path.GetFileNameWithoutExtension(StoCombatRealtimeSettings.Instance.StoAppName));
                if (stoInstanceCount == 0)
                    throw new ParserHaltException("STO is not running.");
            }

            this.ExpireInactivePlayersOrCombat();

            if (this.IsTestMode)
            {
                this.AddTestDataToCombat();
            }
            else
            {
                if (!this.StoCombatLogIsAvailableAndHasUpdates())
                {
                    this.LinesLastReadCount = 0;
                    this.EventsLastAddCount = 0;
                    return;
                }

                if (this._lastPosition == 0)
                    this.ProcessNewFile();
                else
                    this.ProcessCurrentFile();
            }

            if (this.CurrentRealtimeCombat != null) this.CurrentRealtimeCombat.Refresh();

            this.LastTestElapsedTime = DateTime.UtcNow - this.LastTimerStartTimeUtc.Value;

            //Log.Info($"this.LastTestElapsedTime: {this.LastTestElapsedTime}");
        }
        catch (ParserHaltException e)
        {
            this.IsRunning = false;
            this.HasHalted = true;
            Log.Error(e.Message, e);
            AppCommunicationsManager.Instance.SendNotification(this, e.Message, ResultLevel.Halt);
        }
        catch (Exception e)
        {
            this.ErrorsCountSinceNewCombat++;
            var errorMessage = $"Parser failure: {e.Message}";
            Log.Error(errorMessage, e);
            //AppCommunicationsManager.Instance.SendNotification(this, errorMessage, e);
        }
        finally
        {
            if (this.IsRunning)
            {
                var restartTimeSpan = TimeSpan.FromSeconds(this._parserSettings.HowOftenParseLogsInSeconds)
                    .Subtract(DateTime.UtcNow - this.LastTimerStartTimeUtc!.Value);
                if (restartTimeSpan > TimeSpan.Zero)
                    this._timer?.Change(TimeSpan.FromSeconds(this._parserSettings.HowOftenParseLogsInSeconds),
                        Timeout.InfiniteTimeSpan);
                else
                    this._timer?.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
            }
        }
    }

    /// <summary>
    ///     Confirm required parameters are set.
    /// </summary>
    /// <exception cref="ParserHaltException">A required parser settings isn't set.</exception>
    private void ValidateRequiredParserSettings()
    {
        if (this._parserSettings.HowOftenParseLogsInSeconds < 1)
            throw new ParserHaltException("HowOftenParseLogs needs to be greater then 1.");

        if (this._parserSettings.HowLongBeforeNewCombatInSeconds < 1)
            throw new ParserHaltException("HowLongBeforeNewCombatInSeconds needs to be greater then 1.");

        if (string.IsNullOrWhiteSpace(this._parserSettings.CombatLogPath) ||
            !Directory.Exists(this._parserSettings.CombatLogPath))
            throw new ParserHaltException("STO combat log folder not found. Check setting: CombatLogPath");

        if (string.IsNullOrWhiteSpace(this._parserSettings.CombatLogPathFilePattern))
            throw new ParserHaltException(
                "STO combat log file pattern not set. Check setting: CombatLogPathFilePattern");

        if (string.IsNullOrWhiteSpace(this._parserSettings.MyCharacter))
            throw new ParserHaltException("MyCharacter is not set.");
    }

    #endregion
}