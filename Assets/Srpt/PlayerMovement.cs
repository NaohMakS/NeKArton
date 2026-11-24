using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Базовое управление персонажем для 2D-проекта NeKArton.
/// A/D — движение, Tab — характеристики, Enter — затемнение/переход,
/// Esc — пауза, I — инвентарь.
/// </summary>
[RequireComponent(typeof(Transform))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Анимация ходьбы")]
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite idleSprite;
    [SerializeField, Min(1f)] private float animationFramesPerSecond = 9f;

    [Header("Камера и границы локации")]
    [SerializeField] private Camera followCamera;
    [SerializeField] private SpriteRenderer locationBounds;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0f, -10f);
    [SerializeField, Min(0.01f)] private float cameraSmoothTime = 0.12f;

    [Header("Переход между локациями")]
    [SerializeField] private string destinationScene;
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.6f;
    [SerializeField] private Image fadeOverlay;

    [Header("Панели интерфейса (необязательно)")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Sprite inventoryIcon;

    private GameObject runtimeInventoryPanel;
    private GameObject runtimePausePanel;

    private Canvas uiCanvas;
    private GameObject runtimeUi;
    private TextMeshProUGUI statusText;
    private bool isPaused;
    private bool isTransitioning;
    private string activeFallbackPanel = string.Empty;
    private float horizontalInput;
    private float animationTimer;
    private int animationFrame;
    private float cameraVelocityX;
    private float cameraVelocityY;

    private void Awake()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (followCamera == null) followCamera = Camera.main;
        if (locationBounds == null)
        {
            GameObject boundsObject = GameObject.Find("Background_FirstLocation");
            if (boundsObject != null) locationBounds = boundsObject.GetComponent<SpriteRenderer>();
        }
        if (spriteRenderer != null)
            spriteRenderer.sprite = idleSprite != null ? idleSprite : (walkFrames != null && walkFrames.Length > 0 ? walkFrames[0] : null);
        EnsureUi();
        SetPanel(statsPanel, false);
        SetPanel(inventoryPanel, false);
        SetPanel(pausePanel, false);
        if (fadeOverlay != null) SetFade(0f);
    }

    private void Update()
    {
        if (Keyboard.current == null || isTransitioning) return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
            return;
        }

        if (isPaused) return;

        if (DialogueManager.IsActive)
        {
            horizontalInput = 0f;
            if (spriteRenderer != null)
                spriteRenderer.sprite = idleSprite != null ? idleSprite :
                    (walkFrames != null && walkFrames.Length > 0 ? walkFrames[0] : spriteRenderer.sprite);
            return;
        }

        horizontalInput = 0f;
        if (keyboard.aKey.isPressed) horizontalInput -= 1f;
        if (keyboard.dKey.isPressed) horizontalInput += 1f;
        UpdateWalkAnimation(Time.deltaTime);

        if (keyboard.tabKey.wasPressedThisFrame) TogglePanel(statsPanel, "ХАРАКТЕРИСТИКИ");
        if (keyboard.iKey.wasPressedThisFrame) TogglePanel(inventoryPanel, "ИНВЕНТАРЬ");

        if (keyboard.enterKey.wasPressedThisFrame)
        {
            if (!string.IsNullOrWhiteSpace(destinationScene))
                StartCoroutine(LoadLocation(destinationScene));
            else
                StartCoroutine(BriefFade());
        }
    }

    private void FixedUpdate()
    {
        if (isPaused || isTransitioning) return;

        if (DialogueManager.IsActive)
        {
            horizontalInput = 0f;
            if (body != null) body.linearVelocity = Vector2.zero;
            return;
        }

        float nextX = transform.position.x + horizontalInput * moveSpeed * Time.fixedDeltaTime;
        nextX = ClampPlayerX(nextX);

        if (body != null)
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = horizontalInput * moveSpeed;
            body.linearVelocity = velocity;
            body.position = new Vector2(nextX, body.position.y);
        }
        else
        {
            transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
        }
    }

    private float ClampPlayerX(float x)
    {
        if (locationBounds == null) return x;

        Bounds bounds = locationBounds.bounds;
        float halfWidth = GetPlayerHalfWidth();
        float minX = bounds.min.x + halfWidth;
        float maxX = bounds.max.x - halfWidth;

        if (minX > maxX) return bounds.center.x;
        return Mathf.Clamp(x, minX, maxX);
    }

    private float GetPlayerHalfWidth()
    {
        if (spriteRenderer == null) return 0f;

        float maxHalfWidth = spriteRenderer.sprite != null
            ? spriteRenderer.sprite.bounds.extents.x
            : 0f;

        if (walkFrames != null)
        {
            foreach (Sprite frame in walkFrames)
                if (frame != null)
                    maxHalfWidth = Mathf.Max(maxHalfWidth, frame.bounds.extents.x);
        }

        return maxHalfWidth * Mathf.Abs(transform.lossyScale.x);
    }

    private void UpdateWalkAnimation(float deltaTime)
    {
        if (spriteRenderer == null || walkFrames == null || walkFrames.Length == 0)
            return;

        if (Mathf.Abs(horizontalInput) < 0.01f)
        {
            animationTimer = 0f;
            animationFrame = 0;
            spriteRenderer.sprite = idleSprite != null ? idleSprite : walkFrames[0];
            return;
        }

        spriteRenderer.flipX = horizontalInput < 0f;
        animationTimer += deltaTime;
        float frameDuration = 1f / animationFramesPerSecond;

        while (animationTimer >= frameDuration)
        {
            animationTimer -= frameDuration;
            animationFrame = (animationFrame + 1) % walkFrames.Length;
        }

        spriteRenderer.sprite = walkFrames[animationFrame];
    }

    private void LateUpdate()
    {
        if (followCamera == null || locationBounds == null) return;

        Bounds bounds = locationBounds.bounds;
        float halfHeight = followCamera.orthographicSize;
        float halfWidth = halfHeight * followCamera.aspect;

        float minCameraX = bounds.min.x + halfWidth;
        float maxCameraX = bounds.max.x - halfWidth;
        float minCameraY = bounds.min.y + halfHeight;
        float maxCameraY = bounds.max.y - halfHeight;

        float targetX = minCameraX > maxCameraX
            ? bounds.center.x
            : Mathf.Clamp(transform.position.x + cameraOffset.x, minCameraX, maxCameraX);
        float targetY = minCameraY > maxCameraY
            ? bounds.center.y
            : Mathf.Clamp(transform.position.y + cameraOffset.y, minCameraY, maxCameraY);

        Vector3 cameraPosition = followCamera.transform.position;
        cameraPosition.x = Mathf.SmoothDamp(cameraPosition.x, targetX, ref cameraVelocityX, cameraSmoothTime);
        cameraPosition.y = Mathf.SmoothDamp(cameraPosition.y, targetY, ref cameraVelocityY, cameraSmoothTime);
        cameraPosition.z = cameraOffset.z;
        followCamera.transform.position = cameraPosition;
    }

    private void TogglePanel(GameObject panel, string title)
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
            return;
        }

        if (title == "ИНВЕНТАРЬ" && runtimeInventoryPanel != null)
        {
            runtimeInventoryPanel.SetActive(!runtimeInventoryPanel.activeSelf);
            return;
        }

        activeFallbackPanel = activeFallbackPanel == title ? string.Empty : title;
        if (statusText != null)
        {
            statusText.text = string.IsNullOrEmpty(activeFallbackPanel)
                ? string.Empty
                : activeFallbackPanel + (title == "ИНВЕНТАРЬ"
                    ? "\nИнвентарь пока пуст."
                    : "\nХарактеристики персонажа появятся здесь.");
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pausePanel != null) SetPanel(pausePanel, isPaused);
        else if (runtimePausePanel != null) runtimePausePanel.SetActive(isPaused);

        if (statusText != null) statusText.text = string.Empty;
    }

    /// <summary>Можно вызвать из другого скрипта/триггера для перехода в указанную сцену.</summary>
    public void TransitionToScene(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName) && !isTransitioning)
            StartCoroutine(LoadLocation(sceneName));
    }

    private IEnumerator BriefFade()
    {
        isTransitioning = true;
        yield return Fade(1f);
        if (statusText != null) statusText.text = "ПЕРЕХОД МЕЖДУ ЛОКАЦИЯМИ";
        yield return new WaitForSecondsRealtime(fadeDuration * 0.35f);
        yield return Fade(0f);
        if (statusText != null) statusText.text = string.Empty;
        isTransitioning = false;
    }

    private IEnumerator LoadLocation(string sceneName)
    {
        isTransitioning = true;
        Time.timeScale = 1f;
        yield return Fade(1f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError("PlayerMovement: не удалось начать загрузку сцены '" + sceneName +
                           "'. Проверьте имя и добавление сцены в Build Settings.");
            yield return Fade(0f);
            isTransitioning = false;
            yield break;
        }

        while (!operation.isDone) yield return null;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.gameObject.SetActive(true);
        float startAlpha = fadeOverlay.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFade(Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration));
            yield return null;
        }

        SetFade(targetAlpha);
        if (Mathf.Approximately(targetAlpha, 0f))
            fadeOverlay.gameObject.SetActive(false);
    }

    private void SetFade(float alpha)
    {
        if (fadeOverlay == null) return;
        Color color = fadeOverlay.color;
        color.a = alpha;
        fadeOverlay.color = color;
    }

    private static void SetPanel(GameObject panel, bool visible)
    {
        if (panel != null) panel.SetActive(visible);
    }

    private void EnsureUi()
    {
        // Отдельный Overlay Canvas не зависит от масштаба/режима Canvas сцены.
        GameObject canvasObject = new GameObject("PlayerRuntimeCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        uiCanvas = canvasObject.GetComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 200;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        runtimeUi = new GameObject("PlayerRuntimeOverlay", typeof(RectTransform));
        runtimeUi.transform.SetParent(uiCanvas.transform, false);
        RectTransform root = runtimeUi.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        GameObject fadeObject = new GameObject("LocationFade", typeof(RectTransform), typeof(Image));
        fadeObject.transform.SetParent(runtimeUi.transform, false);
        RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
        Stretch(fadeRect);
        fadeOverlay = fadeObject.GetComponent<Image>();
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false;
        fadeObject.SetActive(false);

        GameObject textObject = new GameObject("PlayerStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(runtimeUi.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(700f, 180f);
        statusText = textObject.GetComponent<TextMeshProUGUI>();
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = 30f;
        statusText.color = Color.white;
        statusText.raycastTarget = false;

        CreateInventoryHud();
        CreateRuntimeInventoryPanel();
        CreateRuntimePausePanel();
    }

    private void CreateInventoryHud()
    {
        Button button = CreateUiButton("InventoryHudButton", runtimeUi.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f),
            new Vector2(230f, 76f), "ИНВЕНТАРЬ");
        button.onClick.AddListener(() => TogglePanel(inventoryPanel, "ИНВЕНТАРЬ"));

        if (inventoryIcon != null)
        {
            GameObject iconObject = new GameObject("InventoryIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(48f, 48f);
            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = inventoryIcon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = "ИНВЕНТАРЬ";
            label.alignment = TextAlignmentOptions.Midline;
            label.fontSize = 23f;
            label.margin = new Vector4(inventoryIcon != null ? 68f : 12f, 0f, 8f, 0f);
        }
    }

    private void CreateRuntimeInventoryPanel()
    {
        runtimeInventoryPanel = CreateUiPanel("InventoryPanel", new Vector2(560f, 390f));
        AddPanelTitle(runtimeInventoryPanel.transform, "ИНВЕНТАРЬ");
        AddPanelBody(runtimeInventoryPanel.transform, "Пока здесь пусто. Найденные предметы будут появляться здесь.");
        runtimeInventoryPanel.SetActive(false);
    }

    private void CreateRuntimePausePanel()
    {
        runtimePausePanel = CreateUiPanel("PauseMenu", new Vector2(520f, 390f));
        AddPanelTitle(runtimePausePanel.transform, "ПАУЗА");
        Button resume = CreateUiButton("ResumeButton", runtimePausePanel.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -28f),
            new Vector2(300f, 66f), "ПРОДОЛЖИТЬ");
        resume.onClick.AddListener(TogglePause);

        Button exit = CreateUiButton("ExitButton", runtimePausePanel.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -116f),
            new Vector2(300f, 66f), "ВЫХОД");
        exit.onClick.AddListener(ExitGame);
        runtimePausePanel.SetActive(false);
    }

    private GameObject CreateUiPanel(string name, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(runtimeUi.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.035f, 0.045f, 0.06f, 0.96f);
        return panel;
    }

    private void AddPanelTitle(Transform parent, string title)
    {
        GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(parent, false);
        RectTransform rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -28f);
        rect.sizeDelta = new Vector2(-40f, 64f);
        TextMeshProUGUI text = titleObject.GetComponent<TextMeshProUGUI>();
        text.text = title;
        text.fontSize = 34f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.82f, 0.43f);
    }

    private void AddPanelBody(Transform parent, string value)
    {
        GameObject body = new GameObject("Description", typeof(RectTransform), typeof(TextMeshProUGUI));
        body.transform.SetParent(parent, false);
        RectTransform rect = body.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.32f);
        rect.anchorMax = new Vector2(0.92f, 0.68f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = body.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = 23f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
    }

    private Button CreateUiButton(string name, Transform parent, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 position, Vector2 size, string caption)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMin.x, anchorMin.y);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.19f, 0.23f, 0.98f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.28f, 0.32f, 0.37f, 1f);
        colors.pressedColor = new Color(0.42f, 0.30f, 0.16f, 1f);
        button.colors = colors;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = caption;
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return button;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ExitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisable()
    {
        if (isPaused) Time.timeScale = 1f;
    }
}
