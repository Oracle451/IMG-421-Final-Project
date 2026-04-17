using UnityEngine;

// A static coastal turret mounted on an island.
// Rotates to track and fire at the nearest player ship within range.
public class CoastalTurret : MonoBehaviour
{
    [Header("Stats")]
    public float MaxHealth = 60f;
    public float Damage = 8f;
    public float Range = 18f;
    public float FireRate = 0.6f; // shots per second
    public float RotationSpeed = 80f; // degrees/sec
    public int GoldReward = 30;

    [Header("References")]
    public GameObject ProjectilePrefab;
    public GameObject ExplosionVFX;
    public float ProjectileSpawnForwardOffset = 1.5f;

    [Header("Layer Mask")]
    public LayerMask PlayerLayer;

    // Runtime

    private float _currentHealth;
    private float _fireCooldown;
    private Collider[] _ignoredColliders;

    void Start()
    {
        _currentHealth = MaxHealth;
        if (PlayerLayer.value == 0) PlayerLayer = LayerMask.GetMask("PlayerShip");

        // Ignore collisions with all CoastalTurret colliders in the scene
        CoastalTurret[] allTurrets = FindObjectsByType<CoastalTurret>(FindObjectsSortMode.None);
        int total = 0;
        foreach (CoastalTurret t in allTurrets)
            total += t.GetComponentsInChildren<Collider>().Length;

        _ignoredColliders = new Collider[total];
        int idx = 0;
        foreach (CoastalTurret t in allTurrets)
            foreach (Collider c in t.GetComponentsInChildren<Collider>())
                _ignoredColliders[idx++] = c;
    }

    void Update()
    {
        _fireCooldown -= Time.deltaTime;

        ShipBase target = FindPlayerShip();
        if (target == null) return;

        // Rotate this transform toward target on Y axis only
        Vector3 dir = (target.transform.position - transform.position);
        dir.y = 0f;
        if (dir != Vector3.zero)
        {
            Quaternion desired = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, RotationSpeed * Time.deltaTime);
        }

        if (_fireCooldown <= 0f)
        {
            _fireCooldown = 1f / FireRate;
            FireAt(target);
        }
    }

    // Targeting

    ShipBase FindPlayerShip()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, Range, PlayerLayer);
        ShipBase best = null;
        float bestDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            ShipBase s = col.GetComponentInParent<ShipBase>();
            if (s == null || !s.IsAlive || s.Faction != ShipFaction.Player) continue;
            float d = Vector3.Distance(transform.position, s.transform.position);
            if (d < bestDist) { bestDist = d; best = s; }
        }
        return best;
    }

    // Firing

    void FireAt(ShipBase target)
    {
        if (ProjectilePrefab == null) return;

        Vector3 launchOrigin = transform.position + Vector3.up;
        Vector3 dir = target.transform.position - launchOrigin;
        dir.y = 0f;
        if (dir == Vector3.zero) return;
        dir.Normalize();

        Vector3 spawnPos = launchOrigin + dir * ProjectileSpawnForwardOffset;
        GameObject projGO = Instantiate(ProjectilePrefab, spawnPos, Quaternion.LookRotation(dir));
        Projectile proj = projGO.GetComponent<Projectile>();
        proj?.Launch(dir * 22f, Damage, ShipFaction.Enemy, _ignoredColliders);
    }

    // Damage

    public void TakeDamage(float dmg)
    {
        _currentHealth -= dmg;
        if (_currentHealth <= 0f) Die();
    }

    void Die()
    {
        CurrencyManager.Instance?.AddCurrency(GoldReward);
        if (ExplosionVFX) Instantiate(ExplosionVFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}