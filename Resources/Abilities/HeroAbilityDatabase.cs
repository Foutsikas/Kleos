using Godot;
using Godot.Collections;

[GlobalClass]
public partial class HeroAbilityDatabase : Resource
{
    [Export] public Array<CombatAbility> Abilities = new();
}