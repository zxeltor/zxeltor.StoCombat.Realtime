// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using log4net;
using zxeltor.StoCombat.Lib.Helpers;
using zxeltor.StoCombat.Lib.Parser;
using zxeltor.StoCombat.Realtime.Controls;

namespace zxeltor.StoCombat.Realtime.Classes;

public class AchievementPlaybackManager : INotifyPropertyChanged, IDisposable
{
    #region Private Fields

    private Dictionary<AchievementPlaybackEnum, AchievementMedia>? _achievementMediaDictionary;
    private AchievementOverlayWindow? _achievementOverlayWindow;

    private readonly AchievementOverlayWindowContext _achievementOverlayWindowContext;

    // We use this as an audio playback queue, so we process each audio file one at a time,
    // so they're not trampling on each other.
    private BlockingCollection<AchievementPlaybackEnum>? _achievementRequestQueue;

    private Task? _audioPlaybackTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRunning;
    private readonly ILog _log = LogManager.GetLogger(typeof(AchievementPlaybackManager));

    // The number of consecutive kills to be considered for multi kill processing.
    private int _numberOfConsecutiveKills;

    // The number of consecutive kills with a death. Is reset after player death, or new combat has been detected.
    private int _numberOfPlayerKillsBeforeDeath;

    //private AchievementOverlayWindow? _overlayWindow;
    private readonly Dispatcher _parentDispatcher;
    private readonly RealtimeCombatLogParseSettings _parserSettings;

    // Used to determine if recent consecutive kills can be considered for multi kill processing.
    private DateTime _timeOfLastKill;

    // A timer used for multi kill determination.
    private Timer? _timerMultiKillAnnouncement;

    #endregion

    #region Constructors

    /// <summary>
    ///     Used to manage achievement logic
    /// </summary>
    /// <param name="context">A context object for the achievement overlay window.</param>
    /// <param name="parentParentDispatcher">The parent object dispatcher.</param>
    /// <param name="parserSettings">The main parse settings.</param>
    public AchievementPlaybackManager(Dispatcher parentParentDispatcher)
    {
        this._achievementOverlayWindowContext = StoCombatRealtimeSettings.Instance.AchievementOverlayWindowContext;
        this._parentDispatcher = parentParentDispatcher;
        this._parserSettings = StoCombatRealtimeSettings.Instance.ParseSettings;
    }

    #endregion

    #region Public Properties

    public bool IsRunning
    {
        get => this._isRunning;
        private set => this.SetField(ref this._isRunning, value);
    }

    // The max number of seconds between consecutive kills for a new kill to be considered for multi kill processing.
    private TimeSpan MultiKillWait => TimeSpan.FromSeconds(this._parserSettings.MultiKillWaitInSeconds);

    #endregion

    #region Public Members

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            this.IsRunning = false;

            AppCommunicationsManager.Instance.AchievementEvent -= this.InstanceOnAchievementEvent;
            AppCommunicationsManager.Instance.PlayAchievementAudioRequest -= this.InstanceOnPlayAchievementAudioRequest;

            this._timerMultiKillAnnouncement?.Dispose();

            this._achievementOverlayWindow?.Close();
            this._achievementOverlayWindow = null;

            /*
             * Stop the dequeuing task from taking more entries on the audio playback queue.
             */
            this._achievementRequestQueue?.CompleteAdding();
            this._cancellationTokenSource?.Cancel();

            if (this._audioPlaybackTask != null)
            {
                this._audioPlaybackTask.Wait(5000);
                this._cancellationTokenSource?.Dispose();

                /*
                 * Disposing of everything else.
                 */
                this._achievementRequestQueue?.Dispose();

                this._audioPlaybackTask?.Dispose();
                this._achievementRequestQueue?.Dispose();
            }

            this._achievementMediaDictionary?.Values.ToList().ForEach(achievementMedia =>
            {
                achievementMedia.MediaPlayer?.Close();
            });

