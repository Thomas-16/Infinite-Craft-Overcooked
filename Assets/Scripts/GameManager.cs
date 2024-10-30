using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	[SerializeField] private GameObject elementPrefab;
	[SerializeField] private TextAsset allItemsTxt;

	[Header("Merge Effect Settings")]
	[SerializeField] private ParticleSystem mergeEffectPrefab;
	[SerializeField] private float mergeEffectDuration = 1f;
	[SerializeField] private float mergeEffectScale = 1f;

	private void Awake()
	{
		Instance = this;
	}

	private async void Start()
	{
		Vector3 pos = new Vector3(0, 5f, 3.5f);
		int numPositions = 40;
		for (int i = 0; i < numPositions; i++)
		{
			Vector3 randomOffset = Random.insideUnitSphere * 3f;
			Vector3 newPosition = pos + randomOffset;

			string randomElementName = await GetRandomLineAsync();
			SpawnElement(randomElementName, newPosition);
		}
	}

	public async void MergeElements(LLement llement1, LLement llement2)
	{
		string name1 = llement1.ElementName;
		string name2 = llement2.ElementName;
		Vector3 mergePosition = FindMidpoint(llement1.transform.position, llement2.transform.position) + (Vector3.up * 0.5f);

		// Start the merge effect and get the particle system instance
		ParticleSystem mergeEffect = SpawnMergeEffect(mergePosition);

		// Hide the original elements
		llement1.gameObject.SetActive(false);
		llement2.gameObject.SetActive(false);

		// Get the new element name while the effect is playing
		string response = await ChatGPTClient.Instance.SendChatRequest(
			"What object or concept comes to mind when I combine " + name1 + " with " + name2 +
			"? Say ONLY one simple word that represents an object or concept. Keep it simple, creative and engaging for a game where players combine elements. Do not make up words."
		);
		response = response.Replace(".", "");

		// Wait for the effect duration
		await Task.Delay((int)(mergeEffectDuration * 1000));

		// Destroy original elements
		Destroy(llement1.gameObject);
		Destroy(llement2.gameObject);

		// Spawn new element
		SpawnElement(response, mergePosition);

		// Clean up the effect
		if (mergeEffect != null)
		{
			// Stop emitting new particles
			mergeEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);

			// Destroy the effect after all particles have died
			Destroy(mergeEffect.gameObject, mergeEffect.main.duration + mergeEffect.main.startLifetime.constantMax);
		}
	}

	private ParticleSystem SpawnMergeEffect(Vector3 position)
	{
		if (mergeEffectPrefab != null)
		{
			ParticleSystem effect = Instantiate(mergeEffectPrefab, position, Quaternion.identity);
			effect.transform.localScale = Vector3.one * mergeEffectScale;

			// Make sure the effect plays
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

	public async Task<string> GetRandomLineAsync()
	{
		await Task.Yield();

		string[] lines = allItemsTxt.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length == 0)
		{
			Debug.LogWarning("Text file is empty or has no valid lines.");
			return string.Empty;
		}

		int randomIndex = Random.Range(0, lines.Length);
		return lines[randomIndex];
	}

	public static Vector3 FindMidpoint(Vector3 pointA, Vector3 pointB)
	{
		return (pointA + pointB) / 2;
	}
}