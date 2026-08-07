using UnityEngine;
using WitchChronicle.Alchemy;
using WitchChronicle.IdleFarming;

public class LifeUIManager : MonoBehaviour
{
    public static LifeUIManager Instance { get; private set; }

    // Plot 패널들은 이제 PlotManager가 직접 관리하므로 GameObject 참조는 필요 없음.
    // 필요하면 인스펙터 노출용으로 남겨둘 수 있지만, 열림 확인/닫기는 PlotManager를 통해서만 처리한다.

    [Header("Fishing / Alchemy Panels (열림 상태 확인용)")]
    [SerializeField] private GameObject _fishingPanel;
    [SerializeField] private GameObject _alchemyPanel;

    private AlchemyInteractor _activeAlchemyInteractor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterAlchemy(AlchemyInteractor interactor)
    {
        _activeAlchemyInteractor = interactor;
    }

    public void UnregisterAlchemy(AlchemyInteractor interactor)
    {
        if (_activeAlchemyInteractor == interactor)
        {
            _activeAlchemyInteractor = null;
        }
    }

    /// <summary>
    /// 등록된 생활 패널/컨트롤러 중 하나라도 활성화되어 있는지 확인합니다.
    /// </summary>
    public bool IsAnyLifePanelOpen()
    {
        bool plotOpen = PlotManager.Instance != null && PlotManager.Instance.IsAnyPanelOpen;

        bool fishingOpen = (FishingManager.Instance != null && FishingManager.Instance.IsSessionActive)
            || IsActive(_fishingPanel);

        bool alchemyOpen = _activeAlchemyInteractor != null || IsActive(_alchemyPanel);

        return plotOpen || fishingOpen || alchemyOpen;
    }

    /// <summary>
    /// 등록된 생활 패널/컨트롤러를 전부 정리해서 닫습니다.
    /// 각 시스템의 기존 종료 로직(카메라 복귀, 이동 잠금 해제 등)을 그대로 거쳐서 닫는다.
    /// </summary>
    public void CloseAllLifePanels()
    {
        if (PlotManager.Instance != null)
        {
            PlotManager.Instance.ForceCloseAllPanels();
        }

        if (FishingManager.Instance != null && FishingManager.Instance.IsSessionActive)
        {
            FishingManager.Instance.ExitFishing();
        }

        if (_activeAlchemyInteractor != null)
        {
            _activeAlchemyInteractor.ClosePanel();
        }
    }

    private static bool IsActive(GameObject target)
    {
        return target != null && target.activeSelf;
    }
}