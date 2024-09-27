// Copyright (c) 2024, Todd Taylor (https://github.com/zxeltor)
// All rights reserved.
// 
// This source code is licensed under the Apache-2.0-style license found in the
// LICENSE file in the root directory of this source tree.

using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace zxeltor.StoCombat.Realtime.Classes;

public class AchievementMedia
{
    #region Constructors

    public AchievementMedia(AchievementOverlayWindowContext context, AchievementTypeEnum typeEnum,
        AchievementPlaybackEnum playbackEnum, string audioFile,
        TimeSpan audioDelay, string? displayText = null)
    {
        this.TypeEnum = typeEnum;
        this.PlaybackEnum = playbackEnum;
        this.DisplayText = displayText ?? playbackEnum.ToString();

        this.AudioDuration = audioDelay;

        string audioSubFolder;

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
            audioSubFolder =
                Path.Combine(Path.GetDirectoryName(entryAssembly.Location) ?? Environment.CurrentDirectory,
                    "AudioFiles");
        else
            audioSubFolder = Path.Combine(Environment.CurrentDirectory, "AudioFiles");

        if (File.Exists(Path.Combine(audioSubFolder, audioFile)))
        {
            this.AudioFile = new Uri(Path.Combine(audioSubFolder, audioFile));
            this.MediaPlayer = new MediaPlayer();
            this.MediaPlayer.Open(this.AudioFile);
        }
    }

    #endregion

    #region Public Properties

    public AchievementPlaybackEnum PlaybackEnum { get; set; }
    public AchievementTypeEnum TypeEnum { get; set; }
    public MediaPlayer? MediaPlayer { get; }
    public string DisplayText { get; set; }
    public TimeSpan AudioDuration { get; set; }
    public Uri? AudioFile { get; set; }

    #endregion
}