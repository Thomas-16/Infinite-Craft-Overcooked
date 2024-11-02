using UnityEngine;

public static class LanguageSettings
{
    public static bool IsSpanish { get; private set; } = false;

    public static void ToggleSpanish()
    {
        IsSpanish = !IsSpanish;
        Debug.Log($"Language switched to: {(IsSpanish ? "Spanish" : "English")}");
    }
}