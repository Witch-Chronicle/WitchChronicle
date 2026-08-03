using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// CharacterSelectBtns(Prev/Current/Next 캐러셀 형태)를 관리.
/// - 활성 파티 순서(PersistentCharacterManager.GetActivePartyMembers)를 그대로 순환 리스트로 사용.
/// - 열릴 때 CharacterSelectionManager를 기본 캐릭터로 리셋해서 Current가 항상 기본값에서 시작.
/// - Prev/Next 클릭 시 그 방향의 캐릭터를 CharacterSelectionManager.SetSelected()로 선택 -> 순환.
/// - 인원 1명: Prev/Next 전부 비활성. 인원 2명: wrap하면 둘이 같은 대상을 가리키므로 한쪽만 활성화.
///   인원 3명 이상: Prev/Next 둘 다 항상 순환 활성화.
/// - PrevImg/NextImg는 캐릭터별로 바뀌지 않는 고정 이미지 (텍스트만 갱신됨).
/// - 연출: Prev/Next 버튼 클릭 시 펀치 스케일 피드백 + Current 이름 텍스트가 눌린 방향의
///   반대쪽에서 슬라이드+페이드인하며 전환됨. 최초 진입 시에는 애니메이션 없이 즉시 표시.
/// * CharacterTabController와는 별개(그쪽은 그대로 유지, 이 컴포넌트가 새로 추가되는 화면 전용).
/// </summary>
public class CharacterCarouselController : MonoBehaviour
{
    [Header("Prev")]
    [SerializeField] private Button _prevBtn;
    [SerializeField] private Image _prevImg;
    [SerializeField] private TextMeshProUGUI _prevNameTxt;

    [Header("Current")]
    [SerializeField] private RectTransform _currentNameRect;
    [SerializeField] private CanvasGroup _currentNameCanvasGroup;
    [SerializeField] private TextMeshProUGUI _currentNameTxt;

    [Header("Next")]
    [SerializeField] private Button _nextBtn;
    [SerializeField] private Image _nextImg;
    [SerializeField] private TextMeshProUGUI _nextNameTxt;

    [Header("클릭 피드백 (Prev/Next 버튼)")]
    [SerializeField] private float _btnPunchScale = 0.15f;
    [SerializeField] private float _btnPunchDuration = 0.2f;

    [Header("Current 전환 연출")]
    [SerializeField] private float _currentSlideDistance = 20f;
    [SerializeField] private float _currentSlideDuration = 0.2f;

    private readonly List<PersistentCharacterUnit> _activeParty = new List<PersistentCharacterUnit>();
    private int _currentIndex;
    private int _pendingDirection;
    private Vector2 _currentNameOriginalPos;

    private void OnEnable()
    {
        if (_prevBtn != null) _prevBtn.onClick.AddListener(OnClickPrev);
        if (_nextBtn != null) _nextBtn.onClick.AddListener(OnClickNext);

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        }

        if (_currentNameRect != null)
        {
            _currentNameOriginalPos = _currentNameRect.anchoredPosition;
        }

