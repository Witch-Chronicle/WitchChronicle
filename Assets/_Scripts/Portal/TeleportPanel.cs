using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 빠른 이동 목록 UI.
///
/// 버튼은 인스펙터에 미리 배치해두고, 각 버튼에 TeleportPointId를 지정해서
/// 씬의 TeleportDestination과 매칭한다. 해당 Id의 목적지가 씬에 없으면
/// (미보유/미해금 등) 그 버튼은 자동으로 비활성화된다.
///
/// 싱글톤(.Instance)으로 등록되어 PlayerUIInputReader가 Esc 입력 시
/// 인스펙터 연결 없이 직접 참조해서 닫을 수 있다.
/// </summary>
public class TeleportPanel : MonoBehaviour
{
    public static TeleportPanel Instance { get; private set; }

    [Serializable]
    private class TeleportButtonEntry
    {
        public TeleportPointId id;
        public Button button;
    }

    [Header("패널")]
    [SerializeField] private UIPanelAnimator _panelAnimator;

    [Header("고정 버튼 목록")]
    [Tooltip("미리 배치해둔 버튼과 이동할 목적지의 Id를 짝지어 등록한다.")]
    [SerializeField] private List<TeleportButtonEntry> _entries = new List<TeleportButtonEntry>();

    [Header("닫기")]
    [SerializeField] private Button _closeButton;

    private bool _isOpen;
    private bool _isBound;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_panelAnimator != null)
        {
            _panelAnimator.SetClosedImmediate();
            _panelAnimator.OnClosed += HandlePanelClosed;
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }

        BindButtons();
    }

    private void OnDestroy()
    {
        if (_panelAnimator != null)
        {
            _panelAnimator.OnClosed -= HandlePanelClosed;
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Close);
        }

        UnbindButtons();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void BindButtons()
    {
        if (_isBound)
        {
            return;
        }

        _isBound = true;

        for (int i = 0; i < _entries.Count; i++)
        {
            TeleportButtonEntry entry = _entries[i];

            if (entry == null || entry.button == null)
            {
                continue;
            }

            TeleportPointId capturedId = entry.id; // 클로저 캡처 주의
            entry.button.onClick.AddListener(() => Teleport(capturedId));
        }
    }

    private void UnbindButtons()
    {
        if (_isBound == false)
        {
            return;
        }

        _isBound = false;

        for (int i = 0; i < _entries.Count; i++)
        {
            TeleportButtonEntry entry = _entries[i];

            if (entry != null && entry.button != null)
            {
                entry.button.onClick.RemoveAllListeners();
            }
        }
    }

    /// <summary>
    /// 패널을 연다. 현재 씬에 존재하지 않는 목적지의 버튼은 비활성화한다.
    /// </summary>
    public void Open()
    {
        if (_isOpen)
        {
            return;
        }

        RefreshButtonAvailability();

        _isOpen = true;

        QuestListUI.Instance?.Close();
        MainHUDUIController.Instance?.Close();

        /*
         * 패널을 화면에 표시하기 전에 현재 월드 화면을 캡처해야
         * 패널 자체가 Blur 이미지에 포함되지 않습니다.
         */
        UIBackgroundBlurManager.Instance?.Show();

        if (_panelAnimator != null)
        {
            _panelAnimator.Open();
        }

        CursorLocker.Instance?.EnterUIMode();
    }

    public void Close()
    {
        if (_isOpen == false)
        {
            return;
        }

        _isOpen = false;

        QuestListUI.Instance?.Open();
        MainHUDUIController.Instance?.Open();

        if (_panelAnimator != null)
        {
            _panelAnimator.Close();
        }

        CursorLocker.Instance?.ExitUIMode();
    }

    /// <summary>
    /// 열림/닫힘을 토글한다. ShopNPC.ToggleShop() 등과 동일한 패턴.
    /// </summary>
    public void ToggleTeleportPanel()
    {
        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    private void HandlePanelClosed()
    {
        UIBackgroundBlurManager.Instance?.Hide();
    }

    private void RefreshButtonAvailability()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            TeleportButtonEntry entry = _entries[i];

            if (entry == null || entry.button == null)
            {
                continue;
            }

            bool exists = TeleportDestination.FindById(entry.id) != null;
            entry.button.interactable = exists;
        }
    }

    private void Teleport(TeleportPointId id)
    {
        TeleportDestination destination = TeleportDestination.FindById(id);

        if (destination == null)
        {
            Debug.LogWarning($"[TeleportPanel] 씬에서 목적지를 찾을 수 없습니다: {id}");
            return;
        }

        if (Party.Instance == null)
        {
            Debug.LogWarning("[TeleportPanel] Party가 없어 이동할 수 없습니다.");
            return;
        }

        Party.Instance.MoveTo(destination.Position, destination.Rotation);
        Close();
    }
}