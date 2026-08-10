// FILE: Assets\_Scripts\Dungeon\Interactable\EventGameObject.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상호작용 가능한 이벤트 오브젝트를 관리하고 일회성 실행을 보장하는 클래스입니다.
/// </summary>
public class EventGameObject : MonoBehaviour, ITFInteractable
{
    [SerializeField] private EventDataSO _eventData;
    private bool _isInteracted = false;

    public string Prompt
    {
        get
        {
            if (_isInteracted)
            {
                // 함정(미믹)이었던 경우 전투로 넘어가므로 상호작용 문구를 아예 비움
                if (_eventData != null && _eventData.Type == Event_Type.Trap)
                {
                    return string.Empty;
                }

                return "이미 조사했다...";
            }

            return "[F] 조사하기";
        }
    }

    /// <summary>
    /// 플레이어가 상호작용을 수행할 때 호출됩니다.
    /// </summary>
    public void Interact(GameObject interator)
    {
        if (_isInteracted == true)
        {
            return;
        }

        _isInteracted = true;

        // 함정(미믹) 상호작용 시 더 이상 플레이어 센서에 안 잡히도록 콜라이더 비활성화
        if (_eventData != null && _eventData.Type == Event_Type.Trap)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }

        ExecuteEventEffect();

        Debug.Log("[EventGameObject] 이벤트 상호작용이 완료되어 더 이상 활성화되지 않습니다.");
    }

    /// <summary>
    /// 외부에서 이벤트 데이터를 주입받아 초기화합니다.
    /// </summary>
    public void Setup(EventDataSO eventData)
    {
        if (eventData != null)
        {
            _eventData = eventData;
        }

        _isInteracted = false;

        Debug.Log($"[EventGameObject] 이벤트 설정 완료: {_eventData?.EventName} (타입: {_eventData?.Type})");
    }

    /// <summary>
    /// 이벤트 타입에 따라 적절한 효과를 분기 처리합니다.
    /// </summary>
    private void ExecuteEventEffect()
    {
        if (_eventData == null)
        {
            Debug.LogWarning("[EventGameObject] 실행 시점에 _eventData가 null입니다.");
            return;
        }

        switch (_eventData.Type)
        {
            case Event_Type.Reward:
                ApplyReward();
                SpawnVisualEffect();
                break;
            case Event_Type.Trap:
                ApplyTrap();
                SpawnVisualEffect();
                break;
            case Event_Type.Debuff:
                ApplyDebuff();
                SpawnVisualEffect();
                break;
        }
    }

    // 💡 [수정] 보상 이벤트 발생 시 AlertManager 팝업 띄우기
    private void ApplyReward()
    {
        string message = !string.IsNullOrEmpty(_eventData.Description) ? _eventData.Description : "이벤트 보상을 획득했습니다!";

        if (ShowMessageManager.Instance != null)
        {
            ShowMessageManager.Instance.ShowMessage(message);
        }

        ApplyPartyEffect();

        Debug.Log($"[EventGameObject] 보상 이벤트 실행: {message}");
    }

    // 💡 [수정] 함정 이벤트 발생 시 AlertManager 팝업 띄우기 및 미믹 전투 처리
    private void ApplyTrap()
    {

        if (ShowMessageManager.Instance != null)
        {
            ShowMessageManager.Instance.VisibleMessage(false);
        }

        BattleEncounter battleEncounter = GetComponent<BattleEncounter>();

        if (battleEncounter != null)
        {
            battleEncounter.Initialize(_eventData.mimic);
            battleEncounter.HandleCombatStarted();
        }

        Debug.Log($"[EventGameObject] 함정 이벤트 실행: {_eventData.Value}");
    }

    // 💡 [수정] 디버프 이벤트 발생 시 AlertManager 팝업 띄우기
    private void ApplyDebuff()
    {
        string message = !string.IsNullOrEmpty(_eventData.Description) ? _eventData.Description : "몸이 이상하다... 디버프가 적용되었습니다.";

        if (ShowMessageManager.Instance != null)
        {
            ShowMessageManager.Instance.ShowMessage(message);
        }

        ApplyPartyEffect();

        Debug.Log($"[EventGameObject] 디버프 이벤트 실행: {_eventData.Value}");
    }

    // 파티 조회용 재사용 버퍼
    private static readonly List<PersistentCharacterUnit> _partyBuffer =
        new List<PersistentCharacterUnit>();

    /// <summary>
    /// 이벤트 데이터의 효과를 활성 파티 전원에게 적용한다(HP/MP 회복·피해).
    /// 영속 데이터(CharacterVitals)에 적용되어 이후 전투에도 반영된다.
    /// </summary>
    private void ApplyPartyEffect()
    {
        if (_eventData.EffectKind == EventEffectKind.None || _eventData.Value <= 0)
        {
            return;
        }

        if (PersistentCharacterManager.Instance == null)
        {
            Debug.LogWarning("[EventGameObject] PersistentCharacterManager가 없어 효과를 적용하지 못했습니다.");
            return;
        }

        _partyBuffer.Clear();
        PersistentCharacterManager.Instance.GetActivePartyMembers(_partyBuffer);

        for (int i = 0; i < _partyBuffer.Count; i++)
        {
            CharacterVitals vitals = _partyBuffer[i] != null ? _partyBuffer[i].CharacterVitals : null;

            if (vitals == null)
            {
                continue;
            }

            ApplyEffectToVitals(vitals, _partyBuffer[i].CharacterName);
        }
    }

    /// <summary>효과 종류에 따라 한 캐릭터의 HP/MP를 변경한다.</summary>
    private void ApplyEffectToVitals(CharacterVitals vitals, string characterName)
    {
        int value = _eventData.Value;

        switch (_eventData.EffectKind)
        {
            case EventEffectKind.HealHp:
                vitals.HealHp(value);
                break;

            case EventEffectKind.HealHpPercent:
                vitals.HealHp(Mathf.Max(1, Mathf.RoundToInt(vitals.MaxHp * value * 0.01f)));
                break;

            case EventEffectKind.HealMp:
                vitals.RecoverMp(value);
                break;

            case EventEffectKind.HealMpPercent:
                vitals.RecoverMp(Mathf.Max(1, Mathf.RoundToInt(vitals.MaxMp * value * 0.01f)));
                break;

            case EventEffectKind.DamageHp:
                vitals.TakeDamage(value);
                break;

            case EventEffectKind.DamageHpPercent:
                vitals.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(vitals.MaxHp * value * 0.01f)));
                break;
        }

        Debug.Log(
            $"[EventGameObject] {characterName}: {_eventData.EffectKind} {value} 적용 " +
            $"(HP {vitals.CurrentHp}/{vitals.MaxHp}, MP {vitals.CurrentMp}/{vitals.MaxMp})");
    }

    private void SpawnVisualEffect()
    {
        if (_eventData.EffectPrefab != null)
        {
            Instantiate(_eventData.EffectPrefab, transform.position, Quaternion.identity);
        }
    }
}