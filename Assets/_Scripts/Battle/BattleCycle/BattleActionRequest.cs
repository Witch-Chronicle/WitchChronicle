/// <summary>
/// 전투 중 한 유닛이 실행하려는 행동 요청
/// UI, AI, 자동 전투 시스템은 이 요청을 생성 후 BattleCycleController가 실행
/// </summary>
public class BattleActionRequest
{
    private readonly BattleUnit _actor;
    private readonly BattleUnit _target;
    private readonly CommandType _commandType;
    private readonly SkillData _skillData;
    private readonly float _damageMultiplier;

    public BattleUnit Actor => _actor;
    public BattleUnit Target => _target;
    public CommandType CommandType => _commandType;
    public SkillData SkillData => _skillData;

    /// <summary>
    /// 스킬 데미지에 곱해지는 배율. 기본 1f (배율 없음).
    /// 마법진 그리기(SkillDrawController) 판정 결과 등에서 사용.
    /// </summary>
    public float DamageMultiplier => _damageMultiplier;

    public bool HasTarget => _target != null;
    public bool HasSkill => _skillData != null;

    /// <summary>
    /// 전투 행동 요청을 생성.
    /// </summary>
    /// <param name="actor">행동을 실행하는 유닛</param>
    /// <param name="target">행동 대상 유닛</param>
    /// <param name="commandType">실행할 커맨드 타입</param>
    /// <param name="skillData">사용할 스킬 데이터</param>
    /// <param name="damageMultiplier">데미지 배율 (기본 1f)</param>
    private BattleActionRequest(
        BattleUnit actor,
        BattleUnit target,
        CommandType commandType,
        SkillData skillData,
        float damageMultiplier = 1f)
    {
        _actor = actor;
        _target = target;
        _commandType = commandType;
        _skillData = skillData;
        _damageMultiplier = damageMultiplier;
    }

    /// <summary>
    /// 기본 공격 요청을 생성
    /// </summary>
    /// <param name="actor">공격하는 유닛</param>
    /// <param name="target">공격 대상 유닛</param>
    /// <returns>기본 공격 행동 요청</returns>
    public static BattleActionRequest CreateAttack(BattleUnit actor, BattleUnit target)
    {
        return new BattleActionRequest(
            actor,
            target,
            CommandType.Attack,
            null);
    }

    /// <summary>
    /// 스킬 사용 요청을 생성
    /// </summary>
    /// <param name="actor">스킬을 사용하는 유닛</param>
    /// <param name="skillData">사용할 스킬 데이터</param>
    /// <param name="target">스킬 대상 유닛</param>
    /// <param name="damageMultiplier">데미지 배율 (기본 1f, 마법진 그리기 판정 결과 등)</param>
    /// <returns>스킬 사용 행동 요청</returns>
    public static BattleActionRequest CreateSkill(
        BattleUnit actor,
        SkillData skillData,
        BattleUnit target,
        float damageMultiplier = 1f)
    {
        return new BattleActionRequest(
            actor,
            target,
            CommandType.Skill,
            skillData,
            damageMultiplier);
    }

    /// <summary>
    /// 방어 요청을 생성
    /// </summary>
    /// <param name="actor">방어할 유닛</param>
    /// <returns>방어 행동 요청</returns>
    public static BattleActionRequest CreateDefense(BattleUnit actor)
    {
        return new BattleActionRequest(
            actor,
            null,
            CommandType.Defense,
            null);
    }

    /// <summary>
    /// 아이템 사용 요청을 생성
    /// </summary>
    /// <param name="actor">아이템을 사용할 유닛</param>
    /// <returns>아이템 사용 행동 요청</returns>
    public static BattleActionRequest CreateUsingItem(BattleUnit actor)
    {
        return new BattleActionRequest(
            actor,
            null,
            CommandType.Item,
            null);
    }

    /// <summary>
    /// 도망 요청을 생성
    /// </summary>
    /// <param name="actor">도망을 시도하는 유닛</param>
    /// <returns>도망 행동 요청</returns>
    public static BattleActionRequest CreateEscape(BattleUnit actor)
    {
        return new BattleActionRequest(
            actor,
            null,
            CommandType.Escape,
            null);
    }
}