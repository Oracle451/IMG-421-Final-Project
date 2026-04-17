using UnityEngine;

// Controls a ship's wake trail particle system or TrailRenderer based on velocity.
// Attach to the same GameObject as ShipBase.
[RequireComponent(typeof(ShipBase))]
public class WakeTrailController : MonoBehaviour
{
    [Header("Trail")]
    public TrailRenderer WakeTrail;
    public ParticleSystem WakeParticles;

    [Header("Foam Spray")]
    public ParticleSystem BowSpray;

    [Header("Thresholds")]
    public float MinSpeedForWake = 0.5f;
    public float MaxSpeedForEmit = 10f;

    private ShipBase _ship;
    private Rigidbody _rb;

    void Awake()
    {
        _ship = GetComponent<ShipBase>();
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float speed = _rb.velocity.magnitude;
        bool moving = speed > MinSpeedForWake;

        // TrailRenderer width scales with speed
        if (WakeTrail != null)
        {
            WakeTrail.emitting = moving;
            WakeTrail.widthMultiplier = Mathf.Lerp(0.2f, 1.5f, speed / MaxSpeedForEmit);
        }

        // Particle emission rate
        if (WakeParticles != null)
        {
            var emission = WakeParticles.emission;
            emission.rateOverTime = moving ? Mathf.Lerp(5f, 30f, speed / MaxSpeedForEmit) : 0f;
        }

        // Bow spray
        if (BowSpray != null)
        {
            var emission = BowSpray.emission;
            emission.rateOverTime = moving ? Mathf.Lerp(2f, 15f, speed / MaxSpeedForEmit) : 0f;
        }
    }
}
