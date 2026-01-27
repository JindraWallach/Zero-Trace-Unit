using UnityEngine;

/// <summary>
/// Runtime manager for player class system.
/// Singleton that persists selected class and spawns player.
/// SRP: Class instance management only.
/// </summary>
public class PlayerClassManager : MonoBehaviour
{
    public static PlayerClassManager Instance { get; private set; }

    [Header("Selected Class")]
    [SerializeField] private PlayerClassConfig selectedClass;

    [Header("Spawn Settings")]
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private GameObject spawnedPlayer;

    public PlayerClassConfig SelectedClass => selectedClass;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Set selected class (called from menu).
    /// </summary>
    public void SetSelectedClass(PlayerClassConfig classConfig)
    {
        if (classConfig == null)
        {
            Debug.LogError("[PlayerClassManager] Cannot set null class!");
            return;
        }

        selectedClass = classConfig;

        if (debugLog)
            Debug.Log($"[PlayerClassManager] Selected class: {selectedClass.className}");
    }

    /// <summary>
    /// Spawn player with selected class prefab.
    /// Called when game scene loads.
    /// </summary>
    public GameObject SpawnPlayer()
    {
        if (selectedClass == null)
        {
            Debug.LogError("[PlayerClassManager] No class selected! Cannot spawn player.");
            return null;
        }

        if (selectedClass.playerPrefab == null)
        {
            Debug.LogError($"[PlayerClassManager] {selectedClass.className} has no prefab assigned!");
            return null;
        }

        if (playerSpawnPoint == null)
        {
            Debug.LogWarning("[PlayerClassManager] No spawn point assigned, spawning at origin.");
            playerSpawnPoint = transform;
        }

        // Destroy previous player if exists
        if (spawnedPlayer != null)
        {
            Destroy(spawnedPlayer);
        }

        // Spawn new player
        spawnedPlayer = Instantiate(
            selectedClass.playerPrefab,
            playerSpawnPoint.position,
            playerSpawnPoint.rotation
        );

        if (debugLog)
            Debug.Log($"[PlayerClassManager] Spawned player: {selectedClass.className}");

        return spawnedPlayer;
    }

    /// <summary>
    /// Get spawned player instance.
    /// </summary>
    public GameObject GetPlayer()
    {
        return spawnedPlayer;
    }

    /// <summary>
    /// Check if player has been spawned.
    /// </summary>
    public bool IsPlayerSpawned()
    {
        return spawnedPlayer != null;
    }
}