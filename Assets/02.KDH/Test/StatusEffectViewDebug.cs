using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// StatusEffectView 테스트용 치트. StatusEffectView가 붙은 오브젝트(몬스터/캐릭터)에 함께 부착.
/// P:독  B:화상  S:수면  L:마비  I:침묵  C:혼란 (토글) / 0:전체 끄기
/// </summary>
[RequireComponent(typeof(StatusEffectView))]
public class StatusEffectViewDebug : MonoBehaviour
{
    private StatusEffectView _view;
    private readonly HashSet<StatusEffectType> _shown = new HashSet<StatusEffectType>();

    private void Awake()
    {
        _view = GetComponent<StatusEffectView>();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.pKey.wasPressedThisFrame) Toggle(StatusEffectType.Poison);
        if (kb.bKey.wasPressedThisFrame) Toggle(StatusEffectType.Burn);
        if (kb.sKey.wasPressedThisFrame) Toggle(StatusEffectType.Sleep);
        if (kb.lKey.wasPressedThisFrame) Toggle(StatusEffectType.Paralysis);
        if (kb.iKey.wasPressedThisFrame) Toggle(StatusEffectType.Silence);
        if (kb.cKey.wasPressedThisFrame) Toggle(StatusEffectType.Confusion);
        if (kb.digit0Key.wasPressedThisFrame) ClearAll();
    }

    private void Toggle(StatusEffectType type)
    {
        if (_shown.Contains(type))
        {
            _view.HideStatus(type);
            _shown.Remove(type);
        }
        else
        {
            _view.ShowStatus(type);
            _shown.Add(type);
        }
    }

    private void ClearAll()
    {
        _view.ClearAll();
        _shown.Clear();
    }
}
