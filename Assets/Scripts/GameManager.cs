using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class SpawnZone
{
	public Collider zone;
	[Tooltip("Weight affects how likely items are to spawn in this zone")]
	public float weight = 1f;
}

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	[SerializeField] private GameObject elementPrefab;

	[Header("Player Settings")]
	[SerializeField] private Transform playerRespawnPoint;
	[SerializeField] private GameObject player;
	[Tooltip("Optional particle effect to play when player respawns")]
	[SerializeField] private ParticleSystem respawnEffectPrefab;

	[Header("Spawn Settings")]
	[SerializeField] private List<SpawnZone> spawnZones = new List<SpawnZone>();
	[SerializeField] private int totalItemsToSpawn = 30;

	[Header("Merge Effect Settings")]
	[SerializeField] private ParticleSystem mergeEffectPrefab;
	[SerializeField] private float mergeEffectDuration = 1f;
	[SerializeField] private float mergeEffectScale = 1f;

	[Header("Word Generation Settings")]
	[SerializeField] private int minWordsBeforeReplenish = 20;
	[SerializeField] private float minReplenishDelay = 3f;
	[SerializeField] private float maxReplenishDelay = 8f;

	private HashSet<string> allUsedWords = new HashSet<string>();
	private List<string> activeWords = new List<string>();
	public List<string> GetActiveWords() => new List<string>(activeWords);
	private bool isReplenishing = false;


	[Header("Lightning Settings")]
	[SerializeField] private float minTimeBetweenStrikes = 3f;
	[SerializeField] private float maxTimeBetweenStrikes = 8f;
	[SerializeField] private float lightningChance = 0.7f; // Chance that the timer will actually trigger a strike
	[SerializeField] private int maxSimultaneousStrikes = 3; // Maximum number of simultaneous lightning strikes
	[SerializeField] private bool enableRandomLightning = true;
	//[SerializeField] private float safeZoneRadius = 5f; // Won't strike near player
	[SerializeField] private float lightningPlayerRange = 8f; // Range around player where lightning can strike
	[SerializeField] private float minDistanceFromPlayer = 0f; // Minimum distance from player to prevent too-close strikes

	private float nextLightningTime;
	private bool isLightningCoroutineRunning = false;
	[SerializeField] private HashSet<LLement> activeElements = new HashSet<LLement>();

	[Header("Continuous Spawn Settings")]
	[SerializeField] private float minSpawnDelay = 1f;
	[SerializeField] private float maxSpawnDelay = 3f;
	[SerializeField] private int spawnBatchSize = 3; // How many items to spawn at once when below threshold
	private bool isSpawningElements = false;

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		StartCoroutine(InitializeAndSpawn());
		StartCoroutine(MonitorWordCount());

		// Validate player and respawn point references
		if (player == null)
		{
			player = GameObject.FindGameObjectWithTag("Player");
			if (player == null)
			{
				Debug.LogWarning("No player object assigned or found with 'Player' tag!");
			}
		}

		if (playerRespawnPoint == null)
		{
			Debug.LogWarning("No respawn point assigned! Player will respawn at origin.");
		}

		InitializeLightning();

	}
	private IEnumerator MonitorElementCount()
	{
		while (true)
		{
			CleanupDestroyedElements(); // Remove null references

			int currentElementCount = activeElements.Count;
			if (currentElementCount < totalItemsToSpawn && !isSpawningElements)
			{
				StartCoroutine(SpawnElementsBatch());
			}

			yield return new WaitForSeconds(1f); // Check interval
		}
	}

	private IEnumerator SpawnElementsBatch()
	{
		isSpawningElements = true;

		int elementsNeeded = totalItemsToSpawn - activeElements.Count;
		int batchesToSpawn = Mathf.Min(spawnBatchSize, elementsNeeded);

		for (int i = 0; i < batchesToSpawn; i++)
		{
			Vector3? spawnPosition = GetRandomSpawnPosition();

			if (spawnPosition.HasValue && activeWords.Count > 0)
			{
				string randomWord = activeWords[Random.Range(0, activeWords.Count)];
				SpawnElement(randomWord, spawnPosition.Value);

				// Random delay between individual spawns
				float spawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
				yield return new WaitForSeconds(spawnDelay);
			}
		}

		isSpawningElements = false;
	}

	private void InitializeLightning()
	{
		Debug.Log("====== InitializeLightning");

		nextLightningTime = Random.Range(minTimeBetweenStrikes, maxTimeBetweenStrikes);
		if (!isLightningCoroutineRunning && enableRandomLightning)
		{
			Debug.Log("====== isLightningCoroutineRunning");

			StartCoroutine(LightningStrikeCoroutine());
		}
	}

	private IEnumerator LightningStrikeCoroutine()
	{
		Debug.Log("====== LightningStrikeCoroutine");
		isLightningCoroutineRunning = true;

		float interval = 0f;
		while (enableRandomLightning)
		{
			Debug.Log("====== nextLightningTime: " + nextLightningTime + ", interval: " + interval);

			if (interval >= nextLightningTime)// && Random.value <= lightningChance)
			{
				Debug.Log("====== enableRandomLightning");
				interval = 0f;
				StrikeRandomElement();
				nextLightningTime = Random.Range(minTimeBetweenStrikes, maxTimeBetweenStrikes);
			}
			interval += Time.deltaTime;
			//nextLightningTime = Time.time + Random.Range(minTimeBetweenStrikes, maxTimeBetweenStrikes);
			yield return null; // Check interval
		}

		isLightningCoroutineRunning = false;
	}

	private void StrikeRandomElement()
	{
		if (activeElements.Count == 0 || player == null) return;
		Debug.Log("====== StrikeRandomElement");

		// Get player position
		Vector3 playerPos = player.transform.position;

		// Find all elements within range of player
		List<LLement> elementsInRange = new List<LLement>();
		foreach (LLement element in activeElements)
		{
			if (element != null)
			{
				float distanceToPlayer = Vector3.Distance(element.transform.position, playerPos);
				if (distanceToPlayer <= lightningPlayerRange && distanceToPlayer >= minDistanceFromPlayer)
				{
					elementsInRange.Add(element);
				}
			}
		}

		// If we found any valid targets, strike one randomly
		if (elementsInRange.Count > 0)
		{
			LLement targetElement = elementsInRange[Random.Range(0, elementsInRange.Count)];
			if (targetElement != null)
			{
				LightningSystem.Instance.SpawnLightning(targetElement.transform.position);
			}
		}
	}

	private void CleanupDestroyedElements()
	{
		activeElements.RemoveWhere(element => element == null || !element.gameObject.activeInHierarchy);
	}

	private void Update()
	{
		// Handle respawn input
		if (Input.GetKeyDown(KeyCode.R))
		{
			RespawnPlayer();
		}
	}

	public void RespawnPlayer()
	{
		if (player == null) return;

		// Get respawn position
		Vector3 respawnPosition = playerRespawnPoint != null ?
			playerRespawnPoint.position : Vector3.zero;

		// Reset player position and rotation
		player.transform.position = respawnPosition;
		player.transform.rotation = playerRespawnPoint != null ?
			playerRespawnPoint.rotation : Quaternion.identity;

		// Spawn respawn effect if available
		if (respawnEffectPrefab != null)
		{
			ParticleSystem effect = Instantiate(respawnEffectPrefab,
				respawnPosition, Quaternion.identity);
			Destroy(effect.gameObject,
				effect.main.duration + effect.main.startLifetime.constantMax);
		}

		// Optional: Reset player velocity if it has a Rigidbody
		Rigidbody rb = player.GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}

		Debug.Log("Player respawned at: " + respawnPosition);
	}

	private async void GenerateInitialWords()
	{
		string prompt = "Generate a list of exactly 30 random items. Include everyday objects, pop culture items, or interesting things from history. " +
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

	private Vector3? GetRandomSpawnPosition()
	{
		if (spawnZones.Count == 0) return null;

		// Calculate total weight
		float totalWeight = 0f;
		foreach (var zone in spawnZones)
		{
			if (zone.zone != null)
				totalWeight += zone.weight;
		}

		// Select a random zone based on weights
		float randomWeight = Random.Range(0f, totalWeight);
		float currentWeight = 0f;
		SpawnZone selectedZone = null;

		foreach (var zone in spawnZones)
		{
			if (zone.zone == null) continue;

			currentWeight += zone.weight;
			if (randomWeight <= currentWeight)
			{
				selectedZone = zone;
				break;
			}
		}

		if (selectedZone == null || selectedZone.zone == null) return null;

		// Get random point in selected zone
		Bounds bounds = selectedZone.zone.bounds;
		int maxAttempts = 30;
		int attempts = 0;

		while (attempts < maxAttempts)
		{
			// Generate random point within bounds, including height
			Vector3 randomPoint = new Vector3(
				Random.Range(bounds.min.x, bounds.max.x),
				Random.Range(bounds.min.y, bounds.max.y),
				Random.Range(bounds.min.z, bounds.max.z)
			);

			// Check if point is actually inside collider
			if (selectedZone.zone.ClosestPoint(randomPoint) == randomPoint)
			{
				return randomPoint;
			}

			attempts++;
		}

		return null;
	}

	private IEnumerator InitializeAndSpawn()
	{
		// Wait for initial words to be generated
		while (activeWords.Count == 0)
		{
			GenerateInitialWords();
			yield return new WaitForSeconds(0.1f);
		}

		// Initial spawn
		StartCoroutine(SpawnInitialResources());

		// Start monitoring element count
		StartCoroutine(MonitorElementCount());
	}

	private IEnumerator SpawnInitialResources()
	{
		int attempts = 0;
		int maxAttempts = totalItemsToSpawn * 2; // Reduced from 10 since we have continuous spawning now
		int itemsSpawned = 0;

		while (itemsSpawned < totalItemsToSpawn && attempts < maxAttempts)
		{
			Vector3? spawnPosition = GetRandomSpawnPosition();

			if (spawnPosition.HasValue && activeWords.Count > 0)
			{
				string randomWord = activeWords[Random.Range(0, activeWords.Count)];
				SpawnElement(randomWord, spawnPosition.Value);
				itemsSpawned++;
				yield return new WaitForSeconds(0.1f);
			}

			attempts++;
		}

		if (attempts >= maxAttempts)
		{
			Debug.LogWarning("Reached maximum spawn attempts during initial spawn - continuing with continuous spawning.");
		}
	}

	private IEnumerator SpawnNewWords(List<string> newWords)
	{
		foreach (string word in newWords)
		{
			Vector3? spawnPosition = GetRandomSpawnPosition();

			if (spawnPosition.HasValue)
			{
				activeWords.Add(word);
				SpawnElement(word, spawnPosition.Value);
				yield return new WaitForSeconds(0.2f);
			}
		}

		isReplenishing = false;
	}

	public LLement SpawnElement(string name, Vector3 pos)
	{
		GameObject newObj = Instantiate(elementPrefab, pos, Quaternion.identity);
		LLement newElement = newObj.GetComponent<LLement>();
		newElement.SetElementName(name);
		activeElements.Add(newElement);
		return newElement;
	}


	public float SizeConverter(float sizeFactor)
	{
		return sizeFactor / 4f;
	}

	public async void MergeElements(LLement llement1, LLement llement2)
	{
		string name1 = llement1.ElementName;
		string name2 = llement2.ElementName;

		// Remove the words from active list
		activeWords.Remove(name1.ToLower());
		activeWords.Remove(name2.ToLower());

		activeElements.Remove(llement1);
		activeElements.Remove(llement2);

		Vector3 mergePosition = FindMidpoint(llement1.transform.position, llement2.transform.position) + (Vector3.up * 0.5f);

		ParticleSystem mergeEffect = SpawnMergeEffect(mergePosition);

		llement1.gameObject.SetActive(false);
		llement2.gameObject.SetActive(false);

		string response = await ChatGPTClient.Instance.SendChatRequest(
			"What object or item comes to mind when I combine " + name1 + " with " + name2 +
			"? Say ONLY one simple word that represents an object or item. Keep it simple, creative and engaging for a game where players combine elements. Do not make up words."
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
		LLement newElement = SpawnElement(response, mergePosition);
		activeElements.Add(newElement);

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

	public static Vector3 FindMidpoint(Vector3 pointA, Vector3 pointB)
	{
		return (pointA + pointB) / 2;
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		// Draw spawn zones
		Gizmos.color = Color.green;
		foreach (var zone in spawnZones)
		{
			if (zone.zone != null)
			{
				Gizmos.DrawWireCube(zone.zone.bounds.center, zone.zone.bounds.size);
			}
		}
	}
#endif
}