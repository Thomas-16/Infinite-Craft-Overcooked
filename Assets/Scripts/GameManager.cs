using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {  get; private set; }

    [SerializeField] private GameObject elementPrefab;
    [SerializeField] private TextAsset allItemsTxt;

    private void Awake() {
        Instance = this;
    }

    private async void Start() {
        //Vector3 pos = new Vector3(0, 5f, 3.5f);
        //int numPositions = 40;
        //for (int i = 0; i < numPositions; i++) {
        //    // Generate a random point within a sphere of the given distance around the original position
        //    Vector3 randomOffset = Random.insideUnitSphere * 3f;
        //    Vector3 newPosition = pos + randomOffset;

        //    string randomElementName = await GetRandomLineAsync();
        //    if(randomElementName.Length > 6) {
        //        continue;
        //    }
        //    SpawnElement(randomElementName, newPosition);
        //}
    }

    public async void MergeElements(LLement llement1, LLement llement2) {
        string name1 = llement1.ElementName;
        string name2 = llement2.ElementName;
        Vector3 pos1 = llement1.transform.position;
        Vector3 pos2 = llement2.transform.position;
        Destroy(llement1.gameObject);
        Destroy(llement2.gameObject);

        string response = await ChatGPTClient.Instance.SendChatRequest("What object or concept comes to mind when I combine " + name1 + " with " + name2 + "? Say ONLY one simple word that represents an object or concept. Keep it simple, creative and engaging for a game where players combine elements. Do not make up words.");
        response = response.Replace(".", "");
        SpawnElement(response, FindMidpoint(pos1, pos2) + (Vector3.up * 0.5f));
    }
    
    public void SpawnElement(string name, Vector3 pos) {
        LLement newElement = Instantiate(elementPrefab, pos, Quaternion.identity).GetComponent<LLement>();
        newElement.SetElementName(name);
    }

    // Async method to return a random line from the TextAsset
    public async Task<string> GetRandomLineAsync() {
        // Simulate an asynchronous operation (e.g., waiting for I/O, computation)
        await Task.Yield(); // Yields control back to the caller and waits for the next frame

        // Split the text into an array of lines
        string[] lines = allItemsTxt.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Check if there are any lines in the file
        if (lines.Length == 0) {
            Debug.LogWarning("Text file is empty or has no valid lines.");
            return string.Empty;
        }

        // Get a random index and return the corresponding line
        int randomIndex = Random.Range(0, lines.Length);
        return lines[randomIndex];
    }
    public static Vector3 FindMidpoint(Vector3 pointA, Vector3 pointB) {
        return (pointA + pointB) / 2;
    }
}
