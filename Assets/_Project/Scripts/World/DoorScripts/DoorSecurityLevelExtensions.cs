/// <summary>
/// Security level for doors.
/// Determines which entities can pass through.
/// </summary>
public enum DoorSecurityLevel
{
    Low,    // Enemies can open (patrol routes, public areas)
    Medium, // Enemies can open only when chasing (restricted areas)
    High    // Enemies cannot open (player-only areas)
}

/// <summary>
/// Extension methods for DoorSecurityLevel.
/// </summary>
public static class DoorSecurityLevelExtensions
{
    public static bool CanEnemyOpen(this DoorSecurityLevel level)
    {
        switch (level)
        {
            case DoorSecurityLevel.Low:
                return true; // Always open for enemies

            case DoorSecurityLevel.Medium:
                return true;

            case DoorSecurityLevel.High:
                return false; // Never open for enemies

            default:
                return false;
        }
    }
}