using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Главный экран: временная страница разработки, настройки, достижения и выход.
/// Кнопки автоматически подключаются по именам объектов в сцене.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Существующие панели")]
    [SerializeField] private GameObject creditsPanel;

    private Canvas rootCanvas;
    private GameObject runtimePanel;
    private float previousVolume = 1f;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
        ConnectButtons();
    }

    private void ConnectButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            switch (button.gameObject.name)
            {
                case "Btn_Play":
                    button.onClick.AddListener(OnPlayClicked);
                    break;
                case "Btn_Continue":
                    button.onClick.AddListener(OnContinueClicked);
                    break;
                case "Btn_Settings":
                    button.onClick.AddListener(OnSettingsClicked);
                    break;
                case "Btn_Achievements":
                    button.onClick.AddListener(OnAchievementsClicked);
                    break;
                // Эти три кнопки уже подключены через события сцены.
                case "Btn_Credits":
                case "Btn_CloseCredits":
                case "Btn_Quit":
                    break;
            }
        }
    }

    public void OnPlayClicked() => ShowDevelopmentPage();
    public void OnContinueClicked() => ShowDevelopmentPage();

    private void ShowDevelopmentPage()
    {
        ShowMessage("Ведётся разработка", "Этот раздел игры пока находится в разработке.");
    }

    public void OnSettingsClicked()
    {
        ShowSettings();
    }

    public void OnAchievementsClicked()
    {
        ShowMessage("Достижения", "Система достижений появится в следующих обновлениях.");
    }

    public void OnCreditsClicked()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void OnCloseCreditsClicked()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowMessage(string heading, string message)
    {
        GameObject panel = CreatePanel(heading);
        CreateText(panel.transform, message, 26, new Vector2(0.5f, 0.55f), new Vector2(900, 120));
        CreateButton(panel.transform, "Назад", new Vector2(0.5f, 0.30f), () => Destroy(runtimePanel));
    }

    private void ShowSettings()
    {
        GameObject panel = CreatePanel("Настройки");
        CreateText(panel.transform, "Громкость", 25, new Vector2(0.5f, 0.58f), new Vector2(400, 55));

        GameObject sliderObject = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(panel.transform, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0.48f);
        sliderRect.anchoredPosition = Vector2.zero;
        sliderRect.sizeDelta = new Vector2(420, 24);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = AudioListener.volume;
        slider.onValueChanged.AddListener(value => AudioListener.volume = value);

        CreateButton(panel.transform, "Назад", new Vector2(0.5f, 0.30f), () => Destroy(runtimePanel));
    }

    private GameObject CreatePanel(string heading)
    {
        if (rootCanvas == null)
        {
            Debug.LogError("MainMenuController: в сцене не найден Canvas.");
            return null;
        }

        if (runtimePanel != null) Destroy(runtimePanel);

        runtimePanel = new GameObject("RuntimeMenuPanel", typeof(RectTransform), typeof(Image));
        runtimePanel.transform.SetParent(rootCanvas.transform, false);
        RectTransform rect = runtimePanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image background = runtimePanel.GetComponent<Image>();
        background.color = new Color(0.025f, 0.025f, 0.035f, 0.97f);

        CreateText(runtimePanel.transform, heading, 44, new Vector2(0.5f, 0.72f), new Vector2(1000, 90));
        return runtimePanel;
    }

    private void CreateText(Transform parent, string value, int fontSize, Vector2 anchor, Vector2 size)
    {
        GameObject textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
    }

    private void CreateButton(Transform parent, string caption, Vector2 anchor, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(260, 70);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.20f, 0.20f, 0.24f, 1f);
        buttonObject.GetComponent<Button>().onClick.AddListener(action);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = caption;
        label.fontSize = 26;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
    }
}
