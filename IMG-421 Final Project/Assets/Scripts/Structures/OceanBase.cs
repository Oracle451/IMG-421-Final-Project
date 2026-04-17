using System.Collections.Generic;
using UnityEngine;

// A floating ocean base (oil-rig style). Has its own turrets and optionally
// spawns defending ships. When destroyed it gives a large gold reward.
// The central stronghold is a special instance of this with isCentralStronghold=true.
public class OceanBase : MonoBehaviour
{
    [Header("Stats")]
    public float MaxHealth = 400f;
    public int   GoldReward = 250;
    public bool  IsCentralStronghold = false;

    [Header("Turret Children")]
    public List<CoastalTurret> Turrets = new(); // child turrets auto-collected if empty

    [Header("Defending Ships")]
    public GameObject DefenderPrefab;
    public int DefenderCount = 3;
    public float DefenderSpawnRadius = 12f;

    [Header("VFX")]
    public GameObject ExplosionVFX;
    public GameObject SinkingVFX;

    // Runtime

    public float CurrentHealth { get; private set; }

    void Start()
    {
        CurrentHealth = MaxHealth;

        // Auto-collect child turrets
        if (Turrets.Count == 0) Turrets.AddRange(GetComponentsInChildren<CoastalTurret>());

        SpawnDefenders();
    }

    // Damage

    public void TakeDamage(float dmg)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - dmg);
        if (CurrentHealth <= 0f) Die();
    }

    void Die()
    {
        CurrencyManager.Instance?.AddCurrency(GoldReward);

        if (SinkingVFX) Instantiate(SinkingVFX, transform.position, Quaternion.identity);
        if (ExplosionVFX)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector3 rndPos = transform.position + Random.insideUnitSphere * 5f;
                rndPos.y = transform.position.y;
                Instantiate(ExplosionVFX, rndPos, Quaternion.identity);
            }
        }

        if (IsCentralStronghold)
            GameManager.Instance?.OnStrongholdDestroyed();

        Destroy(gameObject, 0.5f);
    }

    // Defenders

    void SpawnDefenders()
    {
        if (DefenderPrefab == null) return;

        for (int i = 0; i < DefenderCount; i++)
        {
            float angle  = (360f / DefenderCount) * i * Mathf.Deg2Rad;
            Vector3 pos  = transform.position
                           + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))
                           * DefenderSpawnRadius;

            GameObject go = Instantiate(DefenderPrefab, pos, Quaternion.identity);
            ShipBase ship = go.GetComponent<ShipBase>();
            if (ship != null)
            {
                ship.Faction = ShipFaction.Enemy;
                ship.ApplyFactionLayer();
            }

            EnemyShipAI ai = go.GetComponent<EnemyShipAI>();
            if (ai != null)
            {
                ai.DefenseAnchor = transform;
                ai.InitialState = EnemyShipAI.AIState.Defense;
            }
        }
    }

    // Projectile hit forwarding

    // Attach a child Collider to the base and hook this so projectiles can hit it.
    void OnCollisionEnter(Collision col)
    {
        Projectile proj = col.collider.GetComponent<Projectile>();
        // Projectile handles its own damage call via ShipBase.TakeDamage;
        // for structures we need direct forwarding, handled in Projectile via DamageableStructure interface.
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, DefenderSpawnRadius);
    }
}
