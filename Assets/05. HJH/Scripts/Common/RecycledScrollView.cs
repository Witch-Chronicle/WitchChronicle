using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 세로 스크롤 전용 재사용(Recycled) 스크롤 뷰의 공통 베이스 클래스입니다.
///
/// - _columnCount == 1  -> 세로 한 줄 리스트 (예: Shop)
/// - _columnCount &gt;= 2 -> 그리드 (예: Inventory, SkillEquip)
///   두 경우 모두 내부적으로는 "행(row) x 열(column)" 계산으로 동일하게 처리됩니다.
///
/// 동작 방식:
/// - 화면(Viewport)에 실제로 보이는 셀 개수 + 버퍼만큼만 GameObject를 생성해 재사용합니다.
/// - Content의 sizeDelta는 "데이터가 전부 있었다면 필요했을 가상 크기"로만 설정하고,
///   실제 자식 오브젝트 개수는 항상 고정(풀 크기)입니다.
/// - ScrollRect.onValueChanged를 구독해서 스크롤 위치가 바뀔 때마다
///   보여야 할 첫 인덱스를 계산하고, 그 인덱스가 바뀐 경우에만 셀을 재배치/재바인딩합니다.
///
/// 사용법: 이 클래스를 직접 씬에 붙일 수는 없습니다(제네릭 컴포넌트는 Unity가 지원하지 않음).
/// 아래처럼 데이터/셀 타입을 지정한 얇은 서브클래스를 하나 만들어서 사용하세요.
///
/// public class InventoryScrollView : RecycledScrollView&lt;ItemData, InventoryCellView&gt; { }
/// </summary>
/// <typeparam name="TData">리스트에 들어가는 데이터 타입입니다.</typeparam>
/// <typeparam name="TCell">셀 프리팹에 붙는 컴포넌트 타입입니다. IRecycledScrollCell&lt;TData&gt;를 구현해야 합니다.</typeparam>
public abstract class RecycledScrollView<TData, TCell> : MonoBehaviour
    where TCell : Component, IRecycledScrollCell<TData>
{
    [Header("References")]
    [Tooltip("비워두면 이 오브젝트 또는 부모에서 자동으로 찾습니다.")]
    [SerializeField] protected ScrollRect _scrollRect;
    [Tooltip("실제 스크롤되는 Content RectTransform입니다. 비워두면 ScrollRect.content를 사용합니다.")]
    [SerializeField] protected RectTransform _content;
    [Tooltip("재사용될 셀 프리팹입니다. TCell 컴포넌트가 루트에 붙어있어야 합니다.")]
    [SerializeField] protected TCell _cellPrefab;

    [Header("Layout")]
    [Tooltip("한 줄에 몇 개씩 배치할지. 1이면 세로 리스트, 2 이상이면 그리드로 동작합니다.")]
    [SerializeField, Min(1)] protected int _columnCount = 1;
    [Tooltip("셀 하나의 크기(가로/세로)입니다.")]
    [SerializeField] protected Vector2 _cellSize = new Vector2(100f, 100f);
    [Tooltip("셀 사이의 가로/세로 간격입니다.")]
    [SerializeField] protected Vector2 _spacing = new Vector2(8f, 8f);
    [Tooltip("Content 안쪽 여백입니다.")]
    [SerializeField] protected RectOffset _padding = new RectOffset();
    [Tooltip("화면에 보이는 범위 위/아래로 추가 생성해둘 여유 행(row) 개수입니다. 빠르게 스크롤해도 빈 셀이 안 보이도록 여유를 둡니다.")]
    [SerializeField, Min(0)] protected int _bufferRows = 2;

    private readonly List<TData> _data = new List<TData>();
    private readonly List<TCell> _pool = new List<TCell>();
    private int _firstVisibleIndex = -1;
    private bool _isInitialized;

    protected virtual void Awake()
    {
        ResolveReferences();
    }

    protected virtual void OnEnable()
    {
        if (_scrollRect != null)
        {
            _scrollRect.onValueChanged.AddListener(HandleScrollChanged);
        }

        // 재활성화 시 레이아웃이 바뀌었을 수 있으므로 강제로 다시 계산합니다.
        if (_isInitialized)
        {
            RebuildLayout();
        }
    }

    protected virtual void OnDisable()
    {
        if (_scrollRect != null)
        {
            _scrollRect.onValueChanged.RemoveListener(HandleScrollChanged);
        }
    }

    private void ResolveReferences()
    {
        if (_scrollRect == null)
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        if (_scrollRect == null)
        {
            _scrollRect = GetComponentInParent<ScrollRect>();
        }

        if (_content == null && _scrollRect != null)
        {
            _content = _scrollRect.content;
        }

        if (_scrollRect == null)
        {
            Debug.LogError($"[{name}] ScrollRect를 찾을 수 없습니다.", this);
        }

        if (_content == null)
        {
            Debug.LogError($"[{name}] Content RectTransform이 연결되지 않았습니다.", this);
        }

        if (_cellPrefab == null)
        {
            Debug.LogError($"[{name}] Cell Prefab이 연결되지 않았습니다.", this);
        }
    }

    /// <summary>
    /// 전체 데이터를 교체하고 스크롤 뷰를 처음부터 다시 계산합니다.
    /// </summary>
    public void SetData(IReadOnlyList<TData> data)
    {
        _data.Clear();

        if (data != null)
        {
            _data.AddRange(data);
        }

        _isInitialized = true;
        RebuildLayout();
    }

    /// <summary>
    /// 데이터를 비우고 모든 셀을 숨깁니다.
    /// </summary>
    public void Clear()
    {
        SetData(null);
    }

    /// <summary>
    /// 현재 데이터 리스트의 특정 인덱스만 다시 그리고 싶을 때 사용합니다.
    /// (예: 아이템 하나의 수량만 바뀐 경우) 화면에 실제로 보이는 인덱스가 아니면 아무 동작도 하지 않습니다.
    /// </summary>
    public void RefreshIndex(int index)
    {
        if (index < 0 || index >= _data.Count)
        {
            return;
        }

        int slot = index - _firstVisibleIndex;

        if (slot < 0 || slot >= _pool.Count)
        {
            return;
        }

        _pool[slot].Bind(_data[index], index);
    }

    /// <summary>
    /// 데이터 개수 변화 없이 전체 셀만 다시 바인딩하고 싶을 때 사용합니다. (정렬 변경 등)
    /// </summary>
    public void RefreshVisible()
    {
        int cached = _firstVisibleIndex;
        _firstVisibleIndex = -1;
        UpdateVisibleCells(force: true);
        _firstVisibleIndex = cached;
    }

    private void RebuildLayout()
    {
        if (_content == null || _cellPrefab == null)
        {
            return;
        }

        _columnCount = Mathf.Max(1, _columnCount);

        int totalRows = Mathf.CeilToInt(_data.Count / (float)_columnCount);
        float contentHeight = _padding.top + _padding.bottom
            + totalRows * _cellSize.y
            + Mathf.Max(0, totalRows - 1) * _spacing.y;

        _content.sizeDelta = new Vector2(_content.sizeDelta.x, contentHeight);

        EnsurePoolSize();

        _firstVisibleIndex = -1;
        UpdateVisibleCells(force: true);
    }

    private void EnsurePoolSize()
    {
        float viewportHeight = GetViewportHeight();
        float rowStride = _cellSize.y + _spacing.y;

        int visibleRows = rowStride > 0f
            ? Mathf.CeilToInt(viewportHeight / rowStride) + 1
            : 1;

        int neededRows = visibleRows + _bufferRows;
        int neededPoolSize = Mathf.Max(_columnCount, neededRows * _columnCount);

        while (_pool.Count < neededPoolSize)
        {
            TCell cell = Instantiate(_cellPrefab, _content);
            RectTransform rectTransform = cell.transform as RectTransform;

            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.sizeDelta = _cellSize;
            }

            cell.gameObject.SetActive(false);
            _pool.Add(cell);
        }

        // 화면 크기가 작아져서 이미 있는 풀보다 필요한 개수가 줄어드는 경우는
        // 굳이 파괴하지 않습니다 (다시 커질 수도 있고, 여유분이 있어도 문제되지 않음).
    }

    private float GetViewportHeight()
    {
        if (_scrollRect != null && _scrollRect.viewport != null)
        {
            return _scrollRect.viewport.rect.height;
        }

        RectTransform parent = _content.parent as RectTransform;
        return parent != null ? parent.rect.height : 0f;
    }

    private void HandleScrollChanged(Vector2 _)
    {
        UpdateVisibleCells(force: false);
    }

    private void UpdateVisibleCells(bool force)
    {
        if (_pool.Count == 0)
        {
            return;
        }

        if (_data.Count == 0)
        {
            HideAllCells();
            return;
        }

        float rowStride = _cellSize.y + _spacing.y;
        float scrolledY = _content.anchoredPosition.y;

        int firstRow = rowStride > 0f
            ? Mathf.Max(0, Mathf.FloorToInt((scrolledY - _padding.top) / rowStride))
            : 0;

        int firstIndex = firstRow * _columnCount;

        if (!force && firstIndex == _firstVisibleIndex)
        {
            return;
        }

        _firstVisibleIndex = firstIndex;

        for (int slot = 0; slot < _pool.Count; slot++)
        {
            int dataIndex = firstIndex + slot;
            TCell cell = _pool[slot];

            if (dataIndex >= _data.Count)
            {
                cell.gameObject.SetActive(false);
                continue;
            }

            int row = dataIndex / _columnCount;
            int col = dataIndex % _columnCount;

            float x = _padding.left + col * (_cellSize.x + _spacing.x);
            float y = -(_padding.top + row * (_cellSize.y + _spacing.y));

            RectTransform rectTransform = cell.transform as RectTransform;

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(x, y);
            }

            if (cell.gameObject.activeSelf == false)
            {
                cell.gameObject.SetActive(true);
            }

            cell.Bind(_data[dataIndex], dataIndex);
        }
    }

    private void HideAllCells()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (_pool[i].gameObject.activeSelf)
            {
                _pool[i].gameObject.SetActive(false);
            }
        }
    }
}