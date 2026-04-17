using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Function to play the game scene
    public void PlayGame()
    {
        SceneManager.LoadScene("IronTide");
    }

    // Function to quit the game
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }
}