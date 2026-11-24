using System;
using UnityEngine;

// Создание: ПКМ в Project -> Create -> NeKArton -> Dialogue
[CreateAssetMenu(fileName = "NewDialogue", menuName = "NeKArton/Dialogue")]
public class DialogueData : ScriptableObject
{
    public DialogueNode[] nodes;
}

[Serializable]
public class DialogueNode
{
    public string speaker;
    [TextArea(2, 5)] public string text;

    [Tooltip("Если нет выборов: номер следующей реплики. -1 = следующая по списку.")]
    public int next = -1;

    [Tooltip("Отметьте, чтобы диалог закончился после этой реплики.")]
    public bool endsHere;

    [Tooltip("Варианты ответа. Если пусто, игрок просто жмёт «дальше».")]
    public DialogueChoice[] choices;
}

[Serializable]
public class DialogueChoice
{
    public string text;

    [Tooltip("Номер реплики, к которой ведёт этот ответ. -1 = закончить диалог.")]
    public int next = -1;
}
