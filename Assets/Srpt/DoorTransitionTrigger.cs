using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class DoorTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string destinationScene = "";
    [SerializeField] private string hint = "Подойдите к двери, чтобы выйти";
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private Button exitButton;

    private bool playerNearby;
    private bool loading;

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.9f);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
        Gizmos.matrix = oldMatrix;
    }

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        EnsureHintText();
        if (hintText != null) hintText.gameObject.SetActive(false);
    }

    private void EnsureHintText()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("DoorHintCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        if (hintText == null)
        {
            GameObject textObject = new GameObject("DoorEnterHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 36f);
            rect.sizeDelta = new Vector2(900f, 90f);
            hintText = textObject.GetComponent<TextMeshProUGUI>();
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.fontSize = 28f;
            hintText.color = Color.white;
            hintText.raycastTarget = false;
        }

        if (exitButton == null)
        {
            GameObject buttonObject = new GameObject("ExitButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 112f);
            buttonRect.sizeDelta = new Vector2(220f, 58f);
            Image buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.12f, 0.16f, 0.2f, 0.95f);
            exitButton = buttonObject.GetComponent<Button>();
            exitButton.targetGraphic = buttonImage;
            exitButton.onClick.AddListener(LoadDestination);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Выйти";
            label.fontSize = 26f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        hintText.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);
    }

    private void LoadDestination()
    {
        if (loading) return;
        if (string.IsNullOrWhiteSpace(destinationScene))
        {
            Debug.LogWarning("[NeKArton] У двери не задана Destination Scene: " + name, this);
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            Debug.LogError("[NeKArton] Сцена '" + destinationScene +
                "' отсутствует в Build Settings. Добавь её через File > Build Profiles > Scene List.", this);
            return;
        }
        loading = true;
        SceneManager.LoadScene(destinationScene);
    }

    private void Update()
    {
        if (!playerNearby || loading) return;
        if (hintText != null && !hintText.gameObject.activeSelf)
        {
            hintText.text = hint;
            hintText.gameObject.SetActive(true);
        }

        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            LoadDestination();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = true;
        if (hintText != null)
        {
            hintText.text = hint;
            hintText.gameObject.SetActive(true);
        }
        if (exitButton != null) exitButton.gameObject.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = false;
        if (hintText != null) hintText.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(false);
    }
}
