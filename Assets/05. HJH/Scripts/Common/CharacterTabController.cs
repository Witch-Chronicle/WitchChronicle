using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// IntergrationPanel/Equip/EquipSection의 캐릭터 탭 버튼들을 관리.
/// - Party.Instance.Members를 보고, 실제로 파티에 있는 캐릭터의 탭만 활성화
/// - 탭 클릭 시 CharacterSelectionManager.SetSelected() 호출
/// - 지금 선택된 탭은 배경/텍스트 색을 다르게 표시 (기본: 흰 배경/검은 텍스트, 선택: 회색 배경/흰 텍스트)
/// * 아리엘은 항상 파티에 있는 걸로 간주 (기본 선택값도 CharacterSelectionManager 쪽에서 Ariel 고정)
/// </summary>
public class CharacterTabController : MonoBehaviour
{
    [System.Serializable]
    private class CharacterTab
    {
        public CharacterType character;
        public Button button;
        [Tooltip("버튼의 배경 Image (색상 변경용)")]
        public Image background;
        [Tooltip("버튼 안의 텍스트 (색상 변경용)")]
        public TextMeshProUGUI label;
    }

    [Header("캐릭터 탭 버튼 (4개)")]
    [SerializeField] private List<CharacterTab> _tabs = new List<CharacterTab>();

    [Header("탭 색상")]
    [SerializeField] private Color _normalBackgroundColor = Color.white;
    [SerializeField] private Color _normalTextColor = Color.black;
    [SerializeField] private Color _selectedBackgroundColor = Color.gray;
    [SerializeField] private Color _selectedTextColor = Color.white;

    private void OnEnable()
    {
        RefreshTabVisibility();

        if (CharacterSelectionManager.Instance != null)
        {
            UpdateSelectionHighlight(CharacterSelectionManager.Instance.GetSelected());
            CharacterSelectionManager.Instance.OnSelectionChanged += UpdateSelectionHighlight;
        }

        foreach (var tab in _tabs)
        {
            if (tab.button == null) continue;

            CharacterType character = tab.character; // 클로저 캡처용 지역변수
            tab.button.onClick.AddListener(() => OnClickTab(character));
        }
    }

    private void OnDisable()
    {
        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged -= UpdateSelectionHighlight;
        }

        foreach (var tab in _tabs)
        {
            if (tab.button != null)
            {
                tab.button.onClick.RemoveAllListeners();
            }
        }
    }

    /// <summary>
    /// Characters(PersistentCharacterManager)의 ActiveParty에 실제로 있는 캐릭터의 탭만 활성화.
    /// </summary>
    private void RefreshTabVisibility()
    {
        if (PersistentCharacterManager.Instance == null) return;

        List<PersistentCharacterUnit> activeParty = new List<PersistentCharacterUnit>();
        PersistentCharacterManager.Instance.GetActivePartyMembers(activeParty);

        HashSet<CharacterType> partyCharacters = new HashSet<CharacterType>();

        foreach (var unit in activeParty)
        {
            if (unit == null || unit.CharacterEquipment == null) continue;
            partyCharacters.Add(unit.CharacterEquipment.Character);
        }

        foreach (var tab in _tabs)
        {
            if (tab.button != null)
            {
                tab.button.gameObject.SetActive(partyCharacters.Contains(tab.character));
            }
        }
    }

    /// <summary>
    /// 지금 선택된 캐릭터의 탭만 강조 색상으로, 나머지는 기본 색상으로.
    /// </summary>
    private void UpdateSelectionHighlight(CharacterType selected)
    {
        foreach (var tab in _tabs)
        {
            bool isSelected = tab.character == selected;

            if (tab.background != null)
            {
                tab.background.color = isSelected ? _selectedBackgroundColor : _normalBackgroundColor;
            }

            if (tab.label != null)
            {
                tab.label.color = isSelected ? _selectedTextColor : _normalTextColor;
            }
        }
    }

    private void OnClickTab(CharacterType character)
    {
        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.SetSelected(character);
        }
    }
}