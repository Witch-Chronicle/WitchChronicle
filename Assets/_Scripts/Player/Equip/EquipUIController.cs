using UnityEngine;
using DG.Tweening;

/// <summary>
/// IntergrationPanel/Equip 쪽 8개 슬롯을 "지금 선택된 캐릭터"의 장착 상태로 표시.
/// - CharacterSelectionManager가 가리키는 캐릭터가 바뀌면 자동으로 그 캐릭터의 CharacterEquipment로 다시 바인딩
/// 슬롯 클릭 시 ItemDetailPanel(InventoryDetailController)에 해당 장비 정보를 표시.
/// - 캐릭터 전환 시 Main이 좌측으로 슬라이드아웃 -> 내용 교체 -> 우측에서 슬라이드인.
///   (CanvasGroup은 자식 Button들의 레이캐스트/클릭을 막는 문제가 있어서 안 씀. 페이드 없이 이동만 함)
/// * 캐릭터 탭 버튼(누르면 CharacterSelectionManager.SetSelected 호출)이나 슬롯 UI 프리팹화는 별도 UI 작업에서 처리.
/// </summary>
public class EquipUIController : MonoBehaviour
{
    [Header("Item Detail")]
    [SerializeField] private InventoryDetailController _itemDetailController;

    [Header("캐릭터 전환 애니메이션")]
    [Tooltip("Main 오브젝트의 RectTransform (슬라이드 이동용)")]
    [SerializeField] private RectTransform _mainRect;
    [SerializeField] private float _switchSlideDuration = 0.2f;
    [Tooltip("좌우로 밀리는 거리(px). 0 이하로 두면 Main 자기 너비만큼 자동 계산")]
    [SerializeField] private float _slideDistance = 0f;

    private float _mainOriginalX;

    [Header("EquipLeftSlots")]
    [SerializeField] private EquipSlotView _weaponSlot;
    [SerializeField] private EquipSlotView _necklaceSlot;
    [SerializeField] private EquipSlotView _earringSlot;
    [SerializeField] private EquipSlotView _ringSlot;

    [Header("EquipRightSlots")]
    [SerializeField] private EquipSlotView _robeSlot;
    [SerializeField] private EquipSlotView _cloakSlot;
    [SerializeField] private EquipSlotView _gloveSlot;
    [SerializeField] private EquipSlotView _shoeSlot;

    // 지금 이 화면이 보여주고 있는 캐릭터의 CharacterEquipment (선택 캐릭터 바뀌면 다시 바인딩됨)
    private CharacterEquipment _boundEquipment;

    private void Awake()
    {
        if (_mainRect != null)
        {
            _mainOriginalX = _mainRect.anchoredPosition.x;

            if (_slideDistance <= 0f)
            {
                _slideDistance = _mainRect.rect.width;
            }
        }
    }

    private void OnEnable()
    {
        // 패널이 새로 열릴 때는 애니메이션 없이 즉시 원래 위치로
        if (_mainRect != null)
        {
            _mainRect.DOKill();
            _mainRect.anchoredPosition = new Vector2(_mainOriginalX, _mainRect.anchoredPosition.y);
        }

        BindToSelectedCharacter();

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged += HandleCharacterSelectionChanged;
        }
    }

    private void OnDisable()
    {
        if (_boundEquipment != null)
        {
            _boundEquipment.OnEquipmentChanged -= RefreshAllSlots;
            _boundEquipment = null;
        }

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged -= HandleCharacterSelectionChanged;
        }
    }

    /// <summary>
    /// 캐릭터 탭이 바뀌었을 때 호출.
    /// Main이 좌측으로 슬라이드아웃 -> 내용 교체 -> 우측에서 원래 자리로 슬라이드인. (페이드 없음)
    /// </summary>
    private void HandleCharacterSelectionChanged(CharacterType character)
    {
        if (_mainRect == null)
        {
            BindToSelectedCharacter();
            return;
        }

        _mainRect.DOKill();

        float outX = _mainOriginalX - _slideDistance;   // 좌측으로 나가는 위치
        float inStartX = _mainOriginalX + _slideDistance; // 우측에서 등장 시작 위치

        Sequence sequence = DOTween.Sequence();

        // 1. 좌측으로 슬라이드아웃
        sequence.Append(_mainRect.DOAnchorPosX(outX, _switchSlideDuration).SetEase(Ease.InQuad));

        // 2. 안 보이는 타이밍에 내용 교체 + 우측 시작 위치로 순간이동
        sequence.AppendCallback(() =>
        {
            BindToSelectedCharacter();
            _mainRect.anchoredPosition = new Vector2(inStartX, _mainRect.anchoredPosition.y);
        });

        // 3. 우측에서 원래 자리로 슬라이드인
        sequence.Append(_mainRect.DOAnchorPosX(_mainOriginalX, _switchSlideDuration).SetEase(Ease.OutQuad));
    }

    /// <summary>
    /// CharacterSelectionManager가 가리키는 현재 캐릭터의 CharacterEquipment로 (재)구독.
    /// </summary>
    private void BindToSelectedCharacter()
    {
        if (_boundEquipment != null)
        {
            _boundEquipment.OnEquipmentChanged -= RefreshAllSlots;
        }

        _boundEquipment = null;

        if (CharacterSelectionManager.Instance != null && PersistentCharacterManager.Instance != null)
        {
            CharacterType selected = CharacterSelectionManager.Instance.GetSelected();
            string characterId = selected.ToString();

            if (PersistentCharacterManager.Instance.TryGetCharacter(characterId, out PersistentCharacterUnit unit))
            {
                _boundEquipment = unit.CharacterEquipment;
            }
        }

        if (_boundEquipment != null)
        {
            _boundEquipment.OnEquipmentChanged += RefreshAllSlots;
        }

        RefreshAllSlots();
    }

    private void RefreshAllSlots()
    {
        if (_boundEquipment == null) return;

        SetSlot(_weaponSlot, EquipSlotType.Weapon);
        SetSlot(_necklaceSlot, EquipSlotType.Necklace);
        SetSlot(_earringSlot, EquipSlotType.Earring);
        SetSlot(_ringSlot, EquipSlotType.Ring);

        SetSlot(_robeSlot, EquipSlotType.Robe);
        SetSlot(_cloakSlot, EquipSlotType.Cloak);
        SetSlot(_gloveSlot, EquipSlotType.Gloves);
        SetSlot(_shoeSlot, EquipSlotType.Shoes);
    }

    private void SetSlot(EquipSlotView slotView, EquipSlotType slotType)
    {
        if (slotView == null || _boundEquipment == null) return;

        EquipmentInstance equipped = _boundEquipment.GetEquipped(slotType);
        slotView.Setup(equipped, HandleSlotClicked);
    }

    private void HandleSlotClicked(EquipmentInstance equipmentInstance)
    {
        if (_itemDetailController != null)
        {
            _itemDetailController.Show(equipmentInstance);
        }
    }
}