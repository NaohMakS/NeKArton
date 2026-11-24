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
            if (spriteRenderer != null && idleSprite != null) spriteRenderer.sprite = idleSprite;
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
        SetPanel(pausePanel, isPaused);
        if (statusText != null)
            statusText.text = isPaused ? "ПАУЗА\nНажмите ESC, чтобы вернуться" : string.Empty;
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
        if (fadeOverlay != null && statusText != null) return;

        uiCanvas = FindObjectOfType<Canvas>();
        if (uiCanvas == null)
        {
            GameObject canvasObject = new GameObject("PlayerRuntimeCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            uiCanvas = canvasObject.GetComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        runtimeUi = new GameObject("PlayerRuntimeOverlay", typeof(RectTransform));
        runtimeUi.transform.SetParent(uiCanvas.transform, false);
        RectTransform root = runtimeUi.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        if (fadeOverlay == null)
        {
            GameObject fadeObject = new GameObject("LocationFade", typeof(RectTransform), typeof(Image));
            fadeObject.transform.SetParent(runtimeUi.transform, false);
            RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            fadeOverlay = fadeObject.GetComponent<Image>();
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            fadeOverlay.raycastTarget = false;
            fadeObject.SetActive(false);
        }

        if (statusText == null)
        {
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
        }
    }

    private void OnDisable()
    {
        if (isPaused) Time.timeScale = 1f;
    }
}
