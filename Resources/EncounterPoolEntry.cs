using Godot;

[GlobalClass]
public partial class EncounterPoolEntry : Resource
{
    [Export] public EnemyData Enemy { get; set; }
    [Export] public float Weight { get; set; } = 1.0f;
}
