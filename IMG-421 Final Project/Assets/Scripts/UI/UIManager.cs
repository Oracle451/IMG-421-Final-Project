using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Central UI manager: HUD overlays, win/lose screens, ship info panels.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI CurrencyText;
    public TextMeshProUGUI ZoneText;
    public TextMeshProUGUI FleetCountText;

    [Header("Ship Info Panel (right sidebar)")]
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

    [Header("Shop Panel")]
    public GameObject ShopPanel;

    private ShipBase _selectedShip;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CloseShipPanel();
        WinScreen?.SetActive(false);
        LoseScreen?.SetActive(false);
        ShopPanel?.SetActive(false);
    }

    // HUD Updates

    public void UpdateCurrency(int amount)
    {
        if (CurrencyText) CurrencyText.text = $"Gold: {amount}";
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
        _selectedShip = ship;
        ShipInfoPanel?.SetActive(true);

        if (ShipNameText) ShipNameText.text  = ship.ShipName;
        if (ShipClassText) ShipClassText.text  = ship.Stats.ShipClass.ToString();
        if (HealthSlider)
        {
            HealthSlider.maxValue = ship.Stats.MaxHealth;
            HealthSlider.value = ship.CurrentHealth;
        }

        // Wire buttons dynamically (removes previous listeners first)
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

    public void CloseShipPanel()
    {
        _selectedShip = null;
        ShipInfoPanel?.SetActive(false);
    }

    void OnRepairClicked()
    {
        if (_selectedShip == null) return;
        int cost = Mathf.RoundToInt((_selectedShip.Stats.MaxHealth - _selectedShip.CurrentHealth) * 0.5f);
        if (CurrencyManager.Instance.SpendCurrency(cost)) _selectedShip.Repair(_selectedShip.Stats.MaxHealth);
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

    public void ToggleShop() => ShopPanel?.SetActive(!ShopPanel.activeSelf);
}
