using UnityEngine;

/// <summary>
/// 낚시 상태에 따라 손에 든 모델을 교체한다.
/// 평상시에는 무기를, 낚시 중에는 현재 장착한 등급의 낚싯대를 보여준다.
///
/// 낚싯대를 하나도 가지고 있지 않으면 낚시 동작 자체를 재생하지 않고
/// 평소처럼 무기를 든 Idle 상태로 서 있게 한다.
/// (FishingAnimatorHook이 켠 IsFishing을 다시 꺼서 막는다.)
///
/// 플레이어 캐릭터 루트에 붙여서 사용.
/// </summary>
public class FishingEquipSwitcher : MonoBehaviour
{
    [Header("평상시 무기")]
    [SerializeField] private GameObject _weaponRoot;

    [Header("낚싯대 (등급별)")]
    [Tooltip("1등급 - 나뭇가지")]
    [SerializeField] private GameObject _rodRank1;

    [Tooltip("2등급 - 철제")]
    [SerializeField] private GameObject _rodRank2;

    [Tooltip("3등급 - 마법")]
    [SerializeField] private GameObject _rodRank3;

    [Header("애니메이터")]
    [Tooltip("비워두면 자식에서 자동으로 찾는다.")]
    [SerializeField] private Animator _animator;

    [SerializeField] private string _isFishingParam = "IsFishing";

    private int _isFishingHash;
    private bool _hasParam;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        _isFishingHash = Animator.StringToHash(_isFishingParam);
        _hasParam = HasBoolParameter(_isFishingHash);
    }

    private void Update()
    {
        // 낚싯대를 하나도 못 가진 상태면 낚시 세션이 열려 있어도 평상시처럼 취급한다.
        bool fishing = IsSessionActive() && HasAnyRod();
        int rank = fishing ? GetCurrentRank() : 0;

        if (_hasParam)
        {
            // 낚싯대가 없는데 훅이 IsFishing을 켰다면 되돌려서 Idle을 유지시킨다.
            if (_animator.GetBool(_isFishingHash) != fishing)
            {
                _animator.SetBool(_isFishingHash, fishing);
            }
        }

        SetActiveSafe(_weaponRoot, fishing == false);
        SetActiveSafe(_rodRank1, rank == 1);
        SetActiveSafe(_rodRank2, rank == 2);
        SetActiveSafe(_rodRank3, rank == 3);
    }

    /// <summary>
    /// 낚시 세션이 진행 중인지.
    /// </summary>
    private static bool IsSessionActive()
    {
        return FishingManager.Instance != null && FishingManager.Instance.IsSessionActive;
    }

    /// <summary>
    /// 낚싯대를 하나라도 보유했는지.
    /// </summary>
    private static bool HasAnyRod()
    {
        return FishingManager.Instance != null && FishingManager.Instance.HasAnyRod;
    }

    /// <summary>
    /// 현재 장착 낚싯대 등급. 장착한 게 없으면 0.
    /// (CurrentRodRank는 미보유 시에도 1을 주므로 CurrentRod로 판단해야 한다.)
    /// </summary>
    /// <returns>1~3, 없으면 0</returns>
    private static int GetCurrentRank()
    {
        if (FishingManager.Instance == null || FishingManager.Instance.CurrentRod == null)
        {
            return 0;
        }

        return Mathf.Clamp(FishingManager.Instance.CurrentRodRank, 1, 3);
    }

    private static void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    /// <summary>
    /// Animator에 해당 Bool 파라미터가 실제로 있는지 확인.
    /// (없는 파라미터를 건드리면 매 프레임 경고가 쏟아진다.)
    /// </summary>
    /// <param name="hash">파라미터 해시</param>
    /// <returns>존재 여부</returns>
    private bool HasBoolParameter(int hash)
    {
        if (_animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.nameHash == hash)
            {
                return true;
            }
        }

        return false;
    }
}
