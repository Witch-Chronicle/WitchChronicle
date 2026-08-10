using UnityEngine;

/// <summary>
/// 낚시 상태에 따라 손에 든 모델을 교체한다.
/// 평상시에는 무기를, 낚시 중에는 현재 장착한 등급의 낚싯대를 보여준다.
///
/// 플레이어 캐릭터 루트에 붙여서 사용.
///
/// 낚시 여부는 Animator의 IsFishing 파라미터로 판단한다.
/// FishingManager -> FishingAnimatorHook.OnEnterFishing()/OnExitFishing() 순서로
/// 이 파라미터가 켜지고 꺼지므로, 낚시 시스템 코드를 건드리지 않고 상태만 따라갈 수 있다.
///
/// 낚싯대 종류는 FishingManager.CurrentRodRank(1~3)로 고른다.
/// 낚싯대 모델 3종을 손 본 아래에 미리 배치해두고 아래 슬롯에 등록하면 된다.
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

    [Header("낚시 상태 판단")]
    [Tooltip("비워두면 자식에서 자동으로 찾는다.")]
    [SerializeField] private Animator _animator;

    [SerializeField] private string _isFishingParam = "IsFishing";

    private int _isFishingHash;
    private bool _hasParam;
    private bool _isFishing;
    private int _shownRank;
    private bool _subscribed;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        _isFishingHash = Animator.StringToHash(_isFishingParam);
        _hasParam = HasBoolParameter(_isFishingHash);

        if (_hasParam == false)
        {
            Debug.LogWarning($"[FishingEquipSwitcher] Animator에 '{_isFishingParam}' Bool 파라미터가 없습니다.");
        }

        Apply(false);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (_subscribed && FishingManager.Instance != null)
        {
            FishingManager.Instance.OnRodEquipped -= HandleRodEquipped;
        }

        _subscribed = false;
    }

    private void Update()
    {
        // FishingManager는 씬 로드 순서에 따라 늦게 준비될 수 있어 계속 시도한다.
        TrySubscribe();

        if (_hasParam == false)
        {
            return;
        }

        bool fishing = _animator.GetBool(_isFishingHash);

        // 낚시 중에 낚싯대를 바꾸는 경우까지 반영하려면 등급도 같이 본다.
        if (fishing == _isFishing && (fishing == false || _shownRank == GetCurrentRank()))
        {
            return;
        }

        Apply(fishing);
    }

    /// <summary>
    /// 낚싯대 교체 이벤트. 낚시 중이면 즉시 모델을 바꾼다.
    /// </summary>
    /// <param name="rod">새로 장착한 낚싯대</param>
    private void HandleRodEquipped(RodItemData rod)
    {
        if (_isFishing)
        {
            Apply(true);
        }
    }

    private void TrySubscribe()
    {
        if (_subscribed || FishingManager.Instance == null)
        {
            return;
        }

        FishingManager.Instance.OnRodEquipped += HandleRodEquipped;
        _subscribed = true;
    }

    /// <summary>
    /// 현재 장착 낚싯대 등급. FishingManager가 없으면 1등급으로 본다.
    /// </summary>
    /// <returns>1~3</returns>
    private int GetCurrentRank()
    {
        if (FishingManager.Instance == null)
        {
            return 1;
        }

        return Mathf.Clamp(FishingManager.Instance.CurrentRodRank, 1, 3);
    }

    /// <summary>
    /// 낚시 여부와 낚싯대 등급에 맞춰 표시 상태를 정한다.
    /// </summary>
    /// <param name="fishing">낚시 중인지 여부</param>
    private void Apply(bool fishing)
    {
        _isFishing = fishing;
        _shownRank = fishing ? GetCurrentRank() : 0;

        if (_weaponRoot != null)
        {
            _weaponRoot.SetActive(fishing == false);
        }

        SetActiveSafe(_rodRank1, fishing && _shownRank == 1);
        SetActiveSafe(_rodRank2, fishing && _shownRank == 2);
        SetActiveSafe(_rodRank3, fishing && _shownRank == 3);
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
    /// (없는 파라미터를 읽으면 매 프레임 경고가 쏟아진다.)
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
