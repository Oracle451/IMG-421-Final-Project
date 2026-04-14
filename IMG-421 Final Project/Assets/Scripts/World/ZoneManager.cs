using UnityEngine;

/// <summary>
/// Defines concentric ring zones on the circular map.
/// Each zone has a danger level and spawn parameters.
/// Tracks which zone the player fleet is currently in.
/// </summary>
public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    [System.Serializable]
    public class Zone
    {
        public string Name;
        public float OuterRadius;   // distance from map center
        public float InnerRadius;
        public int DangerLevel;     // 1=Outer, 2=Mid, 3=Inner, 4=Core

        [Header("Spawn Config")]
        public int MaxEnemyShips;
        public int MaxBases;
        public ShipClass EnemyShipClass;
        public float EnemyHealthMultiplier  = 1f;
        public float EnemyDamageMultiplier  = 1f;
    }

    [Header("Map Center")]
    public Vector3 MapCenter = Vector3.zero;

    [Header("Zones (outer → inner)")]
    public Zone[] Zones;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private Zone _currentZone;

    void Update()
    {
        if (GameManager.Instance?.PlayerFleet == null) return;
        Vector3 fleetPos = GameManager.Instance.PlayerFleet.FleetCenter();
        float dist       = Vector3.Distance(new Vector3(fleetPos.x, 0, fleetPos.z),
                                            new Vector3(MapCenter.x,  0, MapCenter.z));

        Zone detected = null;
        foreach (Zone z in Zones)
        {
            if (dist <= z.OuterRadius && dist >= z.InnerRadius)
            { detected = z; break; }
        }

        if (detected != null && detected != _currentZone)
        {
            _currentZone = detected;
            UIManager.Instance?.UpdateZone(_currentZone.Name);
            SpawnManager.Instance?.OnZoneChanged(_currentZone);
        }
    }

    public Zone CurrentZone => _currentZone;

    public Zone GetZoneAtPosition(Vector3 worldPos)
    {
        float dist = Vector3.Distance(new Vector3(worldPos.x, 0, worldPos.z),
                                      new Vector3(MapCenter.x,  0, MapCenter.z));
        foreach (Zone z in Zones)
            if (dist <= z.OuterRadius && dist >= z.InnerRadius) return z;
        return null;
    }

    // ── Debug Gizmos ──────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (Zones == null) return;
        Color[] colors = { Color.green, Color.yellow, Color.red, Color.magenta };
        for (int i = 0; i < Zones.Length; i++)
        {
            Gizmos.color = (i < colors.Length) ? colors[i] : Color.white;
            DrawCircle(MapCenter, Zones[i].OuterRadius);
            DrawCircle(MapCenter, Zones[i].InnerRadius);
        }
    }

    void DrawCircle(Vector3 center, float radius)
    {
        int segments = 64;
        float step   = 360f / segments;
        Vector3 prev = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = step * i * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
