using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine.UI;
using System.Threading.Tasks;

public class StorySegment
{
    public string text;
    public string requiredWordType;
    public List<string> acceptableWords;
}

public class StoryMessages : MonoBehaviour
{
    public static StoryMessages Instance { get; private set; }

    [Header("References")]
    [SerializeField] private UIPanel messageUIPrefab;
    [SerializeField] private UIPanel currentPromptPrefab;
    [SerializeField] private RectTransform messageContainer;
    [SerializeField] private RectTransform promptContainer;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float messageDisplayDuration = 6f;
    [SerializeField] private float letterAnimationInterval = 0.05f;

    [Header("Layout Settings")]
    [SerializeField] private float topMargin = 100f;
    [SerializeField] private float leftMargin = 100f;

    [Header("Message Colors")]
    [SerializeField] private Color defaultTextColor = Color.white;
    [SerializeField] private Color correctAnswerColor = new Color(0.1f, 0.3f, 0.1f, 0.8f);
    [SerializeField] private Color incorrectAnswerColor = new Color(0.3f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color storyBackgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.9f);

    private StoryProgress currentStory;
    private UIPanel currentMessagePanel;
    private UIPanel promptPanel;
    private Coroutine currentTextAnimation;
    private bool isInitialized = false;

    private class StoryProgress
    {
        public List<StorySegment> segments = new List<StorySegment>();
        public int currentSegmentIndex = 0;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeContainers();
    }

    private void InitializeContainers()
    {
        if (messageContainer == null || promptContainer == null)
        {
            Debug.LogError("Containers not assigned to StoryMessages!");
            return;
        }

        foreach (Transform child in messageContainer) Destroy(child.gameObject);
        foreach (Transform child in promptContainer) Destroy(child.gameObject);
    }

    private void Start()
    {
        StartCoroutine(WaitForWordsAndInitialize());
    }

    private IEnumerator WaitForWordsAndInitialize()
    {
        // Wait until GameManager has words available
        while (GameManager.Instance == null || GameManager.Instance.GetActiveWords().Count == 0)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Now we can initialize our story
        yield return new WaitForSeconds(1f); // Give a slight delay after words are loaded
        
        // Start async initialization
        InitializeStoryAsync();
    }

    private async void InitializeStoryAsync()
    {
        await InitializeStory();
        isInitialized = true;
    }

