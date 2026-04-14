using UnityEngine;

/// <summary>
/// Manages the player's currency (gold). Singleton.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Starting Currency")]
    public int StartAmount = 200;

    public int CurrentGold { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CurrentGold = StartAmount;
        UIManager.Instance?.UpdateCurrency(CurrentGold);
    }

    public void AddCurrency(int amount)
    {
        CurrentGold += amount;
        UIManager.Instance?.UpdateCurrency(CurrentGold);
    }

    /// <returns>True if purchase succeeded.</returns>
    public bool SpendCurrency(int amount)
    {
        if (amount > CurrentGold) return false;
        CurrentGold -= amount;
        UIManager.Instance?.UpdateCurrency(CurrentGold);
        return true;
    }

    public bool CanAfford(int amount) => CurrentGold >= amount;
}
