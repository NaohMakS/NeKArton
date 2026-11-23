using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Панели")]
    public GameObject creditsPanel;

    private Canvas rootCanvas;
    private GameObject runtimePanel;
    private SceneLoader sceneLoader;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) rootCanvas = FindObjectOfType<Canvas>();
        sceneLoader = GetComponent<SceneLoader>();
        if (sceneLoader == null) sceneLoader = gameObject.AddComponent<SceneLoader>();
        ConnectButtons();
    }

    private void ConnectButtons()
    {
        // В текущей сцене часть пунктов меню — только TMP-текст без компонента Button.
        // Добавляем Button на тот же объект: TMP-графика станет областью нажатия.
        Transform[] objects = FindObjectsOfType<Transform>(true);
        foreach (Transform item in objects)
        {
            switch (item.name)
            {
                case "Btn_Play":
                    Bind(item.gameObject, OnPlayClicked);
                    break;
                case "Btn_Continue":
                    Bind(item.gameObject, OnContinueClicked);
                    break;
                case "Btn_Settings":
                    Bind(item.gameObject, OnSettingsClicked);
                    break;
                case "Btn_Achievements":
                    Bind(item.gameObject, OnAchievementsClicked);
                    break;
            }
        }
    }

    private static void Bind(GameObject target, UnityEngine.Events.UnityAction action)
    {
        Button button = target.GetComponent<Button>();
        if (button == null) button = target.AddComponent<Button>();
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    public void OnPlayClicked()
    {
        // Используем SceneLoader, уже добавленный в проект.
        if (sceneLoader == null) sceneLoader = GetComponent<SceneLoader>();
        if (sceneLoader == null) sceneLoader = gameObject.AddComponent<SceneLoader>();
        sceneLoader.LoadGameScene();
    }
    public void OnContinueClicked() => ShowDevelopmentPage();

    private void ShowDevelopmentPage()
    {
        ShowMessage("Ведётся разработка", "Этот раздел игры пока находится в разработке.");
    }

    public void OnSettingsClicked()
    {
        GameObject panel = CreatePanel("Настройки");
        if (panel == null) return;

        CreateText(panel.transform, "Громкость", 26, new Vector2(0.5f, 0.58f), new Vector2(420, 60));

        GameObject sliderObject = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(panel.transform, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0.47f);
        sliderRect.anchoredPosition = Vector2.zero;
        sliderRect.sizeDelta = new Vector2(420, 28);
        sliderObject.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(10f, 0f);
        fillAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.45f, 0.7f, 0.9f, 1f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(sliderObject.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(22f, 22f);
        handle.GetComponent<Image>().color = Color.white;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = AudioListener.volume;
        slider.onValueChanged.AddListener(value => AudioListener.volume = value);

        CreateButton(panel.transform, "Назад", new Vector2(0.5f, 0.30f), CloseRuntimePanel);
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
        if (panel == null) return;
        CreateText(panel.transform, message, 26, new Vector2(0.5f, 0.55f), new Vector2(900, 120));
        CreateButton(panel.transform, "Назад", new Vector2(0.5f, 0.30f), CloseRuntimePanel);
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

    private void CloseRuntimePanel()
    {
        if (runtimePanel != null) Destroy(runtimePanel);
    }
}
