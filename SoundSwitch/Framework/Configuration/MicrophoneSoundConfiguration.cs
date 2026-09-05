using Newtonsoft.Json;

namespace SoundSwitch.Framework.Configuration;

/// <summary>
/// Custom-fork configuration for separate microphone mute/unmute feedback sounds.
/// Kept in its own configuration file so the upstream SoundSwitch settings schema
/// remains untouched and future upstream merges stay simple.
/// </summary>
public sealed class MicrophoneSoundConfiguration : IConfiguration
{
    public string MutedSoundFilePath { get; set; }
    public string UnmutedSoundFilePath { get; set; }

    [JsonIgnore]
    public string FileLocation { get; set; }

    public bool Migrate() => false;

    public void Save() => ConfigurationManager.SaveConfiguration(this);
}

public static class MicrophoneSoundConfigs
{
    public static MicrophoneSoundConfiguration Configuration { get; } =
        ConfigurationManager.LoadConfiguration<MicrophoneSoundConfiguration>();
}
