using UnityEngine;

/// <summary>
/// 낚시 상태에 따라 손에 든 모델을 교체한다.
/// 평상시에는 무기를, 낚시 중에는 낚싯대를 보여준다.
///
/// 플레이어 캐릭터 루트에 붙여서 사용.
///
/// 낚시 여부는 Animator의 IsFishing 파라미터로 판단한다.
/// FishingManager -> FishingAnimatorHook.OnEnterFishing()/OnExitFishing() 순서로
/// 이 파라미터가 켜지고 꺼지므로, 낚시 시스템 코드를 건드리지 않고 상태만 따라갈 수 있다.
/// </summary>
public class FishingEquipSwitcher : MonoBehaviour
{
    [Header("손에 드는 모델")]
    [Tooltip("평상시 들고 다니는 무기.")]
    [SerializeField] private GameObject _weaponRoot;

    [Tooltip("낚시 중에 드는 낚싯대.")]
    [SerializeField] private GameObject _fishingRodRoot;

    [Header("낚시 상태 판단")]
    [Tooltip("비워두면 자식에서 자동으로 찾는다.")]
    [SerializeField] private Animator _animator;

    [SerializeField] private string _isFishingParam = "IsFishing";

    private int _isFishingHash;
    private bool _hasParam;
    private bool _isFishing;

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

    private void Update()
    {
        if (_hasParam == false)
        {
            return;
        }

        bool fishing = _animator.GetBool(_isFishingHash);

        if (fishing == _isFishing)
        {
            return;
        }

        Apply(fishing);
    }

    /// <summary>
    /// 낚시 여부에 맞춰 두 모델의 표시 상태를 정한다.
    /// </summary>
    /// <param name="fishing">낚시 중인지 여부</param>
    private void Apply(bool fishing)
    {
        _isFishing = fishing;

        if (_weaponRoot != null)
        {
            _weaponRoot.SetActive(fishing == false);
        }

        if (_fishingRodRoot != null)
        {
            _fishingRodRoot.SetActive(fishing);
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
