using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 테스트용 상점 NPC.
/// - Shop 오브젝트에 붙여서 사용
/// - F1 키를 누르면 ShopPanel이 토글(열림/닫힘) 됨
/// - 판매할 아이템들을 타입별로 인스펙터에서 등록 (Equip / Consumable / Material)
/// * KeyItem은 상점에서 판매하지 않으므로 리스트에서 제외
/// * 실제 구매/판매 로직은 ShopController에서 별도로 관리
/// </summary>
public class ShopNPC : MonoBehaviour
{
    // 추가
    // 대화창을 통해, ShopNpc 에 접근해서 특정 함수를 호출하기위한
    public static ShopNPC Instance { get; private set; }

    [Header("Shop UI")]
    [SerializeField] private UIPanelAnimator _shopPanelAnimator; // 하이어라키의 ShopPanel 오브젝트 연결 (UIPanelAnimator 컴포넌트)

    [Header("For Sale - Equip")]
    [SerializeField] private List<WeaponItemData> _weaponItemsForSale;
    [SerializeField] private List<ArmorItemData> _armorItemsForSale;
    [SerializeField] private List<AccessoryItemData> _accessoryItemsForSale;

    [Header("For Sale - Consumable")]
    [SerializeField] private List<ConsumableItemData> _consumableItemsForSale;

    [Header("For Sale - Material")]
    [SerializeField] private List<MaterialItemData> _materialItemsForSale;

    [Header("For Sale - Seed")]
    [SerializeField] private List<SeedItemData> _seedItemsForSale;

    private bool _isShopOpen;

    // 추가
    // 다른 클래스 에서 _isShopOpen 의 상태를 가져오기 위한 프로터티
    public bool IsOpen => _isShopOpen;

    // ShopUIController가 아이템 목록을 읽어갈 수 있도록 읽기 전용으로 공개
    public IReadOnlyList<WeaponItemData> WeaponItems => _weaponItemsForSale;
    public IReadOnlyList<ArmorItemData> ArmorItems => _armorItemsForSale;
    public IReadOnlyList<AccessoryItemData> AccessoryItems => _accessoryItemsForSale;
    public IReadOnlyList<ConsumableItemData> ConsumableItems => _consumableItemsForSale;
    public IReadOnlyList<MaterialItemData> MaterialItems => _materialItemsForSale;
    public IReadOnlyList<SeedItemData> SeedItems => _seedItemsForSale;

    private void Awake()
    {
        // 추가
        // 인스턴스 값
        if (Instance != null)
        {
            Destroy(gameObject);

            return;
        }


        Instance = this;

        FindShopPanel();

        // 시작할 때는 상점 닫힌 상태로 초기화 (애니메이션 없이 즉시)
        _isShopOpen = false;

        if (_shopPanelAnimator != null)
        {
            _shopPanelAnimator.SetClosedImmediate();
        }
    }

    /// <summary>
    /// 씬에 생성된 ShopPanel 탐색, 던전에서 프리팹 생성되고 나서, 익스펙터 직접 할당 X, 런타임 중 가져오기 위한 함수
    /// </summary>
    private void FindShopPanel() // 추가
    {
        if (_shopPanelAnimator != null)
        {
            return;
        }

        ShopUIController shopUIController = FindFirstObjectByType<ShopUIController>(FindObjectsInactive.Include);

        if (shopUIController == null)
        {
            Debug.LogWarning("[ShopNPC] ShopUIController를 찾지 못했습니다.");

            return;
        }
        
        _shopPanelAnimator = shopUIController.GetComponent<UIPanelAnimator>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            ToggleShop();
        }
    }

    public void ToggleShop()
    {
        _isShopOpen = !_isShopOpen;

        if (_shopPanelAnimator != null)
        {
            if (_isShopOpen)
            {
                _shopPanelAnimator.Open();
                // 추가
                // 이 패널떄문에 상점 UI X 버튼이 안클릭됨, 패널을 끄는 함수
                QuestListUI.Instance.Close();
                // 추가, 마우스 커서 락
                CursorLocker.Instance.EnterUIMode();
            }
            else
            {
                _shopPanelAnimator.Close();
                // 추가
                // 패널을 키는 함수
                QuestListUI.Instance.Open();
                // 추가
                CursorLocker.Instance.ExitUIMode();
            }
        }
        else
        {
            Debug.LogWarning("[ShopNPC] shopPanelAnimator가 연결되지 않았습니다.");
        }
    }
}