using DawnOfBlade.Communication;

namespace DawnOfBlade.Simulation;

/// <summary>
/// A player or system intent to be resolved on a specific tick (move step, attack, gather, …).
/// Extends the communication layer's <see cref="IMessage"/> so the same command structs that the
/// in-process loop drains today can later be packed into a network packet and replayed on the
/// server tick without redefining the contract. Keep implementations immutable.
/// </summary>
public interface ISimulationCommand : IMessage
{
}
