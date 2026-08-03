using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 개별 던전 선택 버튼의 입력을 받아 해당 던전 데이터를 상위 컨트롤러에 전달하는 컴포넌트입니다.
/// </summary>
public class DungeonButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private DungeonData _dungeonData;

    public event Action<DungeonData> OnDungeonSelected;

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (_dungeonData == null)
        {
            Debug.LogWarning($"[DungeonButton] {_button.name}에 할당된 DungeonData가 없습니다.");
            return;
        }

        Debug.Log($"[DungeonButton] 버튼 클릭됨: {_dungeonData.DungeonName}");
        OnDungeonSelected?.Invoke(_dungeonData);
    }
}