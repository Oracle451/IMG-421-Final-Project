using UnityEngine;

[RequireComponent(typeof(ShipBase))]
[RequireComponent(typeof(Rigidbody))]
public class ShipMovement : MonoBehaviour
{
    [Header("Tuning")]
    public float StopDistance = 1.5f;

    // Ship won't thrust forward until it's within this many degrees of the target.
    // Prevents the spinning-in-place issue on sharp turns.
    public float AlignmentAngleThreshold = 25f;

    private ShipBase _ship;
    private Rigidbody _rb;
    private Vector3 _destination;
    private bool _hasDestination;

    public System.Action OnDestinationReached;

    void Awake()
    {
        _ship = GetComponent<ShipBase>();
        _rb   = GetComponent<Rigidbody>();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetDestination(Vector3 worldPos)
    {
        _destination     = new Vector3(worldPos.x, transform.position.y, worldPos.z);
        _hasDestination  = true;
    }

    public void ClearDestination() => _hasDestination = false;

    public bool IsMoving => _hasDestination && 
                            Vector3.Distance(transform.position, _destination) > StopDistance;

    void FixedUpdate()
    {
        if (!_hasDestination || !_ship.IsAlive) return;

        Vector3 toTarget = _destination - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        // Arrived
        if (dist <= StopDistance)
        {
            // Brake to a stop smoothly
            _rb.velocity = Vector3.Lerp(_rb.velocity, Vector3.zero, 0.25f);
            OnDestinationReached?.Invoke();
            ClearDestination();
            return;
        }

        Vector3 dir = toTarget.normalized;

        // ── Rotation ─────────────────────────────────────────────────────────
        Quaternion targetRot = Quaternion.LookRotation(dir);
        Quaternion newRot    = Quaternion.RotateTowards(
            _rb.rotation, targetRot,
            _ship.Stats.RotationSpeed * Time.fixedDeltaTime);
        _rb.MoveRotation(newRot);

        // ── Thrust — only when roughly facing the target ──────────────────────
        float angleToTarget = Vector3.Angle(transform.forward, dir);
        float alignment     = 1f - Mathf.Clamp01(angleToTarget / AlignmentAngleThreshold);
        // alignment = 0 when >25° off, ramps to 1 when fully aligned
        // This eliminates sideways/backward drift on sharp turns

        float speed        = _ship.EffectiveMoveSpeed * alignment;
        Vector3 targetVel  = transform.forward * speed;
        targetVel.y        = _rb.velocity.y;  // preserve gravity

        _rb.velocity = Vector3.Lerp(_rb.velocity, targetVel, 0.2f);
    }
}