using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Battle.Rules;

/// <summary>
/// 아이템 리스트 패널 전담. 슬라이드 애니메이션, Btns 숨김/표시, 아이템 데이터 채우기까지 전부 자체 처리.
/// - ItemBtn 클릭 시 우측에서 슬라이드 인 + Btns 숨김
/// - CloseBtn 클릭 시 슬라이드 아웃 + Btns 표시
/// * TODO(임시): BattleItemExecutor/StatusEffectController는 아직 BattleCycleController와 통합 전이라
///   이 컨트롤러가 임시로 자체 소유. 나중에 BattleUIContext를 통해 진짜 인스턴스를 받아오는 방식으로 교체 필요.
///   그 전까지는 상태이상 해제 포션이 실질적으로 동작하지 않음(연동된 상태이상 시스템이 없어서).
/// </summary>
public class ItemListController : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private Button _itemBtn;

    [Header("Panel")]
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Button _closeBtn;
    [SerializeField] private float _duration = 0.25f;
    [SerializeField] private Ease _openEase = Ease.OutQuad;
    [SerializeField] private Ease _closeEase = Ease.InQuad;

    [Header("Content")]
    [SerializeField] private Transform _contentParent;
    [SerializeField] private BattleItemView _itemPrefab;

    [Header("Action Bar (열고 닫힐 때 같이 반응)")]
    [SerializeField] private BattleActionBarController _actionBar;

    private readonly List<BattleItemView> _spawnedItems = new List<BattleItemView>();
    public bool IsOpen { get; private set; }

    private float _visiblePosX;
    private float _hiddenPosX;
    private bool _isInitialized;

    // TODO(임시): 진짜 연결 전까지 이 컨트롤러가 자체 소유
    private StatusEffectController _tempStatusEffectController;
    private BattleItemExecutor _tempItemExecutor;

    private void Awake()
    {
        if (_itemBtn != null) _itemBtn.onClick.AddListener(Open);
        if (_closeBtn != null) _closeBtn.onClick.AddListener(Close);

        // TODO(임시): BattleCycleController와 통합되면 이 두 줄 제거
        _tempStatusEffectController = new StatusEffectController();
        _tempItemExecutor = new BattleItemExecutor(_tempStatusEffectController);
    }

    private void Start()
    {
        EnsureInitialized();

        IsOpen = false;
        SetPosXImmediate(_hiddenPosX);
        gameObject.SetActive(false);
    }

    private void EnsureInitialized()
    {
        if (_isInitialized || _rectTransform == null) return;

        _visiblePosX = _rectTransform.anchoredPosition.x;
        _hiddenPosX = _visiblePosX + _rectTransform.rect.width;
        _isInitialized = true;
    }

    private void Open()
    {
        Debug.Log("[ItemListController] Open 호출됨");
        if (_rectTransform == null) return;

        EnsureInitialized();
        RefreshItemList();

        IsOpen = true;
        gameObject.SetActive(true);
        _rectTransform.DOKill();
        SetPosXImmediate(_hiddenPosX);

        _rectTransform.DOAnchorPosX(_visiblePosX, _duration).SetEase(_openEase);

        if (_actionBar != null) _actionBar.Hide();
    }

    private void Close()
    {
        if (_rectTransform == null) return;

        IsOpen = false;
        _rectTransform.DOKill();

        _rectTransform.DOAnchorPosX(_hiddenPosX, _duration).SetEase(_closeEase)
            .OnComplete(() => gameObject.SetActive(false));

        if (_actionBar != null) _actionBar.Show();
    }

    private void SetPosXImmediate(float posX)
    {
        _rectTransform.anchoredPosition = new Vector2(posX, _rectTransform.anchoredPosition.y);
    }

    /// <summary>
    /// PlayerInventory의 포션 아이템(PotionItemData)만 골라서 Content에 채움.
    /// </summary>
    private void RefreshItemList()
    {
        ClearSpawnedItems();

        if (PlayerInventory.Instance == null || _contentParent == null || _itemPrefab == null) return;

        foreach (var slot in PlayerInventory.Instance.InventorySlots)
        {
            if (slot.ItemData is not PotionItemData potion) continue;

            BattleItemView view = Instantiate(_itemPrefab, _contentParent);
            view.Bind(potion, slot.Quantity, HandleItemClicked);
            _spawnedItems.Add(view);
        }
    }

    private void ClearSpawnedItems()
    {
        foreach (var item in _spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }

    /// <summary>
    /// 포션 클릭 시 사용. 대상은 항상 자신(현재 턴 유닛). 사용 성공 시 전투 액션으로 제출해서 턴을 소모.
    /// </summary>
    private void HandleItemClicked(ItemData itemData)
    {
        if (itemData is not PotionItemData potionData)
        {
            Debug.LogWarning($"[ItemListController] 포션이 아닌 아이템 클릭됨: {itemData.itemName}");
            return;
        }

        if (BattleUIContext.Instance == null)
        {
            Debug.LogWarning("[ItemListController] BattleUIContext.Instance가 없습니다.");
            return;
        }

        BattleUnit currentUnit = BattleUIContext.Instance.CurrentUnit;

        if (currentUnit == null)
        {
            Debug.LogWarning("[ItemListController] 현재 턴 유닛이 없습니다.");
            return;
        }

        BattleItemResult result = _tempItemExecutor.UsePotion(currentUnit, potionData);

        if (result.Success == false)
        {
            Debug.Log($"[ItemListController] {potionData.itemName} 사용 실패");
            return;
        }

        Debug.Log($"[ItemListController] {currentUnit.UnitName}: {potionData.itemName} 사용 성공");

        BattleActionRequest actionRequest = BattleActionRequest.CreateUsingItem(currentUnit);
        BattleUIContext.Instance.SubmitAction(actionRequest);

        RefreshItemList();
        Close();
    }
}