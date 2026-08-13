using EPOOutline;
using UnityEngine;

/// <summary>
/// 전투 Actor 관리
/// </summary>
public class BattleActor : MonoBehaviour
{
    [Header("Actor Info")]
    [SerializeField] private BattleTeamType _teamType;

    [Header("Player Data")]
    [SerializeField] private CharacterStats _characterStats;
    [SerializeField] private CharacterVitals _characterVitals;
    [SerializeField] private PlayerSkillLoadout _playerSkillLoadout;

    [Header("Enemy Data")]
    [SerializeField] private EnemyBattleData _enemyBattleData;

    [Header("View")]
    [SerializeField] private Transform _visualRoot;

    [Header("World Space Canvas (동적 높이 배치)")]
    [Tooltip("적 비주얼 Bounds 계산 후 위치를 맞출 EnemyWorldSpaceCanvas의 Transform")]
    [SerializeField] private Transform _worldSpaceCanvasTransform;
    [Tooltip("비주얼 최고점(Bounds.max.y)에서 이만큼 더 위로 띄움")]
    [SerializeField] private float _canvasHeightOffset = 0.2f;



    [Header("Persistent Source")]
    [SerializeField] private PersistentCharacterUnit _persistentCharacterUnit;

    private BattleUnit _battleUnit;

    public BattleTeamType TeamType => _teamType;
    public BattleUnit BattleUnit => _battleUnit;
    public bool HasBattleUnit => _battleUnit != null;
    public PersistentCharacterUnit PersistentCharacterUnit => _persistentCharacterUnit;
    public EnemyBattleData EnemyBattleData => _enemyBattleData;

    /// <summary>
    /// 참조 자동 연결
    /// </summary>
    private void Awake()
    {
        ResolveReferences();
    }

    /// <summary>
    /// 같은 오브젝트의 전투 참조 연결
    /// </summary>
    public void ResolveReferences()
    {
        if (_characterStats == null)
        {
            _characterStats = GetComponent<CharacterStats>();
        }

        if (_characterVitals == null)
        {
            _characterVitals = GetComponent<CharacterVitals>();
        }

        if (_playerSkillLoadout == null)
        {
            _playerSkillLoadout = GetComponent<PlayerSkillLoadout>();
        }

        if (_visualRoot == null)
        {
            _visualRoot = transform;
        }
    }

    /// <summary>
    /// 유지 캐릭터 데이터 연결
    /// </summary>
    /// <param name="persistentCharacterUnit">연결 캐릭터 데이터</param>
    public void InitializeFromPersistentCharacter(PersistentCharacterUnit persistentCharacterUnit)
    {
        _persistentCharacterUnit = persistentCharacterUnit;

        if (_persistentCharacterUnit == null)
        {
            return;
        }

        _teamType = BattleTeamType.Player;

        _characterStats = _persistentCharacterUnit.CharacterStats;
        _characterVitals = _persistentCharacterUnit.CharacterVitals;
        _playerSkillLoadout = _persistentCharacterUnit.PlayerSkillLoadout;
    }

    /// <summary>
    /// Actor 팀 타입 설정
    /// </summary>
    /// <param name="teamType">설정 팀 타입</param>
    public void SetTeamType(BattleTeamType teamType)
    {
        _teamType = teamType;
    }

    /// <summary>
    /// 현재 Actor 데이터로 BattleUnit 생성
    /// </summary>
    /// <returns>생성 BattleUnit</returns>
    public BattleUnit CreateBattleUnit()
    {
        ResolveReferences();

        if (_teamType == BattleTeamType.Player)
        {
            _battleUnit = CreatePlayerBattleUnit();
            return _battleUnit;
        }

        _battleUnit = CreateEnemyBattleUnit();
        return _battleUnit;
    }

    /// <summary>
    /// 플레이어 BattleUnit 생성
    /// </summary>
    /// <returns>생성 플레이어 BattleUnit</returns>
    private BattleUnit CreatePlayerBattleUnit()
    {
        if (CanCreatePlayerBattleUnit() == false)
        {
            return null;
        }

        string unitId = GetPlayerUnitId();
        string unitName = GetPlayerUnitName();

        return BattleUnit.CreatePlayer(
            unitId,
            unitName,
            _characterStats.CombatMaxHp,
            _characterVitals.CurrentHp,
            _characterStats.CombatMaxMp,
            _characterVitals.CurrentMp,
            _characterStats.CombatAttackPower,
            _characterStats.CombatMagicPower,
            _characterStats.CombatDefensePower,
            _characterStats.CombatMagicDefensePower,
            _characterStats.CombatSpeed,
            _playerSkillLoadout.GetBattleSkillList(),
            _characterStats.Icon,
            _characterStats.Level);
    }

    /// <summary>
    /// 플레이어 UnitId 반환
    /// </summary>
    /// <returns>플레이어 UnitId</returns>
    private string GetPlayerUnitId()
    {
        if (_persistentCharacterUnit != null)
        {
            return _persistentCharacterUnit.CharacterId;
        }

        if (_characterStats != null)
        {
            return _characterStats.CharacterId;
        }

        return string.Empty;
    }

    /// <summary>
    /// 플레이어 UnitName 반환
    /// </summary>
    /// <returns>플레이어 UnitName</returns>
    private string GetPlayerUnitName()
    {
        if (_persistentCharacterUnit != null)
        {
            return _persistentCharacterUnit.CharacterName;
        }

        if (_characterStats != null)
        {
            return _characterStats.CharacterName;
        }

        return name;
    }

