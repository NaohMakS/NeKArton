using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class DoorTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string destinationScene = "";
    [SerializeField] private string hint = "Нажмите ENTER, чтобы войти";
    [SerializeField] private TextMeshProUGUI hintText;

    private bool playerNearby;
    private bool loading;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (hintText != null) hintText.gameObject.SetActive(false);
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
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = false;
        if (hintText != null) hintText.gameObject.SetActive(false);
    }
}
