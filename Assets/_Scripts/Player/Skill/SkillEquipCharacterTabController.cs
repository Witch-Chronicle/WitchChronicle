using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SkillEquipCharacterTabController : MonoBehaviour
{
    [System.Serializable]
    private class CharacterSlotEntry
    {
        public CharacterType character;
        [Tooltip("CharacterSlot 전체 오브젝트. 파티에 없으면 비활성화")]
        public GameObject root;
        public Button button;
        public GameObject baseObj;
        public GameObject selectedObj;
        public RectTransform rectTransform; // SelectedLine 이동 목표 좌표용
    }

    [Header("캐릭터 슬롯 (4개, 고정 배정)")]
    [SerializeField] private List<CharacterSlotEntry> _slots = new List<CharacterSlotEntry>();

    [Header("선택 라인 (단일 오브젝트, 부드럽게 이동)")]
    [SerializeField] private RectTransform _selectedLine;
    [SerializeField] private float _lineMoveDuration = 0.2f;
    [SerializeField] private Ease _lineMoveEase = Ease.OutQuad;

    [Header("레이아웃")]
    [Tooltip("HorizontalLayoutGroup이 붙어있는 CharacterSlotRoot의 RectTransform")]
    [SerializeField] private RectTransform _layoutRoot;

    private Tween _lineTween;
    private bool _hasInitialized;

    private void OnEnable()
    {
        RefreshSlotVisibility();

        // HorizontalLayoutGroup이 슬롯들의 activeSelf 변경을 반영해
        // anchoredPosition을 재계산하도록 강제. 이걸 안 하면 같은 프레임에
        // 위치를 읽을 때 stale한(레이아웃 갱신 전) 값을 가져와서 처음 열 때 위치가 튄다.
        if (_layoutRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutRoot);
        }

        foreach (var slot in _slots)
        {
            if (slot.button == null) continue;

            CharacterType character = slot.character;
            slot.button.onClick.AddListener(() => OnClickSlot(character));
        }

        _hasInitialized = false;

        if (CharacterSelectionManager.Instance != null)
        {
            UpdateSelectionVisual(CharacterSelectionManager.Instance.GetSelected(), animate: false);
            CharacterSelectionManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        }

        _hasInitialized = true;
    }

    private void OnDisable()
    {
        foreach (var slot in _slots)
        {
            if (slot.button != null)
            {
                slot.button.onClick.RemoveAllListeners();
            }
        }

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }

        _lineTween?.Kill();
    }

    private void RefreshSlotVisibility()
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

        foreach (var slot in _slots)
        {
            if (slot.root != null)
            {
                slot.root.SetActive(partyCharacters.Contains(slot.character));
            }
        }
    }

    private void HandleSelectionChanged(CharacterType character)
    {
        UpdateSelectionVisual(character, animate: true);
    }

    private void UpdateSelectionVisual(CharacterType selected, bool animate)
    {
        CharacterSlotEntry target = null;

        foreach (var slot in _slots)
        {
            bool isSelected = slot.character == selected;

            if (slot.baseObj != null) slot.baseObj.SetActive(!isSelected);
            if (slot.selectedObj != null) slot.selectedObj.SetActive(isSelected);

            if (isSelected)
            {
                target = slot;
            }
        }

        if (target == null || target.rectTransform == null || _selectedLine == null)
        {
            return;
        }

        MoveLineTo(target.rectTransform, animate && _hasInitialized);
    }

    private void MoveLineTo(RectTransform targetSlot, bool animate)
    {
        _lineTween?.Kill();

        Vector2 targetPos = _selectedLine.anchoredPosition;
        targetPos.x = targetSlot.anchoredPosition.x;

        if (animate)
        {
            _lineTween = _selectedLine
                .DOAnchorPosX(targetPos.x, _lineMoveDuration)
                .SetEase(_lineMoveEase);
        }
        else
        {
            _selectedLine.anchoredPosition = targetPos;
        }
    }

    private void OnClickSlot(CharacterType character)
    {
        CharacterSelectionManager.Instance?.SetSelected(character);
    }
}