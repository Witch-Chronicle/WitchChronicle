using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 테스트용 상점 NPC.
/// - Shop 오브젝트에 붙여서 사용
/// - F1 키를 누르면 ShopPanel이 토글(열림/닫힘) 됨
/// - 판매할 아이템은 타입 구분 없이 List<ItemData> 하나에 등록 (다형성으로 실제 SO 에셋 삽입).
///   분류(Main/Sub 카테고리)는 각 ItemData 자체의 mainCategory/subCategory 필드로 자동 결정됨.
/// * KeyItem은 이번 카테고리 체계에서 제외되므로 등록해도 상점 UI에는 노출되지 않음.
/// * 실제 구매/판매 로직은 ShopController에서 별도로 관리
/// </summary>
public class ShopNPC : MonoBehaviour
{
    public static ShopNPC Instance { get; private set; }

    [Header("Shop UI")]
    [SerializeField] private UIPanelAnimator _shopPanelAnimator;

    [Header("For Sale")]
    [SerializeField] private List<ItemData> _sellItems;

    private bool _isShopOpen;

    public bool IsOpen => _isShopOpen;

    /// <summary>
    /// ShopUIController가 판매 목록을 읽어갈 수 있도록 읽기 전용으로 공개.
    /// </summary>
    public IReadOnlyList<ItemData> SellItems => _sellItems;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        FindShopPanel();

        _isShopOpen = false;

        if (_shopPanelAnimator != null)
        {
            _shopPanelAnimator.SetClosedImmediate();
            _shopPanelAnimator.OnClosed += HandleShopPanelClosed;
        }
    }

    /// <summary>
    /// 씬에 생성된 ShopPanel 탐색, 던전에서 프리팹 생성되고 나서, 익스펙터 직접 할당 X, 런타임 중 가져오기 위한 함수
    /// </summary>
    private void FindShopPanel()
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
                // 대화 도중 상점이 열리는 경우, 대화창 패널만 끔 (CursorLocker 상태는 안 건드림)
                if (DialogueUI.Instance != null)
                {
                    DialogueUI.Instance.HidePanelOnly();
                }

                // 패널을 화면에 표시하기 전에 배경 Blur부터 요청 (캡처 시 패널 자체가 안 찍히도록)
                UIBackgroundBlurManager.Instance?.Show();

                _shopPanelAnimator.Open();
                QuestListUI.Instance.Close();
                CursorLocker.Instance.EnterUIMode();
            }
            else
            {
                _shopPanelAnimator.Close();
                QuestListUI.Instance.Open();
                CursorLocker.Instance.ExitUIMode();

                // Blur는 여기서 바로 안 끔 - Close 애니메이션이 끝난 뒤 HandleShopPanelClosed()에서 해제
            }
        }
        else
        {
            Debug.LogWarning("[ShopNPC] shopPanelAnimator가 연결되지 않았습니다.");
        }
    }

    private void HandleShopPanelClosed()
    {
        UIBackgroundBlurManager.Instance?.Hide();
    }


    private void OnDestroy()
    {
        if (_shopPanelAnimator != null)
        {
            _shopPanelAnimator.OnClosed -= HandleShopPanelClosed;
        }
    }
}