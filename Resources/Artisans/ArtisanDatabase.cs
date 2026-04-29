using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ArtisanDatabase : Resource
{
    [Export] public Array<ArtisanData> Artisans = new();
}