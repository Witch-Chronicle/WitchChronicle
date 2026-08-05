using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AlertType별 출력 문구와 표시 시간을 정의하는 데이터입니다.
/// </summary>
[Serializable]
public class AlertEntry
{
    [Tooltip("이 Alert 데이터의 식별 타입")]
    public AlertType Type;

    [TextArea(2, 5)]
    [Tooltip("출력 문구. 동적 값은 {0}, {1}, {2} 형식으로 작성합니다.")]
    public string Text;

    [Min(0.1f)]
    [Tooltip("Fade In 완료 후 Alert가 유지되는 시간")]
    public float LifeTime = 2.5f;
}

/// <summary>
/// AlertType별 AlertEntry를 관리하는 ScriptableObject입니다.
/// </summary>
[CreateAssetMenu(
    fileName = "AlertDatabase",
    menuName = "Witch Chronicle/UI/Alert Database"
)]
public class AlertDatabaseSO : ScriptableObject
{
    [SerializeField]
    private List<AlertEntry> _entries = new();

    private Dictionary<AlertType, AlertEntry> _entryDictionary;

    /// <summary>
    /// 데이터베이스를 Dictionary로 초기화합니다.
    /// </summary>
    public void Initialize()
    {
        _entryDictionary = new Dictionary<AlertType, AlertEntry>();

        if (_entries == null)
        {
            return;
        }

        foreach (AlertEntry entry in _entries)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.Type == AlertType.None)
            {
                Debug.LogWarning(
                    "[AlertDatabaseSO] AlertType.None인 Entry가 있습니다.",
                    this
                );

                continue;
            }

            if (_entryDictionary.ContainsKey(entry.Type))
            {
                Debug.LogWarning(
                    $"[AlertDatabaseSO] 중복 AlertType이 있습니다: {entry.Type}",
                    this
                );

                continue;
            }

            _entryDictionary.Add(entry.Type, entry);
        }
    }

    /// <summary>
    /// AlertType에 해당하는 Entry를 반환합니다.
    /// </summary>
    public bool TryGetEntry(
        AlertType type,
        out AlertEntry entry)
    {
        if (_entryDictionary == null)
        {
            Initialize();
        }

        return _entryDictionary.TryGetValue(type, out entry);
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        // 인스펙터에서 값이 변경되면 런타임 Dictionary를 다시 생성하도록 초기화
        _entryDictionary = null;
    }

#endif
}