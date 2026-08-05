using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개별 던전 버튼의 클릭 입력과 선택 연출을 담당합니다.
/// </summary>
public class DungeonButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button _button;
    [SerializeField] private DungeonData _dungeonData;

    [Header("Selection Visual")]
    [Tooltip("기본 상태에서 활성화되는 오브젝트")]
    [SerializeField] private GameObject _baseObject;

    [Tooltip("선택 상태에서 활성화되는 오브젝트")]
    [SerializeField] private GameObject _selectObject;

    [Tooltip("선택 시 확대할 대상. 비워두면 현재 오브젝트의 RectTransform을 사용합니다.")]
    [SerializeField] private RectTransform _scaleTarget;

    [Header("Tween")]
    [SerializeField] private float _selectedScale = 1.2f;
    [SerializeField] private float _scaleDuration = 0.2f;
    [SerializeField] private Ease _scaleEase = Ease.OutBack;

    public event Action<DungeonButton, DungeonData> OnDungeonSelected;

    public DungeonData DungeonData => _dungeonData;
    public bool IsSelected { get; private set; }

    private void Awake()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_scaleTarget == null)
        {
            _scaleTarget = transform as RectTransform;
        }

        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }

        // 초기 상태는 선택 해제 상태로 통일
        SetSelected(false, false);
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        if (_scaleTarget != null)
        {
            _scaleTarget.DOKill();
        }
    }

    private void HandleClick()
    {
        if (_dungeonData == null)
        {
            Debug.LogWarning(
                $"[DungeonButton] {gameObject.name}에 DungeonData가 할당되지 않았습니다.",
                this
            );

            return;
        }

        OnDungeonSelected?.Invoke(this, _dungeonData);
    }

    /// <summary>
    /// 버튼의 선택 여부와 선택 연출을 변경합니다.
    /// </summary>
    public void SetSelected(bool selected, bool useAnimation = true)
    {
        IsSelected = selected;

        if (_baseObject != null)
        {
            _baseObject.SetActive(!selected);
        }

        if (_selectObject != null)
        {
            _selectObject.SetActive(selected);
        }

        if (_scaleTarget == null)
        {
            return;
        }

        float targetScale = selected ? _selectedScale : 1f;

        _scaleTarget.DOKill();

        if (!useAnimation)
        {
            _scaleTarget.localScale = Vector3.one * targetScale;
            return;
        }

        _scaleTarget
            .DOScale(targetScale, _scaleDuration)
            .SetEase(_scaleEase)
            .SetUpdate(true);
    }

    /// <summary>
    /// 현재 버튼을 클릭한 것과 동일하게 선택 이벤트를 발생시킵니다.
    /// 자동 선택 시 사용합니다.
    /// </summary>
    public void Select()
    {
        HandleClick();
    }
}