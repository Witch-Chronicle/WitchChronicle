using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 적 프리팹(EnemyWorldSpaceCanvas)에 부착. 타겟으로 선택된 적 머리 위에 이름/HP를 표시.
/// - 평소엔 꺼져있다가, BattleTargetCycler가 이 적을 타겟으로 지정할 때만 Show().
/// - CharacterWorldSpaceCanvas와 동일하게 매 프레임 카메라를 향해 회전(billboard).
/// - HP는 BattleUnit.OnHpChanged를 구독해서 슬라이더가 DOTween으로 부드럽게 변화.
/// * BattleUnit은 전투 시작 시점에야 생성되는 순수 C# 객체라서, BattleActor를 통해 매번 조회
///   (BattleCharacterUISet/BattleActionBarController와 동일한 패턴).
/// </summary>
public class EnemyTargetOverlay : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameTxt;
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private TMP_Text _hpTxt;

    [Header("Billboard (카메라 향해 회전)")]
    [SerializeField] private bool _billboard = true;

    [Header("HP Slider Animation")]
    [SerializeField] private float _hpTweenDuration = 0.3f;
    [SerializeField] private Ease _hpTweenEase = Ease.OutQuad;

    private BattleActor _ownerActor;
    private Tween _hpTween;
    private bool _isBound;

    private void Awake()
    {
        if (_ownerActor == null)
        {
            _ownerActor = GetComponentInParent<BattleActor>();
        }

        HideImmediate();
    }

    private void LateUpdate()
    {
        if (_billboard == false) return;
        if (Camera.main == null) return;

        transform.rotation = Camera.main.transform.rotation;
    }

    private void OnDestroy()
    {
        UnbindCurrent();
    }

    /// <summary>
    /// BattleTargetCycler가 이 적을 타겟으로 지정할 때 호출.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        BindIfNeeded();
    }

    /// <summary>
    /// 타겟에서 해제되거나 죽었을 때 호출.
    /// </summary>
    public void Hide()
    {
        UnbindCurrent();
        gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        UnbindCurrent();
        gameObject.SetActive(false);
    }

    private void BindIfNeeded()
    {
        if (_isBound) return;
        if (_ownerActor == null || _ownerActor.HasBattleUnit == false) return;

        BattleUnit unit = _ownerActor.BattleUnit;

        if (_nameTxt != null) _nameTxt.text = unit.UnitName;

        unit.OnHpChanged += HandleHpChanged;
        _isBound = true;

        UpdateHpImmediate(unit.CurrentHp, unit.MaxHp);
    }

    private void UnbindCurrent()
    {
        if (_isBound == false) return;

        if (_ownerActor != null && _ownerActor.HasBattleUnit)
        {
            _ownerActor.BattleUnit.OnHpChanged -= HandleHpChanged;
        }

        _isBound = false;
        _hpTween?.Kill();
    }

    private void HandleHpChanged()
    {
        if (_ownerActor == null || _ownerActor.HasBattleUnit == false) return;

        BattleUnit unit = _ownerActor.BattleUnit;
        UpdateHpAnimated(unit.CurrentHp, unit.MaxHp);
    }

    private void UpdateHpImmediate(int currentHp, int maxHp)
    {
        if (_hpSlider != null)
        {
            _hpSlider.DOKill();
            _hpSlider.value = maxHp > 0 ? (float)currentHp / maxHp : 0f;
        }

        if (_hpTxt != null) _hpTxt.text = $"{currentHp} / {maxHp}";
    }

    private void UpdateHpAnimated(int currentHp, int maxHp)
    {
        if (_hpSlider != null)
        {
            float targetValue = maxHp > 0 ? (float)currentHp / maxHp : 0f;

            _hpTween?.Kill();
            _hpTween = _hpSlider.DOValue(targetValue, _hpTweenDuration).SetEase(_hpTweenEase);
        }

        if (_hpTxt != null) _hpTxt.text = $"{currentHp} / {maxHp}";
    }
}