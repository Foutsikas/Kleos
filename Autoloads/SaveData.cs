using Godot;
using Godot.Collections;

public partial class KleosSaveData : RefCounted
{
    public float CurrentKleos { get; set; } = 0f;
    public float TotalKleosPerSecond { get; set; } = 0f;
}

public partial class ArtisanSaveData : RefCounted
{
    public Dictionary<string, int> OwnedCounts { get; set; } = new();
    public Array<string> UnlockedArtisans { get; set; } = new();
}

public partial class UpgradeSaveData : RefCounted
{
    public Array<string> PurchasedUpgradeIds { get; set; } = new();
}

public partial class DungeonSaveData : RefCounted
{
    public Dictionary<string, int> DungeonProgress { get; set; } = new();
    public Array<string> CompletedDungeons { get; set; } = new();
}

public partial class HeroSaveData : RefCounted
{
    public int Level { get; set; } = 1;
    public float CurrentXP { get; set; } = 0f;
    public int AvailableStatPoints { get; set; } = 0;
    public int StrengthUpgrades { get; set; } = 0;
    public int EnduranceUpgrades { get; set; } = 0;
    public int CunningUpgrades { get; set; } = 0;
    public int FavorUpgrades { get; set; } = 0;
}

public partial class SaveData : RefCounted
{
    public string Version { get; set; } = "1.0";
    public long LastSaveTime { get; set; } = 0;
    public KleosSaveData Kleos { get; set; } = new();
    public ArtisanSaveData Artisans { get; set; } = new();
    public UpgradeSaveData Upgrades { get; set; } = new();
    public DungeonSaveData Dungeons { get; set; } = new();
    public HeroSaveData Hero { get; set; } = new();
}
