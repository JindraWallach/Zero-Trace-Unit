using System;

/// <summary>
/// Interface for objects that can be hacked.
/// Registers with HackManager on spawn.
/// </summary>
public interface IHackTarget
{
    string TargetID { get; }
    bool IsHackable { get; }
    void RequestHack(Action onSuccess, Action onFail);
}