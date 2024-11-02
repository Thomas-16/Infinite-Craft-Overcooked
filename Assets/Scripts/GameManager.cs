using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class SpawnExclusionZone
{
    public Vector3 center;
    public float radius;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject elementPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnHeight = 5f;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private int totalItemsToSpawn = 30;
    [SerializeField] private Vector3 spawnCenterOffset = new Vector3(0, 0, 3.5f);
    [SerializeField] private List<SpawnExclusionZone> exclusionZones = new List<SpawnExclusionZone>();

    [Header("Merge Effect Settings")]
    [SerializeField] private ParticleSystem mergeEffectPrefab;
    [SerializeField] private float mergeEffectDuration = 1f;
    [SerializeField] private float mergeEffectScale = 1f;

    [Header("Word Generation Settings")]
    [SerializeField] private int minWordsBeforeReplenish = 10;
    [SerializeField] private float minReplenishDelay = 3f;
    [SerializeField] private float maxReplenishDelay = 8f;

    private HashSet<string> allUsedWords = new HashSet<string>();
    private List<string> activeWords = new List<string>();
	public List<string> GetActiveWords()
	{
		return new List<string>(activeWords);
	}
    private bool isReplenishing = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(InitializeAndSpawn());
        StartCoroutine(MonitorWordCount());
    }

    private IEnumerator InitializeAndSpawn()
    {
        // Wait for initial words to be generated
        while (activeWords.Count == 0)
        {
            GenerateInitialWords();
            yield return new WaitForSeconds(0.1f);
        }

        // Start spawning once we have words
        StartCoroutine(SpawnInitialResources());
    }

    private async void GenerateInitialWords()
    {
        string prompt = "Generate a list of exactly 30 random nouns. Include everyday objects, pop culture items, and general concepts. " +
                       "Each word MUST be unique. Separate words with commas. " +
                       "Keep words relatively simple and recognizable. " +
                       "Include some fun items like 'lightsaber' or 'pokeball' but keep most items realistic. " +
                       "All words must be single words (no spaces). " +
                       "Reply with ONLY the comma-separated list, no other text.";

        try
        {
            string response = await ChatGPTClient.Instance.SendChatRequest(prompt);
            List<string> newWords = new List<string>();
            
            foreach (string word in response.Split(','))
            {
                string cleanWord = word.Trim().ToLower();
                if (!string.IsNullOrWhiteSpace(cleanWord) && !allUsedWords.Contains(cleanWord))
                {
                    newWords.Add(cleanWord);
                    allUsedWords.Add(cleanWord);
                }
            }

            // If we somehow got duplicates or not enough words, fill with fallback words
            while (newWords.Count < 30)
            {
                string fallbackWord = GetFallbackWord();
                if (!allUsedWords.Contains(fallbackWord))
                {
                    newWords.Add(fallbackWord);
                    allUsedWords.Add(fallbackWord);
                }
            }

            activeWords = newWords;
            Debug.Log($"Generated {activeWords.Count} unique words");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to get random words: {e.Message}");
            GenerateFallbackWords();
        }
    }

    private string GetFallbackWord()
    {
        string[] fallbackWords = {
            "book", "phone", "cup", "bag", "chair", "desk", "lamp", "pen", "clock", "mirror",
            "lightsaber", "pokeball", "wand", "ring", "shield", "sword", "crown", "gem", "staff", "orb",
            "camera", "wallet", "key", "brush", "shoes", "hat", "scarf", "glove", "watch", "glasses",
            "telescope", "compass", "map", "journal", "coin", "pearl", "crystal", "feather", "ribbon", "mask"
        };

        return fallbackWords[Random.Range(0, fallbackWords.Length)];
    }

    private void GenerateFallbackWords()
    {
        activeWords.Clear();
        allUsedWords.Clear();

        // Add fallback words until we have 30
        while (activeWords.Count < 30)
        {
            string word = GetFallbackWord();
            if (!allUsedWords.Contains(word))
            {
                activeWords.Add(word);
                allUsedWords.Add(word);
            }
        }
    }

    private IEnumerator MonitorWordCount()
    {
        while (true)
        {
            if (activeWords.Count <= minWordsBeforeReplenish && !isReplenishing)
            {
                StartCoroutine(ReplenishWords());
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator ReplenishWords()
    {
        if (isReplenishing) yield break;
        isReplenishing = true;
        
        // Random delay before replenishing
        float delay = Random.Range(minReplenishDelay, maxReplenishDelay);
        yield return new WaitForSeconds(delay);

        GenerateNewWords();
    }

    private async void GenerateNewWords()
    {
        string prompt = "Generate a list of exactly 15 random nouns that would be fun in a word combination game. " +
                       "Each word MUST be unique and a single word (no spaces). " +
                       "Mix of everyday items, fun concepts, and interesting objects. " +
                       "Reply with ONLY the comma-separated list.";

        try
        {
            string response = await ChatGPTClient.Instance.SendChatRequest(prompt);
            List<string> newWords = new List<string>();

            foreach (string word in response.Split(','))
            {
                string cleanWord = word.Trim().ToLower();
                if (!string.IsNullOrWhiteSpace(cleanWord) && !allUsedWords.Contains(cleanWord))
                {
                    newWords.Add(cleanWord);
                    allUsedWords.Add(cleanWord);
                }
            }

            StartCoroutine(SpawnNewWords(newWords));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to replenish words: {e.Message}");
            isReplenishing = false;
        }
    }

    private IEnumerator SpawnNewWords(List<string> newWords)
    {
        foreach (string word in newWords)
        {
            Vector3 basePosition = new Vector3(0, spawnHeight, 0) + spawnCenterOffset;
            int attempts = 0;
            const int maxAttempts = 10;

            while (attempts < maxAttempts)
            {
                Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
                randomOffset.y = 0;
                Vector3 spawnPosition = basePosition + randomOffset;
                spawnPosition.y = spawnHeight;

                if (IsValidSpawnPosition(spawnPosition))
                {
                    activeWords.Add(word);
                    SpawnElement(word, spawnPosition);
                    yield return new WaitForSeconds(0.2f);
                    break;
                }
                attempts++;
            }
        }

        isReplenishing = false;
    }

    private IEnumerator SpawnInitialResources()
    {
        Vector3 basePosition = new Vector3(0, spawnHeight, 0) + spawnCenterOffset;
        int attempts = 0;
        int maxAttempts = totalItemsToSpawn * 10; // Prevent infinite loops
        int itemsSpawned = 0;

        while (itemsSpawned < totalItemsToSpawn && attempts < maxAttempts)
        {
            // Get random position within spawn radius
            Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
            randomOffset.y = 0; // Keep height consistent
            Vector3 spawnPosition = basePosition + randomOffset;
            spawnPosition.y = spawnHeight;

            // Check if position is valid
            if (IsValidSpawnPosition(spawnPosition))
            {
                // Get random word from our list
                string randomWord = activeWords[Random.Range(0, activeWords.Count)];
                SpawnElement(randomWord, spawnPosition);
                itemsSpawned++;
                yield return new WaitForSeconds(0.1f);
            }

            attempts++;
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("Reached maximum spawn attempts - some items may not have been spawned.");
        }
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Check each exclusion zone
        foreach (var zone in exclusionZones)
        {
            // If position is within any exclusion zone's radius, it's invalid
            if (Vector3.Distance(position, zone.center) < zone.radius)
            {
                return false;
            }
        }
        return true;
    }

    public async void MergeElements(LLement llement1, LLement llement2)
    {
        string name1 = llement1.ElementName;
        string name2 = llement2.ElementName;

        // Remove the words from active list
        activeWords.Remove(name1.ToLower());
        activeWords.Remove(name2.ToLower());

        Vector3 mergePosition = FindMidpoint(llement1.transform.position, llement2.transform.position) + (Vector3.up * 0.5f);

        ParticleSystem mergeEffect = SpawnMergeEffect(mergePosition);

        llement1.gameObject.SetActive(false);
        llement2.gameObject.SetActive(false);

        string response = await ChatGPTClient.Instance.SendChatRequest(
            "What object or concept comes to mind when I combine " + name1 + " with " + name2 +
            "? Say ONLY one simple word that represents an object or concept. Keep it simple, creative and engaging for a game where players combine elements. Do not make up words."
        );
        response = response.Replace(".", "").Trim().ToLower();

        await Task.Delay((int)(mergeEffectDuration * 1000));

        Destroy(llement1.gameObject);
        Destroy(llement2.gameObject);

        if (!allUsedWords.Contains(response))
        {
            allUsedWords.Add(response);
            activeWords.Add(response);
        }
        SpawnElement(response, mergePosition);

        if (mergeEffect != null)
        {
            mergeEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(mergeEffect.gameObject, mergeEffect.main.duration + mergeEffect.main.startLifetime.constantMax);
        }
    }

    private ParticleSystem SpawnMergeEffect(Vector3 position)
    {
        if (mergeEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(mergeEffectPrefab, position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * mergeEffectScale;
            effect.Play();
            return effect;
        }
        return null;
    }

    public void SpawnElement(string name, Vector3 pos)
    {
        LLement newElement = Instantiate(elementPrefab, pos, Quaternion.identity).GetComponent<LLement>();
        newElement.SetElementName(name);
    }

    public static Vector3 FindMidpoint(Vector3 pointA, Vector3 pointB)
    {
        return (pointA + pointB) / 2;
    }

    public float SizeConverter(float sizeFactor)
    {
        return sizeFactor / 4f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw main spawn area
        Gizmos.color = Color.green;
        Vector3 spawnCenter = transform.position + new Vector3(0, spawnHeight, 0) + spawnCenterOffset;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);

        // Draw exclusion zones
        Gizmos.color = Color.red;
        foreach (var zone in exclusionZones)
        {
            Gizmos.DrawWireSphere(zone.center, zone.radius);
        }
    }
#endif
}