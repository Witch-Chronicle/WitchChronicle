using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 필드 캐릭터의 무기 모델을 현재 씬에 따라 표시/숨김.
/// 던전에서는 무기를 들고, 거점(Main)에서는 숨긴다.
///
/// FieldPartySpawner가 씬마다 FieldActorPrefab을 새로 Instantiate 하므로
/// Start()에서 한 번만 판정하면 충분하다.
/// (Party에 DontDestroyOnLoad를 다시 켜서 파티가 씬 전환에도 살아남게 바뀌면,
///  SceneManager.sceneLoaded 구독을 추가해야 한다.)
/// </summary>
public class FieldWeaponVisibility : MonoBehaviour
{
    [Header("무기 모델 루트")]
    [Tooltip("손 본 하위에 배치한 무기 오브젝트. 이 오브젝트만 켜고 끈다.")]
    [SerializeField] private GameObject _weaponRoot;

    [Header("무기를 드는 씬")]
    [SerializeField] private SceneId _armedScene = SceneId.Dungeon;

    private void Start()
    {
        if (_weaponRoot == null)
        {
            Debug.LogWarning($"[FieldWeaponVisibility] {name} 무기 루트가 비어 있음");
            return;
        }

        bool isArmedScene = SceneManager.GetActiveScene().name == _armedScene.ToString();
        _weaponRoot.SetActive(isArmedScene);
    }
}
