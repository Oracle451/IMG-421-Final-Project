using UnityEngine;

// Cannonball projectile. Physical travel time creates potential for misses.
// Damages ShipBase components OR StructureHitReceiver components on impact.
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Config")]
    public float Lifetime = 6f;

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

        _rb.velocity  = velocity;
        Destroy(gameObject, Lifetime);
    }

    // Collision

    void OnCollisionEnter(Collision col)
    {
        if (_hit) return;

        // Try ship first
        ShipBase targetShip = col.collider.GetComponentInParent<ShipBase>();
        if (targetShip != null && targetShip.Faction != _ownerFaction && targetShip.IsAlive)
        {
            _hit = true;
            targetShip.TakeDamage(_damage);
            SpawnFX(HitVFX, HitMetallicSFX, col.contacts[0].point);
            Destroy(gameObject);
            return;
        }

        // Try structure
        StructureHitReceiver structure = col.collider.GetComponent<StructureHitReceiver>();
        if (structure != null)
        {
            _hit = true;
            structure.ReceiveDamage(_damage);
            SpawnFX(HitVFX, HitMetallicSFX, col.contacts[0].point);
            Destroy(gameObject);
            return;
        }

        // Ocean / terrain splash
        if (col.collider.CompareTag("Ocean") || col.collider.CompareTag("Terrain"))
        {
            _hit = true;
            SpawnFX(SplashVFX, SplashSFX, col.contacts[0].point);
            Destroy(gameObject);
        }
    }

    // Helpers

    void SpawnFX(GameObject vfxPrefab, AudioClip sfx, Vector3 pos)
    {
        if (vfxPrefab) Instantiate(vfxPrefab, pos, Quaternion.identity);
        if (sfx) AudioSource.PlayClipAtPoint(sfx, pos);
    }
}
