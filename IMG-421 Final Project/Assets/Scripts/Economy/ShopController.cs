using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shop panel that lets the player purchase new ships.
/// Trigger zone activates when the player enters a safe zone.
/// </summary>
public class ShopController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI GoldDisplay;
    public Button BuySchoonerButton;
    public Button BuyFrigateButton;
    public Button BuyManOWarButton;
    public TextMeshProUGUI SchoonerCostText;
    public TextMeshProUGUI FrigateCostText;
    public TextMeshProUGUI ManOWarCostText;

    [Header("Ship Costs (override if no stats ref)")]
    public int SchoonerCost = 150;
    public int FrigateCost  = 300;
    public int ManOWarCost  = 600;

    void Start()
    {
        if (SchoonerCostText) SchoonerCostText.text = $"Schooner\n{SchoonerCost}g";
        if (FrigateCostText)  FrigateCostText.text  = $"Frigate\n{FrigateCost}g";
        if (ManOWarCostText)  ManOWarCostText.text   = $"Man-O-War\n{ManOWarCost}g";

        BuySchoonerButton?.onClick.AddListener(() => Buy(ShipClass.Schooner, SchoonerCost));
        BuyFrigateButton?.onClick.AddListener(()  => Buy(ShipClass.Frigate,  FrigateCost));
        BuyManOWarButton?.onClick.AddListener(()  => Buy(ShipClass.ManOWar,  ManOWarCost));
    }

    void OnEnable()
    {
        RefreshGoldDisplay();
    }

    void Buy(ShipClass cls, int cost)
    {
        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            Debug.Log("Not enough gold!");
            return;
        }
        GameManager.Instance.PlayerFleet.SpawnShip(cls,
            GameManager.Instance.PlayerFleet.FleetCenter() + Random.insideUnitSphere.With(y: 0) * 8f);
        RefreshGoldDisplay();
    }

    void RefreshGoldDisplay()
    {
        if (GoldDisplay) GoldDisplay.text = $"Gold: {CurrencyManager.Instance.CurrentGold}";
    }

    // Trigger zone entry/exit
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerFleet>() != null)
            UIManager.Instance?.ToggleShop();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerFleet>() != null)
            UIManager.Instance?.ToggleShop();
    }
}
