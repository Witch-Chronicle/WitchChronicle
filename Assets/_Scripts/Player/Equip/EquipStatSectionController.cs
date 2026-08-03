using UnityEngine;

/// <summary>
/// IntegrationPanel/Equip/StatSection 컨트롤러.
/// - 지금 선택된 캐릭터(CharacterSelectionManager 기준)의 장착 장비 스탯 합계를 고정적으로 표시.
/// - 선택된 캐릭터가 바뀌면 CharacterEquipment 구독을 자동으로 갈아끼움.
/// </summary>
public class EquipStatSectionController : MonoBehaviour
{
    [Header("Current")]
    [SerializeField] private EquipStatRow _currentHp;
    [SerializeField] private EquipStatRow _currentMp;
    [SerializeField] private EquipStatRow _currentPower;
    [SerializeField] private EquipStatRow _currentInt;
    [SerializeField] private EquipStatRow _currentDef;
    [SerializeField] private EquipStatRow _currentSpd;
    [SerializeField] private EquipStatRow _currentLuk;

    private CharacterEquipment _boundEquipment;

    private void OnEnable()
    {
        SubscribeSelectionEvent();
        BindToSelectedCharacter();
    }

    private void OnDisable()
    {
        UnsubscribeEquipment();
        UnsubscribeSelectionEvent();
    }

    private void SubscribeSelectionEvent()
    {
        if (CharacterSelectionManager.Instance == null) return;

        CharacterSelectionManager.Instance.OnSelectionChanged -= HandleCharacterSelectionChanged;
        CharacterSelectionManager.Instance.OnSelectionChanged += HandleCharacterSelectionChanged;
    }

    private void UnsubscribeSelectionEvent()
    {
        if (CharacterSelectionManager.Instance == null) return;

        CharacterSelectionManager.Instance.OnSelectionChanged -= HandleCharacterSelectionChanged;
    }

    private void HandleCharacterSelectionChanged(CharacterType character)
    {
        BindToSelectedCharacter();
    }

    private void BindToSelectedCharacter()
    {
        UnsubscribeEquipment();

        if (CharacterSelectionManager.Instance != null && PersistentCharacterManager.Instance != null)
        {
            CharacterType selected = CharacterSelectionManager.Instance.GetSelected();
            string characterId = selected.ToString();

            if (PersistentCharacterManager.Instance.TryGetCharacter(characterId, out PersistentCharacterUnit unit))
            {
                _boundEquipment = unit.CharacterEquipment;
            }
        }

        if (_boundEquipment != null)
        {
            _boundEquipment.OnEquipmentChanged += RefreshCurrent;
        }

        RefreshCurrent();
    }

    private void UnsubscribeEquipment()
    {
        if (_boundEquipment == null) return;

        _boundEquipment.OnEquipmentChanged -= RefreshCurrent;
        _boundEquipment = null;
    }

    private void RefreshCurrent()
    {
        if (_boundEquipment == null) return;

        StatBlock total = _boundEquipment.TotalEquipmentStats;

        _currentHp?.SetValue(total.maxHP);
        _currentMp?.SetValue(total.maxMP);
        _currentPower?.SetValue(total.magicPower);
        _currentInt?.SetValue(total.intelligence);
        _currentDef?.SetValue(total.defense);
        _currentSpd?.SetValue(total.speed);
        _currentLuk?.SetValue(total.luck);
    }
}