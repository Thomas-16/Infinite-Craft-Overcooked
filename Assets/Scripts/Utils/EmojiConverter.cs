using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class EmojiConverter
{
	private static Dictionary<string, Sprite> emojiCache = new Dictionary<string, Sprite>();

	public static async Task<Sprite> GetEmojiSprite(string emoji)
	{
		// Check cache first
		if (emojiCache.TryGetValue(emoji, out Sprite cachedSprite))
		{
			return cachedSprite;
		}

		// Convert emoji to OpenMoji filename format
		string emojiResourceName = GetEmojiResourceName(emoji);

		// Try to load from Resources/Emojis folder
		Sprite emojiSprite = Resources.Load<Sprite>($"Emojis/{emojiResourceName}");

		if (emojiSprite != null)
		{
			emojiCache[emoji] = emojiSprite;
			return emojiSprite;
		}

		// If not found, try without any modifiers
		string baseEmojiName = GetBaseEmojiResourceName(emoji);
		if (baseEmojiName != emojiResourceName)
		{
			emojiSprite = Resources.Load<Sprite>($"Emojis/{baseEmojiName}");
			if (emojiSprite != null)
			{
				emojiCache[emoji] = emojiSprite;
				return emojiSprite;
			}
		}

		// If still not found, load default sprite
		Sprite defaultSprite = Resources.Load<Sprite>("Emojis/default");
		if (defaultSprite != null)
		{
			Debug.LogWarning($"Using default sprite for emoji: {emoji} (Tried: {emojiResourceName} and {baseEmojiName})");
			emojiCache[emoji] = defaultSprite;
			return defaultSprite;
		}

		Debug.LogError($"No sprite found for emoji: {emoji} (Tried: {emojiResourceName} and {baseEmojiName})");
		return null;
	}

	private static string GetEmojiResourceName(string emoji)
	{
		StringBuilder hexValue = new StringBuilder();
		bool isFirstCodePoint = true;

		// Convert each UTF-32 code point
		for (int i = 0; i < emoji.Length; i++)
		{
			// Handle surrogate pairs
			if (char.IsSurrogatePair(emoji, i))
			{
				int codePoint = char.ConvertToUtf32(emoji, i);
				if (!isFirstCodePoint)
				{
					hexValue.Append("-");
				}
				hexValue.Append(codePoint.ToString("X"));
				i++; // Skip the low surrogate
				isFirstCodePoint = false;
			}
			else if (!char.IsSurrogate(emoji[i]))
			{
				// Handle basic characters
				if (!isFirstCodePoint)
				{
					hexValue.Append("-");
				}
				hexValue.Append(((int)emoji[i]).ToString("X"));
				isFirstCodePoint = false;
			}
		}

		return hexValue.ToString();
	}

	private static string GetBaseEmojiResourceName(string emoji)
	{
		// Get just the first code point for compound emojis
		for (int i = 0; i < emoji.Length; i++)
		{
			if (char.IsSurrogatePair(emoji, i))
			{
				int codePoint = char.ConvertToUtf32(emoji, i);
				return codePoint.ToString("X");
			}
			else if (!char.IsSurrogate(emoji[i]))
			{
				return ((int)emoji[i]).ToString("X");
			}
		}
		return "";
	}

	// Debug method to help with troubleshooting
	public static void DebugPrintEmojiInfo(string emoji)
	{
		string resourceName = GetEmojiResourceName(emoji);
		string baseName = GetBaseEmojiResourceName(emoji);
		Debug.Log($"Emoji: {emoji}\nResource Name: {resourceName}\nBase Name: {baseName}");

		// Print individual character codes
		for (int i = 0; i < emoji.Length; i++)
		{
			if (char.IsSurrogatePair(emoji, i))
			{
				int codePoint = char.ConvertToUtf32(emoji, i);
				Debug.Log($"Surrogate Pair at {i}: {codePoint:X}");
				i++;
			}
			else
			{
				Debug.Log($"Character at {i}: {(int)emoji[i]:X}");
			}
		}
	}
}