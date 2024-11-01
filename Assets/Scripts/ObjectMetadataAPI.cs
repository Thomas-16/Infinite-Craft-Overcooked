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

	[Header("Debug Settings")]
	[SerializeField] private bool enableDetailedLogging = true;

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
		if (enableDetailedLogging)
		{
			Debug.Log($"[Metadata] Requesting metadata for: {objectName}");
		}

		// Check cache first
		if (cache.TryGetValue(objectName, out ObjectMetadata cachedMetadata))
		{
			if (enableDetailedLogging)
			{
				Debug.Log($"[Metadata] Found in cache for {objectName}: {cachedMetadata.emoji}");
			}
			return cachedMetadata;
		}

		if (useOfflineDefaults)
		{
			if (enableDetailedLogging)
			{
				Debug.Log("[Metadata] Using offline defaults");
			}
			return CreateAndCacheDefaultMetadata(objectName);
		}

		try
		{
			string url = BASE_URL + Uri.EscapeDataString(objectName);
			Debug.Log($"[Metadata] Fetching from URL: {url}");

			using (UnityWebRequest request = UnityWebRequest.Get(url))
			{
				request.timeout = Mathf.RoundToInt(requestTimeout);

				Debug.Log($"[Metadata] Sending request for {objectName}...");
				var operation = request.SendWebRequest();

				while (!operation.isDone)
				{
					if (request.result == UnityWebRequest.Result.ConnectionError ||
						request.result == UnityWebRequest.Result.DataProcessingError ||
						request.result == UnityWebRequest.Result.ProtocolError)
					{
						Debug.LogError($"[Metadata] Request failed mid-operation: {request.error}");
						break;
					}
					await Task.Yield();
				}

				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.LogError($"[Metadata] Request failed for {objectName}");
					Debug.LogError($"Error: {request.error}");
					Debug.LogError($"Response Code: {request.responseCode}");
					Debug.LogError($"Response Headers: {request.GetResponseHeaders()}");
					Debug.LogError($"Response Text: {request.downloadHandler?.text}");
					return CreateAndCacheDefaultMetadata(objectName);
				}

				string jsonResponse = request.downloadHandler.text;
				Debug.Log($"[Metadata] Received response for {objectName}: {jsonResponse}");

				try
				{
					ObjectMetadata metadata = JsonUtility.FromJson<ObjectMetadata>(jsonResponse);

					if (metadata == null)
					{
						Debug.LogError($"[Metadata] Failed to parse JSON for {objectName}");
						return CreateAndCacheDefaultMetadata(objectName);
					}

					if (string.IsNullOrEmpty(metadata.emoji))
					{
						Debug.LogError($"[Metadata] No emoji in response for {objectName}");
						return CreateAndCacheDefaultMetadata(objectName);
					}

					Debug.Log($"[Metadata] Successfully parsed metadata for {objectName}. Emoji: {metadata.emoji}");
					cache[objectName] = metadata;
					return metadata;
				}
				catch (Exception parseEx)
				{
					Debug.LogError($"[Metadata] JSON Parse error for {objectName}: {parseEx.Message}");
					Debug.LogError($"Raw JSON: {jsonResponse}");
					return CreateAndCacheDefaultMetadata(objectName);
				}
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"[Metadata] Exception for {objectName}: {e.Message}\nStack: {e.StackTrace}");
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