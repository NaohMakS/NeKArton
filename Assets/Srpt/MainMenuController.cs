using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Панели")]
    public GameObject creditsPanel;

    public void OnCreditsClicked()
    {
        creditsPanel.SetActive(true);
    }

    public void OnCloseCreditsClicked()
    {
        creditsPanel.SetActive(false);
    }
    public void OnQuitClicked()
    {

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;

#else
            Application.Quit();
#endif

        Debug.Log("Игра закрывается...");
    }
}
