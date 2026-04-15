using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns a coordinated enemy fleet using BoidsFlock group behavior.
/// Attach to an empty GameObject placed in the scene or spawned at runtime.
/// </summary>
public class EnemyFleetSpawner : MonoBehaviour
{
    [Header("Fleet Composition")]
    public GameObject LeaderShipPrefab;     // Man-O-War typically
    public GameObject EscortShipPrefab;     // Frigates / Schooners
    public int EscortCount = 4;
    public float SpawnRadius = 8f;

    [Header("Behavior")]
    public EnemyShipAI.AIState InitialFleetState = EnemyShipAI.AIState.Patrol;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private BoidsFlock _flock;
    private readonly List<ShipBase> _fleetShips = new();

    void Start()
    {
        // Create flock manager on this GameObject
        _flock = gameObject.AddComponent<BoidsFlock>();

        // Spawn leader
        if (LeaderShipPrefab != null)
        {
            GameObject leader    = Instantiate(LeaderShipPrefab, transform.position, Quaternion.identity, transform);
            _flock.LeaderTarget  = leader.transform;
            RegisterShip(leader);
        }

        // Spawn escorts
        for (int i = 0; i < EscortCount; i++)
        {
            if (EscortShipPrefab == null) break;
            float angle  = (360f / EscortCount) * i * Mathf.Deg2Rad;
            Vector3 pos  = transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * SpawnRadius;
            GameObject escort = Instantiate(EscortShipPrefab, pos, Quaternion.identity, transform);
            RegisterShip(escort);
        }
    }

    void RegisterShip(GameObject go)
    {
        ShipBase ship = go.GetComponent<ShipBase>();
        if (ship != null)
        {
            ship.Faction = ShipFaction.Enemy;
            ship.ApplyFactionLayer();
            ship.OnShipDestroyed += OnFleetShipDestroyed;
            _fleetShips.Add(ship);
        }

        EnemyShipAI ai = go.GetComponent<EnemyShipAI>();
        if (ai != null) ai.InitialState = InitialFleetState;

        // Register with boids
        BoidAgent agent = go.GetComponent<BoidAgent>();
        if (agent == null) agent = go.AddComponent<BoidAgent>();
        _flock.RegisterAgent(agent);
    }

    void OnFleetShipDestroyed(ShipBase ship)
    {
        _fleetShips.Remove(ship);
        _flock.UnregisterAgent(ship.GetComponent<BoidAgent>());
        // Fleet is eliminated when all ships are gone
        if (_fleetShips.Count == 0)
            Destroy(gameObject, 1f);
    }
}
