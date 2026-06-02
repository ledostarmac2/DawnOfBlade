namespace DawnOfBlade.UI.Presentation;

/// <summary>Models an immediate foreground gauge and a delayed trailing gauge for damage feedback.</summary>
public sealed class VitalGaugeState
{
    private const double TrailDelaySeconds = 0.3;
    private const double TrailCatchUpSeconds = 0.5;
    private double _trailDelayRemaining;

    public VitalGaugeState(float current, float maximum)
    {
        Apply(current, maximum);
        TrailingValue = Value;
    }

    public float Value { get; private set; }
    public float TrailingValue { get; private set; }
    public float Maximum { get; private set; }

    public void Apply(float current, float maximum)
    {
        Maximum = System.Math.Max(1, maximum);
        var next = System.Math.Clamp(current, 0, Maximum);
        if (next < Value)
        {
            _trailDelayRemaining = TrailDelaySeconds;
        }

        Value = next;
        TrailingValue = System.Math.Max(TrailingValue, Value);
    }

    public void Advance(double deltaSeconds)
    {
        if (deltaSeconds <= 0 || TrailingValue <= Value)
        {
            return;
        }

        if (_trailDelayRemaining > 0)
        {
            _trailDelayRemaining -= deltaSeconds;
            return;
        }

        var step = (float)(Maximum * deltaSeconds / TrailCatchUpSeconds);
        TrailingValue = System.Math.Max(Value, TrailingValue - step);
    }
}
