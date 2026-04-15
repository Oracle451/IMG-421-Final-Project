using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally spawns enemy ships and fleets based on the player's current zone.
/// Uses object pooling concepts – maintains active enemy count within zone limits.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Enemy Prefabs")]
    public GameObject EnemySchoonerPrefab;
    public GameObject EnemyFrigatePrefab;
    public GameObject EnemyManOWarPrefab;

    [Header("Ocean Base Prefab")]
    public GameObject OceanBasePrefab;

    [Header("Spawn Config")]
    public float SpawnCheckInterval = 10f;
    public float MinSpawnDistFromPlayer = 30f;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private ZoneManager.Zone _currentZone;
    private readonly List<GameObject> _activeEnemies = new();
    private readonly List<GameObject> _activeBases   = new();
    private float _spawnTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= SpawnCheckInterval)
        {
            _spawnTimer = 0f;
            CleanDestroyedEntries();
            TrySpawnEnemies();
        }
    }

    // ── Zone Change ──────────────────────────────────────────────────────────

    public void OnZoneChanged(ZoneManager.Zone newZone)
    {
        _currentZone = newZone;
        _spawnTimer  = SpawnCheckInterval - 2f;  // trigger spawn soon
    }

    // ── Spawning ─────────────────────────────────────────────────────────────

    void TrySpawnEnemies()
    {
        if (_currentZone == null) return;

        int missing = _currentZone.MaxEnemyShips - _activeEnemies.Count;
        for (int i = 0; i < missing; i++)
            SpawnEnemy(_currentZone);

        if (_activeBases.Count < _currentZone.MaxBases)
            SpawnBase(_currentZone);
    }

    void SpawnEnemy(ZoneManager.Zone zone)
    {
        Vector3 pos = GetSpawnPosition(zone);
        if (pos == Vector3.zero) return;

        GameObject prefab = zone.EnemyShipClass switch
        {
            ShipClass.Schooner => EnemySchoonerPrefab,
            ShipClass.ManOWar  => EnemyManOWarPrefab,
            _                  => EnemyFrigatePrefab
        };
        if (prefab == null) return;

        GameObject go   = Instantiate(prefab, pos, Random.rotation);
        ShipBase ship   = go.GetComponent<ShipBase>();
        if (ship != null)
        {
            ship.Faction = ShipFaction.Enemy;
            // Scale stats by zone multipliers (applied via a simple stat scaler)
            go.AddComponent<ZoneStatScaler>().Init(zone.EnemyHealthMultiplier, zone.EnemyDamageMultiplier);
        }

        // Wire defense anchor if near a base
        EnemyShipAI ai = go.GetComponent<EnemyShipAI>();
        if (ai != null && _activeBases.Count > 0)
        {
            int idx = Random.Range(0, _activeBases.Count);
            if (_activeBases[idx] != null)
                ai.DefenseAnchor = _activeBases[idx].transform;
        }

        _activeEnemies.Add(go);
    }

    void SpawnBase(ZoneManager.Zone zone)
    {
        if (OceanBasePrefab == null) return;
        Vector3 pos = GetSpawnPosition(zone);
        if (pos == Vector3.zero) return;

        GameObject go = Instantiate(OceanBasePrefab, pos, Quaternion.identity);
        _activeBases.Add(go);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    Vector3 GetSpawnPosition(ZoneManager.Zone zone)
    {
        Vector3 center    = ZoneManager.Instance?.MapCenter ?? Vector3.zero;
        Vector3 playerPos = GameManager.Instance?.PlayerFleet?.FleetCenter() ?? Vector3.zero;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(zone.InnerRadius, zone.OuterRadius);
            Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            if (Vector3.Distance(candidate, playerPos) >= MinSpawnDistFromPlayer)
                return candidate;
        }
        return Vector3.zero;
    }

    void CleanDestroyedEntries()
    {
        _activeEnemies.RemoveAll(g => g == null);
        _activeBases.RemoveAll(g   => g == null);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Temporarily scales a spawned enemy's stats at runtime per-zone difficulty.
/// </summary>
public class ZoneStatScaler : MonoBehaviour
{
    public void Init(float healthMult, float damageMult)
    {
        ShipBase ship = GetComponent<ShipBase>();
        if (ship == null) return;
        // We poke the private field via reflection to keep ShipBase clean,
        // OR expose a setter. For simplicity we use a public override:
        StartCoroutine(ApplyScaling(ship, healthMult, damageMult));
    }

    System.Collections.IEnumerator ApplyScaling(ShipBase ship, float hm, float dm)
    {
        yield return null;  // wait one frame for ShipBase.Start to set CurrentHealth
        // Can't directly set stats without reflection; instead route through a public method
        ship.ScaleZoneStats(hm, dm);
    }
}
