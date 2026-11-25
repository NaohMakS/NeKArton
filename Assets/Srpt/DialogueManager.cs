using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool IsActive { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Начальная последовательность")]
    [SerializeField] private DialogueData openingDialogue;
    [SerializeField] private DialogueData followupDialogue;

    [Header("Настройки")]
    [SerializeField, Min(1f)] private float charsPerSecond = 40f;

    private DialogueData current;
    private int nodeIndex;
    private bool typingFinished;
    private bool hasChoices;
    private bool openingSequence;
    private int startedFrame;
    private Coroutine typingRoutine;
    private Button skipButton;
    private readonly System.Collections.Generic.List<Button> generatedButtons =
        new System.Collections.Generic.List<Button>();

    private static bool openingPlayed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        IsActive = false;
        openingPlayed = false;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsActive = false;
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (openingDialogue != null && !openingPlayed)
        {
            openingPlayed = true;
            StartCoroutine(StartOpeningNextFrame());
        }
    }

    private IEnumerator StartOpeningNextFrame()
    {
        yield return null;
        StartOpeningSequence();
    }

    public void StartOpeningSequence()
    {
        if (openingDialogue == null || IsActive) return;
        openingSequence = true;
        StartDialogueInternal(openingDialogue);
    }

    public void StartDialogue(DialogueData data)
    {
        if (data == null || IsActive) return;
        openingSequence = false;
        StartDialogueInternal(data);
    }

    private void StartDialogueInternal(DialogueData data)
    {
        if (data == null || data.nodes == null || data.nodes.Length == 0)
        {
            Debug.LogWarning("DialogueManager: диалог не содержит реплик.", this);
            EndSequence();
            return;
        }

        current = data;
        nodeIndex = 0;
        IsActive = true;
        startedFrame = Time.frameCount;
        if (panel != null) panel.SetActive(true);
        CreateSkipButton();
        DisplayNode(nodeIndex);
    }

    private void Update()
    {
        if (!IsActive || Time.frameCount == startedFrame) return;

        bool pressed = (Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame)) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (!pressed) return;
        if (!typingFinished) RevealCurrentLine();
        else if (!hasChoices) Advance();
    }

    private void DisplayNode(int index)
    {
        if (current == null || index < 0 || index >= current.nodes.Length)
        {
            FinishCurrentDialogue();
            return;
        }

        nodeIndex = index;
        DialogueNode node = current.nodes[index];
        ClearGeneratedButtons();
        hasChoices = node.choices != null && node.choices.Length > 0;
        if (speakerText != null) speakerText.text = node.speaker ?? string.Empty;

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeLine(node.text ?? string.Empty));
    }

    private IEnumerator TypeLine(string line)
    {
        typingFinished = false;
        if (bodyText == null)
        {
            typingFinished = true;
            yield break;
        }

        bodyText.text = line;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        int count = bodyText.textInfo.characterCount;
        float visible = 0f;
        while (visible < count)
        {
            visible += charsPerSecond * Time.unscaledDeltaTime;
            bodyText.maxVisibleCharacters = Mathf.FloorToInt(visible);
            yield return null;
        }
        typingRoutine = null;
        RevealCurrentLine();
    }

    private void RevealCurrentLine()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
        if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
        typingFinished = true;
        if (hasChoices && current != null) CreateChoiceButtons(current.nodes[nodeIndex].choices);
    }

    private void Advance()
    {
        if (current == null) { EndSequence(); return; }
        DialogueNode node = current.nodes[nodeIndex];
        if (node.endsHere) { FinishCurrentDialogue(); return; }
        DisplayNode(node.next >= 0 ? node.next : nodeIndex + 1);
    }

    private void FinishCurrentDialogue()
    {
        ClearGeneratedButtons();
        DestroySkipButton();
        if (openingSequence && current == openingDialogue && followupDialogue != null)
        {
            StartDialogueInternal(followupDialogue);
            return;
        }
        EndSequence();
    }

    private void EndSequence()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = null;
        ClearGeneratedButtons();
        DestroySkipButton();
        if (panel != null) panel.SetActive(false);
        current = null;
        IsActive = false;
        openingSequence = false;
    }

    public void SkipSequence()
    {
        if (IsActive) EndSequence();
    }

    public Button CreateInteractionButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        if (parent == null || choiceButtonPrefab == null) return null;
        Button button = Instantiate(choiceButtonPrefab, parent);
        button.name = "TalkToNastyaButton";
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = label;
        RectTransform rect = button.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 44f);
            rect.sizeDelta = new Vector2(260f, 64f);
        }
        button.onClick.AddListener(action);
        return button;
    }

    private void CreateSkipButton()
    {
        DestroySkipButton();
        if (panel == null || choiceButtonPrefab == null) return;
        skipButton = Instantiate(choiceButtonPrefab, panel.transform);
        skipButton.name = "SkipDialogueButton";
        TMP_Text label = skipButton.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = "Пропустить";
        RectTransform rect = skipButton.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -18f);
            rect.sizeDelta = new Vector2(230f, 60f);
        }
        skipButton.onClick.AddListener(SkipSequence);
    }

    private void DestroySkipButton()
    {
        if (skipButton != null) Destroy(skipButton.gameObject);
        skipButton = null;
    }

    private void CreateChoiceButtons(DialogueChoice[] choices)
    {
        if (choiceButtonPrefab == null || choicesContainer == null) return;
        foreach (DialogueChoice choice in choices)
        {
            Button button = Instantiate(choiceButtonPrefab, choicesContainer);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = choice.text;
            int destination = choice.next;
            button.onClick.AddListener(() =>
            {
                ClearGeneratedButtons();
                if (destination < 0) FinishCurrentDialogue();
                else DisplayNode(destination);
            });
            generatedButtons.Add(button);
        }
        if (EventSystem.current != null && generatedButtons.Count > 0)
            EventSystem.current.SetSelectedGameObject(generatedButtons[0].gameObject);
    }

    private void ClearGeneratedButtons()
    {
        foreach (Button button in generatedButtons)
            if (button != null) Destroy(button.gameObject);
        generatedButtons.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            EndSequence();
            Instance = null;
        }
    }
}
