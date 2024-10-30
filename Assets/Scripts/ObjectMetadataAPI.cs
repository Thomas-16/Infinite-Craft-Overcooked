using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

[Serializable]
public class ObjectMetadata
{
	public string emoji;
	public float scale;
	public float mass;
	public List<string> colors;
}

public class ObjectMetadataAPI : MonoBehaviour
{
	private const string BASE_URL = "https://foryu-backend.onrender.com/api/infinite/metadata/";

	public static ObjectMetadataAPI Instance { get; private set; }

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
		try
		{
			string url = BASE_URL + Uri.EscapeDataString(objectName);

			using (UnityWebRequest request = UnityWebRequest.Get(url))
			{
				// Send request and wait for response
				var operation = request.SendWebRequest();
				while (!operation.isDone)
					await Task.Yield();

				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.LogError($"Failed to get metadata for {objectName}: {request.error}");
					return null;
				}

				// Parse the JSON response
				string jsonResponse = request.downloadHandler.text;
				ObjectMetadata metadata = JsonUtility.FromJson<ObjectMetadata>(jsonResponse);

				if (metadata == null)
				{
					Debug.LogError($"Failed to parse metadata for {objectName}");
					return null;
				}

				return metadata;
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"Error fetching metadata for {objectName}: {e.Message}");
			return null;
		}
	}
}