using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab_BattleCharacter_v1에 붙는 뷰.
/// - 구조: 이 컴포넌트가 붙은 루트(Slot)는 HorizontalLayoutGroup이 관리하는 "레이아웃 폭 고정" 영역이라
///   스케일하지 않음. 실제로 보이는 Icon/Status(Header, HpSlider, MpSlider)는 전부 Visual(자식) 밑에 있고,
///   본인 턴 강조 애니메이션은 이 Visual에만 적용됨.
///   -> Visual 전체를 localScale로 확대하면 내부의 Icon/Status 간 여백 비율이 그대로 유지된 채
///      통째로 커져 보이고, Slot(루트) 크기는 그대로라 레이아웃도 안 흔들림.
///   (RectTransform Width/Height를 직접 키우는 방식은 Icon/Status가 각자 다른 방향으로 스트레치되며
///   서로 겹치는 문제가 있어서 사용하지 않음)
/// - Icon은 BattleUnit.Icon을 그대로 바인딩
/// - HP/MP는 BattleUnit.OnHpChanged/OnMpChanged를 구독해서 실시간 갱신
/// - OrderTxt는 "이번 라운드 전체 턴 순서에서 몇 번째인지"를 표시
/// </summary>
public class BattleCharacterStatusView : MonoBehaviour
{
    [Header("Scale 대상 (Slot이 아니라 이 자식만 커짐)")]
    [SerializeField] private RectTransform _visualRoot;

    [Header("Content")]
    [SerializeField] private Image _iconImg;
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private TMP_Text _orderTxt;
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private TMP_Text _hpTxt;
    [SerializeField] private Slider _mpSlider;
    [SerializeField] private TMP_Text _mpTxt;

    public BattleUnit BoundUnit { get; private set; }

    /// <summary>
    /// 스케일 애니메이션 대상. 지정 안 해두면 안전하게 이 오브젝트 자신으로 대체(단, 그럴 경우
    /// Slot까지 같이 커져서 레이아웃이 흔들리니 반드시 Visual 자식을 연결해야 함).
    /// </summary>
    public RectTransform VisualRoot => _visualRoot != null ? _visualRoot : transform as RectTransform;

    public void Bind(BattleUnit unit)
    {
        UnsubscribeCurrent();

        BoundUnit = unit;

        if (unit == null) return;

        if (_nameTxt != null) _nameTxt.text = unit.UnitName;

        UpdateIcon(unit.Icon);

        unit.OnHpChanged += HandleHpChanged;
        unit.OnMpChanged += HandleMpChanged;

        UpdateHp(unit.CurrentHp, unit.MaxHp);
        UpdateMp(unit.CurrentMp, unit.MaxMp);
    }

    /// <summary>
    /// 캐릭터 아이콘 표시. 아이콘이 없으면(null) 이미지 자체를 비활성화.
    /// </summary>
    private void UpdateIcon(Sprite icon)
    {
        if (_iconImg == null) return;

        _iconImg.sprite = icon;
        _iconImg.enabled = icon != null;
    }

    public void UpdateHp(int currentHp, int maxHp)
    {
        if (_hpSlider != null) _hpSlider.value = maxHp > 0 ? (float)currentHp / maxHp : 0f;
        if (_hpTxt != null) _hpTxt.text = $"{currentHp}/{maxHp}";
    }

    public void UpdateMp(int currentMp, int maxMp)
    {
        if (_mpSlider != null) _mpSlider.value = maxMp > 0 ? (float)currentMp / maxMp : 0f;
        if (_mpTxt != null) _mpTxt.text = $"{currentMp}/{maxMp}";
    }

    /// <summary>
    /// 이번 라운드 전체 턴 순서 상 몇 번째인지 표시.
    /// isDead가 true면 순번 대신 "-"로 표시 (죽은 유닛은 다음 라운드부터 턴 순서 자체에서 제외되므로).
    /// </summary>
    public void UpdateOrder(int roundOrderNumber, bool isDead = false)
    {
        if (_orderTxt == null) return;

        if (isDead)
        {
            _orderTxt.text = "-";
            return;
        }

        _orderTxt.text = roundOrderNumber > 0 ? roundOrderNumber.ToString() : string.Empty;
    }

    public void Clear()
    {
        UnsubscribeCurrent();

        BoundUnit = null;
        if (_nameTxt != null) _nameTxt.text = string.Empty;
        if (_orderTxt != null) _orderTxt.text = string.Empty;
        if (_hpSlider != null) _hpSlider.value = 0f;
        if (_hpTxt != null) _hpTxt.text = string.Empty;
        if (_mpSlider != null) _mpSlider.value = 0f;
        if (_mpTxt != null) _mpTxt.text = string.Empty;

        UpdateIcon(null);
    }

    private void HandleHpChanged()
    {
        if (BoundUnit != null) UpdateHp(BoundUnit.CurrentHp, BoundUnit.MaxHp);
    }

    private void HandleMpChanged()
    {
        if (BoundUnit != null) UpdateMp(BoundUnit.CurrentMp, BoundUnit.MaxMp);
    }

    private void UnsubscribeCurrent()
    {
        if (BoundUnit == null) return;

        BoundUnit.OnHpChanged -= HandleHpChanged;
        BoundUnit.OnMpChanged -= HandleMpChanged;
    }

    private void OnDestroy()
    {
        UnsubscribeCurrent();
    }
}