/********************************************************************
 * Copyright (C) 2015-2017 Antoine Aflalo
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 ********************************************************************/

using System;
using System.Drawing;
using System.IO;
using System.Threading;

using NAudio.Wave;
using SoundSwitch.Audio.Manager.Interop.Enum;

using SoundSwitch.Audio.Manager;
using SoundSwitch.Common.Framework.Audio.Device;
using SoundSwitch.Framework.Audio;
using SoundSwitch.Framework.Audio.Play;
using SoundSwitch.Framework.NotificationManager.Notification.Configuration;
using SoundSwitch.Framework.Telemetry;
using SoundSwitch.Framework.Threading;
using SoundSwitch.Localization;
using SoundSwitch.Model;

namespace SoundSwitch.Framework.NotificationManager.Notification;

internal class NotificationSound : INotification
{
    private CancellationTokenSource _cancellationTokenSource;
    public NotificationType TypeEnum => NotificationType.SoundNotification;
    public string Label => SettingsStrings.notification_option_sound;
    public INotificationConfiguration Configuration { get; set; }

    public void NotifyDefaultChanged(DeviceFullInfo audioDevice)
    {
        if (audioDevice.Type != EDataFlow.eRender) return;

        TelemetryService.TrackNotificationSound();

        CachedSound soundNotification;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        if (CustomSoundCheck(audioDevice))
            soundNotification = Configuration.CustomSound;
        else
            soundNotification = new CachedSound(GetStreamCopy());

        JobScheduler.Instance.ScheduleJob(new PlaySoundJob(audioDevice.Id, soundNotification), _cancellationTokenSource.Token);
    }

    public void NotifyProfileChanged(Profile.Profile profile, Bitmap icon, uint? processId)
    {
        if (profile.Playback == null) return;

        try
        {
            var audioDevice = AudioSwitcher.Instance.GetDevice(profile.Playback.Id);
            if (audioDevice == null) return;
            // The AudioDevice properties are an immutable snapshot — safe to read off the ComThread.
            // The DeviceFullInfo owns the AudioDevice; NotifyDefaultChanged only reads it, so the
            // using disposes both here (previously the device leaked on this path).
            using var device = new DeviceFullInfo(audioDevice);
            NotifyDefaultChanged(device);
        }
        catch (Exception)
        {
            //Ignored
        }
    }

    public void NotifyAppRuleMatched(AppSoundRule rule, DeviceFullInfo playback, DeviceFullInfo recording, Bitmap icon, uint processId)
    {
        if (playback != null)
        {
            NotifyDefaultChanged(playback);
        }
    }

    public void NotifyMicrophoneMuteChanged(string deviceId, string microphoneName, bool newMuteState)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        // Discord-style audible feedback: use a distinct short tone for each
        // microphone state so the user can know the result without looking at
        // the screen. Mute is a descending tone; unmute is an ascending tone.
        var soundNotification = CreateMicrophoneStateSound(newMuteState);

        JobScheduler.Instance.ScheduleJob(new PlaySoundJob(null, soundNotification), _cancellationTokenSource.Token);
    }

    public void OnSoundChanged(CachedSound newSound) => Configuration.CustomSound = newSound;

    public bool SupportCustomSound() => true;

    public bool IsAvailable() => true;

    public bool CustomSoundCheck(DeviceFullInfo audioDevice) => audioDevice.Type == EDataFlow.eRender && Configuration.CustomSound != null && File.Exists(Configuration.CustomSound.FilePath);

    private bool HasCustomSound() =>
        Configuration.CustomSound != null && File.Exists(Configuration.CustomSound.FilePath);

    private static CachedSound CreateMicrophoneStateSound(bool isMuted)
    {
        const int sampleRate = 44100;
        const int channels = 1;
        const double durationSeconds = 0.18;
        const float volume = 0.22f;

        var totalSamples = (int)(sampleRate * durationSeconds);
        var audioBytes = new byte[totalSamples * sizeof(float)];

        // Unmute rises; mute falls. The glide makes the two states immediately
        // distinguishable while remaining short and unobtrusive.
        var startFrequency = isMuted ? 880.0 : 520.0;
        var endFrequency = isMuted ? 520.0 : 880.0;
        double phase = 0;

        for (var i = 0; i < totalSamples; i++)
        {
            var progress = (double)i / Math.Max(1, totalSamples - 1);
            var frequency = startFrequency + ((endFrequency - startFrequency) * progress);
            phase += 2.0 * Math.PI * frequency / sampleRate;

            var attack = Math.Min(1.0, progress / 0.08);
            var release = Math.Min(1.0, (1.0 - progress) / 0.28);
            var envelope = Math.Min(attack, release);
            var sample = (float)(Math.Sin(phase) * envelope * volume);

            BitConverter.TryWriteBytes(audioBytes.AsSpan(i * sizeof(float), sizeof(float)), sample);
        }

        using var stream = new MemoryStream(audioBytes, writable: false);
        return new CachedSound(stream, WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels));
    }

    private MemoryStream GetStreamCopy()
    {
        lock (this)
        {
            Configuration.DefaultSound.Position = 0;
            var memoryStreamedSound = new MemoryStream();
            Configuration.DefaultSound.CopyTo(memoryStreamedSound);
            memoryStreamedSound.Position = 0;
            return memoryStreamedSound;
        }
    }
}
