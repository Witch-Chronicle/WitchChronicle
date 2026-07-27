using DamageNumbersPro;
using UnityEngine;

/// <summary>
/// 캐릭터/적 프리팹 하위에 부착. 자기 BattleUnit의 OnDamaged/OnHealed를 구독해서
/// _spawnPoint 위치에 Damage/HealHp 프리팹(Damage Numbers Pro)을 스폰.
/// - 프리팹은 Mesh(3D Worldspace) 타입 기준이며, 카메라를 향하는 회전은 프리팹 자체 옵션에 맡김
///   (별도 회전 처리 안 함).
/// - BattleUnit은 전투 시작 시점에야 BattleActor.CreateBattleUnit()으로 생성되는 순수 C# 객체라서,
///   컴포넌트가 Awake/OnEnable될 때는 아직 없을 수 있음 -> BattleUIContext.OnBattleStarted 시점에
///   구독을 시도.
/// </summary>
public class DamagePopupSpawner : MonoBehaviour
{
    [Header("Spawn Point")]
    [Tooltip("팝업이 생성될 위치(머리 위 등). 비워두면 이 오브젝트 자신의 위치 사용.")]
    [SerializeField] private Transform _spawnPoint;

    [Header("Prefabs (직접 참조)")]
    [SerializeField] private DamageNumber _damagePrefab;
    [SerializeField] private DamageNumber _healPrefab;

    private BattleActor _ownerActor;
    private BattleUnit _subscribedUnit;
    private bool _isContextSubscribed;

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
    }

    private void UnsubscribeUnit()
    {
        if (_subscribedUnit == null) return;

        _subscribedUnit.OnDamaged -= HandleDamaged;
        _subscribedUnit.OnHealed -= HandleHealed;
        _subscribedUnit = null;
    }

    private void HandleDamaged(int amount)
    {
        if (_damagePrefab == null || _spawnPoint == null) return;

        _damagePrefab.Spawn(_spawnPoint.position, (float)amount);
    }

    private void HandleHealed(int amount)
    {
        if (_healPrefab == null || _spawnPoint == null) return;

        _healPrefab.Spawn(_spawnPoint.position, (float)amount);
    }
}