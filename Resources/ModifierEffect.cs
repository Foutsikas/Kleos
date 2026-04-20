using Godot;

[GlobalClass]
public partial class ModifierEffect : Resource
{
    [Export] public ModifierType Type { get; set; }
    [Export] public float Value { get; set; } = 0f;
    [Export] public ModifierMode Mode { get; set; } = ModifierMode.Flat;
    [Export] public string TargetId { get; set; } = "";
}
