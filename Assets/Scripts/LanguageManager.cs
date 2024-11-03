using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using UnityEngine.UI;
using System.Threading.Tasks;
using DG.Tweening;

public enum GameLanguage
{
    English,
    Spanish
}

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }
    
    [SerializeField] private GameLanguage gameLanguage = GameLanguage.English;
    
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>
    {
        ["UI"] = new Dictionary<string, string>
        {
            ["story_complete"] = "Story complete! Here comes another...",
            ["story_complete_es"] = "¡Historia completa! Aquí viene otra...",
            ["try_different"] = "Try using a different",
            ["try_different_es"] = "¡Intenta con otro tipo de"
        }
    };

    public GameLanguage CurrentLanguage => gameLanguage;

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
        }
    }

    public string GetTranslation(string category, string key)
    {
        if (gameLanguage == GameLanguage.English)
            return Translations[category].GetValueOrDefault(key, key);
            
        return Translations[category].GetValueOrDefault($"{key}_es", key);
    }

    public bool IsSpanish => gameLanguage == GameLanguage.Spanish;
}