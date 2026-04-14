using UnityEngine;

/// <summary>
/// State-machine AI for an individual enemy ship.
/// States: Patrol → Search → Aggressive → Defense → Cowardly
/// </summary>
[RequireComponent(typeof(ShipBase))]
[RequireComponent(typeof(ShipMovement))]
public class EnemyShipAI : MonoBehaviour
{
    public enum AIState { Patrol, Search, Aggressive, Defense, Cowardly }

    [Header("Behavior Config")]
    public AIState InitialState = AIState.Patrol;
    public float DetectionRange   = 25f;
    public float CowardlyHPPct    = 0.25f;   // flees below this health percentage
    public float AggressiveHPPct  = 0.5f;    // returns to fighting above this
    public float PatrolRadius     = 15f;
    public float CircleRadius     = 10f;      // radius for circling the player

    [Header("Defense Target")]
    public Transform DefenseAnchor;   // structure to defend

    // ── Runtime ──────────────────────────────────────────────────────────────

    public AIState CurrentState { get; private set; }
    private ShipBase _ship;
    private ShipMovement _movement;
    private Transform _playerTarget;
    private Vector3 _patrolCenter;
    private Vector3 _currentPatrolPoint;
    private float _stateTimer;
    private float _circleAngle;

    void Awake()
    {
        _ship     = GetComponent<ShipBase>();
        _movement = GetComponent<ShipMovement>();
    }

    void Start()
    {
        // Disable AI entirely on player ships
        ShipBase ship = GetComponent<ShipBase>();
        if (ship != null && ship.Faction == ShipFaction.Player)
        {
            enabled = false;
            return;
        }

        _patrolCenter      = transform.position;
        _currentPatrolPoint = GetRandomPatrolPoint();
        TransitionTo(InitialState);
    }

    void Update()
    {
        if (!_ship.IsAlive) return;
        _stateTimer += Time.deltaTime;

        // Always check for player proximity
        Transform player = FindPlayer();

        switch (CurrentState)
        {
            case AIState.Patrol:     UpdatePatrol(player);     break;
            case AIState.Search:     UpdateSearch(player);     break;
            case AIState.Aggressive: UpdateAggressive(player); break;
            case AIState.Defense:    UpdateDefense(player);    break;
            case AIState.Cowardly:   UpdateCowardly();         break;
        }
    }

    // ── State Updates ─────────────────────────────────────────────────────────

    void UpdatePatrol(Transform player)
    {
        // Walk to patrol point
        if (!_movement.IsMoving)
        {
            _currentPatrolPoint = GetRandomPatrolPoint();
            _movement.SetDestination(_currentPatrolPoint);
        }

        if (player != null && Vector3.Distance(transform.position, player.position) <= DetectionRange)
            TransitionTo(AIState.Aggressive);
    }

    void UpdateSearch(Transform player)
    {
        // Wander; transition to aggressive if player found
        if (_stateTimer > 8f)
            TransitionTo(AIState.Patrol);

        if (player != null)
            TransitionTo(AIState.Aggressive);
    }

    void UpdateAggressive(Transform player)
    {
        if (player == null) { TransitionTo(AIState.Search); return; }
        _playerTarget = player;

        // Health-based cowardly check
        if (_ship.CurrentHealth / _ship.EffectiveMaxHealth < CowardlyHPPct)
        { TransitionTo(AIState.Cowardly); return; }

        // Circle the player for tactical engagement
        _circleAngle += Time.deltaTime * 40f;
        Vector3 offset  = new Vector3(
            Mathf.Cos(_circleAngle * Mathf.Deg2Rad),
            0,
            Mathf.Sin(_circleAngle * Mathf.Deg2Rad)) * CircleRadius;

        _movement.SetDestination(player.position + offset);
    }

    void UpdateDefense(Transform player)
    {
        if (DefenseAnchor == null) { TransitionTo(AIState.Patrol); return; }

        if (player != null && Vector3.Distance(DefenseAnchor.position, player.position) < DetectionRange * 1.5f)
        {
            // Aggressive against the intruder
            _circleAngle += Time.deltaTime * 40f;
            Vector3 offset = new Vector3(
                Mathf.Cos(_circleAngle * Mathf.Deg2Rad), 0,
                Mathf.Sin(_circleAngle * Mathf.Deg2Rad)) * CircleRadius;
            _movement.SetDestination(player.position + offset);
        }
        else
        {
            // Return to anchor
            _movement.SetDestination(DefenseAnchor.position +
                Random.insideUnitSphere.With(y: 0) * 5f);
        }
    }

    void UpdateCowardly()
    {
        Transform player = FindPlayer();

        // Flee away from player
        if (player != null)
        {
            Vector3 fleeDir = (transform.position - player.position).normalized;
            _movement.SetDestination(transform.position + fleeDir * 30f);
        }

        // Recover if health restored
        if (_ship.CurrentHealth / _ship.EffectiveMaxHealth > AggressiveHPPct)
            TransitionTo(AIState.Aggressive);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    void TransitionTo(AIState next)
    {
        CurrentState = next;
        _stateTimer  = 0f;
    }

    Transform FindPlayer()
    {
        PlayerFleet fleet = GameManager.Instance?.PlayerFleet;
        if (fleet == null || fleet.Ships.Count == 0) return null;
        // Return centroid proxy — use first alive ship
        return fleet.Ships[0]?.transform;
    }

    Vector3 GetRandomPatrolPoint()
    {
        Vector2 rnd = Random.insideUnitCircle * PatrolRadius;
        return _patrolCenter + new Vector3(rnd.x, 0f, rnd.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, DetectionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, PatrolRadius);
    }
}

// Extension helper
public static class Vector3Extensions
{
    public static Vector3 With(this Vector3 v, float? x = null, float? y = null, float? z = null)
        => new Vector3(x ?? v.x, y ?? v.y, z ?? v.z);
}
