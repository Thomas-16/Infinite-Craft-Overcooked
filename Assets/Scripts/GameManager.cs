using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	#region Inspector References
	[SerializeField] private GameObject elementPrefab;
	[SerializeField] private Transform playerTransform;
	#endregion

	#region Spawn Settings
	[Header("Spawn Settings")]
	[SerializeField] private float spawnHeight = 1.5f;
	[SerializeField] private float minSpawnRadius = 2f; // Minimum distance from player
	[SerializeField] private float maxSpawnRadius = 5f; // Maximum distance from player
	[SerializeField] private float minDistanceBetweenElements = 1f; // Minimum distance between LLements
	[SerializeField] private int maxSpawnAttempts = 30; // Maximum attempts to find a valid spawn position
	#endregion

	#region Game Settings
	[Header("Game Settings")]
	[SerializeField] private int minimumObjects = 2;
	[SerializeField] private int maximumObjects = 10;
	[SerializeField] private float minSpawnDelay = 1f;
	[SerializeField] private float maxSpawnDelay = 3f;
	[SerializeField] private float similarityThreshold = 0.3f;
	#endregion

	#region UI Settings
	[Header("UI Settings")]
	[SerializeField] private UIPanel scorePanel;
	#endregion

	#region Merge Effect Settings
	[Header("Merge Effect Settings")]
	[SerializeField] private ParticleSystem mergeEffectPrefab;
	[SerializeField] private float mergeEffectDuration = 1f;
	[SerializeField] private float mergeEffectScale = 1f;
	#endregion

	#region State Management
	private Dictionary<string, ObjectMetadata> elementDatabase = new Dictionary<string, ObjectMetadata>();
	private HashSet<string> discoveredWords = new HashSet<string>();
	private HashSet<string> allUsedWords = new HashSet<string>();
	private List<string> activeWords = new List<string>();
	private List<LLement> activeElements = new List<LLement>();
	private int playerScore = 0;
	private bool isSpawning = false;
	#endregion

	[Header("Point Bubble Settings")]
	[SerializeField] private PointBubble pointBubblePrefab;

	#region Public Methods
	public List<string> GetActiveWords() => new List<string>(activeWords);
	#endregion

	#region Initialization
	private void Awake() => Instance = this;

	private void Start()
	{
		InitializePlayerTransform();
		StartCoroutine(InitializeAndSpawn());
		StartCoroutine(MonitorObjectCount());
	}

	private void InitializePlayerTransform()
	{
		if (playerTransform != null) return;

		playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
		if (playerTransform == null)
		{
			Debug.LogError("Player transform not found! Please assign it in the inspector or tag your player with 'Player'");
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
				Vector3? spawnPosition = CalculateSpawnPosition();
				if (spawnPosition.HasValue)
				{
					LLement newElement = SpawnElement(word, spawnPosition.Value, metadata);
					activeElements.Add(newElement);
					Debug.Log($"Initial object spawned: {word}, metadata scale: {metadata.scale}");
					yield return new WaitForSeconds(0.2f);
				}
				else
				{
					Debug.LogWarning($"Failed to spawn initial object {word} - no valid position found");
				}
			}
			else
			{
				Debug.LogError($"No metadata found for initial word: {word}");
			}
		}
	}
	#endregion

	#region LLement Management
	private async Task<LLement> CreateLLement(string elementName, Vector3 position)
	{
		ObjectMetadata metadata = await GetOrFetchMetadata(elementName);
		return SpawnElement(elementName, position, metadata);
	}

	private async Task<ObjectMetadata> GetOrFetchMetadata(string elementName)
	{
		if (elementDatabase.TryGetValue(elementName, out ObjectMetadata metadata))
		{
			return metadata;
		}

		metadata = await ObjectMetadataAPI.Instance.GetObjectMetadata(elementName);
		metadata.word = elementName;
		return metadata;
	}

	private LLement SpawnElement(string word, Vector3 position, ObjectMetadata metadata)
	{
		LLement newElement = Instantiate(elementPrefab, position, Quaternion.identity).GetComponent<LLement>();
		newElement.SetElementName(word, metadata);
		Debug.Log("spawned object: " + word);
		return newElement;
	}

	public async void MergeElements(LLement element1, LLement element2)
	{
		string name1 = element1.ElementName.ToLower();
		string name2 = element2.ElementName.ToLower();
		Vector3 mergePosition = FindMidpoint(element1.transform.position, element2.transform.position);

		// Remove from active tracking
		activeElements.Remove(element1);
		activeElements.Remove(element2);
		activeWords.Remove(name1);
		activeWords.Remove(name2);

		// Start merge animations
		ParticleSystem mergeEffect = SpawnMergeEffect(mergePosition);

		// Create a TaskCompletionSource for tracking animation completion
		var animationComplete = new TaskCompletionSource<bool>();
		int completedAnimations = 0;

		void OnAnimationComplete()
		{
			completedAnimations++;
			if (completedAnimations >= 2)
			{
				animationComplete.SetResult(true);
			}
		}

		// Start merge animations on both elements
		element1.StartMergeAnimation(mergePosition, () => {
			Destroy(element1.gameObject);
			OnAnimationComplete();
		});

		element2.StartMergeAnimation(mergePosition, () => {
			Destroy(element2.gameObject);
			OnAnimationComplete();
		});

		// Wait for animations to complete
		await Task.WhenAll(animationComplete.Task);

		// Generate new element
		string newWord = await GenerateNewWord(name1, name2);
		ObjectMetadata metadata = await GetOrFetchMetadata(newWord);

		// Calculate and award points
		AwardPoints(mergePosition, newWord, metadata.emoji, name1, name2);
		elementDatabase[newWord] = metadata;

		// Create new element after effect duration
		//await Task.Delay((int)(mergeEffectDuration * 1000));

		await CreateAndTrackNewElement(newWord, mergePosition, metadata);

		// Cleanup
		CleanupMergeEffect(mergeEffect);
	}

	private void RemoveElements(LLement element1, LLement element2, string name1, string name2)
	{
		activeElements.Remove(element1);
		activeElements.Remove(element2);
		activeWords.Remove(name1);
		activeWords.Remove(name2);
		Destroy(element1.gameObject);
		Destroy(element2.gameObject);
	}

	private async Task<string> GenerateNewWord(string word1, string word2)
	{
		string prompt = $"What word do you associate with the combination of a {word1} with a {word2}?" +
					   $"Say ONLY one simple word. It must be a real word. It must be a noun. You can use famous pop culture examples too. Do not make up words." +
					   $"Here are some examples: 'Mermaid' and 'Japan' combines to create 'Sushi'. 'Curse' and 'Island' creates Bermuda. Dragon and Leviathan creates Megalodon.";
		;

		string newWord = await ChatGPTClient.Instance.SendChatRequest(prompt);
		return newWord.Replace(".", "").Trim().ToLower();
	}

	private async Task CreateAndTrackNewElement(string newWord, Vector3 position, ObjectMetadata metadata)
	{
		if (!allUsedWords.Contains(newWord))
		{
			TrackNewWord(newWord);
		}

		LLement newElement = await CreateLLement(newWord, position);
		activeElements.Add(newElement);
		activeWords.Add(newWord);
	}

	private void TrackNewWord(string newWord)
	{
		allUsedWords.Add(newWord);
		discoveredWords.Add(newWord);
		Debug.Log($"New element discovered: {newWord}");
	}
	#endregion

	#region Word Generation and Management
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
					metadata.word = cleanWord;

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
					metadata.word = fallbackWord;

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

	private void GenerateFallbackWords()
	{
		activeWords.Clear();
		allUsedWords.Clear();
		discoveredWords.Clear();

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

	private string GetFallbackWord()
	{
		string[] fallbackWords = {
			"book", "phone", "cup", "bag", "chair", "desk", "lamp", "pen", "clock", "mirror",
			"lightsaber", "pokeball", "wand", "ring", "shield", "sword", "crown", "gem", "staff", "orb"
		};

		return fallbackWords[Random.Range(0, fallbackWords.Length)];
	}
	#endregion

	#region Spawning System
	private IEnumerator MonitorObjectCount()
	{
		while (true)
		{
			activeElements.RemoveAll(item => item == null);

			if (activeElements.Count < maximumObjects && discoveredWords.Count > 0 && !isSpawning)
			{
				List<string> availableWords = GetAvailableWords();

				if (availableWords.Count > 0 && activeElements.Count < minimumObjects)
				{
					StartCoroutine(SpawnNewObject());
				}
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private List<string> GetAvailableWords()
	{
		List<string> availableWords = new List<string>();
		foreach (string word in discoveredWords)
		{
			if (!activeWords.Contains(word))
			{
				availableWords.Add(word);
			}
		}
		return availableWords;
	}
	private IEnumerator SpawnNewObject()
	{
		isSpawning = true;
		yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));

		List<string> availableWords = GetAvailableWords();
		if (availableWords.Count > 0)
		{
			string selectedWord = availableWords[Random.Range(0, availableWords.Count)];
			Vector3? spawnPosition = CalculateSpawnPosition();

			if (spawnPosition.HasValue)
			{
				StartCoroutine(SpawnElementCoroutine(selectedWord, spawnPosition));
				activeWords.Add(selectedWord);
			}
			else
			{
				Debug.LogWarning($"Failed to spawn {selectedWord} - no valid position found");
			}
		}

		isSpawning = false;
	}


	// New coroutine to handle the async spawning
	private IEnumerator SpawnElementCoroutine(string word, Vector3? position)
	{
		if (!position.HasValue)
		{
			Debug.LogWarning($"Failed to spawn {word} - no valid position found");
			yield break;
		}

		var task = CreateLLement(word, position.Value);
		while (!task.IsCompleted)
		{
			yield return null;
		}

		LLement newElement = task.Result;
		activeElements.Add(newElement);
	}

	// Keep this method for other async calls
	/*private async Task SpawnElementNearPlayer(string word)
	{
		Vector3 spawnPosition = CalculateSpawnPosition();
		LLement newElement = await CreateLLement(word, spawnPosition);
		activeElements.Add(newElement);
	}*/

	private Vector3? CalculateSpawnPosition()
	{
		for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
		{
			// Generate a random angle
			float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

			// Generate a random distance between min and max radius
			float distance = Mathf.Sqrt(Random.Range(minSpawnRadius * minSpawnRadius, maxSpawnRadius * maxSpawnRadius));

			// Calculate position
			float x = Mathf.Cos(angle) * distance;
			float z = Mathf.Sin(angle) * distance;
			Vector3 potentialPosition = playerTransform.position + new Vector3(x, spawnHeight, z);

			// Check if position is valid (not too close to other LLements)
			if (IsValidSpawnPosition(potentialPosition))
			{
				return potentialPosition;
			}
		}

		Debug.LogWarning("Could not find valid spawn position after " + maxSpawnAttempts + " attempts");
		return null;
	}

	private bool IsValidSpawnPosition(Vector3 position)
	{
		// Check distance from all active elements
		foreach (LLement element in activeElements)
		{
			if (element == null) continue;

			float distance = Vector3.Distance(position, element.transform.position);
			if (distance < minDistanceBetweenElements)
			{
				return false;
			}
		}
		return true;
	}
	#endregion

	#region Scoring and Points
	private void UpdateScoreDisplay()
	{
		if (scorePanel != null)
		{
			scorePanel.SetText($"{playerScore} pts");
		}
	}

	private void AwardPoints(Vector3 mergePosition, string newWord, string newEmoji, string parent1, string parent2)
	{
		int points = CalculatePoints(newWord, newEmoji, parent1, parent2);
		// Create point bubble
		string reason = GetPointReason(points, newWord, newEmoji, parent1, parent2);
		CreatePointBubble(mergePosition, points, reason);

		playerScore += points;
		UpdateScoreDisplay();

		// Add time for successful merge
		if (points > 0)
		{
			TimerManager.Instance.AddTime(points);
		}
	}

	private string GetPointReason(int points, string newWord, string newEmoji, string parent1, string parent2)
	{
		if (points == 0) return "duplicate";

		List<string> reasons = new List<string>();

		if (!elementDatabase.ContainsKey(newWord))
			reasons.Add("new word");

		bool differentEmoji = true;
		if (elementDatabase.ContainsKey(parent1) && elementDatabase[parent1].emoji == newEmoji)
			differentEmoji = false;
		if (elementDatabase.ContainsKey(parent2) && elementDatabase[parent2].emoji == newEmoji)
			differentEmoji = false;
		if (differentEmoji)
			reasons.Add("new emoji");

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
			reasons.Add("unique");

		return string.Join(", ", reasons);
	}

	private void CreatePointBubble(Vector3 worldPosition, int points, string reason)
	{
		PointBubble bubble = UIManager.Instance.CreateScreenSpacePanel(
			pointBubblePrefab,
			Vector2.zero
		) as PointBubble;

		if (bubble != null)
		{
			// Get timer UI reference
			RectTransform timerRect = TimerManager.Instance.GetTimerRect();
			bubble.Show(worldPosition, points, reason, timerRect);
		}
	}

	private int CalculatePoints(string newWord, string newEmoji, string parent1, string parent2)
	{
		int points = 1;

		// New word bonus
		if (!elementDatabase.ContainsKey(newWord))
		{
			points += 1;
			Debug.Log("Point awarded: New word");
		}
		else
		{
			return 1; // Duplicate combination
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
	#endregion

	#region Utility Methods
	private Vector3 FindMidpoint(Vector3 pointA, Vector3 pointB) => (pointA + pointB) / 2;

	private ParticleSystem SpawnMergeEffect(Vector3 position)
	{
		if (mergeEffectPrefab == null) return null;

		ParticleSystem effect = Instantiate(mergeEffectPrefab, position, Quaternion.identity);
		effect.transform.localScale = Vector3.one * mergeEffectScale;
		effect.Play();
		return effect;
	}

	private void CleanupMergeEffect(ParticleSystem effect)
	{
		if (effect != null)
		{
			effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
		}
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

	public float SizeConverter(float sizeFactor) => sizeFactor / 4f;
	#endregion



	[SerializeField] private GameOverPanel gameOverPanel;

	// Add to State Management region
	private bool isGameOver = false;

	// Add new method
	public void OnGameOver()
	{
		if (isGameOver) return;
		isGameOver = true;

		// Stop spawning and timer
		StopAllCoroutines();
		TimerManager.Instance.PauseTimer();

		StartCoroutine(gameOverPanel.FadeIn());
	}

	public void RestartGame()
	{
		// Reset player
		if (playerTransform != null)
		{
			// Get the player component
			Player player = playerTransform.GetComponent<Player>();
			if (player != null)
			{
				// Drop any held object
				player.DropCurrentObject();

				// Reset position and rotation
				playerTransform.position = Vector3.zero;
				playerTransform.rotation = Quaternion.identity;
			}
		}

		// Clear existing elements
		foreach (var element in activeElements)
		{
			if (element != null)
				Destroy(element.gameObject);
		}

		// Reset state
		activeElements.Clear();
		activeWords.Clear();
		discoveredWords.Clear();
		allUsedWords.Clear();
		elementDatabase.Clear();
		isGameOver = false;
		playerScore = 0;
		UpdateScoreDisplay();

		// Restart timer
		TimerManager.Instance.ResetTimer();

		// Start game systems
		StartCoroutine(InitializeAndSpawn());
		StartCoroutine(MonitorObjectCount());
		StartCoroutine(gameOverPanel.FadeOut());

	}
}