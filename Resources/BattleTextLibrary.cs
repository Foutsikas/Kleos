using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BattleTextLibrary : Resource
{
    [ExportGroup("Hero Attack")]
    [Export] public Array<string> HeroAttackLines { get; set; } = new();
    [Export] public Array<string> HeroCritLines { get; set; } = new();
    [Export] public Array<string> HeroDodgeLines { get; set; } = new();

    [ExportGroup("Enemy Attack")]
    [Export] public Array<string> EnemyAttackLines { get; set; } = new();
    [Export] public Array<string> EnemyAnticipationLines { get; set; } = new();

    [ExportGroup("Victory")]
    [Export] public Array<string> VictoryLines { get; set; } = new();
    [Export] public Array<string> VictorySubtitles { get; set; } = new();

    [ExportGroup("Defeat")]
    [Export] public Array<string> DefeatLines { get; set; } = new();
    [Export] public Array<string> DefeatSubtitles { get; set; } = new();
    [Export] public Array<string> DefeatConsolations { get; set; } = new();

    // --- Accessors with fallbacks ---

    private string GetRandom(Array<string> pool, string fallback)
    {
        if (pool == null || pool.Count == 0)
            return fallback;
        return pool[GD.RandRange(0, pool.Count - 1)];
    }

    public string GetRandomHeroAttack() =>
        GetRandom(HeroAttackLines, "You strike with purpose.");

    public string GetRandomHeroCrit() =>
        GetRandom(HeroCritLines, "A devastating blow!");

    public string GetRandomHeroDodge() =>
        GetRandom(HeroDodgeLines, "You sidestep the attack.");

    public string GetRandomEnemyAttack(string enemyName) =>
        string.Format(GetRandom(EnemyAttackLines, "The {0} strikes at you."), enemyName);

    public string GetRandomEnemyAnticipation(string enemyName) =>
        string.Format(GetRandom(EnemyAnticipationLines, "The {0} prepares to attack."), enemyName);

    public string GetRandomVictoryLine(string enemyName) =>
        string.Format(GetRandom(VictoryLines, "You have defeated the {0}."), enemyName);

    public string GetRandomVictorySubtitle() =>
        GetRandom(VictorySubtitles, "The deed is done.");

    public string GetRandomDefeatLine(string enemyName) =>
        string.Format(GetRandom(DefeatLines, "The {0} has bested you."), enemyName);

    public string GetRandomDefeatSubtitle() =>
        GetRandom(DefeatSubtitles, "The fates are cruel.");

    public string GetRandomConsolation() =>
        GetRandom(DefeatConsolations, "Even heroes fall.");
}
