using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopController : MonoBehaviour
{
    private Button[] buyButtons;

    [Header("Ship Costs")]
    public int SchoonerCost = 150;
    public int FrigateCost = 300;
    public int ManOWarCost = 600;

    void Start()
    {
        // Grab ALL buttons under this shop
        buyButtons = GetComponentsInChildren<Button>(true);

        if (buyButtons.Length < 3)
        {
            Debug.LogError("Not enough buttons found in ShopController");
            return;
        }

        // Clear existing listeners
        foreach (Button btn in buyButtons)
            btn.onClick.RemoveAllListeners();

        // Assign based on order in hierarchy
        buyButtons[0].onClick.AddListener(BuySchooner);
        buyButtons[1].onClick.AddListener(BuyFrigate);
        buyButtons[2].onClick.AddListener(BuyManOWar);

        Debug.Log("Buttons wired automatically");
    }

    public void BuySchooner()
    {
        Buy(ShipClass.Schooner, SchoonerCost);
    }

    public void BuyFrigate()
    {
        Buy(ShipClass.Frigate, FrigateCost);
    }

    public void BuyManOWar()
    {
        Buy(ShipClass.ManOWar, ManOWarCost);
    }

    private void Buy(ShipClass shipClass, int cost)
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("CurrencyManager missing");
            return;
        }

        PlayerFleet fleet = null;

        if (GameManager.Instance != null)
            fleet = GameManager.Instance.PlayerFleet;

        if (fleet == null)
            fleet = FindObjectOfType<PlayerFleet>();

        if (fleet == null)
        {
            Debug.LogError("PlayerFleet missing");
            return;
        }

        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            Debug.Log("Not enough gold");
            return;
        }

        Vector3 spawnPosition = fleet.transform.position;

        ShipBase newShip = fleet.SpawnShip(shipClass, spawnPosition);

        if (newShip == null)
        {
            CurrencyManager.Instance.AddCurrency(cost);
            Debug.LogError("Spawn failed, refunded");
            return;
        }

        Debug.Log("Purchased " + shipClass);
    }
    public void RefreshShopUI()
{
    // Kept because UIManager calls this.
    // Button purchasing still works without UI text updates here.
}
}