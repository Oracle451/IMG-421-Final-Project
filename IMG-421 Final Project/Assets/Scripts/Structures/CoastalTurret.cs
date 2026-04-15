using UnityEngine;

/// <summary>
/// A static coastal turret mounted on an island.
/// Rotates to track and fire at the nearest player ship within range.
/// </summary>
public class CoastalTurret : MonoBehaviour
{
    [Header("Stats")]
    public float MaxHealth      = 60f;
    public float Damage         = 8f;
    public float Range          = 18f;
    public float FireRate       = 0.6f;   // shots per second
    public float RotationSpeed  = 80f;    // degrees/sec
    public int   GoldReward     = 30;

    [Header("References")]
    public Transform TurretPivot;
    public Transform MuzzlePoint;
    public GameObject ProjectilePrefab;
    public GameObject ExplosionVFX;

    [Header("Layer Mask")]
    public LayerMask PlayerLayer;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private float _currentHealth;
    private float _fireCooldown;

    void Start()
    {
        _currentHealth = MaxHealth;
        if (PlayerLayer.value == 0)
            PlayerLayer = LayerMask.GetMask("PlayerShip");
    }

    void Update()
    {
        _fireCooldown -= Time.deltaTime;

        ShipBase target = FindPlayerShip();
        if (target == null) return;

        // Rotate turret pivot toward target on Y axis only
        if (TurretPivot != null)
        {
            Vector3 dir = (target.transform.position - TurretPivot.position);
            dir.y = 0f;
            if (dir != Vector3.zero)
            {
                Quaternion desired = Quaternion.LookRotation(dir);
                TurretPivot.rotation = Quaternion.RotateTowards(
                    TurretPivot.rotation, desired, RotationSpeed * Time.deltaTime);
            }
        }

        if (_fireCooldown <= 0f)
        {
            _fireCooldown = 1f / FireRate;
            FireAt(target);
        }
    }

    // ── Targeting ────────────────────────────────────────────────────────────

    ShipBase FindPlayerShip()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, Range, PlayerLayer);
        ShipBase best   = null;
        float bestDist  = float.MaxValue;

        foreach (Collider col in hits)
        {
            ShipBase s = col.GetComponentInParent<ShipBase>();
            if (s == null || !s.IsAlive || s.Faction != ShipFaction.Player) continue;
            float d = Vector3.Distance(transform.position, s.transform.position);
            if (d < bestDist) { bestDist = d; best = s; }
        }
        return best;
    }

    // ── Firing ───────────────────────────────────────────────────────────────

    void FireAt(ShipBase target)
    {
        if (ProjectilePrefab == null) return;
        Transform origin = MuzzlePoint != null ? MuzzlePoint : transform;
        Vector3 dir = (target.transform.position - origin.position).normalized;

        GameObject projGO = Instantiate(ProjectilePrefab, origin.position, Quaternion.LookRotation(dir));
        Projectile proj   = projGO.GetComponent<Projectile>();
        proj?.Launch(dir * 22f, Damage, ShipFaction.Enemy);
    }

    // ── Damage ───────────────────────────────────────────────────────────────

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