    /// <summary>
    /// 적 BattleUnit 생성
    /// </summary>
    /// <returns>생성 적 BattleUnit</returns>
    private BattleUnit CreateEnemyBattleUnit()
    {
        if (_enemyBattleData == null)
        {
            Debug.LogError($"{name}에 EnemyBattleData 없음");
            return null;
        }

        return BattleUnit.CreateEnemy(_enemyBattleData);
    }

    /// <summary>
    /// 플레이어 BattleUnit 생성 가능 여부 확인
    /// </summary>
    /// <returns>생성 가능 여부</returns>
    private bool CanCreatePlayerBattleUnit()
    {
        if (_characterStats == null)
        {
            Debug.LogError($"{name}에 CharacterStats 없음");
            return false;
        }

        if (_characterVitals == null)
        {
            Debug.LogError($"{name}에 CharacterVitals 없음");
            return false;
        }

        if (_playerSkillLoadout == null)
        {
            Debug.LogError($"{name}에 PlayerSkillLoadout 없음");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 전투 결과 HP/MP 반영.
    /// isVictory가 true이고 이 캐릭터가 전투 중 사망했다면, HP 0 대신 1로 보정해서 반영(부활 처리).
    /// </summary>
    /// <param name="isVictory">전투 승리 여부</param>
    public void ApplyBattleResultToVitals(bool isVictory)
    {
        if (_teamType != BattleTeamType.Player)
        {
            return;
        }

        if (_battleUnit == null)
        {
            return;
        }

        int finalHp = _battleUnit.CurrentHp;

        // 승리 시, 전투 중 사망한 캐릭터는 HP 1로 부활 처리
        if (isVictory && _battleUnit.IsAlive == false)
        {
            finalHp = 1;
        }

        if (_persistentCharacterUnit != null)
        {
            _persistentCharacterUnit.ApplyVitals(
                finalHp,
                _battleUnit.CurrentMp);

            return;
        }

        if (_characterVitals == null)
        {
            return;
        }

        _characterVitals.SetCurrentVitals(
            finalHp,
            _battleUnit.CurrentMp);
    }

    /// <summary>
    /// 전투 배치 위치와 회전 설정
    /// </summary>
    /// <param name="position">배치 위치</param>
    /// <param name="rotation">배치 회전</param>
    public void SetFormationPose(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// BattleUnit 참조 제거
    /// </summary>
    public void ClearBattleUnit()
    {
        _battleUnit = null;
    }

    /// <summary>
    /// 적 전투 데이터 연결
    /// </summary>
    /// <param name="enemyBattleData">연결할 적 전투 데이터</param>
    public void InitializeEnemyData(EnemyBattleData enemyBattleData)
    {
        _teamType = BattleTeamType.Enemy;
        _enemyBattleData = enemyBattleData;

        SpawnVisualPrefab();
    }

    // 추가 프리팹 생성

    /// <summary>
    /// 적 외형 프리팹 생성
    /// </summary>
    private void SpawnVisualPrefab()
    {
        if (_enemyBattleData.Prefab == null)
        {
            Debug.LogWarning($"[BattleActor] {_enemyBattleData.name}에 연결된 몬스터 프리팹이 없습니다.");
            return;
        }

        if (_visualRoot == null)
        {
            _visualRoot = transform;
        }

        GameObject visualInstance = Instantiate(_enemyBattleData.Prefab, _visualRoot);

        visualInstance.transform.localPosition = new Vector3(0f, -1f, 0f);
        visualInstance.transform.localRotation = Quaternion.identity;

        if (_enemyBattleData.IsBoss)
        {
            visualInstance.transform.localPosition = new Vector3(0f, -1f, -1.5f);
            visualInstance.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
        }

        SetupOutlineForVisual(visualInstance);
        PositionWorldSpaceCanvas(visualInstance);

        Debug.Log($"[BattleActor] 적 외형 프리팹 생성 완료: {_enemyBattleData.Prefab.name}");
    }

    /// <summary>
    /// 스폰된 비주얼(EnemyBattleData.Prefab 인스턴스)의 렌더러 Bounds를 합산해서,
    /// 그 꼭대기(max.y) + 오프셋 위치로 EnemyWorldSpaceCanvas를 배치.
    /// 스폰 시점에 한 번만 계산 (이후 애니메이션에 따른 흔들림은 반영하지 않음).
    /// </summary>
    private void PositionWorldSpaceCanvas(GameObject visualInstance)
    {
        if (_worldSpaceCanvasTransform == null) return;

        Renderer[] renderers = visualInstance.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 canvasPosition = _worldSpaceCanvasTransform.position;
        canvasPosition.y = combinedBounds.max.y + _canvasHeightOffset;

        _worldSpaceCanvasTransform.position = canvasPosition;
    }

    /// <summary>
    /// 생성된 비주얼에 Outlinable 컴포넌트를 세팅하고 하위 메쉬들을 자동 등록한다.
    /// </summary>
    private void SetupOutlineForVisual(GameObject visualInstance)
    {
        // 1. BattleActor 루트에 Outlinable 컴포넌트가 있는지 확인, 없으면 추가
        Outlinable outlinable = GetComponent<Outlinable>();
        if (outlinable == null)
        {
            outlinable = gameObject.AddComponent<Outlinable>();
        }

        // 2. 에셋 내장 함수를 사용하여 깊은 자식 계층의 SkinnedMeshRenderer와 MeshRenderer를 모두 자동 등록
        outlinable.AddAllChildRenderersToRenderingList(
            RenderersAddingMode.SkinnedMeshRenderer | RenderersAddingMode.MeshRenderer
        );
    }
}