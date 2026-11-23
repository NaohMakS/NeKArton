using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadGameScene()
    {
        // Загружаем сцену по её точному имени из Build Profiles
        SceneManager.LoadScene("Location-1");
    }
}