using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Manages the player's fleet: selection, movement orders, formation, and win/lose tracking.
/// Attach to a persistent GameObject in the scene.
/// </summary>
public class PlayerFleet : MonoBehaviour
{
    [Header("Ship Prefabs (assign in Inspector)")]
    public GameObject SchoonerPrefab;
    public GameObject FrigatePrefab;
    public GameObject ManOWarPrefab;
    private GameObject _activeMoveMarker;

    [Header("Starting Fleet")]
    public ShipClass StartingShipClass = ShipClass.Frigate;
    public int StartingShipCount = 1;

    [Header("Formation")]
    public float FormationSpacing = 4f;

    [Header("Move Indicator")]
    public GameObject MoveMarkerPrefab;

    // ── Runtime ──────────────────────────────────────────────────────────────

    public List<ShipBase> Ships { get; } = new();
    private ShipBase _selectedShip;
    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        for (int i = 0; i < StartingShipCount; i++)
            SpawnShip(StartingShipClass, transform.position + Vector3.right * i * FormationSpacing);
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        HandleInput();
    }

    // ── Input ────────────────────────────────────────────────────────────────

    void HandleInput()
    {
        // Right-click to move selected ship or whole fleet
        if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, LayerMask.GetMask("Ocean")))
            {
                Vector3 target = hit.point;
                if (_selectedShip != null)
                {
                    var movement = _selectedShip.GetComponent<ShipMovement>();
                    if (movement != null)
                    {
                        movement.SetDestination(target);

                        // Subscribe (remove old first to avoid stacking)
                        movement.OnDestinationReached -= HandleDestinationReached;
                        movement.OnDestinationReached += HandleDestinationReached;
                    }
                }
                else
                {
                    MoveFleetTo(target);
                }

                SpawnMoveMarker(target);
            }
        }

        // Deselect on left click on ocean
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                ShipBase clicked = hit.collider.GetComponentInParent<ShipBase>();
                if (clicked != null && clicked.Faction == ShipFaction.Player)
                    SelectShip(clicked);
                else
                    DeselectShip();
            }
        }
    }

    // ── Selection ────────────────────────────────────────────────────────────

    public void SelectShip(ShipBase ship)
    {
        _selectedShip = ship;
        UIManager.Instance?.ShowShipPanel(ship);
    }

    public void DeselectShip()
    {
        _selectedShip = null;
        UIManager.Instance?.CloseShipPanel();
    }

    // ── Movement ─────────────────────────────────────────────────────────────

    void MoveFleetTo(Vector3 center)
    {
        List<Vector3> positions = GetFormationPositions(center);

        for (int i = 0; i < Ships.Count; i++)
        {
            var movement = Ships[i].GetComponent<ShipMovement>();
            if (movement != null)
            {
                movement.SetDestination(positions[i]);

                movement.OnDestinationReached -= HandleDestinationReached;
                movement.OnDestinationReached += HandleDestinationReached;
            }
        }
    }

    // ── Formation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Reorders formation slots every frame based on ship class priority.
    /// Heavy → front, Medium → middle, Light → rear/interior.
    /// </summary>
    void UpdateFormation()
    {
        // Ships are sorted: ManOWar, then Frigate, then Schooner
        Ships.Sort((a, b) => GetFormationPriority(a).CompareTo(GetFormationPriority(b)));
    }

    int GetFormationPriority(ShipBase s)
    {
        return s.Stats.ShipClass switch
        {
            ShipClass.ManOWar  => 0,
            ShipClass.Frigate  => 1,
            ShipClass.Schooner => 2,
            _                  => 1
        };
    }

    List<Vector3> GetFormationPositions(Vector3 center)
    {
        var positions = new List<Vector3>();
        int count = Ships.Count;

        // Simple line/wedge formation
        for (int i = 0; i < count; i++)
        {
            float angle  = (360f / count) * i * Mathf.Deg2Rad;
            float radius = (count == 1) ? 0f : FormationSpacing;
            positions.Add(center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
        }
        return positions;
    }

    // ── Ship Lifecycle ───────────────────────────────────────────────────────

    public ShipBase SpawnShip(ShipClass cls, Vector3 pos)
    {
        GameObject prefab = cls switch
        {
            ShipClass.Schooner => SchoonerPrefab,
            ShipClass.ManOWar  => ManOWarPrefab,
            _                  => FrigatePrefab
        };
        if (prefab == null) { Debug.LogError($"No prefab for {cls}"); return null; }

        GameObject go  = Instantiate(prefab, pos, Quaternion.identity);
        ShipBase ship  = go.GetComponent<ShipBase>();
        ship.Faction   = ShipFaction.Player;
        ship.OnShipDestroyed += s => OnShipLost(s);
        Ships.Add(ship);
        UIManager.Instance?.UpdateFleetCount(Ships.Count, Ships.Count);

        UpdateFormation();
        return ship;
    }

    public void RemoveShip(ShipBase ship)
    {
        Ships.Remove(ship);
        UIManager.Instance?.UpdateFleetCount(Ships.Count, Ships.Count);
    }

    public void OnShipLost(ShipBase ship)
    {
        Ships.Remove(ship);
        UIManager.Instance?.UpdateFleetCount(Ships.Count, Ships.Count);

        UpdateFormation();

        if (Ships.Count == 0) GameManager.Instance.OnPlayerFleetDestroyed();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    void SpawnMoveMarker(Vector3 pos)
    {
        if (MoveMarkerPrefab == null) return;

        // Destroy old marker if it exists
        if (_activeMoveMarker != null)
            Destroy(_activeMoveMarker);

        // Spawn new one
        _activeMoveMarker = Instantiate(
            MoveMarkerPrefab, 
            pos + Vector3.up * 0.1f, 
            Quaternion.identity
        );
    }

    void HandleDestinationReached()
    {
        if (_activeMoveMarker != null)
        {
            Destroy(_activeMoveMarker);
            _activeMoveMarker = null;
        }
    }

    /// <summary>Returns fleet centroid for camera follow.</summary>
    public Vector3 FleetCenter()
    {
        if (Ships.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (var s in Ships) sum += s.transform.position;
        return sum / Ships.Count;
    }
}
