using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shop panel that lets the player purchase new ships.
// Layout is editor-driven: this script does not create, move, resize, hide, or rebuild UI objects.
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

    [Header("Ship Costs")]
    public int SchoonerCost = 150;
    public int FrigateCost = 300;
    public int ManOWarCost = 600;

    void Awake()
    {
        AutoBindFromHierarchy();
    }

    void Start()
    {
        AutoBindFromHierarchy();
    }

    void OnEnable()
    {
        AutoBindFromHierarchy();
    }

    public void AutoBindFromHierarchy()
    {
        // These are fallbacks only. If references are assigned in the Inspector,
        // those assignments are preserved.
        if (GoldDisplay == null) GoldDisplay = FindTextByName("GoldDisplay", "GoldText", "Gold");
        if (BuySchoonerButton == null) BuySchoonerButton = FindButtonByName("BuySchoonerButton", "SchoonerButton", "Buy Schooner", "Schooner");
        if (BuyFrigateButton == null) BuyFrigateButton = FindButtonByName("BuyFrigateButton", "FrigateButton", "Buy Frigate", "Frigate");
        if (BuyManOWarButton == null) BuyManOWarButton = FindButtonByName("BuyManOWarButton", "BuyManOWar", "ManOWarButton", "Man-O-War", "Man O War");
        if (SchoonerCostText == null) SchoonerCostText = FindTextByName("SchoonerCostText", "SchoonerCost");
        if (FrigateCostText == null) FrigateCostText = FindTextByName("FrigateCostText", "FrigateCost");
        if (ManOWarCostText == null) ManOWarCostText = FindTextByName("ManOWarCostText", "ManOWarCost", "Man-O-WarCost");

        WireButtons();
        FixTextRaycastTargets();
        RefreshShopUI();
    }

    Button FindButtonByName(params string[] possibleNames)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (string wanted in possibleNames)
        {
            foreach (Button button in buttons)
            {
                if (button.name.Equals(wanted, System.StringComparison.OrdinalIgnoreCase))
                    return button;
            }
        }

        foreach (string wanted in possibleNames)
        {
            string lowered = wanted.ToLowerInvariant().Replace(" ", "").Replace("-", "");
            foreach (Button button in buttons)
            {
                string candidate = button.name.ToLowerInvariant().Replace(" ", "").Replace("-", "");
                if (candidate.Contains(lowered) || lowered.Contains(candidate))
                    return button;
            }
        }

        return null;
    }

    TextMeshProUGUI FindTextByName(params string[] possibleNames)
    {
        TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (string wanted in possibleNames)
        {
            foreach (TextMeshProUGUI label in labels)
            {
                if (label.name.Equals(wanted, System.StringComparison.OrdinalIgnoreCase))
                    return label;
            }
        }

        foreach (string wanted in possibleNames)
        {
            string lowered = wanted.ToLowerInvariant().Replace(" ", "").Replace("-", "");
            foreach (TextMeshProUGUI label in labels)
            {
                string candidate = label.name.ToLowerInvariant().Replace(" ", "").Replace("-", "");
                if (candidate.Contains(lowered) || lowered.Contains(candidate))
                    return label;
            }
        }

        return null;
    }

    void WireButtons()
    {
        if (BuySchoonerButton != null)
        {
            BuySchoonerButton.onClick.RemoveAllListeners();
            BuySchoonerButton.onClick.AddListener(BuySchooner);
        }

        if (BuyFrigateButton != null)
        {
            BuyFrigateButton.onClick.RemoveAllListeners();
            BuyFrigateButton.onClick.AddListener(BuyFrigate);
        }

        if (BuyManOWarButton != null)
        {
            BuyManOWarButton.onClick.RemoveAllListeners();
            BuyManOWarButton.onClick.AddListener(BuyManOWar);
        }
    }

    void FixTextRaycastTargets()
    {
        foreach (TextMeshProUGUI label in GetComponentsInChildren<TextMeshProUGUI>(true))
            label.raycastTarget = false;
    }

    public void RefreshShopUI()
    {
        int gold = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentGold : 0;

        if (GoldDisplay != null)
            GoldDisplay.text = $"Gold: {gold}";

        SetShipButtonText(BuySchoonerButton, "Buy Schooner", "Light ship", SchoonerCost);
        SetShipButtonText(BuyFrigateButton, "Buy Frigate", "Speed and damage", FrigateCost);
        SetShipButtonText(BuyManOWarButton, "Buy Man-O-War", "Heavy and slow", ManOWarCost);

        if (SchoonerCostText != null) SchoonerCostText.text = $"{SchoonerCost}g";
        if (FrigateCostText != null) FrigateCostText.text = $"{FrigateCost}g";
        if (ManOWarCostText != null) ManOWarCostText.text = $"{ManOWarCost}g";

        // Keep all buttons clickable so the player can get feedback in the Console
        // if they lack gold. Change these to gold >= cost if you prefer disabling.
        if (BuySchoonerButton != null) BuySchoonerButton.interactable = true;
        if (BuyFrigateButton != null) BuyFrigateButton.interactable = true;
        if (BuyManOWarButton != null) BuyManOWarButton.interactable = true;
    }

    void SetShipButtonText(Button button, string shipName, string description, int cost)
    {
        if (button == null) return;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null) return;

        label.text = $"{shipName}\n{description}    Cost: {cost}g";
        label.raycastTarget = false;
    }

    public void BuySchooner() => Buy(ShipClass.Schooner, SchoonerCost);
    public void BuyFrigate() => Buy(ShipClass.Frigate, FrigateCost);
    public void BuyManOWar() => Buy(ShipClass.ManOWar, ManOWarCost);

    void Buy(ShipClass cls, int cost)
    {
        Debug.Log($"ShopController: Buy button clicked for {cls}.");

        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("Cannot buy ship: CurrencyManager.Instance is missing.");
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.PlayerFleet == null)
        {
            Debug.LogError("Cannot buy ship: GameManager.Instance.PlayerFleet is missing.");
            return;
        }

        if (!CurrencyManager.Instance.SpendCurrency(cost))
        {
            Debug.Log($"Not enough gold to buy {cls}. Cost: {cost}, Current: {CurrencyManager.Instance.CurrentGold}");
            RefreshShopUI();
            return;
        }

        PlayerFleet fleet = GameManager.Instance.PlayerFleet;
        Vector2 randomCircle = Random.insideUnitCircle * 8f;
        Vector3 spawnPos = fleet.FleetCenter() + new Vector3(randomCircle.x, 0f, randomCircle.y);

        ShipBase purchasedShip = fleet.SpawnShip(cls, spawnPos);
        if (purchasedShip == null)
        {
            CurrencyManager.Instance.AddCurrency(cost);
            Debug.LogError($"Purchase failed: no prefab or ShipBase setup for {cls}. Refunded {cost} gold.");
            RefreshShopUI();
            return;
        }

        RefreshShopUI();
        UIManager.Instance?.UpdateCurrency(CurrencyManager.Instance.CurrentGold);
        Debug.Log($"Purchased {cls} for {cost} gold.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerFleet>() != null && UIManager.Instance?.ShopPanel != null && !UIManager.Instance.ShopPanel.activeSelf)
            UIManager.Instance.ToggleShop();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerFleet>() != null && UIManager.Instance?.ShopPanel != null && UIManager.Instance.ShopPanel.activeSelf)
            UIManager.Instance.ToggleShop();
    }
}
