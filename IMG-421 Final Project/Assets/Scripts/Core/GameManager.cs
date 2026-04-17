using UnityEngine;
using UnityEngine.SceneManagement;

// Central singleton that owns top-level game state and coordinates major systems.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public PlayerFleet PlayerFleet;
    public ZoneManager ZoneManager;
    public CurrencyManager CurrencyManager;
    public SpawnManager SpawnManager;
    public CameraController CameraController;

    [Header("Win / Lose")]
    public GameObject CentralStronghold;

    public enum GameState { Playing, Won, Lost, Paused }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (PlayerFleet == null) PlayerFleet = FindObjectOfType<PlayerFleet>();
        if (ZoneManager == null) ZoneManager = FindObjectOfType<ZoneManager>();
        if (CurrencyManager == null) CurrencyManager = FindObjectOfType<CurrencyManager>();
        if (SpawnManager == null) SpawnManager = FindObjectOfType<SpawnManager>();
        if (CameraController == null) CameraController = FindObjectOfType<CameraController>();
    }

    // Win / Lose

    public void OnStrongholdDestroyed()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.Won;
        UIManager.Instance?.ShowWinScreen();
        Debug.Log("IronTide: Player Won!");
    }

    public void OnPlayerFleetDestroyed()
    {
        if (CurrentState != GameState.Playing) return;
        CurrentState = GameState.Lost;
        UIManager.Instance?.ShowLoseScreen();
        Debug.Log("IronTide: Player Lost!");
    }

    // Pause

    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
        }
        else if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
        }
    }

    // Restart

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
