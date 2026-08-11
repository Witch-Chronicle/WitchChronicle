using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingSceneUIController : MonoBehaviour
{
    public static LoadingSceneUIController Instance { get; private set; }

    [Header("Progress")]
    [SerializeField] private Image _progressFillImage;
    [SerializeField] private TMP_Text _progressText;

    [Header("Loading Tips")]
    [SerializeField] private TMP_Text _tipText;

    [TextArea(2, 4)]
    [SerializeField]
    private string[] _tips = new string[]
    {
        "포션을 미리 준비하면 전투를 한결 수월하게 진행할 수 있습니다.",
        "몬스터마다 약점 속성이 존재합니다. 상성을 활용해보세요.",
        "마법진을 정확하게 그릴수록 스킬의 피해량이 대폭 증가합니다.",
        "농사로 얻은 재료는 요리나 연금술 포션 제작에 유용하게 쓰입니다.",
        "낚시로 잡은 물고기는 상점에 팔거나 요리 재료로 활용할 수 있습니다."
    };

    [Header("Smoothing")]
    [Tooltip("표시되는 진행률이 실제 목표치를 따라가는 속도")]
    [SerializeField] private float _smoothSpeed = 2.5f;

    private float _targetProgress;
    private float _displayedProgress;

    public bool IsDisplayComplete => Mathf.Approximately(_displayedProgress, _targetProgress);

    private void Awake()
    {
        Instance = this;
        _targetProgress = 0f;
        _displayedProgress = 0f;
        ApplyProgress(0f);

        ShowRandomTip();
    }

    private void ShowRandomTip()
    {
        if (_tipText == null)
        {
            return;
        }

        if (_tips == null || _tips.Length == 0)
        {
            _tipText.gameObject.SetActive(false);
            return;
        }

        int randomIndex = Random.Range(0, _tips.Length);
        _tipText.text = $"{_tips[randomIndex]}";
        _tipText.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Mathf.Approximately(_displayedProgress, _targetProgress))
        {
            return;
        }

        _displayedProgress = Mathf.MoveTowards(
            _displayedProgress,
            _targetProgress,
            _smoothSpeed * Time.unscaledDeltaTime);

        ApplyProgress(_displayedProgress);
    }

    /// <summary>목표 진행률만 갱신. 실제 표시는 Update에서 부드럽게 따라간다.</summary>
    public void SetProgress(float progress01)
    {
        _targetProgress = Mathf.Clamp01(progress01);
    }

    /// <summary>보간 없이 즉시 특정 값으로 맞춘다(0%에서 시작할 때 등).</summary>
    public void SetProgressImmediate(float progress01)
    {
        _targetProgress = Mathf.Clamp01(progress01);
        _displayedProgress = _targetProgress;
        ApplyProgress(_displayedProgress);
    }

    private void ApplyProgress(float progress01)
    {
        if (_progressFillImage != null)
        {
            _progressFillImage.fillAmount = progress01;
        }

        if (_progressText != null)
        {
            _progressText.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";
        }
    }
}