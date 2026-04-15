using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipBase : MonoBehaviour, IDamageable
{
    [Header("Config")]
    public ShipStats Stats;
    public ShipFaction Faction;
    public string ShipName;

    [Header("VFX")]
    public GameObject ExplosionVFX;
    public TrailRenderer WakeTrail;

    // ── Runtime state ────────────────────────────────────────────────────────
    public float CurrentHealth { get; private set; }

    private float _healthMultiplier = 1f;
    private float _damageMultiplier = 1f;

    public int CannonUpgradeLevel  { get; private set; }
    public int SpeedUpgradeLevel   { get; private set; }
    public int ArmorUpgradeLevel   { get; private set; }

    public float EffectiveMaxHealth      => (Stats.MaxHealth + ArmorUpgradeLevel * Stats.HealthPerUpgrade) * _healthMultiplier;
    public float EffectiveDR             =>  Stats.ArmorDamageReduction + ArmorUpgradeLevel * Stats.DamageResistPerUpgrade;
    public float EffectiveMoveSpeed      =>  Stats.MoveSpeed + SpeedUpgradeLevel * Stats.MoveSpeedPerUpgrade;
    public float EffectiveCannonDamage   => (Stats.CannonDamage + CannonUpgradeLevel * Stats.CannonDamagePerUpgrade) * _damageMultiplier;
    public float EffectiveCannonRange    =>  Stats.CannonRange + CannonUpgradeLevel * Stats.CannonRangePerUpgrade;
    public float EffectiveCannonFireRate =>  Stats.CannonFireRate + CannonUpgradeLevel * Stats.CannonFireRatePerUpgrade;

    public bool IsAlive => CurrentHealth > 0f;

    public event Action<ShipBase> OnShipDestroyed;

    protected Rigidbody Rb;

    // ── Bobbing — runs on the MODEL child, not the root Rigidbody ────────────
    [Header("Bobbing")]
    public Transform ModelRoot;      // drag your mesh child here in Inspector
    public float BobAmplitude = 0.1f;
    public float BobFrequency = 1f;
    private float _bobOffset;

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    protected virtual void Start()
    {
        CurrentHealth = Stats != null ? Stats.MaxHealth : 100f;
    }

    protected virtual void Update()
    {
        // Bob the visual model child up and down — never touch the root transform
        if (ModelRoot != null)
        {
            _bobOffset += Time.deltaTime * BobFrequency;
            Vector3 localPos = ModelRoot.localPosition;
            localPos.y = Mathf.Sin(_bobOffset) * BobAmplitude;
            ModelRoot.localPosition = localPos;
        }
    }

    // ── Zone Scaling ──────────────────────────────────────────────────────────

    public void ScaleZoneStats(float healthMult, float damageMult)
    {
        _healthMultiplier = healthMult;
        _damageMultiplier = damageMult;
        CurrentHealth     = EffectiveMaxHealth;
    }

    // ── Damage / Heal ─────────────────────────────────────────────────────────

    public virtual void TakeDamage(float rawDamage)
    {
        if (!IsAlive) return;
        float reduced = rawDamage * (1f - Mathf.Clamp01(EffectiveDR));
        CurrentHealth = Mathf.Max(0f, CurrentHealth - reduced);

        if (Faction == ShipFaction.Player)
            UIManager.Instance?.ShowShipPanel(this);

        if (CurrentHealth <= 0f) Die();
    }

    public void Repair(float amount)
    {
        CurrentHealth = Mathf.Min(EffectiveMaxHealth, CurrentHealth + amount);
    }

    // ── Upgrades ──────────────────────────────────────────────────────────────

    public bool CanUpgradeCannons => Stats != null && CannonUpgradeLevel < Stats.MaxCannonUpgrades;
    public bool CanUpgradeSpeed   => Stats != null && SpeedUpgradeLevel  < Stats.MaxSpeedUpgrades;
    public bool CanUpgradeArmor   => Stats != null && ArmorUpgradeLevel  < Stats.MaxArmorUpgrades;

    public void ApplyCannonUpgrade()
    {
        if (!CanUpgradeCannons) return;
        CannonUpgradeLevel++;
        GetComponentInChildren<CannonController>()?.RefreshStats();
    }

    public void ApplySpeedUpgrade()
    {
        if (!CanUpgradeSpeed) return;
        SpeedUpgradeLevel++;
    }

    public void ApplyArmorUpgrade()
    {
        if (!CanUpgradeArmor) return;
        ArmorUpgradeLevel++;
        CurrentHealth = Mathf.Min(CurrentHealth + Stats.HealthPerUpgrade, EffectiveMaxHealth);
    }

    // ── Death ─────────────────────────────────────────────────────────────────

    protected virtual void Die()
    {
        if (ExplosionVFX) Instantiate(ExplosionVFX, transform.position, Quaternion.identity);
        OnShipDestroyed?.Invoke(this);

        if (Faction == ShipFaction.Player)
            GameManager.Instance?.PlayerFleet?.OnShipLost(this);
        else
            CurrencyManager.Instance?.AddCurrency(Mathf.RoundToInt(Stats.SellValue * 0.6f));

        Destroy(gameObject, 0.1f);
    }

    void OnMouseDown()
    {
        if (Faction == ShipFaction.Player)
            GameManager.Instance?.PlayerFleet?.SelectShip(this);
    }
}