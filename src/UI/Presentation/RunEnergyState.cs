namespace DawnOfBlade.UI.Presentation;

/// <summary>Presentation state for authoritative run-energy snapshots and local toggle availability.</summary>
public sealed class RunEnergyState
{
    public const float ReenableThreshold = 15.0f;

    public float Energy { get; private set; } = 100.0f;
    public bool IsRunning { get; private set; }
    public bool CanToggleRun { get; private set; } = true;

    public void ApplyAuthoritative(float energy, bool isRunning)
    {
        Energy = System.Math.Clamp(energy, 0, 100);
        if (Energy <= 0)
        {
            IsRunning = false;
            CanToggleRun = false;
            return;
        }

        if (!CanToggleRun && Energy >= ReenableThreshold)
        {
            CanToggleRun = true;
        }

        IsRunning = isRunning && CanToggleRun;
    }
}
