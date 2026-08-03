using UnityEngine;

/// <summary>
/// 마도서(그리모어). 사용하면 지정된 티어 범위의 스킬 중 하나를 무작위로 습득한다.
/// 일반 마도서는 범위를 넓게(1~4), 티어별 마도서는 좁게(예: 3~3) 설정한다.
/// </summary>
[CreateAssetMenu(fileName = "NewSkillBook", menuName = "Witch Chronicle/Item/SkillBookItemData")]
public class SkillBookItemData : ConsumableItemData
{
    /// <summary>티어별 등장 가중치. Weight가 클수록 자주 나온다.</summary>
    [System.Serializable]
    public struct TierWeight
    {
        [Tooltip("대상 티어 (1이 최상급)")]
        public int Tier;

        [Tooltip("가중치. 전체 합 대비 비율로 뽑힌다. 0이면 나오지 않음")]
        public int Weight;
    }

    [Header("마도서 - 획득 티어 범위")]
    [Tooltip("뽑을 수 있는 최소 티어")]
    [SerializeField] private int _minTier = 1;

    [Tooltip("뽑을 수 있는 최대 티어")]
    [SerializeField] private int _maxTier = 4;

    [Header("마도서 - 티어별 확률")]
    [Tooltip("티어별 등장 가중치. 비워두면 범위 내 티어가 균등 확률로 나온다")]
    [SerializeField] private TierWeight[] _tierWeights;

    [Header("마도서 - 후보 스킬")]
    [Tooltip("이 마도서에서 나올 수 있는 스킬 전체. 여기서 티어 범위로 다시 걸러진다")]
    [SerializeField] private SkillData[] _candidateSkills;

    [Header("마도서 - 중복 보상")]
    [Tooltip("뽑을 스킬이 남지 않았을 때(전부 습득) 대신 지급할 골드")]
    [SerializeField] private int _duplicateGold = 100;

    public int MinTier => _minTier;
    public int MaxTier => _maxTier;
    public TierWeight[] TierWeights => _tierWeights;
    public SkillData[] CandidateSkills => _candidateSkills;
    public int DuplicateGold => _duplicateGold;

    /// <summary>
    /// 가중치에 따라 티어 하나를 뽑는다.
    /// 가중치가 설정돼 있지 않으면 범위 내에서 균등하게 뽑는다.
    /// </summary>
    /// <returns>뽑힌 티어. 뽑을 수 없으면 -1</returns>
    public int RollTier()
    {
        int total = 0;

        if (_tierWeights != null)
        {
            for (int i = 0; i < _tierWeights.Length; i++)
            {
                if (IsTierUsable(_tierWeights[i].Tier) && _tierWeights[i].Weight > 0)
                {
                    total += _tierWeights[i].Weight;
                }
            }
        }

        // 가중치 미설정 → 범위 내 균등
        if (total <= 0)
        {
            if (_maxTier < _minTier)
            {
                return -1;
            }

            return Random.Range(_minTier, _maxTier + 1);
        }

        int roll = Random.Range(0, total);
        int acc = 0;

        for (int i = 0; i < _tierWeights.Length; i++)
        {
            if (IsTierUsable(_tierWeights[i].Tier) == false || _tierWeights[i].Weight <= 0)
            {
                continue;
            }

            acc += _tierWeights[i].Weight;

            if (roll < acc)
            {
                return _tierWeights[i].Tier;
            }
        }

        return _minTier;
    }

    /// <summary>티어가 이 마도서의 범위 안인지.</summary>
    private bool IsTierUsable(int tier)
    {
        return tier >= _minTier && tier <= _maxTier;
    }

    /// <summary>해당 스킬이 이 마도서의 티어 범위에 드는지.</summary>
    public bool IsInTierRange(SkillData skill)
    {
        return skill != null && skill.Tier >= _minTier && skill.Tier <= _maxTier;
    }
}
