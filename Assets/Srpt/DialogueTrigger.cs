using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Диалог")]
    [SerializeField] private DialogueData dialogue;

    [Header("Взаимодействие")]
    [SerializeField, Min(0.5f)] private float interactionRange = 2.5f;
    [SerializeField] private GameObject hint;

    private Transform player;
    private Button interactionButton;
    private Transform uiRoot;
    private bool playerInRange;

    private void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void Start()
    {
        FindPlayer();
        FindDialogueCanvas();

        if (uiRoot == null)
            Debug.LogError("DialogueTrigger: не найден DialogueCanvas. Кнопка взаимодействия не появится.", this);
        if (player == null)
            Debug.LogError("DialogueTrigger: не найден игрок. Проверь Player tag или компонент PlayerMovement.", this);
        if (dialogue == null)
            Debug.LogError("DialogueTrigger: не назначен DialogueData для Насти.", this);
    }

    private void Update()
    {
        if (player == null) FindPlayer();
        if (uiRoot == null) FindDialogueCanvas();

        playerInRange = player != null && Vector2.Distance(player.position, transform.position) <= interactionRange;
        bool canInteract = playerInRange && !DialogueManager.IsActive;

        if (hint != null) hint.SetActive(canInteract);

        if (canInteract)
            ShowPrompt();
        else
            HidePrompt();

        if (canInteract && Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
            BeginDialogue();
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            PlayerMovement movement = FindObjectOfType<PlayerMovement>();
            if (movement != null) playerObject = movement.gameObject;
        }

        if (playerObject != null) player = playerObject.transform;
    }

    private void FindDialogueCanvas()
    {
        GameObject canvasObject = GameObject.Find("DialogueCanvas");
        Canvas canvas = canvasObject != null ? canvasObject.GetComponent<Canvas>() : null;

        if (canvas == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            foreach (Canvas candidate in canvases)
            {
                if (candidate.name == "DialogueCanvas")
                {
                    canvas = candidate;
                    break;
                }
            }
        }

        if (canvas != null)
        {
            uiRoot = canvas.transform;
            RectTransform rect = uiRoot as RectTransform;
            if (rect != null) rect.localScale = Vector3.one;
        }
    }

    private void ShowPrompt()
    {
        if (interactionButton != null || DialogueManager.IsActive ||
            DialogueManager.Instance == null || uiRoot == null)
            return;

        interactionButton = DialogueManager.Instance.CreateInteractionButton(
            uiRoot, "Поговорить", BeginDialogue);

        if (interactionButton == null)
            Debug.LogError("DialogueTrigger: кнопка не создана. Проверьте Choice Button Prefab в DialogueManager.", this);
    }

    private void HidePrompt()
    {
        if (interactionButton != null)
            Destroy(interactionButton.gameObject);
        interactionButton = null;
    }

    private void BeginDialogue()
    {
        if (!playerInRange || DialogueManager.IsActive ||
            DialogueManager.Instance == null || dialogue == null)
            return;

        HidePrompt();
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void OnDisable()
    {
        HidePrompt();
    }
}
