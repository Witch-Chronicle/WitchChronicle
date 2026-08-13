using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 상세 정보에 표시되는 개별 몬스터 슬롯 UI입니다.
/// </summary>
public class MapMonsterSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image _monsterIcon;
    [SerializeField] private TMP_Text _nameTxt;

    public EnemyBattleData EnemyData { get; private set; }

    /// <summary>
    /// 몬스터 데이터를 슬롯 UI에 적용합니다.
    /// </summary>
    public void Bind(EnemyBattleData enemyData)
    {
        EnemyData = enemyData;

        if (enemyData == null)
        {
            Clear();
            return;
        }

        if (_monsterIcon != null)
        {
            _monsterIcon.sprite = enemyData.Icon;
            _monsterIcon.enabled = enemyData.Icon != null;
        }

        if (_nameTxt != null)
        {
            _nameTxt.text = enemyData.EnemyName;
        }
    }

    /// <summary>
    /// 풀로 반환하기 전에 슬롯 데이터를 초기화합니다.
    /// </summary>
    public void Clear()
    {
        EnemyData = null;

        if (_monsterIcon != null)
        {
            _monsterIcon.sprite = null;
            _monsterIcon.enabled = false;
        }

        if (_nameTxt != null)
        {
            _nameTxt.text = string.Empty;
        }
    }
}