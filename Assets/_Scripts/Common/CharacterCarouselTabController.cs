using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// CharacterSelectBtns에 부착. 캐릭터 4명의 탭(Characters_1~4)을 관리.
/// - 각 Characters_N: NotSelectedBtn(평소 보이는 버튼) + Selected(선택 시 나타나는 아이콘) + NotSelectedZone/SelectedZone(위치 기준점)
/// - NotSelectedBtn 클릭 -> CharacterSelectionManager.SetSelected() 호출
/// - 선택되면: Selected가 NotSelectedZone 위치에서 SelectedZone 위치로 슬라이드+페이드인, NotSelectedBtn은 페이드아웃
/// - 다른 캐릭터로 바뀌면: 이전 캐릭터의 Selected가 SelectedZone->NotSelectedZone으로 슬라이드+페이드아웃 되는 것과
///   동시에(크로스페이드) 그 캐릭터의 NotSelectedBtn이 페이드인으로 복귀
/// - 패널이 열릴 때 기본 선택 캐릭터는 애니메이션 없이 즉시 Selected가 SelectedZone에 위치한 상태로 시작
/// * CharacterTabController(배경/텍스트 색상 강조 방식)와는 별개의 새 UI 패턴.
/// </summary>
public class CharacterCarouselTabController : MonoBehaviour
{
    [System.Serializable]
    private class CharacterSlot
    {
        [Tooltip("Characters_N 전체 오브젝트 (파티에 없는 캐릭터면 이 자체를 비활성화)")]
        public GameObject root;

        public CharacterType character;

        [Tooltip("평소 보이는 선택 버튼")]
        public Button notSelectedBtn;
        public CanvasGroup notSelectedCanvasGroup;

        [Tooltip("선택됐을 때 나타나는 아이콘(캐릭터 마스크 등)")]
        public RectTransform selected;
        public CanvasGroup selectedCanvasGroup;

        [Tooltip("Selected가 이동할 목표 위치 기준점 (선택 시)")]
        public RectTransform selectedZone;
        [Tooltip("Selected가 돌아갈 위치 기준점 (미선택 시). NotSelectedBtn과 같은 위치)")]
        public RectTransform notSelectedZone;
    }

    [Header("캐릭터 슬롯 (4개)")]
    [SerializeField] private List<CharacterSlot> _slots = new List<CharacterSlot>();

    [Header("애니메이션")]
    [SerializeField] private float _slideDuration = 0.25f;
    [SerializeField] private Ease _slideEase = Ease.OutQuad;

    private CharacterType _currentSelected;
    private bool _hasInitialized;

    private void OnEnable()
    {
        RefreshSlotVisibility();

        foreach (var slot in _slots)
        {
            if (slot.notSelectedBtn == null) continue;

            CharacterType character = slot.character;
            slot.notSelectedBtn.onClick.AddListener(() => OnClickSlot(character));
        }

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        }

        _hasInitialized = false;

        if (CharacterSelectionManager.Instance != null)
        {
            ApplySelectionImmediate(CharacterSelectionManager.Instance.GetSelected());
        }

