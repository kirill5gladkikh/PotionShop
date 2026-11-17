
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NPCDialogueAI : MonoBehaviour
{
    public static NPCDialogueAI Instance { get; private set; }

    [Header("GPT4All")]
    [SerializeField]
    private string apiUrl =
        "http://localhost:4891/v1/chat/completions";

    [SerializeField]
    private string modelName =
        "Llama 3.2 3B Instruct";

    [SerializeField] private float timeoutSeconds = 90f;

    [Header("Generation")]
    [Range(0f, 2f)]
    [SerializeField] private float temperature = 0.7f;

    public bool IsGenerating { get; private set; }

    public event Action<DialogueResult> OnDialogueGenerated;
    public event Action<string> OnDialogueError;

    private readonly List<ChatMessage> history =
        new List<ChatMessage>();

    private const string SystemPrompt =
        "You write interactive dialogue for a cozy winter fantasy " +
        "potion shop game. The NPC is a customer and the player " +
        "is the shopkeeper. Write natural, short English dialogue. " +
        "Always return ONLY a valid JSON object with exactly " +
        "these fields: npcLine (string), choices (array of exactly " +
        "3 strings). Choices are things the PLAYER can say, " +
        "never things the NPC says. Each line should be short. " +
        "No markdown, no explanations. Do not invent game " +
        "mechanics, prices, recipes, or rewards.";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartConversation(
        string npcName,
        string personality,
        string situation)
    {
        history.Clear();

        string prompt =
            "Start a new conversation.\n" +
            "NPC name: " + npcName + "\n" +
            "Personality: " + personality + "\n" +
            "Situation: " + situation + "\n" +
            "Generate the NPC opening line and three player choices.";

        StartCoroutine(GenerateDialogue(prompt));
    }

    public void ChooseResponse(string playerChoice)
    {
        if (IsGenerating)
            return;

        string prompt =
            "The player chose this response: " + playerChoice +
            "\nContinue the conversation. " +
            "Generate the NPC reply and three new player choices.";

        StartCoroutine(GenerateDialogue(prompt));
    }

    public void GenerateCustomDialogue(string context)
    {
        if (IsGenerating)
            return;

        StartCoroutine(GenerateDialogue(context));
    }

    public void ResetConversation()
    {
        if (IsGenerating)
        {
            Debug.LogWarning(
                "Cannot reset dialogue while generating.");
            return;
        }

        history.Clear();
    }

    private IEnumerator GenerateDialogue(string prompt)
    {
        if (IsGenerating)
            yield break;

        IsGenerating = true;

        List<ChatMessage> messages = new List<ChatMessage>();

        messages.Add(new ChatMessage
        {
            role = "system",
            content = SystemPrompt
        });

        messages.AddRange(history);

        messages.Add(new ChatMessage
        {
            role = "user",
            content = prompt
        });

        ChatRequest requestData = new ChatRequest
        {
            model = modelName,
            messages = messages.ToArray(),
            temperature = temperature,
            max_tokens = 600,
            stream = false
        };

        string json = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request =
            new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type", "application/json");

            request.timeout = Mathf.CeilToInt(timeoutSeconds);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                IsGenerating = false;
                ReportError(request.error);
                yield break;
            }

            ChatResponse response;

            try
            {
                response = JsonUtility.FromJson<ChatResponse>(
                    request.downloadHandler.text);
            }
            catch (Exception e)
            {
                IsGenerating = false;
                ReportError("Invalid API response: " + e.Message);
                yield break;
            }

            if (response == null ||
                response.choices == null ||
                response.choices.Length == 0 ||
                response.choices[0].message == null)
            {
                IsGenerating = false;
                ReportError("GPT4All returned an empty response.");
                yield break;
            }

            string aiText = response.choices[0].message.content;

            // Remove optional markdown fences.
            string cleanJson = aiText.Trim();

            if (cleanJson.StartsWith("```"))
            {
                int firstNewline = cleanJson.IndexOf('\n');
                int lastFence = cleanJson.LastIndexOf("```");

                if (firstNewline >= 0 && lastFence > firstNewline)
                    cleanJson = cleanJson.Substring(
                        firstNewline + 1,
                        lastFence - firstNewline - 1).Trim();
            }

            DialogueResult result = null;

            try
            {
                result = JsonUtility.FromJson<DialogueResult>(
                    cleanJson);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
            }

            if (result == null ||
                string.IsNullOrWhiteSpace(result.npcLine) ||
                result.choices == null ||
                result.choices.Length != 3)
            {
                IsGenerating = false;
                ReportError(
                    "AI returned invalid dialogue JSON:\n" + aiText);
                yield break;
            }

            history.Add(new ChatMessage
            {
                role = "user",
                content = prompt
            });

            history.Add(new ChatMessage
            {
                role = "assistant",
                content = cleanJson
            });

            IsGenerating = false;

            Debug.Log("NPC: " + result.npcLine);

            for (int i = 0; i < result.choices.Length; i++)
                Debug.Log((i + 1) + ": " + result.choices[i]);

            OnDialogueGenerated?.Invoke(result);
        }
    }

    private void ReportError(string error)
    {
        Debug.LogError("NPCDialogueAI: " + error);
        OnDialogueError?.Invoke(error);
    }

    [Serializable]
    private class ChatMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class ChatRequest
    {
        public string model;
        public ChatMessage[] messages;
        public float temperature;
        public int max_tokens;
        public bool stream;
    }

    [Serializable]
    private class ChatResponse
    {
        public ChatChoice[] choices;
    }

    [Serializable]
    private class ChatChoice
    {
        public ChatMessage message;
    }
}

[Serializable]
public class DialogueResult
{
    public string npcLine;
    public string[] choices;
}
