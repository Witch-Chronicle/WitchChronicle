using DamageNumbersPro;
using UnityEngine;

/// <summary>
/// 캐릭터/적 프리팹 하위에 부착. 자기 BattleUnit의 OnDamaged/OnHealed/OnMpChanged를 구독해서
/// _spawnPoint 위치에 Damage/HealHp/HealMana 프리팹(Damage Numbers Pro)을 스폰.
/// - 프리팹은 Mesh(3D Worldspace) 타입 기준이며, 카메라를 향하는 회전은 프리팹 자체 옵션에 맡김
///   (별도 회전 처리 안 함).
/// - BattleUnit은 전투 시작 시점에야 BattleActor.CreateBattleUnit()으로 생성되는 순수 C# 객체라서,
///   컴포넌트가 Awake/OnEnable될 때는 아직 없을 수 있음 -> BattleUIContext.OnBattleStarted 시점에
///   구독을 시도.
/// - OnMpChanged는 UseMp/RestoreMp 양쪽에서 파라미터 없이 동일하게 발동되므로(BattleUnit은 수정하지 않음),
///   여기서 직전 MP 값을 직접 기억해뒀다가 실제로 증가했을 때만 HealMana 팝업을 띄움.
/// </summary>
public class DamagePopupSpawner : MonoBehaviour
{
    [Header("Spawn Point")]
    [Tooltip("팝업이 생성될 위치(머리 위 등). 비워두면 이 오브젝트 자신의 위치 사용.")]
    [SerializeField] private Transform _spawnPoint;

    [Header("Prefabs (직접 참조)")]
    [SerializeField] private DamageNumber _damagePrefab;
    [SerializeField] private DamageNumber _healPrefab;
    [Tooltip("MP가 실제로 회복됐을 때만(소모 시엔 스폰 안 함) 사용")]
    [SerializeField] private DamageNumber _healManaPrefab;

    private BattleActor _ownerActor;
    private BattleUnit _subscribedUnit;
    private bool _isContextSubscribed;

    // OnMpChanged가 소모/회복 구분 없이 발동되므로, 직접 이전 값을 기억해서 증가분만 팝업 처리
    private int _lastKnownMp;

    private void Awake()
    {
        if (_ownerActor == null)
        {
            _ownerActor = GetComponentInParent<BattleActor>();
        }

        if (_spawnPoint == null)
        {
            _spawnPoint = transform;
        }
    }

    private void OnEnable()
    {
        TrySubscribeBattleContext();
        TrySubscribeUnit();
    }

    private void OnDisable()
    {
        UnsubscribeBattleContext();
        UnsubscribeUnit();
    }

    /// <summary>
    /// BattleUnit이 생성되는 시점(전투 시작)을 놓쳤을 수 있으니 OnBattleStarted에도 재시도.
    /// </summary>
    private void TrySubscribeBattleContext()
    {
        if (_isContextSubscribed) return;
        if (BattleUIContext.Instance == null) return;

        BattleUIContext.Instance.OnBattleStarted += HandleBattleStarted;
        _isContextSubscribed = true;
    }

    private void UnsubscribeBattleContext()
    {
        if (_isContextSubscribed == false) return;

        if (BattleUIContext.Instance != null)
        {
            BattleUIContext.Instance.OnBattleStarted -= HandleBattleStarted;
        }

        _isContextSubscribed = false;
    }

    private void HandleBattleStarted()
    {
        TrySubscribeUnit();
    }

    private void TrySubscribeUnit()
    {
        if (_subscribedUnit != null) return;
        if (_ownerActor == null || _ownerActor.HasBattleUnit == false) return;

        _subscribedUnit = _ownerActor.BattleUnit;
        _subscribedUnit.OnDamaged += HandleDamaged;
        _subscribedUnit.OnHealed += HandleHealed;
        _subscribedUnit.OnMpChanged += HandleMpChanged;

        // 구독 시점의 MP를 기준값으로 저장 - 이후 변화량 비교에 사용
        _lastKnownMp = _subscribedUnit.CurrentMp;
    }

    private void UnsubscribeUnit()
    {
        if (_subscribedUnit == null) return;

        _subscribedUnit.OnDamaged -= HandleDamaged;
        _subscribedUnit.OnHealed -= HandleHealed;
        _subscribedUnit.OnMpChanged -= HandleMpChanged;
        _subscribedUnit = null;
    }

    private void HandleDamaged(int amount)
    {
        // 테스트 용
        // amount = 0;

        if (_damagePrefab == null || _spawnPoint == null) return;

        if (amount == 0)
        {
            _damagePrefab.Spawn(_spawnPoint.position, "Miss");
            return;
        }

        _damagePrefab.Spawn(_spawnPoint.position, (float)amount);
    }

    private void HandleHealed(int amount)
    {
        // 테스트 용
        // amount = 0;

        if (_healPrefab == null || _spawnPoint == null) return;

        if (amount == 0)
        {
            _healPrefab.Spawn(_spawnPoint.position, "Miss");
            return;
        }

        _healPrefab.Spawn(_spawnPoint.position, (float)amount);
    }

    /// <summary>
    /// UseMp/RestoreMp 양쪽에서 다 호출되므로, 직전 값 대비 실제로 늘어난 경우에만 팝업.
    /// (소모 시엔 delta가 음수 또는 0이라 자연히 무시됨)
    /// </summary>
    private void HandleMpChanged()
    {
        if (_subscribedUnit == null) return;

        int currentMp = _subscribedUnit.CurrentMp;
        int delta = currentMp - _lastKnownMp;

        if (delta > 0 && _healManaPrefab != null && _spawnPoint != null)
        {
            _healManaPrefab.Spawn(_spawnPoint.position, (float)delta);
        }

        _lastKnownMp = currentMp;
    }
}