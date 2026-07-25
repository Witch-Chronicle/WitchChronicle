using System.Collections.Generic;
using UnityEngine;

public class BattleCharacterUIManager : MonoBehaviour
{
    public static BattleCharacterUIManager Instance { get; private set; }

    private readonly List<BattleCharacterUISet> _registeredSets =
        new List<BattleCharacterUISet>();

    private BattleCharacterUISet _activeSet;
    private bool _isSubscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (BattleUIInputReader.Instance != null)
        {
            BattleUIInputReader.Instance.ClearCommandUI();
        }
    }

    private void TrySubscribe()
    {
        if (_isSubscribed) return;
        if (BattleUIContext.Instance == null) return;

        BattleUIContext.Instance.OnTurnStarted += HandleTurnStarted;
        BattleUIContext.Instance.OnTurnEnded += HandleTurnEnded;
        BattleUIContext.Instance.OnBattleEnded += HandleBattleEnded;

        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed) return;

        if (BattleUIContext.Instance != null)
        {
            BattleUIContext.Instance.OnTurnStarted -= HandleTurnStarted;
            BattleUIContext.Instance.OnTurnEnded -= HandleTurnEnded;
            BattleUIContext.Instance.OnBattleEnded -= HandleBattleEnded;
        }

        _isSubscribed = false;
    }

    public void Register(BattleCharacterUISet uiSet)
    {
        if (uiSet == null ||
            _registeredSets.Contains(uiSet))
        {
            return;
        }

        _registeredSets.Add(uiSet);
        uiSet.Hide();
    }

    public void Unregister(BattleCharacterUISet uiSet)
    {
        if (uiSet == null)
            return;

        if (_activeSet == uiSet)
        {
            BattleUIInputReader.Instance?.ClearCommandUI(
                uiSet.CommandUI
            );

            _activeSet = null;
        }

        _registeredSets.Remove(uiSet);
    }

    private void HandleTurnStarted(BattleUnit unit)
    {
        if (unit == null ||
            unit.TeamType != BattleTeamType.Player)
        {
            ClearActiveSet();
            HideAll();
            return;
        }

        _activeSet = null;

        for (int i = 0; i < _registeredSets.Count; i++)
        {
            BattleCharacterUISet set = _registeredSets[i];

            if (set == null)
                continue;

            if (set.OwnerUnit == unit)
            {
                set.Show();
                _activeSet = set;
            }
            else
            {
                set.Hide();
            }
        }

        if (_activeSet != null)
        {
            BattleUIInputReader.Instance?.SetCommandUI(
                _activeSet.CommandUI
            );
        }
        else
        {
            BattleUIInputReader.Instance?.ClearCommandUI();
        }
    }

    private void HandleTurnEnded(BattleUnit unit)
    {
        ClearActiveSet();
        HideAll();
    }

    private void HandleBattleEnded(BattleTeamType winner)
    {
        ClearActiveSet();
        HideAll();
    }

    public void HideCurrentUI()
    {
        if (_activeSet == null)
            return;

        _activeSet.Hide();

        BattleUIInputReader.Instance?.SuspendCommandUI();
    }

    public void ShowCurrentUI()
    {
        if (_activeSet == null)
            return;

        _activeSet.Show();

        BattleUIInputReader.Instance?.ResumeCommandUI();
    }

    private void ClearActiveSet()
    {
        if (_activeSet != null)
        {
            BattleUIInputReader.Instance?.ClearCommandUI(
                _activeSet.CommandUI
            );
        }
        else
        {
            BattleUIInputReader.Instance?.ClearCommandUI();
        }

        _activeSet = null;
    }

    private void HideAll()
    {
        for (int i = 0; i < _registeredSets.Count; i++)
        {
            BattleCharacterUISet set = _registeredSets[i];

            if (set != null)
            {
                set.Hide();
            }
        }
    }
}