using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally places island GameObjects within each zone ring at startup.
/// Islands can have optional CoastalTurret children.
/// </summary>
public class IslandGenerator : MonoBehaviour
{
    [System.Serializable]
    public class IslandZoneConfig
    {
        public float InnerRadius;
        public float OuterRadius;
        public int   IslandCount;
        public int   TurretsPerIsland;
        public GameObject IslandPrefab;
        public GameObject TurretPrefab;
    }

    [Header("Island Zone Configs")]
    public List<IslandZoneConfig> Zones = new();

    [Header("Map Center")]
    public Vector3 MapCenter = Vector3.zero;

    [Header("Min distance between islands")]
    public float MinIslandSeparation = 20f;

    void Start()
    {
        foreach (IslandZoneConfig zone in Zones)
            PlaceIslands(zone);
    }

    void PlaceIslands(IslandZoneConfig zone)
    {
        if (zone.IslandPrefab == null) return;

        List<Vector3> placed = new();

        for (int i = 0; i < zone.IslandCount * 10 && placed.Count < zone.IslandCount; i++)
        {
            float angle    = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius   = Random.Range(zone.InnerRadius, zone.OuterRadius);
            Vector3 candidate = MapCenter + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            // Check separation
            bool tooClose = false;
            foreach (Vector3 p in placed)
                if (Vector3.Distance(p, candidate) < MinIslandSeparation) { tooClose = true; break; }

            if (tooClose) continue;

            placed.Add(candidate);
            GameObject island = Instantiate(zone.IslandPrefab, candidate, Quaternion.Euler(0, Random.Range(0f, 360f), 0));

            // Spawn turrets as children
            for (int t = 0; t < zone.TurretsPerIsland; t++)
            {
                if (zone.TurretPrefab == null) break;
                float ta     = (360f / zone.TurretsPerIsland) * t * Mathf.Deg2Rad;
                Vector3 tPos = candidate + new Vector3(Mathf.Cos(ta), 0, Mathf.Sin(ta)) * 3f;
                Instantiate(zone.TurretPrefab, tPos, Quaternion.identity, island.transform);
            }
        }
    }
}
