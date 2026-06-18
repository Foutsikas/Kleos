using Godot;

public partial class StatusEffectDisplay : VBoxContainer
{
	// Color for buff tags
	private static readonly Color BuffColor = new Color("7A8A3A");
	// Color for debuff tags
	private static readonly Color DebuffColor = new Color("8A3A3A");
	// Font size for effect tags
	private const int TagFontSize = 11;

	// Refresh the display from a StatusEffectManager's active effects.
	// Call this at the start of each round and after any effect changes.
	public void Refresh(StatusEffectManager manager)
	{
		// Remove all existing tags
		foreach (Node child in GetChildren())
			child.QueueFree();

		if (manager == null) return;

		var effects = manager.GetActiveEffects();
		if (effects.Count == 0) return;

		for (int i = 0; i < effects.Count; i++)
		{
			StatusEffect effect = effects[i];
			var label = new Label();

			// Format: "EffectName (N)" where N is remaining rounds
			string stackText = effect.CurrentStacks > 1
				? $" x{effect.CurrentStacks}"
				: "";
			label.Text = $"{effect.EffectName} ({effect.Duration}){stackText}";

			label.AddThemeFontSizeOverride("font_size", TagFontSize);

			Color tagColor = effect.IsDebuff ? DebuffColor : BuffColor;
			label.Modulate = tagColor;

			AddChild(label);
		}
	}

	// Clear all effect tags. Call on battle end.
	public void Clear()
	{
		foreach (Node child in GetChildren())
			child.QueueFree();
	}
}