using Godot;

[GlobalClass]
public partial class HeroData : Resource
{
    [ExportGroup("Progression")]
    [Export] public int Level { get; set; } = 1;
    [Export] public float CurrentXP { get; set; } = 0f;
    [Export] public int AvailableStatPoints { get; set; } = 0;

    [ExportGroup("Stat Upgrades")]
    [Export] public int StrengthUpgrades { get; set; } = 0;
    [Export] public int EnduranceUpgrades { get; set; } = 0;
    [Export] public int CunningUpgrades { get; set; } = 0;
    [Export] public int FavorUpgrades { get; set; } = 0;

    [ExportGroup("Base Stats")]
    [Export] public int BaseStrength { get; set; } = 10;
    [Export] public int BaseEndurance { get; set; } = 10;
    [Export] public int BaseCunning { get; set; } = 5;
    [Export] public int BaseFavor { get; set; } = 0;

    // --- Calculated stat accessors ---

    public int GetStrength() => BaseStrength + StrengthUpgrades;
    public int GetEndurance() => BaseEndurance + EnduranceUpgrades;
    public int GetCunning() => BaseCunning + CunningUpgrades;
    public int GetFavor() => BaseFavor + FavorUpgrades;

    public float GetMaxHP() => 40f + (GetEndurance() * 5f);
    public float GetDamage() => 3f + (GetStrength() * 1.0f);
    public float GetDodgeChance() => Mathf.Clamp(GetCunning() * 0.01f, 0f, 0.3f);
    public float GetCritChance() => Mathf.Clamp((GetCunning() * 0.005f) + (GetFavor() * 0.01f), 0f, 0.25f);
    public float GetCritMultiplier() => 2.0f + (GetFavor() * 0.1f);
}
