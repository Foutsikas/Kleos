using Godot;
using Godot.Collections;

[GlobalClass]
public partial class UpgradesDatabase : Resource
{
    [Export] public Array<UpgradeConfig> Upgrades = new();
}