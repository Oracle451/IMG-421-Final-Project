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

    public void UpgradeCannons(ShipBase ship)
    {
        if (ship == null || ship.Stats == null) return;

        if (!ship.CanUpgradeCannons)
        {
            Debug.Log($"{ship.ShipName}: Cannon upgrades maxed.");
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }

        int cost = ship.Stats.UpgradeCost(ship.CannonUpgradeLevel);

        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            Debug.Log("Not enough gold!");
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }

        ship.ApplyCannonUpgrade();
        RefreshPanel(ship);
        Debug.Log($"{ship.ShipName} cannon upgraded to level {ship.CannonUpgradeLevel}");
    }

    public void UpgradeSpeed(ShipBase ship)
    {
        if (ship == null || ship.Stats == null) return;

        if (!ship.CanUpgradeSpeed)
        {
            Debug.Log($"{ship.ShipName}: Speed upgrades maxed.");
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }

        int cost = ship.Stats.UpgradeCost(ship.SpeedUpgradeLevel);

        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            Debug.Log("Not enough gold!");
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }

        ship.ApplySpeedUpgrade();
        RefreshPanel(ship);
    }

    public void UpgradeArmor(ShipBase ship)
    {
        if (ship == null || ship.Stats == null) return;

        if (!ship.CanUpgradeArmor)
        {
            Debug.Log($"{ship.ShipName}: Armor upgrades maxed.");
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }

        int cost = ship.Stats.UpgradeCost(ship.ArmorUpgradeLevel);
        
        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            Debug.Log("Not enough gold!");
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }
        
        ship.ApplyArmorUpgrade();
        RefreshPanel(ship);
    }

    void RefreshPanel(ShipBase ship)
    {
        UIManager.Instance?.ShowShipPanel(ship);
        if (CurrencyManager.Instance != null)
            UIManager.Instance?.UpdateCurrency(CurrencyManager.Instance.CurrentGold);
    }
}
