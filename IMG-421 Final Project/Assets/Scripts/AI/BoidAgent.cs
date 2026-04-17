using UnityEngine;

// Attach to each enemy ship that participates in Boids flocking.
// Must be registered with a BoidsFlock.
[RequireComponent(typeof(Rigidbody))]
public class BoidAgent : MonoBehaviour
{
    public Vector3 Velocity { get; private set; }

    private Rigidbody _rb;
    private BoidsFlock _flock;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotationX |
                          RigidbodyConstraints.FreezeRotationZ |
                          RigidbodyConstraints.FreezePositionY;
    }

    void Start()
    {
        ShipBase ship = GetComponent<ShipBase>();
        if (ship != null && ship.Faction == ShipFaction.Player)
        {
            enabled = false;
            return;
        }

        _flock = GetComponentInParent<BoidsFlock>();
        if (_flock == null) _flock = FindObjectOfType<BoidsFlock>();
        _flock?.RegisterAgent(this);
    }

    void OnDestroy() => _flock?.UnregisterAgent(this);

    public void ApplySteering(Vector3 direction, float speed)
    {
        if (direction == Vector3.zero) return;
        direction.y = 0f;

        // Smooth rotation
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation   = Quaternion.RotateTowards(transform.rotation, targetRot, 90f * Time.deltaTime);

        Velocity = transform.forward * speed;
        _rb.velocity = Velocity;
    }
}
