using System;
using System.IO;
using System.Windows.Forms;

using SoundSwitch.Framework.Audio;
using SoundSwitch.Framework.Configuration;

namespace SoundSwitch.UI.Forms;

public sealed partial class SettingsForm
{
    private GroupBox _microphoneSoundsGroupBox;
    private Button _selectMutedSoundButton;
    private Button _selectUnmutedSoundButton;
    private Label _mutedSoundLabel;
    private Label _unmutedSoundLabel;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        EnsureMicrophoneSoundControls();
    }

    private void EnsureMicrophoneSoundControls()
    {
        if (_microphoneSoundsGroupBox != null) return;

        _microphoneSoundsGroupBox = new GroupBox
        {
            Text = "Microphone Sounds",
            Location = new System.Drawing.Point(359, 62),
            Size = new System.Drawing.Size(300, 107),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        var mutedCaption = new Label
        {
            Text = "Muted:",
            AutoSize = true,
            Location = new System.Drawing.Point(9, 27)
        };
        _mutedSoundLabel = new Label
        {
            AutoEllipsis = true,
            Location = new System.Drawing.Point(66, 27),
            Size = new System.Drawing.Size(150, 18)
        };
        _selectMutedSoundButton = new Button
        {
            Text = "Choose...",
            Location = new System.Drawing.Point(220, 22),
            Size = new System.Drawing.Size(72, 25)
        };
        _selectMutedSoundButton.Click += (_, _) => SelectMicrophoneSound(true);

        var unmutedCaption = new Label
        {
            Text = "Unmuted:",
            AutoSize = true,
            Location = new System.Drawing.Point(9, 69)
        };
        _unmutedSoundLabel = new Label
        {
            AutoEllipsis = true,
            Location = new System.Drawing.Point(66, 69),
            Size = new System.Drawing.Size(150, 18)
        };
        _selectUnmutedSoundButton = new Button
        {
            Text = "Choose...",
            Location = new System.Drawing.Point(220, 64),
            Size = new System.Drawing.Size(72, 25)
        };
        _selectUnmutedSoundButton.Click += (_, _) => SelectMicrophoneSound(false);

        _microphoneSoundsGroupBox.Controls.AddRange(new Control[]
        {
            mutedCaption, _mutedSoundLabel, _selectMutedSoundButton,
            unmutedCaption, _unmutedSoundLabel, _selectUnmutedSoundButton
        });
        notificationsTabPage.Controls.Add(_microphoneSoundsGroupBox);
        _microphoneSoundsGroupBox.BringToFront();
        RefreshMicrophoneSoundLabels();
    }

    private void SelectMicrophoneSound(bool muted)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Audio files|*.wav;*.mp3;*.aac;*.m4a",
            CheckFileExists = true,
            CheckPathExists = true,
            Title = muted ? "Choose microphone muted sound" : "Choose microphone unmuted sound"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            // Validate now, so a bad/unsupported file is rejected before saving it.
            _ = new CachedSound(dialog.FileName);
        }
        catch (Exception)
        {
            MessageBox.Show("Please select another audio file.", "Invalid sound file",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (muted)
            MicrophoneSoundConfigs.Configuration.MutedSoundFilePath = dialog.FileName;
        else
            MicrophoneSoundConfigs.Configuration.UnmutedSoundFilePath = dialog.FileName;

        MicrophoneSoundConfigs.Configuration.Save();
        RefreshMicrophoneSoundLabels();
    }

    private void RefreshMicrophoneSoundLabels()
    {
        var mutedPath = MicrophoneSoundConfigs.Configuration.MutedSoundFilePath;
        var unmutedPath = MicrophoneSoundConfigs.Configuration.UnmutedSoundFilePath;

        _mutedSoundLabel.Text = string.IsNullOrWhiteSpace(mutedPath) ? "Default" : Path.GetFileName(mutedPath);
        _unmutedSoundLabel.Text = string.IsNullOrWhiteSpace(unmutedPath) ? "Default" : Path.GetFileName(unmutedPath);
        _mutedSoundLabel.Tag = mutedPath;
        _unmutedSoundLabel.Tag = unmutedPath;
    }
}
