using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Prefab_TurnOrder에 붙는 뷰. 전투 참가자 전원(아군+적)의 턴 순서 카드 하나를 표시.
/// - Prefab_TurnOrder 자신의 Image 색상으로 팀 구분(아군 파랑/적 빨강)
/// - Dimmed: 본인 턴이 아니면 켜짐(어두워짐), 본인 턴이면 꺼짐
/// - Death: 죽은 유닛이면 켜짐. 죽었으면 Dimmed도 항상 같이 켜짐(본인 턴이 될 수 없으므로).
/// - NameTxt: 죽었거나 본인 턴이 아니면(Dimmed 상태) 알파를 110/255로 낮춰서 흐리게 표시.
/// - Icon: BattleUnit.Icon을 그대로 바인딩, 없으면 비활성화. 카드 크기(sizeDelta) 자체를 키움
///   (내부에 이미지만 있는 단순 구조라 스케일 대신 실제 크기 조절 사용).
///   본인 턴이면 80x80 -> 100x100으로 커짐. 카드가 매 턴마다 새로 생성되는 구조라
///   생성 시점에 바로 목표 크기로 애니메이션.
/// </summary>
public class TurnOrderCardView : MonoBehaviour
{
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Image _cardImage;
    [SerializeField] private Image _iconImg;
    [SerializeField] private GameObject _dimmed;
    [SerializeField] private GameObject _death;
    [SerializeField] private TMP_Text _nameTxt;

    [Header("Team Colors")]
    [SerializeField] private Color _playerColor = Color.blue;
    [SerializeField] private Color _enemyColor = Color.red;

    [Header("Dimmed NameTxt Alpha")]
    [SerializeField] private float _dimmedAlpha = 110f / 255f;
    [SerializeField] private float _normalAlpha = 1f;

    [Header("Size (본인 턴 강조)")]
    [SerializeField] private Vector2 _baseSize = new Vector2(80f, 80f);
    [SerializeField] private Vector2 _selectedSize = new Vector2(100f, 100f);
    [SerializeField] private float _sizeDuration = 0.2f;
    [SerializeField] private Ease _sizeEase = Ease.OutBack;

    public void Bind(BattleUnit unit, bool isCurrentTurn)
    {
        if (unit == null) return;

        bool isDead = unit.IsAlive == false;
        bool isDimmed = isDead || isCurrentTurn == false;

        if (_nameTxt != null)
        {
            _nameTxt.text = unit.UnitName;
            _nameTxt.alpha = isDimmed ? _dimmedAlpha : _normalAlpha;
        }

        if (_cardImage != null)
        {
            _cardImage.color = unit.TeamType == BattleTeamType.Player ? _playerColor : _enemyColor;
        }

        UpdateIcon(unit.Icon);

        if (_death != null)
        {
            _death.SetActive(isDead);
        }

        if (_dimmed != null)
        {
            _dimmed.SetActive(isDimmed);
        }

        AnimateSize(isCurrentTurn && isDead == false);
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

    /// <summary>
    /// 카드가 매 턴마다 새로 생성되는 구조라, 생성 직후 기본 크기에서 목표 크기로 튀어 들어오는 연출.
    /// </summary>
    private void AnimateSize(bool isSelected)
    {
        if (_rectTransform == null) return;

        Vector2 targetSize = isSelected ? _selectedSize : _baseSize;

        _rectTransform.DOKill();
        _rectTransform.sizeDelta = _baseSize;
        _rectTransform.DOSizeDelta(targetSize, _sizeDuration).SetEase(_sizeEase);
    }
}