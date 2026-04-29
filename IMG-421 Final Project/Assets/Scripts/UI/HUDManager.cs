using UnityEngine;
using TMPro;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI MessageText;

    [Header("Message Settings")]
    public float MessageDisplayDuration = 2f;
    public Color NormalMessageColor = Color.white;
    public Color WarningMessageColor = Color.red;
    public Color SuccessMessageColor = Color.green;

    private Coroutine _currentMessageCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        if (MessageText != null)
            MessageText.text = "";
    }

    public void ShowMessage(string message, bool isWarning = false, bool isSuccess = false)
    {
        if (MessageText == null)
        {
            Debug.LogWarning("HUDManager: MessageText not assigned!");
            return;
        }

        if (_currentMessageCoroutine != null)
            StopCoroutine(_currentMessageCoroutine);

        _currentMessageCoroutine = StartCoroutine(DisplayMessage(message, isWarning, isSuccess));
    }

    private IEnumerator DisplayMessage(string message, bool isWarning, bool isSuccess)
    {
        if (isWarning)
            MessageText.color = WarningMessageColor;
        else if (isSuccess)
            MessageText.color = SuccessMessageColor;
        else
            MessageText.color = NormalMessageColor;

        MessageText.text = message;

        yield return new WaitForSeconds(MessageDisplayDuration);

        MessageText.text = "";
        _currentMessageCoroutine = null;
    }

    public void ShowUpgradeMessage(string shipName, string upgradeType, int newLevel)
    {
        ShowMessage($"{shipName}: {upgradeType} upgraded to Level {newLevel}!", isSuccess: true);
    }

    public void ShowNotEnoughCoinsMessage(int cost)
    {
        ShowMessage($"Not enough coins! Need {cost} gold.", isWarning: true);
    }

    public void ShowPurchaseMessage(string itemName, int cost)
    {
        ShowMessage($"Purchased {itemName} for {cost} gold!", isSuccess: true);
    }
}