            this._achievementMediaDictionary?.Clear();
        }
        catch (Exception e)
        {
            this._log.Error($"Failed to dispose of {nameof(AchievementPlaybackManager)}. Reason={e.Message}", e);
        }
    }

    #endregion

    /// <summary>
    ///     Add an achievement to the playback queue, so it can get in line for playback.
    /// </summary>
    /// <param name="achievementPlaybackEnum">The achievement audio playbackEnum to play.</param>
    public void PlayAudio(AchievementPlaybackEnum achievementPlaybackEnum)
    {
        if (!this.IsRunning) return;

        try
        {
            this._achievementRequestQueue?.Add(achievementPlaybackEnum);
        }
        catch (Exception e)
        {
            this._log.Error($"Failed to add achievement {achievementPlaybackEnum} to audio playback queue.", e);
        }
    }

    public void ProcessEvent(AchievementEvent achievementEvent)
    {
        if (!this.IsRunning) return;

        try
        {
            if (achievementEvent == AchievementEvent.Kill)
            {
                this.ProcessKill(DateTime.Now);
            }
            else if (achievementEvent == AchievementEvent.Death)
            {
                this._log.Debug($"Processed event {achievementEvent}");
                this.ProcessDeath();
            }
            else
            {
                this._log.Debug($"Processed event {achievementEvent}");
                this.Reset();
            }
        }
        catch (Exception e)
        {
            this._log.Error($"Failed to process achievement event: {achievementEvent}", e);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Start()
    {
        try
        {
            if (this.IsRunning) return;
            this.IsRunning = true;

            this.SetupAchievementMediaDictionary();

            this._achievementOverlayWindow =
                new AchievementOverlayWindow(this._achievementOverlayWindowContext, string.Empty);
            this._achievementOverlayWindow.ShowOverlay();

            this._cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken());

            this._timerMultiKillAnnouncement = new Timer(this.MultiKillAnnouncementTimerCallback, null,
                Timeout.Infinite,
                Timeout.Infinite);

            this._achievementRequestQueue =
                new BlockingCollection<AchievementPlaybackEnum>(new ConcurrentQueue<AchievementPlaybackEnum>());
            this._audioPlaybackTask =
                new Task(this.ProcessAchievementRequestQueue, this._cancellationTokenSource.Token);
            this._audioPlaybackTask.Start();

            AppCommunicationsManager.Instance.AchievementEvent += this.InstanceOnAchievementEvent;
            AppCommunicationsManager.Instance.PlayAchievementAudioRequest += this.InstanceOnPlayAchievementAudioRequest;
        }
        catch (Exception e)
        {
            this._log.Error($"Failed to start: {nameof(AchievementPlaybackManager)}. Reason={e.Message}", e);
            this.Dispose();
        }
    }

    #endregion

    #region Other Members

    /// <summary>
    ///     Determine if a kill at this time should be considered part of a multi kill achievement.
    /// </summary>
    /// <param name="timeOfCurrentKill">The time of the current kill.</param>
    private void DetermineIfMultiKill(DateTime timeOfCurrentKill)
    {
        if (timeOfCurrentKill - this._timeOfLastKill <= this.MultiKillWait)
        {
            this._numberOfConsecutiveKills++;
            this.StopMultiKillAnnouncementTimer();
            this.StartMultiKillAnnouncementTimer();
        }
    }

    private void InstanceOnAchievementEvent(object? sender, AchievementEvent e)
    {
        this.ProcessEvent(e);
    }

    private void InstanceOnPlayAchievementAudioRequest(object? sender, AchievementPlaybackEnum e)
    {
        this.PlayAudio(e);
    }

    private void MultiKillAnnouncementTimerCallback(object? state)
    {
        try
        {
            var tmpConsecutiveKillCount = this._numberOfConsecutiveKills;
            if (tmpConsecutiveKillCount > 7) tmpConsecutiveKillCount = 7;

            this._log.Debug($"Processing multi-kill: x{tmpConsecutiveKillCount+1}: {(AchievementPlaybackEnum)tmpConsecutiveKillCount}");
            this._achievementRequestQueue?.Add((AchievementPlaybackEnum)tmpConsecutiveKillCount);
            this._numberOfConsecutiveKills = 0;
        }
        catch (Exception e)
        {
            this._log.Error("MultiKillAnnouncementTimer failed.", e);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ProcessAchievementRequestQueue()
    {
        while (this._achievementRequestQueue is { IsAddingCompleted: false } && this._cancellationTokenSource != null &&
               !this._cancellationTokenSource.Token.IsCancellationRequested)
            try
            {
                // This will block until an item is pulled from the queue.
                var nextAchievementType = this._achievementRequestQueue.Take(this._cancellationTokenSource.Token);

                var delay = TimeSpan.Zero;
                if (this._achievementMediaDictionary != null &&
                    this._achievementMediaDictionary.TryGetValue(nextAchievementType, out var achievementMedia))
                {
                    this._parentDispatcher.Invoke(() =>
                    {
                        // Do audio playback of achievement, if it's enabled.
                        if ((this._parserSettings.IsProcessKillingSpreeAnnouncements &&
                             achievementMedia.TypeEnum == AchievementTypeEnum.Spree)
                            || (this._parserSettings.IsProcessMultiKillAnnouncements &&
                                achievementMedia.TypeEnum == AchievementTypeEnum.Multi)
                            || (this._parserSettings.IsProcessMiscAnnouncements &&
                                achievementMedia.TypeEnum == AchievementTypeEnum.Misc))
                            if (achievementMedia.MediaPlayer != null)
                            {
                                achievementMedia.MediaPlayer.Position = TimeSpan.Zero;
                                achievementMedia.MediaPlayer.Volume =
                                    this._parserSettings.AnnouncementPlaybackVolumePercentage / 100d;
                                achievementMedia.MediaPlayer.Play();

                                delay = achievementMedia.MediaPlayer.NaturalDuration.HasTimeSpan
                                    ? achievementMedia.MediaPlayer.NaturalDuration.TimeSpan
                                    : achievementMedia.AudioDuration;
                            }

                        // Display splash screen for achievement, if it's enabled.
                        if ((this._parserSettings.IsProcessKillingSpreeSplash &&
                             achievementMedia.TypeEnum == AchievementTypeEnum.Spree)
                            || (this._parserSettings.IsProcessMultiKillSplash &&
                                achievementMedia.TypeEnum == AchievementTypeEnum.Multi)
                            || (this._parserSettings.IsProcessMiscSplash &&
                                achievementMedia.TypeEnum == AchievementTypeEnum.Misc))
                        {
                            if (this._achievementOverlayWindow != null && !this._achievementOverlayWindow.IsOpen())
                                this._achievementOverlayWindow.ShowOverlay();

                            this._achievementOverlayWindow?.SetText(achievementMedia.DisplayText);

                            if (delay == TimeSpan.Zero)
                                delay = achievementMedia.AudioDuration;
                        }
                    });

                    // Not ideal, but media player doesn't have a blocking call for playback.
                    Thread.Sleep(delay);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (InvalidOperationException e)
            {
                this._log.Error($"Failed to process achievement on queue. Reason={e.Message}", e);
            }
            catch (Exception e)
            {
                this._log.Error($"Failed to process achievement on queue. Reason={e.Message}", e);
            }
            finally
            {
                if (this._achievementOverlayWindow != null)
                    this._parentDispatcher.Invoke(() => this._achievementOverlayWindow?.SetText(string.Empty));
            }
    }

    private void ProcessDeath()
    {
        this._achievementRequestQueue?.Add(AchievementPlaybackEnum.DEAD);
    }

    private void ProcessKill(DateTime timeStamp)
    {
        this._numberOfPlayerKillsBeforeDeath++;

        if (this._numberOfPlayerKillsBeforeDeath >= 2 && this._parserSettings.IsProcessMultiKillAnnouncements)
            this.DetermineIfMultiKill(timeStamp);
        this._timeOfLastKill = timeStamp;

        if ((this._numberOfPlayerKillsBeforeDeath <= 30 && this._numberOfPlayerKillsBeforeDeath % 5 == 0)
            || (this._numberOfPlayerKillsBeforeDeath > 30 && this._numberOfPlayerKillsBeforeDeath % 10 == 0)
            || (this._numberOfPlayerKillsBeforeDeath > 50 && this._numberOfPlayerKillsBeforeDeath % 20 == 0
                                                          && this._parserSettings.IsProcessKillingSpreeAnnouncements))
            this.ProcessKillingSpree();
    }

    private void ProcessKillingSpree()
    {
        if (this._numberOfPlayerKillsBeforeDeath >= 30)
        {
            this._log.Debug($"Processing spree {this._numberOfPlayerKillsBeforeDeath}: {AchievementPlaybackEnum.WICKED}");
            this._achievementRequestQueue?.Add(AchievementPlaybackEnum.WICKED);
        }
        else if (this._numberOfPlayerKillsBeforeDeath == 25)
        {
            this._log.Debug($"Processing spree {this._numberOfPlayerKillsBeforeDeath}: {AchievementPlaybackEnum.GODLIKE}");
            this._achievementRequestQueue?.Add(AchievementPlaybackEnum.GODLIKE);
        }
        else if (this._numberOfPlayerKillsBeforeDeath == 20)
        {
            this._log.Debug($"Processing spree {this._numberOfPlayerKillsBeforeDeath}: {AchievementPlaybackEnum.UNSTOPPABLE}");
            this._achievementRequestQueue?.Add(AchievementPlaybackEnum.UNSTOPPABLE);
        }
        else if (this._numberOfPlayerKillsBeforeDeath == 15)
        {
            this._log.Debug($"Processing spree {this._numberOfPlayerKillsBeforeDeath}: {AchievementPlaybackEnum.DOMINATING}");
            this._achievementRequestQueue?.Add(AchievementPlaybackEnum.DOMINATING);
        }
        else if (this._numberOfPlayerKillsBeforeDeath == 10)
        {
            this._log.Debug($"Processing spree {this._numberOfPlayerKillsBeforeDeath}: {AchievementPlaybackEnum.RAMPAGE}");
            this._achievementRequestQueue?.Add(AchievementPlaybackEnum.RAMPAGE);
        }
        else if (this._numberOfPlayerKillsBeforeDeath == 5)
        {
            this._log.Debug($"Processing spree {this._numberOfPlayerKillsBeforeDeath}: {AchievementPlaybackEnum.KILLSPREE}");
            this._achievementRequestQueue?.Add(AchievementPlaybackEnum.KILLSPREE);
        }
    }

    private void Reset()
    {
        this._numberOfPlayerKillsBeforeDeath = 0;
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        this.OnPropertyChanged(propertyName);
        return true;
    }

    private void SetupAchievementMediaDictionary()
    {
        this._achievementMediaDictionary = new Dictionary<AchievementPlaybackEnum, AchievementMedia>
        {
            {
                AchievementPlaybackEnum.DEAD,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Misc,
                    AchievementPlaybackEnum.DEAD, "pacmandies.mp3", TimeSpan.FromSeconds(1.75), "YOU'VE BEEN POWNED!")
            },
            {
                AchievementPlaybackEnum.DOMINATING,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Spree,
                    AchievementPlaybackEnum.DOMINATING, "dominating.mp3", TimeSpan.FromSeconds(1))
            },
            {
                AchievementPlaybackEnum.DOUBLE,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Multi,
                    AchievementPlaybackEnum.DOUBLE, "doublekill.mp3", TimeSpan.FromSeconds(1),
                    $"{AchievementPlaybackEnum.DOUBLE} KILL")
            },
            {
                AchievementPlaybackEnum.TRIPPLE,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Multi,
                    AchievementPlaybackEnum.TRIPPLE, "triplekill.mp3", TimeSpan.FromSeconds(1),
                    $"{AchievementPlaybackEnum.TRIPPLE} KILL")
            },
            {
                AchievementPlaybackEnum.GODLIKE,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Spree,
                    AchievementPlaybackEnum.GODLIKE, "godlike.mp3", TimeSpan.FromSeconds(1),
                    "GOD LIKE")
            },
            {
                AchievementPlaybackEnum.HOLYSHIT,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Spree,
                    AchievementPlaybackEnum.HOLYSHIT, "holyshit.mp3", TimeSpan.FromSeconds(3),
                    "HOLY SHIT!")
            },
            {
                AchievementPlaybackEnum.KILLSPREE,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Spree,
                    AchievementPlaybackEnum.KILLSPREE, "killingspree.mp3", TimeSpan.FromSeconds(2),
                    "KILLING SPREE")
            },
            {
                AchievementPlaybackEnum.LUDICROUS,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Multi,
                    AchievementPlaybackEnum.LUDICROUS, "ludicrouskill.mp3", TimeSpan.FromSeconds(4),
                    $"{AchievementPlaybackEnum.LUDICROUS} KILL")
            },
            {
                AchievementPlaybackEnum.MEGA,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Multi,
                    AchievementPlaybackEnum.MEGA, "megakill.mp3", TimeSpan.FromSeconds(2),
                    $"{AchievementPlaybackEnum.MEGA} KILL")
            },
            {
                AchievementPlaybackEnum.MONSTER,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Multi,
                    AchievementPlaybackEnum.MONSTER, "monsterkill.mp3", TimeSpan.FromSeconds(4),
                    $"{AchievementPlaybackEnum.MONSTER} KILL")
            },
            {
                AchievementPlaybackEnum.MULTI,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Multi,
                    AchievementPlaybackEnum.MULTI, "multikill.mp3", TimeSpan.FromSeconds(2),
                    $"{AchievementPlaybackEnum.MULTI} KILL")
            },
            {
                AchievementPlaybackEnum.RAMPAGE,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Spree,
                    AchievementPlaybackEnum.RAMPAGE, "rampage.mp3", TimeSpan.FromSeconds(1))
            },
            {
                AchievementPlaybackEnum.ULTRA,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Multi,
                    AchievementPlaybackEnum.ULTRA, "ultrakill.mp3", TimeSpan.FromSeconds(2),
                    $"{AchievementPlaybackEnum.ULTRA} KILL")
            },
            {
                AchievementPlaybackEnum.UNSTOPPABLE,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Spree,
                    AchievementPlaybackEnum.UNSTOPPABLE, "unstoppable.mp3", TimeSpan.FromSeconds(2))
            },
            {
                AchievementPlaybackEnum.WICKED,
                new AchievementMedia(this._achievementOverlayWindowContext, AchievementTypeEnum.Spree,
                    AchievementPlaybackEnum.WICKED, "wickedsick.mp3", TimeSpan.FromSeconds(2),
                    "WICKED SICK")
            }
        };
    }

    private void StartMultiKillAnnouncementTimer()
    {
        this._timerMultiKillAnnouncement?.Change(this.MultiKillWait, Timeout.InfiniteTimeSpan);
    }

    private void StopMultiKillAnnouncementTimer()
    {
        this._timerMultiKillAnnouncement?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    #endregion
}