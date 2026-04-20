using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EnemyData : Resource
{
    [ExportGroup("Identity")]
    [Export] public string EnemyName { get; set; } = "";
    [Export] public string EnemyId { get; set; } = "";
    [Export] public Texture2D EnemySprite { get; set; }

    [ExportGroup("Combat")]
    [Export] public float Health { get; set; } = 100f;
    [Export] public float DamagePerSecond { get; set; } = 3f;
    [Export] public float AttackRate { get; set; } = 1.5f;
    [Export] public int KleosReward { get; set; } = 0;

    [ExportGroup("Flavor")]
    [Export] public Array<string> EncounterFlavorTexts { get; set; } = new();
}