    private async Task<Dictionary<string, List<string>>> CategorizeWords(List<string> words)
    {
        var categorizedWords = new Dictionary<string, List<string>>();
        
        string prompt = @"Categorize these words by type AND common use. 
Reply with 'word:type' where type indicates both category and typical use.
Use specific types like:
- container (things that hold other things)
- tool (things used to make or fix)
- furniture (things in a home)
- food (things to eat)
- place (locations)
- creature (living things)
Be strict about categorization - ensure words fit their category's typical use.
Words: " + string.Join(", ", words);

        try
        {
            string response = await ChatGPTClient.Instance.SendChatRequest(prompt);
            string[] categorizations = response.Split('\n');

            foreach (string cat in categorizations)
            {
                string[] parts = cat.Split(':');
                if (parts.Length != 2) continue;

                string word = parts[0].Trim().ToLower();
                string type = parts[1].Trim().ToLower();

                if (!categorizedWords.ContainsKey(type))
                {
                    categorizedWords[type] = new List<string>();
                }
                categorizedWords[type].Add(word);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to categorize words: {e.Message}");
        }

        return categorizedWords;
    }

    private void DisplayCurrentPrompt()
    {
        if (currentStory == null || currentStory.currentSegmentIndex >= currentStory.segments.Count)
        {
            QueueMessage("Story complete! Well done!", defaultTextColor, storyBackgroundColor);
            return;
        }

        StorySegment segment = currentStory.segments[currentStory.currentSegmentIndex];
        string promptText = $"{segment.text}";
        
        // Update prompt panel
        if (promptPanel != null) Destroy(promptPanel.gameObject);
        
        promptPanel = Instantiate(currentPromptPrefab, promptContainer);
        RectTransform promptRect = promptPanel.RectTransform;
        promptRect.anchorMin = new Vector2(0, 0.5f);
        promptRect.anchorMax = new Vector2(0, 0.5f);
        promptRect.pivot = new Vector2(0, 0.5f);
        promptRect.anchoredPosition = new Vector2(leftMargin, 0f);

        promptPanel.SetText(promptText);
        promptPanel.SetTextColor(defaultTextColor);
        promptPanel.SetPanelColor(storyBackgroundColor);

        // Fade in effect
        CanvasGroup canvasGroup = promptPanel.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, fadeInDuration)
            .SetEase(Ease.OutQuad);
    }

private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            LanguageSettings.ToggleSpanish();
            RestartStory();
        }
    }

    private async void RestartStory()
    {
        if (currentStory != null)
        {
            currentStory = null;
            if (promptPanel != null)
            {
                Destroy(promptPanel.gameObject);
            }
            await InitializeStory();
        }
    }

    private async Task InitializeStory()
    {
        List<string> availableWords = GameManager.Instance.GetActiveWords();
        Dictionary<string, List<string>> categorizedWords = await CategorizeWords(availableWords);

        var viableCategories = categorizedWords
            .Where(kvp => kvp.Value.Count >= 1)
            .Select(kvp => kvp.Key)
            .ToList();

        if (viableCategories.Count < 2)
        {
            Debug.LogError("Not enough variety in available words to create a story!");
            return;
        }

        string wordTypesDesc = string.Join(", ", viableCategories.Select(cat => $"'{cat}'"));
        string prompt = LanguageSettings.IsSpanish ?
            $@"Create a story template in Spanish with 2-3 segments.
Available word types are: {wordTypesDesc}
Requirements:
1. Write in natural Spanish
2. Use 'el/la/los/las/un/una' appropriately
3. Ensure logical relationships
4. Use proper Spanish grammar
5. Make the story engaging

Format each segment as type:text where [type] will be replaced.
Example formats:
food:El chef encuentra un [food] en
place:y corre hacia el [place] para cocinarlo
object:El mago usa el [object] para lanzar un hechizo

Reply ONLY with formatted segments separated by commas. No other text."
            :
            $@"Create a very short story template with 2-3 segments that makes logical sense. 
Available word types are: {wordTypesDesc}
Requirements:
1. Each segment must be grammatically complete
2. Use 'the' or 'a' before word placeholders
3. Ensure logical relationships between segments
4. Each action must make sense for the type of object
5. Include proper prepositions and articles

Format each segment as type:text where [type] will be replaced with a word.
Example good formats:
food:The hungry chef discovers a [food] in
place:and rushes to the [place] to cook it
object:The wizard uses the [object] to cast a spell

Reply ONLY with properly formatted segments separated by commas. No other text.";

        try
        {
            string response = await ChatGPTClient.Instance.SendChatRequest(prompt);
            string[] segments = response.Split(',');
            
            currentStory = new StoryProgress();

            foreach (string segment in segments)
            {
                string[] parts = segment.Split(':');
                if (parts.Length != 2) continue;

                string wordType = parts[0].Trim().ToLower();
                string text = parts[1].Trim();

                if (categorizedWords.ContainsKey(wordType) && categorizedWords[wordType].Any())
                {
                    currentStory.segments.Add(new StorySegment
                    {
                        text = text,
                        requiredWordType = wordType,
                        acceptableWords = categorizedWords[wordType]
                    });
                }
            }

            DisplayCurrentPrompt();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize story: {e.Message}");
        }
    }

    public async Task<bool> EvaluateWord(string word)
    {
        if (!isInitialized || currentStory == null || 
            currentStory.currentSegmentIndex >= currentStory.segments.Count)
            return false;

        StorySegment currentSegment = currentStory.segments[currentStory.currentSegmentIndex];
        
        // If in Spanish mode, translate the submitted word for comparison
        string wordToCheck = word.ToLower();
        if (LanguageSettings.IsSpanish)
        {
            try
            {
                string prompt = $"Translate '{word}' to English. Reply with ONLY the English word, no other text.";
                wordToCheck = await ChatGPTClient.Instance.SendChatRequest(prompt);
                wordToCheck = wordToCheck.Trim().ToLower();
            }
            catch (Exception e)
            {
                Debug.LogError($"Translation failed: {e.Message}");
                return false;
            }
        }

        bool isCorrect = currentSegment.acceptableWords.Contains(wordToCheck);

        string response;
        Color backgroundColor;

        if (isCorrect)
        {
            response = currentSegment.text.Replace($"[{currentSegment.requiredWordType}]", word);
            backgroundColor = correctAnswerColor;
            currentStory.currentSegmentIndex++;
            await Task.Delay(1500);
            
            if (currentStory.currentSegmentIndex >= currentStory.segments.Count)
            {
                string completeMessage = LanguageSettings.IsSpanish ? 
                    "¡Historia completa! Aquí viene otra..." : 
                    "Story complete! Here comes another...";
                QueueMessage(completeMessage, defaultTextColor, storyBackgroundColor);
                await Task.Delay(2000);
                await InitializeStory();
            }
            else
            {
                DisplayCurrentPrompt();
            }
        }
        else
        {
            string tryAgainMessage = LanguageSettings.IsSpanish ?
                $"¡Intenta con otro tipo de {currentSegment.requiredWordType}!" :
                $"Try using a different {currentSegment.requiredWordType}!";
            response = tryAgainMessage;
            backgroundColor = incorrectAnswerColor;
        }

        QueueMessage(response, defaultTextColor, backgroundColor);
        return isCorrect;
    }

    public StorySegment GetCurrentSegment()
    {
        if (!isInitialized || currentStory == null || 
            currentStory.currentSegmentIndex >= currentStory.segments.Count)
        {
            return null;
        }
        return currentStory.segments[currentStory.currentSegmentIndex];
    }

    public void QueueMessage(string text, Color textColor, Color backgroundColor)
    {
        StartCoroutine(DisplayNewMessage(new StoryMessage
        {
            Text = text,
            TextColor = textColor,
            BackgroundColor = backgroundColor
        }));
    }

    private IEnumerator DisplayNewMessage(StoryMessage newMessage)
    {
        // If there's a current message, fade it out
        if (currentMessagePanel != null)
        {
            CanvasGroup oldCanvasGroup = currentMessagePanel.GetComponent<CanvasGroup>();
            if (oldCanvasGroup != null)
            {
                // Stop current text animation if it's running
                if (currentTextAnimation != null)
                {
                    StopCoroutine(currentTextAnimation);
                }

                // Fade out the old message
                DOTween.To(() => oldCanvasGroup.alpha, x => oldCanvasGroup.alpha = x, 0f, fadeOutDuration)
                    .SetEase(Ease.InQuad);

                // Keep reference to destroy after fade
                UIPanel oldPanel = currentMessagePanel;
                yield return new WaitForSeconds(fadeOutDuration);
                if (oldPanel != null)
                {
                    Destroy(oldPanel.gameObject);
                }
            }
        }

        // Create and display the new message
        currentMessagePanel = Instantiate(messageUIPrefab, messageContainer);

        // Set anchors for top justification
        RectTransform messageRect = currentMessagePanel.RectTransform;
        messageRect.anchorMin = new Vector2(0.5f, 1f);
        messageRect.anchorMax = new Vector2(0.5f, 1f);
        messageRect.pivot = new Vector2(0.5f, 1f);

        // Position from top edge
        messageRect.anchoredPosition = new Vector2(0f, -topMargin);

        currentMessagePanel.SetText("");
        currentMessagePanel.SetTextColor(newMessage.TextColor);
        currentMessagePanel.SetPanelColor(newMessage.BackgroundColor);

        // Setup fade in
        CanvasGroup canvasGroup = currentMessagePanel.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Fade in new message
        DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, fadeInDuration)
            .SetEase(Ease.OutQuad);

        yield return new WaitForSeconds(fadeInDuration);

        // Start text animation
        currentTextAnimation = StartCoroutine(AnimateText(newMessage.Text));
    }

    private IEnumerator AnimateText(string fullText)
    {
        string currentText = "";

        foreach (char letter in fullText)
        {
            currentText += letter;
            if (currentMessagePanel != null)
            {
                currentMessagePanel.SetText(currentText);
            }

            float delay = letter == '.' || letter == '!' || letter == '?' ?
                letterAnimationInterval * 4 : letterAnimationInterval;

            yield return new WaitForSeconds(delay);
        }

        // Wait for display duration after text is complete
        yield return new WaitForSeconds(messageDisplayDuration);

        // Fade out if this is still the current message
        if (currentMessagePanel != null)
        {
            CanvasGroup canvasGroup = currentMessagePanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, fadeOutDuration)
                    .SetEase(Ease.InQuad);

                yield return new WaitForSeconds(fadeOutDuration);

                if (currentMessagePanel != null)
                {
                    Destroy(currentMessagePanel.gameObject);
                    currentMessagePanel = null;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private class StoryMessage
    {
        public string Text { get; set; }
        public Color TextColor { get; set; }
        public Color BackgroundColor { get; set; }
    }
}