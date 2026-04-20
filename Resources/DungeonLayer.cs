using Godot;

[GlobalClass]
public partial class DungeonLayer : Resource
{
    [Export] public EnemyData Enemy { get; set; }
    [Export] public float BaseKleosReward { get; set; } = 10f;
    [Export] public bool IsBossLayer { get; set; } = false;
    [Export] public bool IsMiniBossLayer { get; set; } = false;
}
