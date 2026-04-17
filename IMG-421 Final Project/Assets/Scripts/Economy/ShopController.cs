using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shop panel that lets the player purchase new ships.
// Trigger zone activates when the player enters a safe zone.
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
    public int FrigateCost = 300;
    public int ManOWarCost = 600;

    [Header("Layout")]
    public float ButtonPreferredHeight = 72f;
    public float LabelPreferredHeight = 44f;

    void Start()
    {
        if (SchoonerCostText) SchoonerCostText.text = $"Schooner\n{SchoonerCost}g";
        if (FrigateCostText) FrigateCostText.text = $"Frigate\n{FrigateCost}g";
        if (ManOWarCostText) ManOWarCostText.text = $"Man-O-War\n{ManOWarCost}g";

        ConfigureButtonLayout(BuySchoonerButton, SchoonerCostText);
        ConfigureButtonLayout(BuyFrigateButton, FrigateCostText);
        ConfigureButtonLayout(BuyManOWarButton, ManOWarCostText);

        BuySchoonerButton?.onClick.AddListener(() => Buy(ShipClass.Schooner, SchoonerCost));
        BuyFrigateButton?.onClick.AddListener(() => Buy(ShipClass.Frigate,  FrigateCost));
        BuyManOWarButton?.onClick.AddListener(() => Buy(ShipClass.ManOWar,  ManOWarCost));
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

    void ConfigureButtonLayout(Button button, TextMeshProUGUI label)
    {
        if (button != null)
        {
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout == null) layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = ButtonPreferredHeight;
            layout.preferredHeight = ButtonPreferredHeight;
        }

        if (label != null)
        {
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;

            LayoutElement layout = label.GetComponent<LayoutElement>();
            if (layout == null) layout = label.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = LabelPreferredHeight;
            layout.preferredHeight = LabelPreferredHeight;
        }
    }

    // Trigger zone entry/exit
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerFleet>() != null) UIManager.Instance?.ToggleShop();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerFleet>() != null) UIManager.Instance?.ToggleShop();
    }
}
