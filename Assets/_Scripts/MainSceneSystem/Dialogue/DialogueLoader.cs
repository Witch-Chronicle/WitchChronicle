using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dialogue 로드
/// </summary>
public class DialogueLoader
{
    private readonly Dictionary<string, DialogueData> _cache = new();

    /// <summary>
    /// Dialogue 조회
    /// </summary>
    public DialogueData Load(TextAsset jsonFile)
    {
        if (jsonFile == null)
        {
            Debug.LogError("Dialogue Json Missing.");

            return null;
        }

        if (_cache.TryGetValue(jsonFile.name, out DialogueData data))
        {
            return data;
        }

        data = JsonUtility.FromJson<DialogueData>(jsonFile.text);

        _cache.Add(jsonFile.name, data);

        return data;
    }
}