using UnityEngine;

/// <summary>
/// 상호작용 가능한 이벤트 오브젝트를 관리하고 일회성 실행을 보장하는 클래스입니다.
/// </summary>
public class EventGameObject : MonoBehaviour, ITFInteractable
{
    [SerializeField] private EventDataSO _eventData;
    private bool _isInteracted = false;

    public string Prompt => _isInteracted ? "이미 조사했다..." : "[F] 조사하기";

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
        ExecuteEventEffect();

        Debug.Log("[EventGameObject] 이벤트 상호작용이 완료되어 더 이상 활성화되지 않습니다.");
    }

    /// <summary>
    /// 외부에서 이벤트 데이터를 주입받아 초기화합니다.
    /// </summary>
    public void Setup(EventDataSO eventData)
    {
        if(eventData != null)
        {
            _eventData = eventData;
        }
        
        _isInteracted = false;

        if (_eventData != null && _eventData.Prefab != null)
        {
            Instantiate(_eventData.Prefab, transform);
        }

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

    private void ApplyReward()
    {
        if (ShowMessageManager.Instance != null)
        {
            ShowMessageManager.Instance.ShowMessage(_eventData.Description);
        }
        Debug.Log($"[EventGameObject] 보상 이벤트 실행: {_eventData.Description}");
    }

    private void ApplyTrap()
    {
        if (ShowMessageManager.Instance != null)
        {
            ShowMessageManager.Instance.ShowMessage($"함정이다..");
        }

        BattleEncounter battleEncounter = GetComponent<BattleEncounter>();

        battleEncounter.Initialize(_eventData.mimic);

        battleEncounter.HandleCombatStarted();

        Debug.Log($"[EventGameObject] 함정 이벤트 실행: {_eventData.Value}");
    }

    private void ApplyDebuff()
    {
        if (ShowMessageManager.Instance != null)
        {
            ShowMessageManager.Instance.ShowMessage($"몸이 이상하다... 디버프 됨");
        }
        Debug.Log($"[EventGameObject] 디버프 이벤트 실행: {_eventData.Value}");
    }

    private void SpawnVisualEffect()
    {
        if (_eventData.EffectPrefab != null)
        {
            Instantiate(_eventData.EffectPrefab, transform.position, Quaternion.identity);
        }
    }
}