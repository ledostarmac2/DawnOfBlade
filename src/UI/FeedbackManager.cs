using Godot;

namespace DawnOfBlade.UI;

/// <summary>Creates lightweight world-space combat feedback for the local sandbox.</summary>
public partial class FeedbackManager : Node3D
{
    public void ShowDamage(Node3D target, int damage, bool landed)
    {
        var label = new Label3D
        {
            Text = landed ? damage.ToString() : "Miss",
            Position = target.GlobalPosition + Vector3.Up * 2.2f,
            Modulate = landed ? new Color(0.95f, 0.22f, 0.18f) : new Color(0.75f, 0.75f, 0.75f),
            FontSize = 30,
            OutlineSize = 6,
            NoDepthTest = true,
        };

        AddChild(label);

        var tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(label, "position", label.Position + Vector3.Up * 1.25f, 0.8);
        tween.TweenProperty(label, "modulate:a", 0.0f, 0.8);
        tween.Chain().TweenCallback(Callable.From(label.QueueFree));
    }
}
