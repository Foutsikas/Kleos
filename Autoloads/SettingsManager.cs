using Godot;

public partial class SettingsManager : Node
{
    public static SettingsManager Instance { get; private set; }

    // --- Keys ---
    private const string KeyMusicVolume = "music_volume";
    private const string KeySfxVolume = "sfx_volume";
    private const string KeyFullscreen = "fullscreen";
    private const string KeyResolutionIndex = "resolution_index";

    // --- Defaults ---
    private const float DefaultMusicVolume = 1f;
    private const float DefaultSfxVolume = 1f;
    private const bool DefaultFullscreen = false;
    private const int DefaultResolutionIndex = 0;

    // --- State ---
    public float MusicVolume { get; private set; } = DefaultMusicVolume;
    public float SfxVolume { get; private set; } = DefaultSfxVolume;
    public bool Fullscreen { get; private set; } = DefaultFullscreen;
    public int ResolutionIndex { get; private set; } = DefaultResolutionIndex;

    // --- Lifecycle ---

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
        Load();
        ApplyAll();
        GD.Print("[SettingsManager] Ready.");
    }

    // --- Setters ---

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp(value, 0f, 1f);
        ApplyAudio();
        Save();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp(value, 0f, 1f);
        ApplyAudio();
        Save();
    }

    public void SetFullscreen(bool value)
    {
        Fullscreen = value;
        ApplyDisplay();
        Save();
    }

    public void SetResolutionIndex(int value)
    {
        ResolutionIndex = value;
        ApplyDisplay();
        Save();
    }

    // --- Apply ---

    public void ApplyAll()
    {
        ApplyAudio();
        ApplyDisplay();
    }

    private void ApplyAudio()
    {
        // Convert linear 0-1 volume to decibels for Godot's audio bus
        float musicDb = Mathf.LinearToDb(Mathf.Max(MusicVolume, 0.0001f));
        float sfxDb = Mathf.LinearToDb(Mathf.Max(SfxVolume, 0.0001f));

        int musicBus = AudioServer.GetBusIndex("Music");
        int sfxBus = AudioServer.GetBusIndex("SFX");

        if (musicBus >= 0) AudioServer.SetBusVolumeDb(musicBus, musicDb);
        if (sfxBus >= 0) AudioServer.SetBusVolumeDb(sfxBus, sfxDb);
    }

    private void ApplyDisplay()
    {
        DisplayServer.WindowSetMode(
            Fullscreen
                ? DisplayServer.WindowMode.Fullscreen
                : DisplayServer.WindowMode.Windowed
        );
    }

    // --- Persistence ---
    // Settings use ConfigFile stored in user:// rather than the save system.
    // This keeps preferences separate from game progress.

    private void Save()
    {
        var config = new ConfigFile();
        config.SetValue("audio", KeyMusicVolume, MusicVolume);
        config.SetValue("audio", KeySfxVolume, SfxVolume);
        config.SetValue("display", KeyFullscreen, Fullscreen);
        config.SetValue("display", KeyResolutionIndex, ResolutionIndex);
        config.Save("user://settings.cfg");
    }

    private void Load()
    {
        var config = new ConfigFile();
        Error err = config.Load("user://settings.cfg");
        if (err != Error.Ok)
        {
            GD.Print("[SettingsManager] No settings file found. Using defaults.");
            return;
        }

        MusicVolume = (float)config.GetValue("audio", KeyMusicVolume, DefaultMusicVolume);
        SfxVolume = (float)config.GetValue("audio", KeySfxVolume, DefaultSfxVolume);
        Fullscreen = (bool)config.GetValue("display", KeyFullscreen, DefaultFullscreen);
        ResolutionIndex = (int)config.GetValue("display", KeyResolutionIndex, DefaultResolutionIndex);
    }

    // --- Reset ---

    public void ResetToDefaults()
    {
        MusicVolume = DefaultMusicVolume;
        SfxVolume = DefaultSfxVolume;
        Fullscreen = DefaultFullscreen;
        ResolutionIndex = DefaultResolutionIndex;
        ApplyAll();
        Save();
        GD.Print("[SettingsManager] Reset to defaults.");
    }

    public bool HasSaveData() => SaveManager.Instance.HasSaveData();

    public void DeleteSaveData() => SaveManager.Instance.ResetAllSaveData();
}