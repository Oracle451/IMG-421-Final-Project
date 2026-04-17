using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Renders a minimap by projecting world positions onto a RawImage UI element.
// Tracks player ships (blue), enemies (red), and bases (orange).

// Setup: Create a RawImage in your Canvas for the minimap background,
// then assign it to MinimapBackground. Create small Image prefabs for the dots.
public class MinimapController : MonoBehaviour
{
    [Header("UI")]
    public RawImage MinimapBackground;
    public RectTransform MinimapRect;

    [Header("Dot Prefabs")]
    public GameObject PlayerDotPrefab;
    public GameObject EnemyDotPrefab;
    public GameObject BaseDotPrefab;

    [Header("World Scale")]
    public float WorldRadius = 200f; // half the map diameter

    // Runtime

    private readonly List<(Transform world, RectTransform dot, bool isPlayer)> _tracked = new();

    void Update()
    {
        if (MinimapRect == null) return;

        // Refresh player dots
        SyncPlayerDots();

        // Update positions of all tracked objects
        foreach (var (world, dot, _) in _tracked)
        {
            if (world == null || dot == null) continue;
            dot.anchoredPosition = WorldToMinimap(world.position);
        }

        // Remove null entries
        _tracked.RemoveAll(t => t.Item1 == null || t.Item2 == null);
    }

    void SyncPlayerDots()
    {
        // To be completed, a full implementation would diff the fleet list each frame
    }

    // Public registration

    public void RegisterShip(Transform shipTransform, bool isPlayer)
    {
        GameObject prefab = isPlayer ? PlayerDotPrefab : EnemyDotPrefab;
        if (prefab == null || MinimapRect == null) return;

        GameObject dot    = Instantiate(prefab, MinimapRect);
        RectTransform rt  = dot.GetComponent<RectTransform>();
        _tracked.Add((shipTransform, rt, isPlayer));
    }

    public void RegisterBase(Transform baseTransform)
    {
        if (BaseDotPrefab == null || MinimapRect == null) return;
        GameObject dot   = Instantiate(BaseDotPrefab, MinimapRect);
        RectTransform rt = dot.GetComponent<RectTransform>();
        _tracked.Add((baseTransform, rt, false));
    }

    // Coordinate mapping

    Vector2 WorldToMinimap(Vector3 worldPos)
    {
        float halfW = MinimapRect.rect.width * 0.5f;
        float halfH = MinimapRect.rect.height * 0.5f;
        float x = (worldPos.x / WorldRadius) * halfW;
        float z = (worldPos.z / WorldRadius) * halfH;
        return new Vector2(x, z);
    }
}
