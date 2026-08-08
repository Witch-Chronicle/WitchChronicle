using System.Collections;
using UnityEngine;

/// <summary>
/// 이 컴포넌트가 배치된 씬이 자신의 초기화(Awake/Start)를 끝냈음을
/// SceneTransitionManager에게 알린다. LoadSceneWithLoading(waitForReadySignal: true)로
/// 진입하는 모든 씬(Main, Dungeon 등)에 하나씩 배치해야 한다.
/// </summary>
public class SceneReadySignal : MonoBehaviour
{
    [Tooltip("이 프레임 수만큼 더 기다린 뒤 Ready를 보고한다(마지막 초기화 잔여 프레임 여유용)")]
    [SerializeField] private int _extraFramesToWait = 2;

    private IEnumerator Start()
    {
        for (int i = 0; i < _extraFramesToWait; i++)
        {
            yield return null;
        }

        SceneTransitionManager.Instance?.ReportSceneReady();
    }
}