        RefreshPartyOrder();

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.ResetToDefault();
        }

        RefreshDisplay(direction: 0); // 최초 진입은 애니메이션 없이 즉시 표시
    }

    private void OnDisable()
    {
        if (_prevBtn != null) _prevBtn.onClick.RemoveListener(OnClickPrev);
        if (_nextBtn != null) _nextBtn.onClick.RemoveListener(OnClickNext);

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        }
    }

    private void RefreshPartyOrder()
    {
        _activeParty.Clear();

        if (PersistentCharacterManager.Instance != null)
        {
            PersistentCharacterManager.Instance.GetActivePartyMembers(_activeParty);
        }
    }

    private void HandleSelectionChanged(CharacterType character)
    {
        RefreshDisplay(_pendingDirection);
        _pendingDirection = 0;
    }

    private void OnClickPrev()
    {
        if (_activeParty.Count == 0) return;

        PlayBtnPunch(_prevBtn);

        int count = _activeParty.Count;
        int prevIndex = (_currentIndex - 1 + count) % count;

        SelectByIndex(prevIndex, direction: -1);
    }

    private void OnClickNext()
    {
        if (_activeParty.Count == 0) return;

        PlayBtnPunch(_nextBtn);

        int count = _activeParty.Count;
        int nextIndex = (_currentIndex + 1) % count;

        SelectByIndex(nextIndex, direction: 1);
    }

    private void PlayBtnPunch(Button btn)
    {
        if (btn == null) return;

        RectTransform rt = btn.transform as RectTransform;
        if (rt == null) return;

        rt.DOKill();
        rt.DOPunchScale(Vector3.one * _btnPunchScale, _btnPunchDuration, vibrato: 1, elasticity: 0.5f);
    }

    private void SelectByIndex(int index, int direction)
    {
        if (index < 0 || index >= _activeParty.Count) return;

        PersistentCharacterUnit unit = _activeParty[index];
        if (unit == null || unit.CharacterEquipment == null) return;

        _pendingDirection = direction;

        CharacterSelectionManager.Instance?.SetSelected(unit.CharacterEquipment.Character);
    }

    /// <summary>
    /// 지금 CharacterSelectionManager의 선택값을 기준으로 Prev/Current/Next 전체를 다시 그림.
    /// direction: 0=애니메이션 없이 즉시(최초 진입), -1=Prev를 눌러서 옴, +1=Next를 눌러서 옴.
    /// </summary>
    private void RefreshDisplay(int direction)
    {
        if (CharacterSelectionManager.Instance == null) return;
        if (_activeParty.Count == 0) return;

        CharacterType selected = CharacterSelectionManager.Instance.GetSelected();

        _currentIndex = FindIndex(selected);
        if (_currentIndex < 0) _currentIndex = 0;

        PersistentCharacterUnit currentUnit = _activeParty[_currentIndex];
        if (_currentNameTxt != null) _currentNameTxt.text = currentUnit.CharacterName;

        if (direction != 0)
        {
            PlayCurrentSlideIn(direction);
        }

        int count = _activeParty.Count;

        if (count == 1)
        {
            SetPrevActive(false);
            SetNextActive(false);
            return;
        }

        if (count == 2)
        {
            if (_currentIndex == 0)
            {
                SetPrevActive(false);
                SetNextActive(true, (_currentIndex + 1) % count);
            }
            else
            {
                SetPrevActive(true, (_currentIndex - 1 + count) % count);
                SetNextActive(false);
            }
            return;
        }

        // 3명 이상: Prev/Next 둘 다 순환
        int prevIndex = (_currentIndex - 1 + count) % count;
        int nextIndex = (_currentIndex + 1) % count;

        SetPrevActive(true, prevIndex);
        SetNextActive(true, nextIndex);
    }

    /// <summary>
    /// Current 이름 텍스트를, 선택이 온 방향의 반대쪽에서 시작해 원래 위치로 슬라이드 + 페이드인.
    /// Prev를 눌렀다는 건 왼쪽에서 새 이름이 온다는 느낌이라 오른쪽에서 시작, Next는 그 반대.
    /// </summary>
    private void PlayCurrentSlideIn(int direction)
    {
        if (_currentNameRect == null) return;

        _currentNameRect.DOKill();
        _currentNameCanvasGroup?.DOKill();

        float offsetX = direction < 0 ? _currentSlideDistance : -_currentSlideDistance;

        _currentNameRect.anchoredPosition = _currentNameOriginalPos + new Vector2(offsetX, 0f);
        _currentNameRect
            .DOAnchorPos(_currentNameOriginalPos, _currentSlideDuration)
            .SetEase(Ease.OutQuad);

        if (_currentNameCanvasGroup != null)
        {
            _currentNameCanvasGroup.alpha = 0f;
            _currentNameCanvasGroup.DOFade(1f, _currentSlideDuration).SetEase(Ease.OutQuad);
        }
    }

    private void SetPrevActive(bool active, int index = -1)
    {
        if (_prevBtn != null) _prevBtn.gameObject.SetActive(active);

        if (!active) return;

        PersistentCharacterUnit unit = _activeParty[index];

        if (_prevNameTxt != null) _prevNameTxt.text = unit.CharacterName;
    }

    private void SetNextActive(bool active, int index = -1)
    {
        if (_nextBtn != null) _nextBtn.gameObject.SetActive(active);

        if (!active) return;

        PersistentCharacterUnit unit = _activeParty[index];

        if (_nextNameTxt != null) _nextNameTxt.text = unit.CharacterName;
    }

    private int FindIndex(CharacterType character)
    {
        for (int i = 0; i < _activeParty.Count; i++)
        {
            if (_activeParty[i] != null
                && _activeParty[i].CharacterEquipment != null
                && _activeParty[i].CharacterEquipment.Character == character)
            {
                return i;
            }
        }

        return -1;
    }
}