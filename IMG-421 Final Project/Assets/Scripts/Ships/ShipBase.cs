using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipBase : MonoBehaviour, IDamageable
{
    [Header("Config")]
    public ShipStats Stats;
    public ShipFaction Faction { get; set; }
    public string ShipName;

    [Header("VFX")]
    public GameObject ExplosionVFX;
    public TrailRenderer WakeTrail;

    [Header("Audio")]
    public AudioClip[] ExplosionSounds;

    // Runtime state
    public float CurrentHealth { get; private set; }

    private float _healthMultiplier = 1f;
    private float _damageMultiplier = 1f;
    private bool _isDying;

    public int CannonUpgradeLevel { get; private set; }
    public int SpeedUpgradeLevel { get; private set; }
    public int ArmorUpgradeLevel { get; private set; }

    public float EffectiveMaxHealth => (Stats.MaxHealth + ArmorUpgradeLevel * Stats.HealthPerUpgrade) * _healthMultiplier;
    public float EffectiveDR => Stats.ArmorDamageReduction + ArmorUpgradeLevel * Stats.DamageResistPerUpgrade;
    public float EffectiveMoveSpeed => Stats.MoveSpeed + SpeedUpgradeLevel * Stats.MoveSpeedPerUpgrade;
    public float EffectiveCannonDamage => (Stats.CannonDamage + CannonUpgradeLevel * Stats.CannonDamagePerUpgrade) * _damageMultiplier;
    public float EffectiveCannonRange => Stats.CannonRange + CannonUpgradeLevel * Stats.CannonRangePerUpgrade;
    public float EffectiveCannonFireRate => Stats.CannonFireRate + CannonUpgradeLevel * Stats.CannonFireRatePerUpgrade;

    public bool IsAlive => CurrentHealth > 0f;

    public event Action<ShipBase> OnShipDestroyed;

    protected Rigidbody Rb;

    // Bobbing: runs on the MODEL child, not the root Rigidbody
    [Header("Bobbing")]
    public Transform ModelRoot;
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
        // Bob the visual model child up and down, never touch the root transform
        if (ModelRoot != null)
        {
            _bobOffset += Time.deltaTime * BobFrequency;
            Vector3 localPos = ModelRoot.localPosition;
            localPos.y = Mathf.Sin(_bobOffset) * BobAmplitude;
            ModelRoot.localPosition = localPos;
        }
    }

    public void ApplyFactionLayer()
    {
        string layerName = Faction == ShipFaction.Player ? "PlayerShip" : "EnemyShip";
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) return;

        SetLayerRecursively(gameObject, layer);
    }

    void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // Zone Scaling

    public void ScaleZoneStats(float healthMult, float damageMult)
    {
        _healthMultiplier = healthMult;
        _damageMultiplier = damageMult;
        CurrentHealth = EffectiveMaxHealth;
    }

    // Damage / Heal

    public virtual void TakeDamage(float rawDamage)
    {
        if (!IsAlive || _isDying) return;
        float reduced = rawDamage * (1f - Mathf.Clamp01(EffectiveDR));
        CurrentHealth = Mathf.Max(0f, CurrentHealth - reduced);

        if (Faction == ShipFaction.Player) UIManager.Instance?.ShowShipPanel(this);

        if (CurrentHealth <= 0f) Die();
    }

    public void Repair(float amount)
    {
        CurrentHealth = Mathf.Min(EffectiveMaxHealth, CurrentHealth + amount);
    }

    // Upgrades 

    public bool CanUpgradeCannons => Stats != null && CannonUpgradeLevel < Stats.MaxCannonUpgrades;
    public bool CanUpgradeSpeed => Stats != null && SpeedUpgradeLevel < Stats.MaxSpeedUpgrades;
    public bool CanUpgradeArmor => Stats != null && ArmorUpgradeLevel < Stats.MaxArmorUpgrades;

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

    // Death

    protected virtual void Die()
    {
        if (_isDying) return;
        _isDying = true;
        CurrentHealth = 0f;

        Vector3 deathPosition = transform.position;

        PlayRandomExplosionSound(deathPosition);  

        DisableGameplay();

        if (ExplosionVFX) Instantiate(ExplosionVFX, transform.position, Quaternion.identity);

        if (OnShipDestroyed != null)
        {
            foreach (Action<ShipBase> handler in OnShipDestroyed.GetInvocationList())
            {
                try
                {
                    handler(this);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                }
            }
        }

        if (Faction == ShipFaction.Player)
        {
            GameManager.Instance?.PlayerFleet?.OnShipLost(this);
        }
        else
        {
            CurrencyManager.Instance?.AddCurrency(Mathf.RoundToInt(Stats.SellValue * 0.6f));
        }

        Destroy(gameObject, 0.1f);
    }

    void PlayRandomExplosionSound(Vector3 position)
    {
        if (ExplosionSounds == null || ExplosionSounds.Length == 0) return;
        AudioClip clip = ExplosionSounds[UnityEngine.Random.Range(0, ExplosionSounds.Length)];
        if (clip == null) return;

        GameObject tempGO = new GameObject("ExplosionSFX");
        tempGO.transform.position = position;

        AudioSource source = tempGO.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 0f;   // 0 = fully 2D, always audible regardless of distance
        source.volume = 1f;
        source.Play();

        Destroy(tempGO, clip.length);
    }

    void DisableGameplay()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
            rend.enabled = false;

        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour != this)
                behaviour.enabled = false;
        }

        if (WakeTrail != null)
            WakeTrail.emitting = false;

        if (Rb != null)
        {
            Rb.velocity = Vector3.zero;
            Rb.angularVelocity = Vector3.zero;
            Rb.isKinematic = true;
        }
    }

    void OnMouseDown()
    {
        if (Faction == ShipFaction.Player)
            GameManager.Instance?.PlayerFleet?.SelectShip(this);
    }
}
