using UnityEngine;

/// <summary>
/// Steers a ship toward a destination using physics.
/// Works for both player-commanded ships and AI-controlled ships.
/// </summary>
[RequireComponent(typeof(ShipBase))]
[RequireComponent(typeof(Rigidbody))]
public class ShipMovement : MonoBehaviour
{
    [Header("Tuning")]
    public float StopDistance = 1.5f;

    private ShipBase _ship;
    private Rigidbody _rb;
    private Vector3 _destination;
    private Vector3 _currentVelocity;
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
        _destination    = new Vector3(worldPos.x, transform.position.y, worldPos.z);
        _hasDestination = true;
        // Seed the smoothing buffer from current physics velocity so there's
        // no lag spike when a new order arrives mid-move.
        _currentVelocity = _rb.velocity;
    }

    public void ClearDestination() => _hasDestination = false;

    public bool IsMoving => _hasDestination && Vector3.Distance(transform.position, _destination) > StopDistance;

    void FixedUpdate()
    {
        if (!_hasDestination || !_ship.IsAlive) return;

        Vector3 dir = (_destination - transform.position);
        dir.y = 0f;
        float dist = dir.magnitude;

        if (dist <= StopDistance)
        {
            OnDestinationReached?.Invoke();
            ClearDestination();
            return; // CRITICAL: Stop executing here!
        }

        dir.Normalize();

        // Rotate safely using the Rigidbody, not the Transform
        Quaternion targetRot = Quaternion.LookRotation(dir);
        Quaternion newRot = Quaternion.RotateTowards(_rb.rotation, targetRot, _ship.Stats.RotationSpeed * Time.fixedDeltaTime);
        _rb.MoveRotation(newRot); 

        // Move forward (preserve existing Y velocity so gravity still works)
        float speed = _ship.EffectiveMoveSpeed;
        _currentVelocity = Vector3.Lerp(_rb.velocity, transform.forward * speed, 0.5f);
        _currentVelocity.y = _rb.velocity.y; 
        
        _rb.velocity = _currentVelocity;
    }
}