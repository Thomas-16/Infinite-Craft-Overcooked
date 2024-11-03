using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	[SerializeField] private GameObject elementPrefab;
	[SerializeField] private Transform playerTransform;

	[Header("Spawn Settings")]
	[SerializeField] private float spawnHeight = 1.5f;
	[SerializeField] private float spawnRadius = 5f;

	[Header("Game Settings")]
	[SerializeField] private int minimumObjects = 2;
	[SerializeField] private int maximumObjects = 10;
	[SerializeField] private float minSpawnDelay = 1f;
	[SerializeField] private float maxSpawnDelay = 3f;
	[SerializeField] private float similarityThreshold = 0.3f;

	[Header("UI Settings")]
	[SerializeField] private TMPro.TextMeshProUGUI scoreText;

	[Header("Merge Effect Settings")]
	[SerializeField] private ParticleSystem mergeEffectPrefab;
	[SerializeField] private float mergeEffectDuration = 1f;
	[SerializeField] private float mergeEffectScale = 1f;

	private Dictionary<string, ObjectMetadata> elementDatabase = new Dictionary<string, ObjectMetadata>();
	private HashSet<string> discoveredWords = new HashSet<string>();
	private HashSet<string> allUsedWords = new HashSet<string>();
	private List<string> activeWords = new List<string>();
	private List<LLement> activeElements = new List<LLement>();
	private int playerScore = 0;
	private bool isSpawning = false;

	public List<string> GetActiveWords() => new List<string>(activeWords);

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		if (playerTransform == null)
		{
			playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
			if (playerTransform == null)
			{
				Debug.LogError("Player transform not found! Please assign it in the inspector or tag your player with 'Player'");
				return;
			}
		}

		StartCoroutine(InitializeAndSpawn());
		StartCoroutine(MonitorObjectCount());
	}

	private void UpdateScoreDisplay()
	{
		if (scoreText != null)
		{
			scoreText.text = $"Score: {playerScore}";
		}
	}

	private IEnumerator InitializeAndSpawn()
	{
		var wordGenerationTask = GenerateInitialWords();

		while (!wordGenerationTask.IsCompleted)
		{
			yield return null;
		}

		yield return null;
		StartCoroutine(SpawnInitialObjects());
	}

	private IEnumerator SpawnInitialObjects()
	{
		foreach (string word in activeWords)
		{
			if (elementDatabase.TryGetValue(word, out ObjectMetadata metadata))
			{
				Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
				Vector3 spawnPosition = playerTransform.position + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);

				LLement newElement = SpawnElement(word, spawnPosition, metadata);
				activeElements.Add(newElement);
				yield return new WaitForSeconds(0.2f);
			}
			else
			{
				Debug.LogError($"No metadata found for initial word: {word}");
			}
		}
	}

	private Vector3 FindMidpoint(Vector3 pointA, Vector3 pointB)
	{
		return (pointA + pointB) / 2;
	}

	private async Task GenerateInitialWords()
	{
		string prompt = "Generate 2 random nouns suitable for a word combination game. " +
					   "Each word MUST be unique and a single word (no spaces). " +
					   "Separate words with commas. " +
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
					var metadata = await ObjectMetadataAPI.Instance.GetObjectMetadata(cleanWord);
					metadata.word = cleanWord; // Set the word in metadata

					elementDatabase[cleanWord] = metadata;
					newWords.Add(cleanWord);
					allUsedWords.Add(cleanWord);
					discoveredWords.Add(cleanWord);
				}
			}

			while (newWords.Count < 2)
			{
				string fallbackWord = GetFallbackWord();
				if (!allUsedWords.Contains(fallbackWord))
				{
					var metadata = await ObjectMetadataAPI.Instance.GetObjectMetadata(fallbackWord);
					metadata.word = fallbackWord; // Set the word in metadata

					elementDatabase[fallbackWord] = metadata;
					newWords.Add(fallbackWord);
					allUsedWords.Add(fallbackWord);
					discoveredWords.Add(fallbackWord);
				}
			}

			activeWords = newWords;
			Debug.Log($"Generated {activeWords.Count} initial words");
		}
		catch (System.Exception e)
		{
			Debug.LogError($"Failed to get random words: {e.Message}");
			GenerateFallbackWords();
		}
	}

	private IEnumerator MonitorObjectCount()
	{
		while (true)
		{
			activeElements.RemoveAll(item => item == null);

			if (activeElements.Count < maximumObjects && discoveredWords.Count > 0 && !isSpawning)
			{
				List<string> availableWords = new List<string>();
				foreach (string word in discoveredWords)
				{
					if (!activeWords.Contains(word))
					{
						availableWords.Add(word);
					}
				}

				if (availableWords.Count > 0 && activeElements.Count < minimumObjects)
				{
					StartCoroutine(SpawnNewObject());
				}
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private IEnumerator SpawnNewObject()
	{
		isSpawning = true;
		float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
		yield return new WaitForSeconds(delay);

		List<string> availableWords = new List<string>();
		foreach (string word in discoveredWords)
		{
			if (!activeWords.Contains(word))
			{
				availableWords.Add(word);
			}
		}

		if (availableWords.Count > 0)
		{
			string selectedWord = availableWords[Random.Range(0, availableWords.Count)];
			SpawnElementNearPlayer(selectedWord);
			activeWords.Add(selectedWord);
		}

		isSpawning = false;
	}

	private void SpawnElementNearPlayer(string word)
	{
		Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
		Vector3 spawnPosition = playerTransform.position + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);

		if (elementDatabase.TryGetValue(word, out ObjectMetadata metadata))
		{
			LLement newElement = SpawnElement(word, spawnPosition, metadata);
			activeElements.Add(newElement);
		}
		else
		{
			Debug.LogError($"No metadata found for word: {word}");
		}
	}

	public async void MergeElements(LLement element1, LLement element2)
	{
		string name1 = element1.ElementName.ToLower();
		string name2 = element2.ElementName.ToLower();

		Vector3 mergePosition = FindMidpoint(element1.transform.position, element2.transform.position);

		activeElements.Remove(element1);
		activeElements.Remove(element2);
		activeWords.Remove(name1);
		activeWords.Remove(name2);

		ParticleSystem mergeEffect = SpawnMergeEffect(mergePosition);

		string prompt = $"You are helping with a word combination game. When combining {name1} with {name2}, " +
					   $"what new word is created? Say ONLY one simple word. Keep it creative and engaging. Do not make up words.";

		string newWord = await ChatGPTClient.Instance.SendChatRequest(prompt);
		newWord = newWord.Replace(".", "").Trim().ToLower();

		// Get metadata for new word
		ObjectMetadata metadata = await ObjectMetadataAPI.Instance.GetObjectMetadata(newWord);
		metadata.word = newWord;

		// Calculate points
		int points = CalculatePoints(newWord, metadata.emoji, name1, name2);
		playerScore += points;
		UpdateScoreDisplay();

		// Store metadata
		elementDatabase[newWord] = metadata;

		// Destroy original objects
		Destroy(element1.gameObject);
		Destroy(element2.gameObject);

		await Task.Delay((int)(mergeEffectDuration * 1000));

		if (!allUsedWords.Contains(newWord))
		{
			allUsedWords.Add(newWord);
			discoveredWords.Add(newWord);
			Debug.Log($"New element discovered: {newWord} ({metadata.emoji}) (+{points} points)");
		}
		else
		{
			Debug.Log($"Created existing element: {newWord} ({metadata.emoji}) (+{points} points)");
		}

		// Spawn new combined element
		LLement newLElement = SpawnElement(newWord, mergePosition, metadata);
		activeElements.Add(newLElement);
		activeWords.Add(newWord);

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

	public LLement SpawnElement(string word, Vector3 pos, ObjectMetadata metadata)
	{
		LLement newElement = Instantiate(elementPrefab, pos, Quaternion.identity).GetComponent<LLement>();
		newElement.SetElementName(word, metadata);
		return newElement;
	}

	private int CalculatePoints(string newWord, string newEmoji, string parent1, string parent2)
	{
		int points = 0;

		// New word bonus
		if (!elementDatabase.ContainsKey(newWord))
		{
			points += 1;
			Debug.Log("Point awarded: New word");
		}
		else
		{
			return 0; // Duplicate combination
		}

		// Different emoji bonus
		bool differentEmoji = true;
		if (elementDatabase.ContainsKey(parent1))
		{
			if (elementDatabase[parent1].emoji == newEmoji) differentEmoji = false;
		}
		if (elementDatabase.ContainsKey(parent2))
		{
			if (elementDatabase[parent2].emoji == newEmoji) differentEmoji = false;
		}
		if (differentEmoji)
		{
			points += 1;
			Debug.Log("Point awarded: Different emoji");
		}

		// Distant word bonus
		bool isDistant = true;
		foreach (var existingWord in discoveredWords)
		{
			if (CalculateLevenshteinSimilarity(newWord, existingWord) > similarityThreshold)
			{
				isDistant = false;
				break;
			}
		}
		if (isDistant)
		{
			points += 1;
			Debug.Log("Point awarded: Distant word");
		}

		return points;
	}

	private float CalculateLevenshteinSimilarity(string s1, string s2)
	{
		int distance = CalculateLevenshteinDistance(s1, s2);
		int maxLength = Mathf.Max(s1.Length, s2.Length);
		return 1 - ((float)distance / maxLength);
	}

	private int CalculateLevenshteinDistance(string s1, string s2)
	{
		int[,] d = new int[s1.Length + 1, s2.Length + 1];

		for (int i = 0; i <= s1.Length; i++)
			d[i, 0] = i;

		for (int j = 0; j <= s2.Length; j++)
			d[0, j] = j;

		for (int j = 1; j <= s2.Length; j++)
		{
			for (int i = 1; i <= s1.Length; i++)
			{
				if (s1[i - 1] == s2[j - 1])
					d[i, j] = d[i - 1, j - 1];
				else
					d[i, j] = Mathf.Min(
						d[i - 1, j] + 1,     // deletion
						Mathf.Min(
							d[i, j - 1] + 1,  // insertion
							d[i - 1, j - 1] + 1 // substitution
						)
					);
			}
		}

		return d[s1.Length, s2.Length];
	}

	private string GetFallbackWord()
	{
		string[] fallbackWords = {
			"book", "phone", "cup", "bag", "chair", "desk", "lamp", "pen", "clock", "mirror",
			"lightsaber", "pokeball", "wand", "ring", "shield", "sword", "crown", "gem", "staff", "orb"
		};

		return fallbackWords[Random.Range(0, fallbackWords.Length)];
	}

	private void GenerateFallbackWords()
	{
		activeWords.Clear();
		allUsedWords.Clear();
		discoveredWords.Clear();

		// Generate just 2 initial words
		for (int i = 0; i < minimumObjects; i++)
		{
			string word = GetFallbackWord();
			while (allUsedWords.Contains(word))
			{
				word = GetFallbackWord();
			}
			activeWords.Add(word);
			allUsedWords.Add(word);
			discoveredWords.Add(word);
		}
	}

	public float SizeConverter(float sizeFactor)
	{
		return sizeFactor / 4f;
	}
}