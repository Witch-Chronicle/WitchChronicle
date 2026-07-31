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

    private MonoBehaviour thirdPersonController;
    private MonoBehaviour starterAssetsInputs;

    private void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

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
        thirdPersonController = player.GetComponent("ThirdPersonController") as MonoBehaviour;
        starterAssetsInputs = player.GetComponent("StarterAssetsInputs") as MonoBehaviour;
        if (thirdPersonController != null) thirdPersonController.enabled = false;
        if (starterAssetsInputs != null) starterAssetsInputs.enabled = false;

        // CharacterController 잠깐 끄고 SitPoint로 순간이동
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.SetPositionAndRotation(sitPoint.position, sitPoint.rotation);

        if (cc != null) cc.enabled = true;

        cameraController.SwitchToFishing();
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
            if (thirdPersonController != null) thirdPersonController.enabled = true;
            if (starterAssetsInputs != null) starterAssetsInputs.enabled = true;
        }

        if (isPlayerInRange) promptUI.SetActive(true);
    }
}