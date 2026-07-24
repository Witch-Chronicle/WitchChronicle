using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// WizardAnimations 클립으로 전투용 Animator Controller를 자동 생성하는 툴.
/// 메뉴: Tools/KDH/전투 애니메이터 컨트롤러 생성
/// 생성 위치: Assets/02.KDH/Animations/BattleAnimator.controller
/// </summary>
public static class BattleAnimatorBuilder
{
    private const string OutputFolder = "Assets/02.KDH/Animations";
    private const string OutputPath = OutputFolder + "/BattleAnimator.controller";

    [MenuItem("Tools/KDH/전투 애니메이터 컨트롤러 생성")]
    private static void Build()
    {
        // 1. 필요한 클립 로드
        AnimationClip idle = FindClip("ANIM_IP_idle_01");
        AnimationClip attack1 = FindClip("ANIM_IP_attack_01");
        AnimationClip attack2 = FindClip("ANIM_IP_attack_02");
        AnimationClip attack3 = FindClip("ANIM_IP_attack_03");
        AnimationClip skill = FindClip("ANIM_IP_buff");
        AnimationClip hit = FindClip("ANIM_IP_hit_F");
        AnimationClip parry = FindClip("ANIM_IP_block_hit");
        AnimationClip death = FindClip("ANIM_RM_death");
        AnimationClip victory = FindClip("ANIM_RM_taunt_01");

        if (idle == null || attack1 == null || death == null)
        {
            Debug.LogError("[BattleAnimatorBuilder] 필수 클립을 찾지 못했습니다. WizardAnimations 폴더를 확인하세요.");
            return;
        }

        // 2. 컨트롤러 생성
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/02.KDH", "Animations");

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(OutputPath);

        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
        controller.AddParameter("Skill", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Parry", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Victory", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // 3. 상태 생성
        AnimatorState idleState = AddState(sm, "Idle", idle, new Vector3(0, 0));
        AnimatorState attackState1 = AddState(sm, "Attack_01", attack1, new Vector3(300, -120));
        AnimatorState attackState2 = AddState(sm, "Attack_02", attack2, new Vector3(300, -60));
        AnimatorState attackState3 = AddState(sm, "Attack_03", attack3, new Vector3(300, 0));
        AnimatorState skillState = AddState(sm, "Skill", skill, new Vector3(300, 60));
        AnimatorState parryState = AddState(sm, "Parry", parry, new Vector3(300, 120));
        AnimatorState victoryState = AddState(sm, "Victory", victory, new Vector3(300, 180));
        AnimatorState hitState = AddState(sm, "Hit", hit, new Vector3(-300, 60));
        // Death는 나가는 전이 없음: 비루프 클립이라 마지막 프레임(쓰러진 자세)에서 멈춘다.
        AnimatorState deathState = AddState(sm, "Death", death, new Vector3(-300, 150));

        sm.defaultState = idleState;

        // 4. 전이 연결
        // 공격: Attack 트리거 + AttackIndex로 3종 분기
        AddAttackTransition(idleState, attackState1, 0);
        AddAttackTransition(idleState, attackState2, 1);
        AddAttackTransition(idleState, attackState3, 2);
        AddReturnToIdle(attackState1, idleState);
        AddReturnToIdle(attackState2, idleState);
        AddReturnToIdle(attackState3, idleState);

        AddTriggerTransition(idleState, skillState, "Skill");
        AddReturnToIdle(skillState, idleState);

        AddTriggerTransition(idleState, parryState, "Parry");
        AddReturnToIdle(parryState, idleState);

        AddTriggerTransition(idleState, victoryState, "Victory");
        AddReturnToIdle(victoryState, idleState);

        // 피격/사망: 어떤 상태에서든 반응 (AnyState)
        AnimatorStateTransition anyToHit = sm.AddAnyStateTransition(hitState);
        anyToHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        anyToHit.duration = 0.1f;
        anyToHit.canTransitionToSelf = false;
        AddReturnToIdle(hitState, idleState);

        AnimatorStateTransition anyToDeath = sm.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");
        anyToDeath.duration = 0.1f;
        anyToDeath.canTransitionToSelf = false;

        AssetDatabase.SaveAssets();
        Selection.activeObject = controller;
        Debug.Log("[BattleAnimatorBuilder] 생성 완료: " + OutputPath);
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip, Vector3 position)
    {
        AnimatorState state = sm.AddState(name, position);
        state.motion = clip;
        state.iKOnFeet = true; // 발 미끄러짐 방지 (Foot IK)
        return state;
    }

    private static void AddAttackTransition(AnimatorState from, AnimatorState to, int attackIndex)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        t.AddCondition(AnimatorConditionMode.Equals, attackIndex, "AttackIndex");
        t.hasExitTime = false;
        t.duration = 0.1f;
    }

    private static void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.hasExitTime = false;
        t.duration = 0.1f;
    }

    private static void AddReturnToIdle(AnimatorState from, AnimatorState idle)
    {
        AnimatorStateTransition t = from.AddTransition(idle);
        t.hasExitTime = true;
        t.exitTime = 0.95f;
        t.duration = 0.25f;
    }

    private static AnimationClip FindClip(string fbxName)
    {
        string[] guids = AssetDatabase.FindAssets(fbxName + " t:Model", new[] { "Assets/_Resources/WizardAnimations" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!System.IO.Path.GetFileNameWithoutExtension(path).Equals(fbxName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip != null)
                return clip;
        }

        Debug.LogWarning("[BattleAnimatorBuilder] 클립을 찾지 못했습니다: " + fbxName);
        return null;
    }
}
