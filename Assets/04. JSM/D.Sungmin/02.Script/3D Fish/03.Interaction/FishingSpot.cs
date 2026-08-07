using UnityEngine;

public class FishingSpot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform sitPoint;
    [SerializeField] private GameObject promptUI;
    [SerializeField] private FishingCameraController cameraController;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private string playerTag = "Player";

    private GameObject player;
    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;
    private bool isPlayerInRange;
    private bool isFishing;

    // 낚시 중 잠글 이동 스크립트들.
    // StarterAssets(ThirdPersonController)와 우리 PlayerController를 모두 지원한다.
    private readonly System.Collections.Generic.List<MonoBehaviour> lockedMovers =
        new System.Collections.Generic.List<MonoBehaviour>();

    private static readonly string[] MoverTypeNames =
    {
        "PlayerController",        // 우리 캐릭터
        "ThirdPersonController",   // StarterAssets
        "StarterAssetsInputs",
    };

    /// <summary>플레이어의 이동 스크립트를 찾아 끄고, 나중에 되살릴 수 있게 기억한다.</summary>
    private void LockPlayerMovement()
    {
        lockedMovers.Clear();

        for (int i = 0; i < MoverTypeNames.Length; i++)
        {
            MonoBehaviour mb = player.GetComponent(MoverTypeNames[i]) as MonoBehaviour;

            if (mb != null && mb.enabled)
            {
                mb.enabled = false;
                lockedMovers.Add(mb);
            }
        }
    }

    /// <summary>LockPlayerMovement로 껐던 것들만 다시 켠다.</summary>
    private void UnlockPlayerMovement()
    {
        for (int i = 0; i < lockedMovers.Count; i++)
        {
            if (lockedMovers[i] != null)
            {
                lockedMovers[i].enabled = true;
            }
        }

        lockedMovers.Clear();
    }

    // private void Start()
    // {
    //     if (promptUI != null) promptUI.SetActive(false);
    // }

    public void HandlePlayerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) || isFishing) return;

        player = other.gameObject;
        isPlayerInRange = true;
        promptUI.SetActive(true);
    }

    public void HandlePlayerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        isPlayerInRange = false;
        if (!isFishing) promptUI.SetActive(false);
    }

    private void Update()
    {
        if (!isPlayerInRange || isFishing) return;
        if (!Input.GetKeyDown(interactKey)) return;

        EnterFishing();
    }

    private void EnterFishing()
    {
        isFishing = true;
        promptUI.SetActive(false);

        // 원위치 저장
        originalPlayerPosition = player.transform.position;
        originalPlayerRotation = player.transform.rotation;

        // 이동 컨트롤러 스크립트 찾아서 비활성화
        LockPlayerMovement();

        // CharacterController 잠깐 끄고 SitPoint로 순간이동
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.SetPositionAndRotation(sitPoint.position, sitPoint.rotation);

        if (cc != null) cc.enabled = true;

        cameraController.SwitchToFishing();

        // 플레이어는 런타임에 생성되므로 애니메이터 훅도 여기서 연결한다
        FishingManager.Instance.BindAnimatorHook(
            player.GetComponentInChildren<WitchChronicle.Fishing.FishingAnimatorHook>());

        FishingManager.Instance.EnterFishing(this);
    }

    public void ExitFishing()
    {
        isFishing = false;

        cameraController.SwitchToMain();

        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.SetPositionAndRotation(originalPlayerPosition, originalPlayerRotation);

            if (cc != null) cc.enabled = true;

            // 이동 컨트롤러 다시 활성화
            UnlockPlayerMovement();
        }

        if (isPlayerInRange) promptUI.SetActive(true);
    }
}