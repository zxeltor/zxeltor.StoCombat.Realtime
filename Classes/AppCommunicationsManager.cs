// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using zxeltor.StoCombat.Lib.Classes;
using zxeltor.Types.Lib.Logging;
using zxeltor.Types.Lib.Result;

namespace zxeltor.StoCombat.Realtime.Classes;

/// <summary>
///     A singleton class used to handle communications between various services and the main ui.
/// </summary>
public sealed class AppCommunicationsManager
{
    #region Static Fields and Constants

    private static AppCommunicationsManager? _instance;
    private static readonly object Padlock = new();

    #endregion

    #region Constructors

    /// <summary>
    ///     A private constructor to block creating instances outside of the singleton pattern.
    /// </summary>
    private AppCommunicationsManager()
    {
    }

    #endregion

    #region Public Properties

    public static AppCommunicationsManager Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ??= new AppCommunicationsManager();
            }
        }
    }

    #endregion

    #region Public Members

    public event EventHandler<AchievementEvent>? AchievementEvent;
    public event EventHandler<DataGridRowContext>? Notification;
    public event EventHandler<AchievementPlaybackEnum>? PlayAchievementAudioRequest;

    public void SendAchievementAudio(object sender, AchievementPlaybackEnum achievementPlaybackEnum)
    {
        this.PlayAchievementAudioRequest?.Invoke(sender, achievementPlaybackEnum);
    }

    public void SendAchievementEvent(object sender, AchievementEvent achievementEvent)
    {
        this.AchievementEvent?.Invoke(sender, achievementEvent);
    }

    public void SendNotification(object sender, string message, ResultLevel resultLevel = ResultLevel.Info)
    {
        this.Notification?.Invoke(sender, new DataGridRowContext(System.DateTime.Now, message, resultLevel));
    }

    public void SendNotification(object sender, string message, Exception exception,
        ResultLevel resultLevel = ResultLevel.Error)
    {
        this.Notification?.Invoke(sender, new DataGridRowContext(System.DateTime.Now, message, exception, resultLevel));
    }

    public void SendNotification(object sender, Exception exception, ResultLevel resultLevel = ResultLevel.Error)
    {
        this.Notification?.Invoke(sender, new DataGridRowContext(System.DateTime.Now, exception, resultLevel));
    }

    #endregion
}