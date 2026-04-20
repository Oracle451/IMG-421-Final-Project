using UnityEngine;

// Cannonball projectile. Physical travel time creates potential for misses.
// Damages ShipBase components OR StructureHitReceiver components on impact.
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Config")]
    public float Lifetime = 6f;
    public float StructureAssistRadius = 1.5f;

    [Header("VFX / SFX")]
    public GameObject HitVFX;
    public GameObject SplashVFX;
    public AudioClip HitMetallicSFX;
    public AudioClip SplashSFX;

    // Runtime
    private float _damage;
    private ShipFaction _ownerFaction;
    private Rigidbody _rb;
    private bool _hit;
    private Collider[] _ignoredColliders;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    // Public API
    public void Launch(Vector3 velocity, float damage, ShipFaction ownerFaction, Collider[] ignoredColliders = null)
    {
        _damage = damage;
        _ownerFaction = ownerFaction;
        _ignoredColliders = ignoredColliders;

        Collider projectileCollider = GetComponent<Collider>();
        if (projectileCollider != null && _ignoredColliders != null)
        {
            foreach (Collider ignored in _ignoredColliders)
            {
                if (ignored != null) Physics.IgnoreCollision(projectileCollider, ignored, true);
            }
        }

        _rb.velocity = velocity;
        Destroy(gameObject, Lifetime);
    }

    // Collision
    void OnCollisionEnter(Collision col)
    {
        if (_hit) return;

        ContactPoint contact = col.contactCount > 0 ? col.contacts[0] : default;
        Vector3 hitPoint = col.contactCount > 0 ? contact.point : transform.position;

        // Try ship first
        ShipBase targetShip = col.collider.GetComponentInParent<ShipBase>();
        if (targetShip != null && targetShip.Faction != _ownerFaction && targetShip.IsAlive)
        {
            _hit = true;
            targetShip.TakeDamage(_damage);
            SpawnFX(HitVFX, HitMetallicSFX, hitPoint);
            Destroy(gameObject);
            return;
        }

        // Try structure on hit object or any parent
        StructureHitReceiver structure = col.collider.GetComponent<StructureHitReceiver>();
        if (structure == null)
            structure = col.collider.GetComponentInParent<StructureHitReceiver>();
        if (structure == null)
            structure = FindNearbyStructureReceiver(hitPoint);

        if (structure != null)
        {
            _hit = true;
            structure.ReceiveDamage(_damage);
            SpawnFX(HitVFX, HitMetallicSFX, hitPoint);
            Destroy(gameObject);
            return;
        }

        // Ocean / terrain splash
        if (col.collider.CompareTag("Ocean") || col.collider.CompareTag("Terrain"))
        {
            _hit = true;
            SpawnFX(SplashVFX, SplashSFX, hitPoint);
            Destroy(gameObject);
            return;
        }

        // Any other solid impact should still consume the projectile.
        _hit = true;
        SpawnFX(HitVFX, HitMetallicSFX, hitPoint);
        Destroy(gameObject);
    }

    // Helpers
    StructureHitReceiver FindNearbyStructureReceiver(Vector3 hitPoint)
    {
        Collider[] nearby = Physics.OverlapSphere(hitPoint, StructureAssistRadius);
        StructureHitReceiver best = null;
        float bestDistSq = float.MaxValue;

        foreach (Collider nearbyCollider in nearby)
        {
            if (nearbyCollider == null) continue;
            StructureHitReceiver candidate = nearbyCollider.GetComponent<StructureHitReceiver>();
            if (candidate == null)
                candidate = nearbyCollider.GetComponentInParent<StructureHitReceiver>();
            if (candidate == null) continue;

            float distSq = (nearbyCollider.ClosestPoint(hitPoint) - hitPoint).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = candidate;
            }
        }

        return best;
    }

    void SpawnFX(GameObject vfxPrefab, AudioClip sfx, Vector3 pos)
    {
        if (vfxPrefab) Instantiate(vfxPrefab, pos, Quaternion.identity);
        if (sfx) AudioSource.PlayClipAtPoint(sfx, pos);
    }
}
