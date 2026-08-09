using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// NPC별 Animator Controller를 만들어주는 에디터 도구.
///
/// 메뉴: Tools > Witch Chronicle > NPC 애니메이터 생성
///
/// 만들어지는 구조
///   파라미터 : Greet (Trigger)
///   상태     : Idle (기본), Greet
///   전이     : Idle -> Greet  (조건 Greet, 즉시)
///              Greet -> Idle  (Exit Time)
///
/// Idle에는 프로젝트에 있는 대기 클립을 임시로 넣어둔다.
/// 인사 클립은 아직 없으므로 Greet 상태는 비워두며, 나중에 클립을 드래그해서 채우면 된다.
/// NPC마다 컨트롤러가 따로라서 인사 동작을 개별로 다르게 줄 수 있다.
/// </summary>
public static class NpcAnimatorBuilder
{
    private const string OutputFolder = "Assets/02.KDH/Character/Model/Animations/NPC";

    // 임시 Idle 클립. 마음에 안 들면 컨트롤러에서 교체하면 된다.
    private const string DefaultIdlePath =
        "Assets/_Resources/WizardAnimations/Animations/Idle/ANIM_IP_idle_01.FBX";

    private static readonly string[] NpcNames =
    {
        "ShopKeeper",
        "EnhanceNPC",
        "Cassandra",
        "FarmNPC",
    };

    [MenuItem("Tools/Witch Chronicle/NPC 애니메이터 생성")]
    private static void Build()
    {
        EnsureFolder();

        AnimationClip idleClip = LoadFirstClip(DefaultIdlePath);

        if (idleClip == null)
        {
            Debug.LogWarning($"[NpcAnimatorBuilder] 기본 Idle 클립을 찾지 못했습니다: {DefaultIdlePath}\n" +
                             "컨트롤러는 만들되 Idle은 비워둡니다.");
        }

        List<string> created = new List<string>();

        foreach (string npcName in NpcNames)
        {
            string path = $"{OutputFolder}/NPC_{npcName}.controller";

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            {
                Debug.Log($"[NpcAnimatorBuilder] 이미 있어서 건너뜁니다: {path}");
                continue;
            }

            CreateController(path, idleClip);
            created.Add(path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (created.Count == 0)
        {
            Debug.Log("[NpcAnimatorBuilder] 새로 만든 컨트롤러가 없습니다.");
            return;
        }

        Debug.Log($"[NpcAnimatorBuilder] 컨트롤러 {created.Count}개 생성:\n  " + string.Join("\n  ", created));
    }

    /// <summary>
    /// Idle / Greet 두 상태를 가진 컨트롤러를 만든다.
    /// </summary>
    /// <param name="path">저장 경로</param>
    /// <param name="idleClip">Idle에 넣을 클립 (없으면 비움)</param>
    private static void CreateController(string path, AnimationClip idleClip)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Greet", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState idle = stateMachine.AddState("Idle");
        idle.motion = idleClip;
        stateMachine.defaultState = idle;

        AnimatorState greet = stateMachine.AddState("Greet");
        // 인사 클립은 아직 없다. 나중에 이 상태의 Motion에 드래그하면 된다.

        AnimatorStateTransition toGreet = idle.AddTransition(greet);
        toGreet.hasExitTime = false;
        toGreet.duration = 0.15f;
        toGreet.AddCondition(AnimatorConditionMode.If, 0f, "Greet");

        AnimatorStateTransition toIdle = greet.AddTransition(idle);
        toIdle.hasExitTime = true;
        toIdle.exitTime = 0.9f;
        toIdle.duration = 0.2f;
    }

    /// <summary>
    /// FBX 안의 첫 번째 AnimationClip을 가져온다.
    /// </summary>
    /// <param name="assetPath">FBX 경로</param>
    /// <returns>찾은 클립 (없으면 null)</returns>
    private static AnimationClip LoadFirstClip(string assetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

        foreach (Object asset in assets)
        {
            AnimationClip clip = asset as AnimationClip;

            // Unity가 내부적으로 만드는 미리보기 클립은 제외
            if (clip != null && clip.name.StartsWith("__preview__") == false)
            {
                return clip;
            }
        }

        return null;
    }

    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(OutputFolder))
        {
            return;
        }

        string parent = "Assets/02.KDH/Character/Model/Animations";

        if (AssetDatabase.IsValidFolder(parent) == false)
        {
            Debug.LogError($"[NpcAnimatorBuilder] 상위 폴더가 없습니다: {parent}");
            return;
        }

        AssetDatabase.CreateFolder(parent, "NPC");
    }
}
