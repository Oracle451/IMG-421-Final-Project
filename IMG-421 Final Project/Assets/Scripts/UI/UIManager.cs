using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Central UI manager: HUD overlays, win/lose screens, ship info panels.
// Layout is editor-driven: this script does not move, resize, create, or rebuild UI objects.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI CurrencyText;
    public TextMeshProUGUI ZoneText;
    public TextMeshProUGUI FleetCountText;

    [Header("Ship Info / Upgrade Panel")]
    public GameObject ShipInfoPanel;
    public TextMeshProUGUI ShipNameText;
    public TextMeshProUGUI ShipClassText;
    public Slider HealthSlider;
    public Button RepairButton;
    public Button UpgradeSpeedButton;
    public Button UpgradeArmorButton;
    public Button UpgradeCannonsButton;
    public Button SellButton;

    [Header("Win / Lose Screens")]
    public GameObject WinScreen;
    public GameObject LoseScreen;

    [Header("Ship Purchase Panel")]
    public GameObject ShopPanel;

    private ShipBase _selectedShip;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        EnsureShopController();

        CloseShipPanel();
        WinScreen?.SetActive(false);
        LoseScreen?.SetActive(false);
        ShopPanel?.SetActive(false);
    }

    void EnsureShopController()
    {
        if (ShopPanel == null) return;

        ShopController controller = ShopPanel.GetComponent<ShopController>();
        if (controller == null)
            controller = ShopPanel.AddComponent<ShopController>();

        controller.AutoBindFromHierarchy();
    }

    // HUD Updates

    public void UpdateCurrency(int amount)
    {
        if (CurrencyText) CurrencyText.text = $"Gold: {amount}";

        ShopController shop = ShopPanel != null ? ShopPanel.GetComponent<ShopController>() : null;
        shop?.RefreshShopUI();

        if (_selectedShip != null)
            RefreshShipPanelText(_selectedShip);
    }

    public void UpdateZone(string zoneName)
    {
        if (ZoneText) ZoneText.text = $"Zone: {zoneName}";
    }

    public void UpdateFleetCount(int alive, int total)
    {
        if (FleetCountText) FleetCountText.text = $"Fleet: {alive}/{total}";
    }

    // Ship Panel

    public void ShowShipPanel(ShipBase ship)
    {
        if (ship == null) return;

        _selectedShip = ship;
        ShipInfoPanel?.SetActive(true);

        RefreshShipPanelText(ship);

        RepairButton?.onClick.RemoveAllListeners();
        RepairButton?.onClick.AddListener(() => OnRepairClicked());

        UpgradeArmorButton?.onClick.RemoveAllListeners();
        UpgradeArmorButton?.onClick.AddListener(() => UpgradeSystem.Instance?.UpgradeArmor(ship));

        UpgradeSpeedButton?.onClick.RemoveAllListeners();
        UpgradeSpeedButton?.onClick.AddListener(() => UpgradeSystem.Instance?.UpgradeSpeed(ship));

        UpgradeCannonsButton?.onClick.RemoveAllListeners();
        UpgradeCannonsButton?.onClick.AddListener(() => UpgradeSystem.Instance?.UpgradeCannons(ship));

        SellButton?.onClick.RemoveAllListeners();
        SellButton?.onClick.AddListener(() => OnSellClicked());
    }

    void RefreshShipPanelText(ShipBase ship)
    {
        if (ship == null || ship.Stats == null) return;

        if (ShipNameText) ShipNameText.text = ship.ShipName;
        if (ShipClassText)
        {
            int gold = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentGold : 0;
            ShipClassText.text = $"{ship.Stats.ShipClass}    Gold: {gold}";
        }

        if (HealthSlider)
        {
            HealthSlider.maxValue = ship.EffectiveMaxHealth;
            HealthSlider.value = ship.CurrentHealth;
        }

        SetUpgradeButtonText(
            UpgradeSpeedButton,
            "Speed",
            ship.SpeedUpgradeLevel,
            ship.Stats.MaxSpeedUpgrades,
            ship.CanUpgradeSpeed ? ship.Stats.UpgradeCost(ship.SpeedUpgradeLevel) : 0,
            ship.CanUpgradeSpeed
        );

        SetUpgradeButtonText(
            UpgradeArmorButton,
            "Armor",
            ship.ArmorUpgradeLevel,
            ship.Stats.MaxArmorUpgrades,
            ship.CanUpgradeArmor ? ship.Stats.UpgradeCost(ship.ArmorUpgradeLevel) : 0,
            ship.CanUpgradeArmor
        );

        SetUpgradeButtonText(
            UpgradeCannonsButton,
            "Cannons",
            ship.CannonUpgradeLevel,
            ship.Stats.MaxCannonUpgrades,
            ship.CanUpgradeCannons ? ship.Stats.UpgradeCost(ship.CannonUpgradeLevel) : 0,
            ship.CanUpgradeCannons
        );
    }

    void SetUpgradeButtonText(Button button, string labelName, int level, int maxLevel, int cost, bool canUpgrade)
    {
        if (button == null) return;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = canUpgrade
                ? $"{labelName} Lv {level}/{maxLevel}  |  {cost}g"
                : $"{labelName} Lv {level}/{maxLevel}  |  MAX";

            label.raycastTarget = false;
        }

        int gold = CurrencyManager.Instance != null ? CurrencyManager.Instance.CurrentGold : 0;
        button.interactable = canUpgrade && gold >= cost;
    }

    public void CloseShipPanel()
    {
        _selectedShip = null;
        ShipInfoPanel?.SetActive(false);
    }

    void OnRepairClicked()
    {
        if (_selectedShip == null) return;

        int cost = Mathf.RoundToInt((_selectedShip.EffectiveMaxHealth - _selectedShip.CurrentHealth) * 0.5f);
        if (CurrencyManager.Instance.SpendCurrency(cost))
        {
            _selectedShip.Repair(_selectedShip.EffectiveMaxHealth);
            RefreshShipPanelText(_selectedShip);
        }
    }

    void OnSellClicked()
    {
        if (_selectedShip == null) return;

        CurrencyManager.Instance.AddCurrency(_selectedShip.Stats.SellValue);
        GameManager.Instance.PlayerFleet.RemoveShip(_selectedShip);
        Destroy(_selectedShip.gameObject);
        CloseShipPanel();
    }

    // Win / Lose

    public void ShowWinScreen()  => WinScreen?.SetActive(true);
    public void ShowLoseScreen() => LoseScreen?.SetActive(true);

    // Shop

    public void ToggleShop()
    {
        if (ShopPanel == null) return;

        EnsureShopController();
        ShopPanel.SetActive(!ShopPanel.activeSelf);

        ShopController shop = ShopPanel.GetComponent<ShopController>();
        shop?.RefreshShopUI();
    }
}
