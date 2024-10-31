using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

[Serializable]
public class ObjectMetadata
{
	public string emoji = "🎲"; // Default emoji
	public float scale = 1f;
	public float mass = 1f;
	public List<string> colors = new List<string> { "#FFFFFF" };

	// Constructor for default metadata
	public ObjectMetadata(string objectName)
	{
		// Set some sensible defaults based on the object name
		emoji = GetDefaultEmoji(objectName);
		scale = 1f;
		mass = 1f;
		colors = new List<string> { "#FFFFFF" };
	}

	private string GetDefaultEmoji(string objectName)
	{
		// Add some basic mappings for common objects
		switch (objectName.ToLower())
		{
			case "fruit": return "🍎";
			case "apple": return "🍎";
			case "banana": return "🍌";
			case "water": return "💧";
			case "fire": return "🔥";
			case "heart": return "❤️";
			case "star": return "⭐";
			case "sun": return "☀️";
			case "moon": return "🌙";
			case "tree": return "🌲";
			case "rock": return "🪨";
			case "earth": return "🌍";
			default: return "❓"; // Default fallback emoji
		}
	}
}

public class ObjectMetadataAPI : MonoBehaviour
{
	private const string BASE_URL = "https://foryu-backend.onrender.com/api/infinite/metadata/";

	public static ObjectMetadataAPI Instance { get; private set; }

	[Header("Settings")]
	[SerializeField] private bool useOfflineDefaults = false;  // Toggle for testing without API
	[SerializeField] private float requestTimeout = 5f;  // Timeout in seconds

	private Dictionary<string, ObjectMetadata> cache = new Dictionary<string, ObjectMetadata>();

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(this);
			return;
		}
		Instance = this;
	}

	public async Task<ObjectMetadata> GetObjectMetadata(string objectName)
	{
		// Check cache first
		if (cache.TryGetValue(objectName, out ObjectMetadata cachedMetadata))
		{
			return cachedMetadata;
		}

		if (useOfflineDefaults)
		{
			return CreateAndCacheDefaultMetadata(objectName);
		}

		try
		{
			string url = BASE_URL + Uri.EscapeDataString(objectName);

			using (UnityWebRequest request = UnityWebRequest.Get(url))
			{
				request.timeout = Mathf.RoundToInt(requestTimeout);

				// Send request and wait for response
				var operation = request.SendWebRequest();

				while (!operation.isDone)
				{
					if (request.result == UnityWebRequest.Result.ConnectionError ||
						request.result == UnityWebRequest.Result.DataProcessingError ||
						request.result == UnityWebRequest.Result.ProtocolError)
					{
						break;
					}
					await Task.Yield();
				}

				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.LogWarning($"Failed to get metadata for {objectName}, using defaults. Error: {request.error}");
					return CreateAndCacheDefaultMetadata(objectName);
				}

				// Parse the JSON response
				string jsonResponse = request.downloadHandler.text;
				ObjectMetadata metadata = JsonUtility.FromJson<ObjectMetadata>(jsonResponse);

				if (metadata == null)
				{
					Debug.LogWarning($"Failed to parse metadata for {objectName}, using defaults");
					return CreateAndCacheDefaultMetadata(objectName);
				}

				// Cache the result
				cache[objectName] = metadata;
				return metadata;
			}
		}
		catch (Exception e)
		{
			Debug.LogWarning($"Error fetching metadata for {objectName}: {e.Message}, using defaults");
			return CreateAndCacheDefaultMetadata(objectName);
		}
	}

	private ObjectMetadata CreateAndCacheDefaultMetadata(string objectName)
	{
		var metadata = new ObjectMetadata(objectName);
		cache[objectName] = metadata;
		return metadata;
	}

	// Optional: Clear cache if needed
	public void ClearCache()
	{
		cache.Clear();
	}
}