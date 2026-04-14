using UnityEngine;

public enum ShipClass { Schooner, Frigate, ManOWar }
public enum ShipFaction { Player, Enemy }

/// <summary>
/// ScriptableObject that stores base stats for a ship type.
/// Create via Assets > Create > IronTide > ShipStats
/// </summary>
[CreateAssetMenu(menuName = "IronTide/ShipStats", fileName = "NewShipStats")]
public class ShipStats : ScriptableObject
{
    [Header("Identity")]
    public ShipClass ShipClass;
    public string DisplayName;

    [Header("Base Combat Stats")]
    public float MaxHealth         = 100f;
    public float ArmorDamageReduction = 0f;   // 0–1 flat DR percentage
    public float CannonRange       = 20f;
    public float CannonDamage      = 10f;
    public float CannonFireRate    = 1f;       // shots per second
    public float CannonAccuracyCone = 10f;    // degrees half-angle
    public float ProjectileSpeed   = 25f;
    public int   CannonCount       = 2;

    [Header("Movement")]
    public float MoveSpeed         = 5f;
    public float RotationSpeed     = 60f;     // degrees/sec

    [Header("Economy")]
    public int   SellValue         = 50;
    public int   PurchaseCost      = 100;

    // ── Upgrade tracking (runtime, not asset data) ───────────────────────────
    // Upgrade levels are stored on ShipBase instances, not here.
    [Header("Upgrade Caps (max times each can be upgraded)")]
    public int MaxCannonUpgrades   = 5;
    public int MaxSpeedUpgrades    = 5;
    public int MaxArmorUpgrades    = 5;

    [Header("Per-Upgrade Delta")]
    public float CannonDamagePerUpgrade    = 5f;
    public float CannonRangePerUpgrade     = 3f;
    public float CannonFireRatePerUpgrade  = 0.15f;
    public float MoveSpeedPerUpgrade       = 0.5f;
    public float HealthPerUpgrade          = 25f;
    public float DamageResistPerUpgrade    = 0.04f;

    public int UpgradeCost(int currentLevel) => 75 + currentLevel * 50;
}
