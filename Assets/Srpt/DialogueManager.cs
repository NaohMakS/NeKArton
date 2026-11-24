using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool IsActive { get; private set; }   // проверяйте в скрипте движения игрока

    [Header("UI")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text speakerText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] Transform choicesContainer;
    [SerializeField] Button choiceButtonPrefab;

    [Header("Настройки")]
    [SerializeField] float charsPerSecond = 40f;

    DialogueData current;
    int index;
    bool lineFinished;
    bool hasChoices;
    int startFrame;
    Coroutine typing;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        IsActive = false;
        panel.SetActive(false);
    }

    public void StartDialogue(DialogueData data)
    {
        if (IsActive || data == null || data.nodes == null || data.nodes.Length == 0) return;
        current = data;
        IsActive = true;
        startFrame = Time.frameCount;
        panel.SetActive(true);
        ShowNode(0);
    }

    void Update()
    {
        if (!IsActive || Time.frameCount == startFrame) return;

        bool keyboardPressed = Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool pressed = keyboardPressed || mousePressed;
        if (!pressed) return;

        if (!lineFinished) FinishTyping();          // первый клик: показать реплику целиком
        else if (!hasChoices) Next();               // второй клик: дальше (если нет выбора)
    }

    void ShowNode(int i)
    {
        if (i < 0 || i >= current.nodes.Length) { End(); return; }

        index = i;
        DialogueNode n = current.nodes[i];
        ClearChoices();

        speakerText.text = n.speaker;
        hasChoices = n.choices != null && n.choices.Length > 0;

        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeLine(n.text));
    }

    IEnumerator TypeLine(string text)
    {
        lineFinished = false;
        bodyText.text = text;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();

        int total = bodyText.textInfo.characterCount;
        float shown = 0f;
        while (shown < total)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            bodyText.maxVisibleCharacters = Mathf.FloorToInt(shown);
            yield return null;
        }
        FinishTyping();
    }

    void FinishTyping()
    {
        if (typing != null) { StopCoroutine(typing); typing = null; }
        bodyText.maxVisibleCharacters = int.MaxValue;
        lineFinished = true;
        if (hasChoices) ShowChoices(current.nodes[index].choices);
    }

    void Next()
    {
        DialogueNode n = current.nodes[index];
        if (n.endsHere) { End(); return; }
        ShowNode(n.next >= 0 ? n.next : index + 1);
    }

    void ShowChoices(DialogueChoice[] choices)
    {
        foreach (DialogueChoice c in choices)
        {
            Button btn = Instantiate(choiceButtonPrefab, choicesContainer);
            btn.GetComponentInChildren<TMP_Text>().text = c.text;
            int target = c.next;
            btn.onClick.AddListener(() => ShowNode(target));
        }

        // чтобы можно было выбирать стрелками / геймпадом
        if (EventSystem.current != null && choicesContainer.childCount > 0)
            EventSystem.current.SetSelectedGameObject(choicesContainer.GetChild(0).gameObject);
    }

    void ClearChoices()
    {
        foreach (Transform child in choicesContainer) Destroy(child.gameObject);
    }

    void End()
    {
        ClearChoices();
        panel.SetActive(false);
        IsActive = false;
    }
}
