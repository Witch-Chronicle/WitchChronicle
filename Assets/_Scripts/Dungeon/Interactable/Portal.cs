using UnityEngine;

public class Portal : MonoBehaviour, ITFInteractable
{
    [SerializeField] private ParticleSystem _particle;

    public string Prompt => "[F] 던전 나가기";


    public void Interact(GameObject interactor)
    {
        ClearDungeon();

        _particle.Play();

        SceneTransitionManager.Instance.LoadSceneWithLoading(SceneId.Main, waitForReadySignal: true);

        ShowMessageManager.Instance.ShowMessage("거점으로 돌아갑니다");
    }

    private void ClearDungeon()
    {
        DungeonController dungeonController = FindAnyObjectByType<DungeonController>();

        if (dungeonController == null)
        {
            Debug.LogWarning("DungeonController Missing");

            return;
        }

        dungeonController.ClearDungeon();
    }
}
