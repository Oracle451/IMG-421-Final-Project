using UnityEngine;
using UnityEngine.UI;

public class ControlsMenu : MonoBehaviour
{
    public GameObject ControlsPanel;
    public Button OpenButton;
    public Button CloseButton;

    void Start()
    {
        if (ControlsPanel != null)
            ControlsPanel.SetActive(false);

        if (OpenButton != null)
            OpenButton.onClick.AddListener(() => ControlsPanel.SetActive(true));

        if (CloseButton != null)
            CloseButton.onClick.AddListener(() => ControlsPanel.SetActive(false));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && ControlsPanel != null && ControlsPanel.activeSelf)
            ControlsPanel.SetActive(false);
    }
}