        _hasInitialized = true;
    }

    private void OnDisable()
    {
        foreach (var slot in _slots)
        {
            if (slot.notSelectedBtn != null)
            {
                slot.notSelectedBtn.onClick.RemoveAllListeners();
            }
        }

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }
    }

    private void OnClickSlot(CharacterType character)
    {
        CharacterSelectionManager.Instance?.SetSelected(character);
    }

    private void HandleSelectionChanged(CharacterType character)
    {
        ApplySelectionAnimated(character);
    }

    /// <summary>
    /// 패널이 열리는 시점 등, 애니메이션 없이 즉시 선택 상태를 세팅.
    /// </summary>
    private void ApplySelectionImmediate(CharacterType selected)
    {
        _currentSelected = selected;

        foreach (var slot in _slots)
        {
            bool isSelected = slot.character == selected;

            KillSlotTweens(slot);

            if (isSelected)
            {
                SetImmediate(slot.notSelectedCanvasGroup, 0f, interactable: false);
                if (slot.notSelectedBtn != null) slot.notSelectedBtn.gameObject.SetActive(false);

                if (slot.selected != null)
                {
                    slot.selected.gameObject.SetActive(true);
                    if (slot.selectedZone != null) slot.selected.anchoredPosition = slot.selectedZone.anchoredPosition;
                }
                SetImmediate(slot.selectedCanvasGroup, 1f, interactable: false);
            }
            else
            {
                if (slot.notSelectedBtn != null) slot.notSelectedBtn.gameObject.SetActive(true);
                SetImmediate(slot.notSelectedCanvasGroup, 1f, interactable: true);

                if (slot.selected != null)
                {
                    if (slot.notSelectedZone != null) slot.selected.anchoredPosition = slot.notSelectedZone.anchoredPosition;
                }
                SetImmediate(slot.selectedCanvasGroup, 0f, interactable: false);
                if (slot.selected != null) slot.selected.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 캐릭터 선택이 바뀔 때, 이전 선택/새 선택 슬롯 각각에 애니메이션 적용.
    /// </summary>
    private void ApplySelectionAnimated(CharacterType selected)
    {
        if (!_hasInitialized || selected == _currentSelected)
        {
            return;
        }

        CharacterType previous = _currentSelected;
        _currentSelected = selected;

        foreach (var slot in _slots)
        {
            if (slot.character == selected)
            {
                AnimateIntoSelected(slot);
            }
            else if (slot.character == previous)
            {
                AnimateOutOfSelected(slot);
            }
        }
    }

    /// <summary>
    /// 이 슬롯을 "선택됨" 상태로: NotSelectedBtn 페이드아웃, Selected가 NotSelectedZone -> SelectedZone으로 슬라이드+페이드인.
    /// </summary>
    private void AnimateIntoSelected(CharacterSlot slot)
    {
        KillSlotTweens(slot);

        // NotSelectedBtn 페이드아웃
        if (slot.notSelectedCanvasGroup != null)
        {
            slot.notSelectedCanvasGroup.interactable = false;
            slot.notSelectedCanvasGroup.blocksRaycasts = false;

            slot.notSelectedCanvasGroup
                .DOFade(0f, _slideDuration)
                .SetEase(_slideEase)
                .OnComplete(() =>
                {
                    if (slot.notSelectedBtn != null) slot.notSelectedBtn.gameObject.SetActive(false);
                });
        }

        // Selected 슬라이드인 + 페이드인
        if (slot.selected != null)
        {
            slot.selected.gameObject.SetActive(true);

            if (slot.notSelectedZone != null)
            {
                slot.selected.anchoredPosition = slot.notSelectedZone.anchoredPosition;
            }

            if (slot.selectedZone != null)
            {
                slot.selected
                    .DOAnchorPos(slot.selectedZone.anchoredPosition, _slideDuration)
                    .SetEase(_slideEase);
            }
        }

        if (slot.selectedCanvasGroup != null)
        {
            slot.selectedCanvasGroup.alpha = 0f;
            slot.selectedCanvasGroup
                .DOFade(1f, _slideDuration)
                .SetEase(_slideEase);
        }
    }

    /// <summary>
    /// 이 슬롯을 "미선택" 상태로: Selected가 SelectedZone -> NotSelectedZone으로 슬라이드+페이드아웃,
    /// 동시에(크로스페이드) NotSelectedBtn이 페이드인.
    /// </summary>
    private void AnimateOutOfSelected(CharacterSlot slot)
    {
        KillSlotTweens(slot);

        // Selected 슬라이드아웃 + 페이드아웃
        if (slot.selected != null && slot.notSelectedZone != null)
        {
            slot.selected
                .DOAnchorPos(slot.notSelectedZone.anchoredPosition, _slideDuration)
                .SetEase(_slideEase);
        }

        if (slot.selectedCanvasGroup != null)
        {
            slot.selectedCanvasGroup
                .DOFade(0f, _slideDuration)
                .SetEase(_slideEase)
                .OnComplete(() =>
                {
                    if (slot.selected != null) slot.selected.gameObject.SetActive(false);
                });
        }

        // NotSelectedBtn 페이드인 (동시 시작, 크로스페이드)
        if (slot.notSelectedBtn != null) slot.notSelectedBtn.gameObject.SetActive(true);

        if (slot.notSelectedCanvasGroup != null)
        {
            slot.notSelectedCanvasGroup.alpha = 0f;
            slot.notSelectedCanvasGroup
                .DOFade(1f, _slideDuration)
                .SetEase(_slideEase)
                .OnComplete(() =>
                {
                    slot.notSelectedCanvasGroup.interactable = true;
                    slot.notSelectedCanvasGroup.blocksRaycasts = true;
                });
        }
    }

    private void KillSlotTweens(CharacterSlot slot)
    {
        slot.notSelectedCanvasGroup?.DOKill();
        slot.selectedCanvasGroup?.DOKill();
        slot.selected?.DOKill();
    }

    private void SetImmediate(CanvasGroup canvasGroup, float alpha, bool interactable)
    {
        if (canvasGroup == null) return;

        canvasGroup.DOKill();
        canvasGroup.alpha = alpha;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    /// <summary>
    /// PersistentCharacterManager의 ActiveParty에 실제로 있는 캐릭터의 슬롯(Characters_N)만 활성화.
    /// </summary>
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
}