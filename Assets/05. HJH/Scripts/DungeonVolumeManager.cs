using System.Collections;
using UnityEngine;

/// <summary>
/// 던전 씬에서 사용하는 Global Volume 2개(ToonVolume, BackgroundVolume)의
/// 활성화 순서를 관리합니다.
///
/// - 씬 시작 시 둘 다 비활성화
/// - 씬 로드가 끝난 뒤 ToonVolume을 먼저 활성화하고,
///   한 프레임 뒤에 BackgroundVolume을 활성화합니다.
///
/// 두 Volume을 같은 프레임에 동시에 켜면 Volume Manager가
/// 그 프레임의 Weight 블렌딩을 한꺼번에 계산하면서 값이 꼬일 수 있어
/// (예: Fog/Color Adjustments 등이 과도기적으로 섞여 3D 오브젝트가
/// 안 보이는 현상), 활성화 시점을 한 프레임씩 분리합니다.
/// </summary>
public class DungeonVolumeManager : MonoBehaviour
{
    [Header("Volumes")]
    [Tooltip("먼저 활성화할 Volume입니다.")]
    [SerializeField] private GameObject _toonVolume;
    [Tooltip("ToonVolume 활성화 다음 프레임에 활성화할 Volume입니다.")]
    [SerializeField] private GameObject _backgroundVolume;

    [Header("Timing")]
    [Tooltip("ToonVolume 활성화 후 BackgroundVolume을 활성화하기까지 대기할 프레임 수입니다.")]
    [SerializeField, Min(0)] private int _framesBetweenActivation = 1;

    private Coroutine _activationRoutine;

    private void Awake()
    {
        SetActiveSafe(_toonVolume, false);
        SetActiveSafe(_backgroundVolume, false);
    }

    private void Start()
    {
        ActivateVolumesInOrder();
    }

    /// <summary>
    /// ToonVolume -> (N프레임 대기) -> BackgroundVolume 순서로 활성화를 시작합니다.
    /// 외부(씬 전환 완료 콜백 등)에서 다시 호출해도 안전하게 재시작됩니다.
    /// </summary>
    public void ActivateVolumesInOrder()
    {
        if (_activationRoutine != null)
        {
            StopCoroutine(_activationRoutine);
        }

        _activationRoutine = StartCoroutine(ActivationSequence());
    }

    /// <summary>
    /// 두 Volume을 즉시 모두 비활성화합니다. (씬 종료/전환 시 사용)
    /// </summary>
    public void DeactivateAll()
    {
        if (_activationRoutine != null)
        {
            StopCoroutine(_activationRoutine);
            _activationRoutine = null;
        }

        SetActiveSafe(_toonVolume, false);
        SetActiveSafe(_backgroundVolume, false);
    }

    private IEnumerator ActivationSequence()
    {
        SetActiveSafe(_backgroundVolume, false);
        SetActiveSafe(_toonVolume, true);

        for (int i = 0; i < _framesBetweenActivation; i++)
        {
            yield return null;
        }

        SetActiveSafe(_backgroundVolume, true);

        _activationRoutine = null;
    }

    private void SetActiveSafe(GameObject target, bool active)
    {
        if (target == null)
        {
            return;
        }

        if (target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private void OnDisable()
    {
        if (_activationRoutine != null)
        {
            StopCoroutine(_activationRoutine);
            _activationRoutine = null;
        }
    }
}