using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 빠른 이동 목록 UI.
///
/// 열릴 때 씬에 있는 TeleportDestination을 훑어 버튼을 만들고,
/// 버튼을 누르면 파티 전원을 그 위치로 옮긴 뒤 닫힌다.
/// </summary>
public class TeleportPanel : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private UIPanelAnimator _panelAnimator;

    [Header("목록")]
    [Tooltip("목적지 하나를 표시할 버튼 프리팹. 하위에 TMP_Text가 있어야 이름이 표시된다.")]
    [SerializeField] private Button _entryPrefab;

    [Tooltip("버튼이 생성될 부모 (Scroll View의 Content 등).")]
    [SerializeField] private Transform _entryRoot;

    [Header("닫기")]
    [SerializeField] private Button _closeButton;

    private readonly List<Button> _spawned = new List<Button>();
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (_panelAnimator != null)
        {
            _panelAnimator.SetClosedImmediate();
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Close);
        }
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Close);
        }
    }

    /// <summary>
    /// 목록을 새로 만들고 패널을 연다.
    /// </summary>
    public void Open()
    {
        if (_isOpen)
        {
            return;
        }

        Rebuild();

        _isOpen = true;

        if (_panelAnimator != null)
        {
            _panelAnimator.Open();
        }

        CursorLocker.Instance?.EnterUIMode();
    }

    /// <summary>
    /// 패널을 닫는다.
    /// </summary>
    public void Close()
    {
        if (_isOpen == false)
        {
            return;
        }

        _isOpen = false;

        if (_panelAnimator != null)
        {
            _panelAnimator.Close();
        }

        CursorLocker.Instance?.ExitUIMode();
    }

    /// <summary>
    /// 현재 씬의 목적지들로 버튼 목록을 다시 만든다.
    /// </summary>
    private void Rebuild()
    {
        ClearEntries();

        if (_entryPrefab == null || _entryRoot == null)
        {
            Debug.LogWarning("[TeleportPanel] 버튼 프리팹 또는 생성 위치가 연결되지 않았습니다.");
            return;
        }

        IReadOnlyList<TeleportDestination> destinations = TeleportDestination.All;

        for (int i = 0; i < destinations.Count; i++)
        {
            TeleportDestination destination = destinations[i];

            if (destination == null)
            {
                continue;
            }

            Button entry = Instantiate(_entryPrefab, _entryRoot);
            entry.gameObject.SetActive(true);

            TMP_Text label = entry.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = destination.DisplayName;
            }

            // 루프 변수를 그대로 캡처하면 마지막 값이 잡히므로 지역 변수에 담아 전달
            TeleportDestination captured = destination;
            entry.onClick.AddListener(() => Teleport(captured));

            _spawned.Add(entry);
        }
    }

    /// <summary>
    /// 생성해둔 버튼들을 제거한다.
    /// </summary>
    private void ClearEntries()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
            {
                Destroy(_spawned[i].gameObject);
            }
        }

        _spawned.Clear();
    }

    /// <summary>
    /// 파티 전원을 목적지로 옮기고 패널을 닫는다.
    /// </summary>
    /// <param name="destination">이동할 목적지</param>
    private void Teleport(TeleportDestination destination)
    {
        if (destination == null)
        {
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
