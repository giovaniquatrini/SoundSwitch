using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using SoundSwitch.Framework.Audio;
using SoundSwitch.Framework.Configuration;

namespace SoundSwitch.UI.Forms;

public sealed partial class SettingsForm
{
    private GroupBox _microphoneSoundsGroupBox;
    private Label _muteSoundLabel;
    private Label _unmuteSoundLabel;

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
            Location = new Point(359, 62),
            Size = new Size(300, 107),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        var muteCaption = new Label { Text = "Mute", AutoSize = true, Location = new Point(8, 25) };
        _muteSoundLabel = new Label
        {
            AutoEllipsis = true,
            Location = new Point(62, 24),
            Size = new Size(166, 20)
        };
        var muteSelect = new Button { Text = "...", Location = new Point(234, 20), Size = new Size(27, 25) };
        var muteClear = new Button { Text = "X", Location = new Point(265, 20), Size = new Size(27, 25) };

        var unmuteCaption = new Label { Text = "Unmute", AutoSize = true, Location = new Point(8, 67) };
        _unmuteSoundLabel = new Label
        {
            AutoEllipsis = true,
            Location = new Point(62, 66),
            Size = new Size(166, 20)
        };
        var unmuteSelect = new Button { Text = "...", Location = new Point(234, 62), Size = new Size(27, 25) };
        var unmuteClear = new Button { Text = "X", Location = new Point(265, 62), Size = new Size(27, 25) };

        muteSelect.Click += (_, _) => SelectMicrophoneSound(true);
        unmuteSelect.Click += (_, _) => SelectMicrophoneSound(false);
        muteClear.Click += (_, _) => ClearMicrophoneSound(true);
        unmuteClear.Click += (_, _) => ClearMicrophoneSound(false);

        _microphoneSoundsGroupBox.Controls.AddRange(new Control[]
        {
            muteCaption, _muteSoundLabel, muteSelect, muteClear,
            unmuteCaption, _unmuteSoundLabel, unmuteSelect, unmuteClear
        });
        notificationsTabPage.Controls.Add(_microphoneSoundsGroupBox);
        RefreshMicrophoneSoundLabels();
    }

    private void SelectMicrophoneSound(bool muted)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Audio files|*.wav;*.mp3;*.aac;*.m4a",
            CheckFileExists = true,
            CheckPathExists = true,
            Title = muted ? "Select microphone mute sound" : "Select microphone unmute sound"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            // Decode once now so invalid/unsupported files are rejected immediately.
            _ = new CachedSound(dialog.FileName);
        }
        catch (Exception)
        {
            MessageBox.Show("Please select another file", "Invalid Sound file",
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

    private void ClearMicrophoneSound(bool muted)
    {
        if (muted)
            MicrophoneSoundConfigs.Configuration.MutedSoundFilePath = null;
        else
            MicrophoneSoundConfigs.Configuration.UnmutedSoundFilePath = null;

        MicrophoneSoundConfigs.Configuration.Save();
        RefreshMicrophoneSoundLabels();
    }

    private void RefreshMicrophoneSoundLabels()
    {
        var mutePath = MicrophoneSoundConfigs.Configuration.MutedSoundFilePath;
        var unmutePath = MicrophoneSoundConfigs.Configuration.UnmutedSoundFilePath;

        _muteSoundLabel.Text = string.IsNullOrWhiteSpace(mutePath) ? "Default" : Path.GetFileName(mutePath);
        _unmuteSoundLabel.Text = string.IsNullOrWhiteSpace(unmutePath) ? "Default" : Path.GetFileName(unmutePath);
        _muteSoundLabel.Tag = mutePath;
        _unmuteSoundLabel.Tag = unmutePath;

        var toolTip = new ToolTip();
        toolTip.SetToolTip(_muteSoundLabel, mutePath ?? "Built-in mute sound");
        toolTip.SetToolTip(_unmuteSoundLabel, unmutePath ?? "Built-in unmute sound");
    }
}
