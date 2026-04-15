using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all cannons on a ship. Finds the nearest enemy, rotates turrets,
/// and fires projectiles with accuracy cone spread.
/// </summary>
public class CannonController : MonoBehaviour
{
    [Header("Cannon Mount Points")]
    public List<Transform> CannonMounts = new();  // assign in Inspector

    [Header("Projectile")]
    public GameObject ProjectilePrefab;

    [Header("Layer Masks")]
    public LayerMask EnemyLayer;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private ShipBase _ship;
    private float _fireCooldown;
    private Collider[] _ownerColliders;

    void Awake()
    {
        _ship = GetComponentInParent<ShipBase>();
        _ownerColliders = _ship != null ? _ship.GetComponentsInChildren<Collider>() : null;
        if (EnemyLayer.value == 0 && _ship != null)
        {
            string targetLayer = _ship.Faction == ShipFaction.Player ? "EnemyShip" : "PlayerShip";
            EnemyLayer = LayerMask.GetMask(targetLayer);
        }
    }

    public void RefreshStats() { /* cannons re-read from ship stats each fire */ }

    void Update()
    {
        if (!_ship.IsAlive) return;

        _fireCooldown -= Time.deltaTime;

        ShipBase target = FindNearestEnemy();
        if (target == null) return;

        // Rotate all cannon mounts to face target
        foreach (Transform mount in CannonMounts)
        {
            Vector3 dir = (target.transform.position - mount.position).normalized;
            dir.y = 0f;
            dir = dir.normalized;
            if (dir != Vector3.zero)
                mount.rotation = Quaternion.Slerp(mount.rotation,
                    Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        }

        // Fire when cooldown ready
        if (_fireCooldown <= 0f)
        {
            _fireCooldown = 1f / _ship.EffectiveCannonFireRate;
            Fire(target);
        }
    }

    // ── Targeting ────────────────────────────────────────────────────────────

    ShipBase FindNearestEnemy()
    {
        float range = _ship.EffectiveCannonRange;
        Collider[] hits = Physics.OverlapSphere(transform.position, range, EnemyLayer);
        ShipBase best  = null;
        float bestDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            ShipBase candidate = col.GetComponentInParent<ShipBase>();
            if (candidate == null || !candidate.IsAlive) continue;
            if (candidate.Faction == _ship.Faction) continue;   // same team — skip

            float d = Vector3.Distance(transform.position, candidate.transform.position);
            if (d < bestDist) { bestDist = d; best = candidate; }
        }
        return best;
    }

    // ── Firing ───────────────────────────────────────────────────────────────

    void Fire(ShipBase target)
    {
        if (ProjectilePrefab == null) return;

        int count = _ship.Stats.CannonCount;
        for (int i = 0; i < count; i++)
        {
            Transform mount = (CannonMounts.Count > 0)
                ? CannonMounts[i % CannonMounts.Count]
                : transform;

            Vector3 baseDir = target.transform.position - mount.position;
            baseDir.y = 0f;
            if (baseDir == Vector3.zero) continue;
            baseDir.Normalize();

            // Apply horizontal-only accuracy spread so cannonballs stay on the water plane.
            float halfCone = _ship.Stats.CannonAccuracyCone;
            Vector3 spread = Quaternion.Euler(
                0f,
                Random.Range(-halfCone, halfCone),
                0f) * baseDir;

            Vector3 spawnPos = mount.position + spread * 1.5f;
            GameObject projGO = Instantiate(ProjectilePrefab, spawnPos, Quaternion.LookRotation(spread));
            Projectile proj   = projGO.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Launch(spread * _ship.Stats.ProjectileSpeed,
                            _ship.EffectiveCannonDamage,
                            _ship.Faction,
                            _ownerColliders);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (_ship == null) _ship = GetComponentInParent<ShipBase>();
        if (_ship == null || _ship.Stats == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _ship.EffectiveCannonRange);
    }
}
