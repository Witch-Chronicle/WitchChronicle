using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 거점/던전 필드 파티 생성 관리
/// </summary>
public class FieldPartySpawner : MonoBehaviour
{
    [Header("Party Prefab")]
    [SerializeField] private Party _partyPrefab;

    [Header("Spawn")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private bool _spawnOnStart = true;

    [Header("Formation")]
    [SerializeField] private float _memberSideSpacing = 1.2f;
    [SerializeField] private float _memberBackDistance = 1.5f;

    [Header("Camera")]
    [SerializeField] private string _cameraRootChildName = "CameraRoot";

    private readonly List<PersistentCharacterUnit> _activeParty = new List<PersistentCharacterUnit>();
    private readonly List<StatController> _spawnedMembers = new List<StatController>();

    //// <summary>
    //// 시작 시 파티 생성
    ////</summary>
    private void Start()
    {
        if (_spawnOnStart && IsReturningFromBattle() == false)
        {
            SpawnAtSpawnPoint();
        }
    }

    /// <summary>
    /// 전투 복귀 상황인지 확인 (전투 복귀 시에는 ResultController가 복귀 위치로 직접 스폰함)
    /// </summary>
    private bool IsReturningFromBattle()
    {
        return BattleEncounterContext.Instance != null && BattleEncounterContext.Instance.HasEncounter;
    }

    /// <summary>
    /// 스폰 포인트 기준 파티 생성
    /// </summary>
    public void SpawnAtSpawnPoint()
    {
        Vector3 position = _spawnPoint != null
            ? _spawnPoint.position
            : transform.position;

        Quaternion rotation = _spawnPoint != null
            ? _spawnPoint.rotation
            : Quaternion.identity;

        SpawnParty(position, rotation);
    }

    /// <summary>
    /// 지정 위치에 필드 파티 생성
    /// </summary>
    /// <param name="position">생성 위치</param>
    /// <param name="rotation">생성 회전</param>
    public void SpawnParty(Vector3 position, Quaternion rotation)
    {
        if (PersistentCharacterManager.Instance == null)
        {
            Debug.LogWarning("[FieldPartySpawner] PersistentCharacterManager.Instance 없음");
            return;
        }

        if (EnsurePartyInstance(position, rotation) == false)
        {
            return;
        }

        GameObject partyObject = Party.Instance.gameObject;
        CinemachineCamera freeLookCamera = partyObject.GetComponentInChildren<CinemachineCamera>();

        SpawnFieldMembers(partyObject.transform, position, rotation, freeLookCamera);
    }

    /// <summary>
    /// Party 인스턴스 보장
    /// </summary>
    /// <param name="position">생성 위치</param>
    /// <param name="rotation">생성 회전</param>
    /// <returns>Party 인스턴스 존재 여부</returns>
    private bool EnsurePartyInstance(Vector3 position, Quaternion rotation)
    {
        if (Party.Instance != null)
        {
            Party.Instance.transform.SetPositionAndRotation(position, rotation);
            return true;
        }

        if (_partyPrefab == null)
        {
            Debug.LogWarning("[FieldPartySpawner] Party 프리팹 없음");
            return false;
        }

        Instantiate(_partyPrefab, position, rotation);

        if (Party.Instance == null)
        {
            Debug.LogWarning("[FieldPartySpawner] Party 생성 실패");
            return false;
        }

        return true;
    }

    /// <summary>
    /// ActiveParty 기준 필드 멤버 생성
    /// </summary>
    /// <param name="parent">생성 부모</param>
    /// <param name="position">기준 위치</param>
    /// <param name="rotation">기준 회전</param>
    /// <param name="freeLookCamera">연결 카메라</param>
    private void SpawnFieldMembers(
        Transform parent,
        Vector3 position,
        Quaternion rotation,
        CinemachineCamera freeLookCamera)
    {
        _activeParty.Clear();
        _spawnedMembers.Clear();

        PersistentCharacterManager.Instance.GetActivePartyMembers(_activeParty);

        Transform leaderTransform = null;

        for (int i = 0; i < _activeParty.Count; i++)
        {
            PersistentCharacterUnit unit = _activeParty[i];

            if (unit == null)
            {
                continue;
            }

            if (unit.HasFieldActorPrefab() == false)
            {
                Debug.LogWarning($"[FieldPartySpawner] {unit.CharacterName} FieldActorPrefab 없음");
                continue;
            }

            GameObject instance = CreateFieldActor(unit, i, position, rotation, parent);

            if (instance == null)
            {
                continue;
            }

            StatController statController = instance.GetComponent<StatController>();

            if (statController == null)
            {
                Debug.LogWarning($"[FieldPartySpawner] {unit.CharacterName} FieldActorPrefab에 StatController 없음");
                continue;
            }

            _spawnedMembers.Add(statController);

            if (i == 0)
            {
                leaderTransform = instance.transform;
                instance.tag = "Player";
                LinkFreeLookCamera(freeLookCamera, leaderTransform);
            }
            else
            {
                if (instance.CompareTag("Player"))
                {
                    instance.tag = "Untagged";
                }

                LinkFollower(instance, leaderTransform, unit.CharacterName);
            }
        }

        Party.Instance.SetMembers(_spawnedMembers);
    }

    /// <summary>
    /// 필드 Actor 생성
    /// </summary>
    /// <param name="unit">원본 캐릭터 데이터</param>
    /// <param name="index">파티 인덱스</param>
    /// <param name="position">기준 위치</param>
    /// <param name="rotation">기준 회전</param>
    /// <param name="parent">생성 부모</param>
    /// <returns>생성 Actor</returns>
    private GameObject CreateFieldActor(
        PersistentCharacterUnit unit,
        int index,
        Vector3 position,
        Quaternion rotation,
        Transform parent)
    {
        Vector3 spawnPosition = GetMemberSpawnPosition(index, position, rotation);

        GameObject instance = Instantiate(
            unit.FieldActorPrefab,
            spawnPosition,
            rotation,
            parent);

        instance.name = unit.CharacterId;

        BindFieldMember(instance, unit);

        return instance;
    }

    /// <summary>
    /// 파티 멤버 생성 위치 계산
    /// </summary>
    /// <param name="index">파티 인덱스</param>
    /// <param name="position">기준 위치</param>
    /// <param name="rotation">기준 회전</param>
    /// <returns>계산 위치</returns>
    private Vector3 GetMemberSpawnPosition(int index, Vector3 position, Quaternion rotation)
    {
        if (index == 0)
        {
            return position;
        }

        Vector3 offset = rotation * new Vector3(
            (index - 2) * _memberSideSpacing,
            0f,
            -_memberBackDistance);

        return position + offset;
    }

    /// <summary>
    /// 추종자 앵커 연결
    /// </summary>
    /// <param name="instance">추종자 오브젝트</param>
    /// <param name="leaderTransform">리더 Transform</param>
    /// <param name="characterName">캐릭터 이름</param>
    private void LinkFollower(GameObject instance, Transform leaderTransform, string characterName)
    {
        if (leaderTransform == null)
        {
            return;
        }

        NpcFollower follower = instance.GetComponent<NpcFollower>();

        if (follower == null)
        {
            Debug.LogWarning($"[FieldPartySpawner] {characterName} FieldActorPrefab에 NpcFollower 없음");
            return;
        }

        follower.SetAnchor(leaderTransform);
    }

    /// <summary>
    /// FreeLook Camera 추적 대상 연결
    /// </summary>
    /// <param name="freeLookCamera">연결 카메라</param>
    /// <param name="leaderTransform">리더 Transform</param>
    private void LinkFreeLookCamera(CinemachineCamera freeLookCamera, Transform leaderTransform)
    {
        if (freeLookCamera == null)
        {
            Debug.LogWarning("[FieldPartySpawner] PartyManager 하위에서 CinemachineCamera를 찾지 못함");
            return;
        }

        Transform cameraRoot = leaderTransform.Find(_cameraRootChildName);

        if (cameraRoot == null)
        {
            Debug.LogWarning($"[FieldPartySpawner] 리더 하위에 {_cameraRootChildName} 없음");
            return;
        }

        freeLookCamera.Follow = cameraRoot;
        freeLookCamera.LookAt = cameraRoot;
    }

    /// <summary>
    /// 필드 멤버와 유지 캐릭터 데이터 연결
    /// </summary>
    /// <param name="fieldObject">필드 캐릭터 오브젝트</param>
    /// <param name="unit">원본 캐릭터 데이터</param>
    private void BindFieldMember(GameObject fieldObject, PersistentCharacterUnit unit)
    {
        if (fieldObject == null)
        {
            return;
        }

        PartyFieldMember fieldMember = fieldObject.GetComponent<PartyFieldMember>();

        if (fieldMember == null)
        {
            fieldMember = fieldObject.AddComponent<PartyFieldMember>();
        }

        fieldMember.Bind(unit);
    }
}