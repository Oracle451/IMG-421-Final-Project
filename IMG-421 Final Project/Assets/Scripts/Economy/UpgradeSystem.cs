using UnityEngine;

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
            HUDManager.Instance?.ShowMessage($"{ship.ShipName}: Cannons already at max!", isWarning: true);
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }

        int cost = ship.Stats.UpgradeCost(ship.CannonUpgradeLevel);

        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            HUDManager.Instance?.ShowNotEnoughCoinsMessage(cost);
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }

        ship.ApplyCannonUpgrade();
        RefreshPanel(ship);
        HUDManager.Instance?.ShowUpgradeMessage(ship.ShipName, "Cannons", ship.CannonUpgradeLevel);
        Debug.Log($"{ship.ShipName} cannon upgraded to level {ship.CannonUpgradeLevel}");
    }

    public void UpgradeSpeed(ShipBase ship)
    {
        if (ship == null || ship.Stats == null) return;

        if (!ship.CanUpgradeSpeed)
        {
            HUDManager.Instance?.ShowMessage($"{ship.ShipName}: Speed already at max!", isWarning: true);
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }

        int cost = ship.Stats.UpgradeCost(ship.SpeedUpgradeLevel);

        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            HUDManager.Instance?.ShowNotEnoughCoinsMessage(cost);
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }

        ship.ApplySpeedUpgrade();
        RefreshPanel(ship);
        HUDManager.Instance?.ShowUpgradeMessage(ship.ShipName, "Speed", ship.SpeedUpgradeLevel);
        Debug.Log($"{ship.ShipName} speed upgraded to level {ship.SpeedUpgradeLevel}");
    }

    public void UpgradeArmor(ShipBase ship)
    {
        if (ship == null || ship.Stats == null) return;

        if (!ship.CanUpgradeArmor)
        {
            HUDManager.Instance?.ShowMessage($"{ship.ShipName}: Armor already at max!", isWarning: true);
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }

        int cost = ship.Stats.UpgradeCost(ship.ArmorUpgradeLevel);
        
        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            HUDManager.Instance?.ShowNotEnoughCoinsMessage(cost);
            UIManager.Instance?.ShowShipPanel(ship);
            return;
        }
        
        ship.ApplyArmorUpgrade();
        RefreshPanel(ship);
        HUDManager.Instance?.ShowUpgradeMessage(ship.ShipName, "Armor", ship.ArmorUpgradeLevel);
        Debug.Log($"{ship.ShipName} armor upgraded to level {ship.ArmorUpgradeLevel}");
    }

    void RefreshPanel(ShipBase ship)
    {
        UIManager.Instance?.ShowShipPanel(ship);
        if (CurrencyManager.Instance != null)
            UIManager.Instance?.UpdateCurrency(CurrencyManager.Instance.CurrentGold);
    }
}