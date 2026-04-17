using UnityEngine;

// Central system for upgrading ship stats.
// Called by UIManager button callbacks.
public class UpgradeSystem : MonoBehaviour
{
    public static UpgradeSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Public Upgrade Methods
    public void UpgradeCannons(ShipBase ship)
    {
        if (!ship.CanUpgradeCannons)
        {
            Debug.Log($"{ship.ShipName}: Cannon upgrades maxed.");
            return;
        }

        int cost = ship.Stats.UpgradeCost(ship.CannonUpgradeLevel);

        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            Debug.Log("Not enough gold!");
            return;
        }

        ship.ApplyCannonUpgrade();
        RefreshPanel(ship);
        Debug.Log($"{ship.ShipName} cannon upgraded to level {ship.CannonUpgradeLevel}");
    }

    public void UpgradeSpeed(ShipBase ship)
    {
        if (!ship.CanUpgradeSpeed)
        {
            Debug.Log($"{ship.ShipName}: Speed upgrades maxed.");
            return;
        }

        int cost = ship.Stats.UpgradeCost(ship.SpeedUpgradeLevel);

        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            Debug.Log("Not enough gold!");
            return;
        }

        ship.ApplySpeedUpgrade();
        RefreshPanel(ship);
    }

    public void UpgradeArmor(ShipBase ship)
    {
        if (!ship.CanUpgradeArmor)
        {
            Debug.Log($"{ship.ShipName}: Armor upgrades maxed.");
            return;
        }

        int cost = ship.Stats.UpgradeCost(ship.ArmorUpgradeLevel);
        
        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            Debug.Log("Not enough gold!");
            return;
        }
        
        ship.ApplyArmorUpgrade();
        RefreshPanel(ship);
    }

    // Ship Purchase

    public void PurchaseShip(ShipClass cls)
    {
        PlayerFleet fleet = GameManager.Instance.PlayerFleet;
        ShipStats stats = GetStatsForClass(cls);
        if (stats == null) return;

        if (!CurrencyManager.Instance.SpendCurrency(stats.PurchaseCost))
        {
            Debug.Log("Not enough gold to purchase ship!");
            return;
        }

        Vector3 spawnPos = fleet.FleetCenter() + Random.insideUnitSphere.With(y: 0) * 10f;
        fleet.SpawnShip(cls, spawnPos);
    }

    // Helpers

    ShipStats GetStatsForClass(ShipClass cls)
    {
        // Find a ship with the matching class in the player fleet as reference
        foreach (ShipBase s in GameManager.Instance.PlayerFleet.Ships) if (s.Stats.ShipClass == cls) return s.Stats;
        return null;
    }

    void RefreshPanel(ShipBase ship) => UIManager.Instance?.ShowShipPanel(ship);
}
