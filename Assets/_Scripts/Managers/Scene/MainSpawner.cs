using Unity.Cinemachine;
using System.Collections.Generic;
using UnityEngine;

public class MainSpawner : MonoBehaviour
{
    [Header("Party Prefab")]
    [SerializeField] private Party _partyPrefab;

    [Header("Spawn Point")]
    [SerializeField] private Transform _spawnPoint;

    [Header("Camera")]
    [SerializeField] private string _cameraRootChildName = "CameraRoot"; // 리더 하위 오브젝트 이름

    private void Start()
    {
        SpawnParty();
    }

    private void SpawnParty()
    {
        if (PersistentCharacterManager.Instance == null)
        {
            Debug.LogWarning("PersistentCharacterManager.Instance가 없습니다.");
            return;
        }

        Vector3 position = _spawnPoint != null ? _spawnPoint.position : transform.position;
        Quaternion rotation = _spawnPoint != null ? _spawnPoint.rotation : Quaternion.identity;

        if (Party.Instance == null)
        {
            if (_partyPrefab == null)
            {
                Debug.LogWarning("PartyManager(Party) 프리팹이 지정되지 않았습니다.");
                return;
            }

            Instantiate(_partyPrefab, position, rotation);
        }

        if (Party.Instance == null)
        {
            Debug.LogWarning("PartyManager(Party) 생성에 실패했습니다.");
            return;
        }

        GameObject memberContainer = Party.Instance.gameObject;

        // PartyManager 프리팹 자식으로 같이 생성된 FreeLook Camera를 찾음
        CinemachineCamera freeLookCamera = memberContainer.GetComponentInChildren<CinemachineCamera>();

        List<PersistentCharacterUnit> activeParty = new List<PersistentCharacterUnit>();
        PersistentCharacterManager.Instance.GetActivePartyMembers(activeParty);

        List<StatController> spawnedMembers = new List<StatController>();
        Transform leaderTransform = null;

        for (int i = 0; i < activeParty.Count; i++)
        {
            PersistentCharacterUnit unit = activeParty[i];

            if (unit.HasFieldActorPrefab() == false)
            {
                Debug.LogWarning($"{unit.CharacterName}의 FieldActorPrefab이 없습니다.");
                continue;
            }

            Vector3 offset = (i == 0) ? Vector3.zero : rotation * new Vector3((i - 2) * 1.2f, 0f, -1.5f);
            GameObject instance = Instantiate(unit.FieldActorPrefab, position + offset, rotation, memberContainer.transform);
            instance.name = unit.CharacterId;

            StatController statController = instance.GetComponent<StatController>();
            if (statController == null)
            {
                Debug.LogWarning($"{unit.CharacterName}의 FieldActorPrefab에 StatController가 없습니다.");
                continue;
            }

            spawnedMembers.Add(statController);

            if (i == 0)
            {
                leaderTransform = instance.transform;
                LinkFreeLookCamera(freeLookCamera, leaderTransform);
            }
            else
            {
                NpcFollower follower = instance.GetComponent<NpcFollower>();
                if (follower != null && leaderTransform != null)
                {
                    follower.SetAnchor(leaderTransform);
                }
                else if (follower == null)
                {
                    Debug.LogWarning($"{unit.CharacterName}의 FieldActorPrefab에 NpcFollower가 없습니다.");
                }
            }
        }

        Party.Instance.SetMembers(spawnedMembers);
    }

    /// <summary>
    /// 리더 하위의 CameraRoot를 FreeLook Camera의 추적 대상으로 연결
    /// </summary>
    private void LinkFreeLookCamera(CinemachineCamera freeLookCamera, Transform leaderTransform)
    {
        if (freeLookCamera == null)
        {
            Debug.LogWarning("PartyManager 하위에서 FreeLook Camera(CinemachineCamera)를 찾지 못했습니다.");
            return;
        }

        Transform cameraRoot = leaderTransform.Find(_cameraRootChildName);
        if (cameraRoot == null)
        {
            Debug.LogWarning($"리더 하위에 {_cameraRootChildName}이 없습니다.");
            return;
        }

        freeLookCamera.Follow = cameraRoot;
        freeLookCamera.LookAt = cameraRoot;
    }
}