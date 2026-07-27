using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 스킬 확정 시, 그 스킬의 JSON 궤적을 화면(뷰포트) 기준으로 가이드라인으로 보여주고
/// 플레이어가 마우스로 따라 그리게 한 뒤 GestureMatcher로 판정, 데미지 배율을 콜백으로 돌려줌.
///
/// ShapeTracing과 동일하게 Canvas 없이 순수 LineRenderer + Camera.main 기준으로 동작.
/// SkillDrawCamera가 활성화된 동안 Camera.main이 그 시점을 렌더링하므로(Cinemachine Brain),
/// 별도로 SkillDrawCamera를 직접 참조하지 않고 Camera.main만 사용.
///
/// * 그리기 영역 제한(_drawAreaRect): 지정하면 화면 전체가 아니라 그 UI Image(RectTransform)의
///   사각형 안에서만 좌표를 정규화(0~1)해서 사용. 미지정 시 기존처럼 화면 전체 기준으로 동작(하위 호환).
///   내부적으로 궤적 판정(GestureMatcher)은 중심점+크기로 정규화해서 비교하므로,
///   좌표계를 화면 전체 대신 Rect 기준으로 바꿔도 판정 정확도에는 영향 없음.
///
/// * 진행 흐름: Play() -> SkillDrawCanvas 활성화 + 가이드라인 즉시 표시 -> DrawingPlace(패널)
///   CanvasGroup Fade In -> Fade In 완료 시점부터 타이머(_timeLimit) 카운트 시작 + 입력 활성화.
///   종료 시: 판정 -> DrawingPlace Fade Out -> Fade Out 완료 후 SkillDrawCanvas 비활성화 + 콜백.
///
/// 좌표는 지정된 영역(또는 화면) 기준 0~1로 저장 - JSON 포맷과 동일한 좌표계라 가이드/플레이어 입력이
/// 그대로 정합됨. 실제 화면 표시는 Camera.ViewportToWorldPoint로 카메라 앞 고정 거리 평면에 투영.
///
/// 가이드라인은 표시된 지 _guideVisibleDuration이 지나면 사라지고(전체 제한시간 _timeLimit과 별개),
/// 그 이후에도 플레이어는 계속 그릴 수 있음. 가이드 표시 위치/크기는 판정에 영향 없이 순수 표시용으로만
/// _guideViewportCenter / _guideDisplayScale로 조정 가능 (_drawAreaRect 기준 상대 좌표).
///
/// 타이머 UI만 별도 Canvas(_timerRoot)로 표시.
/// New Input System 기준(Mouse.current)으로 폴링. Active Input Handling이
/// "Input System Package (New)" 또는 "Both"로 설정되어 있어야 함.
/// </summary>
public class SkillDrawController : MonoBehaviour
{
    public static SkillDrawController Instance { get; private set; }

    [Header("Timer UI (Canvas, 그리기 도형 자체와는 별개)")]
    [SerializeField] private GameObject _timerRoot;
    [SerializeField] private TMPro.TMP_Text _timerText;
    [SerializeField] private UnityEngine.UI.Image _timerFillImage;

    [Header("Line Renderer Prefabs")]
    [SerializeField] private LineRenderer _guideLinePrefab;
    [SerializeField] private LineRenderer _playerLinePrefab;

    [Header("Draw Area (지정 시 이 UI Image 안에서만 그려짐, 미지정 시 화면 전체)")]
    [Tooltip("SkillDrawCanvas 루트 오브젝트. 평소 비활성화 상태로 두고, 그리는 동안만 켬.")]
    [SerializeField] private GameObject _skillDrawCanvasRoot;
    [Tooltip("그리기를 국한시킬 UI Image의 RectTransform (예: DrawPanel/DrawImage)")]
    [SerializeField] private RectTransform _drawAreaRect;
    [Tooltip("_drawAreaRect가 속한 Canvas의 Render Camera. Screen Space-Overlay면 비워둬도 됨.")]
    [SerializeField] private Camera _drawAreaUICamera;

    [Header("Draw Plane")]
    [Tooltip("Camera.main 기준, 그리기 도형을 투영할 평면까지의 거리")]
    [SerializeField] private float _drawDistance = 5f;

    [Header("DrawingPlace Fade (패널 페이드 인/아웃)")]
    [Tooltip("DrawingPlace(BG+Header+DrawingRect 전체)에 붙은 CanvasGroup. " +
             "Fade In이 끝난 시점부터 타이머가 흐르고 입력이 활성화됨.")]
    [SerializeField] private CanvasGroup _drawingPlaceCanvasGroup;
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private Ease _fadeEase = Ease.OutQuad;

    [Header("가이드라인 표시 설정")]
    [Tooltip("가이드라인이 사라지기까지 걸리는 시간 (_timeLimit과 별개, 이 시간이 지나면 가이드만 사라지고 그리기는 계속 가능)")]
    [SerializeField] private float _guideVisibleDuration = 1.5f;
    [Tooltip("가이드 도형이 그리기 영역 어디에 표시될지 (영역 기준 0~1, 0.5,0.5 = 영역 중앙)")]
    [SerializeField] private Vector2 _guideViewportCenter = new Vector2(0.5f, 0.5f);
    [Tooltip("가이드 도형 표시 크기 배율 (판정 점수에는 영향 없음, 순수 표시용)")]
    [SerializeField] private float _guideDisplayScale = 1f;

    [Header("공통 판정 설정 (모든 스킬 동일 적용)")]
    [SerializeField] private float _timeLimit = 3f;
    [SerializeField] private float _interStrokeTimeout = 0.45f;
    [Range(0f, 100f)]
    [SerializeField] private float _scoreThreshold = 70f;

    [Header("데미지 배율 (threshold 미만 x1, threshold~100을 min~max로 선형 매핑)")]
    [SerializeField] private float _minDamageMultiplier = 1f;
    [SerializeField] private float _maxDamageMultiplier = 2f;

    private Camera _cam;

    private bool _isActive;
    private bool _isPointerDrawing;
    private bool _hasStroke;
    private bool _guideHidden;

    private readonly List<SkillPoint> _allPoints = new List<SkillPoint>();
    private List<SkillPoint> _guidePoints;
    private Vector2 _guideRawCentroid; // JSON 원본 좌표 기준 중심점 (재배치 계산용)

    private readonly List<LineRenderer> _guideLines = new List<LineRenderer>();
    private readonly List<LineRenderer> _playerLines = new List<LineRenderer>();
    private LineRenderer _currentPlayerLine;
    private int _currentStrokeId = -1;

    private float _gestureStartTime;
    private float _strokeEndTime;

    private System.Action<float> _onComplete;

    // 그리기 영역의 화면 픽셀 사각형. Play() 시작 시 한 번 계산해서 캐싱.
    private Rect _drawAreaScreenRect;

    private Tween _fadeTween;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (_timerRoot != null) _timerRoot.SetActive(false);
        if (_skillDrawCanvasRoot != null) _skillDrawCanvasRoot.SetActive(false);

        if (_drawingPlaceCanvasGroup != null)
        {
            _drawingPlaceCanvasGroup.alpha = 0f;
            _drawingPlaceCanvasGroup.interactable = false;
            _drawingPlaceCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (_isActive == false) return;

        HandleInput();

        float elapsed = Time.time - _gestureStartTime;
        UpdateTimerUI(Mathf.Max(0f, _timeLimit - elapsed));

        if (_guideHidden == false && elapsed > _guideVisibleDuration)
        {
            ClearGuideLines();
            _guideHidden = true;
        }

        if (elapsed > _timeLimit)
        {
            FinishDrawing();
            return;
        }

        if (_isPointerDrawing == false && _hasStroke && Time.time - _strokeEndTime > _interStrokeTimeout)
        {
            FinishDrawing();
        }
    }

    /// <summary>
    /// 스킬 확정 시 호출. 그리기(또는 시간초과)가 끝나면 onComplete(damageMultiplier) 호출.
    /// DrawGuideJson이 없거나 Camera.main을 못 찾으면 즉시 배율 1로 콜백.
    /// </summary>
    public void Play(SkillData skillData, System.Action<float> onComplete)
    {
        if (skillData == null || skillData.DrawGuideJson == null)
        {
            onComplete?.Invoke(1f);
            return;
        }

        _guidePoints = SkillShapeTemplate.ParsePoints(skillData.DrawGuideJson);

        if (_guidePoints == null || _guidePoints.Count < 2)
        {
            Debug.LogWarning($"[SkillDrawController] {skillData.SkillName} 가이드 JSON 파싱 실패");
            onComplete?.Invoke(1f);
            return;
        }

        _cam = Camera.main;

        if (_cam == null)
        {
            Debug.LogWarning("[SkillDrawController] Camera.main 없음");
            onComplete?.Invoke(1f);
            return;
        }

        _fadeTween?.Kill();

        if (_skillDrawCanvasRoot != null) _skillDrawCanvasRoot.SetActive(true);

        _drawAreaScreenRect = CalculateDrawAreaScreenRect();

        _onComplete = onComplete;

        _allPoints.Clear();
        ClearPlayerLines();
        _currentStrokeId = -1;
        _isPointerDrawing = false;
        _hasStroke = false;
        _guideHidden = false;

        // 타이머/입력은 아직 시작하지 않음 (Fade In 완료 후 시작)
        _isActive = false;

        _guideRawCentroid = ComputeCentroid(_guidePoints);
        DrawGuideLines(); // 가이드는 Fade In 중에도 이미 표시되어 있어도 무방

        PlayFadeIn();
    }

    // ---------- DrawingPlace Fade In/Out ----------

    private void PlayFadeIn()
    {
        if (_drawingPlaceCanvasGroup == null)
        {
            StartTimerAndInput();
            return;
        }

        _drawingPlaceCanvasGroup.alpha = 0f;
        _drawingPlaceCanvasGroup.interactable = false;
        _drawingPlaceCanvasGroup.blocksRaycasts = false;

        _fadeTween = _drawingPlaceCanvasGroup
            .DOFade(1f, _fadeDuration)
            .SetEase(_fadeEase)
            .OnComplete(StartTimerAndInput);
    }

    /// <summary>
    /// Fade In 완료 시점. 여기서부터 타이머가 흐르고 입력이 활성화됨.
    /// </summary>
    private void StartTimerAndInput()
    {
        if (_drawingPlaceCanvasGroup != null)
        {
            _drawingPlaceCanvasGroup.interactable = true;
            _drawingPlaceCanvasGroup.blocksRaycasts = true;
        }

        if (_timerRoot != null) _timerRoot.SetActive(true);

        _gestureStartTime = Time.time;
        _isActive = true;
    }

    private void PlayFadeOut(float multiplier)
    {
        if (_drawingPlaceCanvasGroup == null)
        {
            FinalizeFinish(multiplier);
            return;
        }

        _drawingPlaceCanvasGroup.interactable = false;
        _drawingPlaceCanvasGroup.blocksRaycasts = false;

        _fadeTween?.Kill();

        _fadeTween = _drawingPlaceCanvasGroup
            .DOFade(0f, _fadeDuration)
            .SetEase(_fadeEase)
            .OnComplete(() => FinalizeFinish(multiplier));
    }

    private void FinalizeFinish(float multiplier)
    {
        if (_skillDrawCanvasRoot != null) _skillDrawCanvasRoot.SetActive(false);

        var callback = _onComplete;
        _onComplete = null;
        callback?.Invoke(multiplier);
    }

    // ---------- 그리기 영역 (지정 시 화면 전체가 아니라 그 Rect 안에서만) ----------

    /// <summary>
    /// _drawAreaRect의 화면 픽셀 사각형을 계산. 미지정 시 화면 전체를 반환(기존 동작 유지).
    /// </summary>
    private Rect CalculateDrawAreaScreenRect()
    {
        if (_drawAreaRect == null)
        {
            return new Rect(0f, 0f, Screen.width, Screen.height);
        }

        Vector3[] corners = new Vector3[4];
        _drawAreaRect.GetWorldCorners(corners); // 0: bottom-left, 2: top-right

        Vector2 min = RectTransformUtility.WorldToScreenPoint(_drawAreaUICamera, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(_drawAreaUICamera, corners[2]);

        return new Rect(min.x, min.y, Mathf.Max(1f, max.x - min.x), Mathf.Max(1f, max.y - min.y));
    }

    /// <summary>
    /// 화면 픽셀 좌표를 그리기 영역 기준 0~1 로컬 좌표로 변환 (영역 밖은 0~1로 클램프).
    /// </summary>
    private Vector2 ScreenToAreaLocal(Vector2 screenPos)
    {
        float u = _drawAreaScreenRect.width > 0f
            ? (screenPos.x - _drawAreaScreenRect.xMin) / _drawAreaScreenRect.width
            : 0f;

        float v = _drawAreaScreenRect.height > 0f
            ? (screenPos.y - _drawAreaScreenRect.yMin) / _drawAreaScreenRect.height
            : 0f;

        return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
    }

    /// <summary>
    /// 그리기 영역 기준 0~1 로컬 좌표를 실제 화면 전체 기준 뷰포트(0~1)로 환산.
    /// (Camera.ViewportToWorldPoint는 화면 전체 기준 뷰포트를 요구하므로 필요한 변환)
    /// </summary>
    private Vector2 AreaLocalToViewport(Vector2 areaLocal)
    {
        Vector2 screenPixel = new Vector2(
            _drawAreaScreenRect.xMin + areaLocal.x * _drawAreaScreenRect.width,
            _drawAreaScreenRect.yMin + areaLocal.y * _drawAreaScreenRect.height);

        return new Vector2(screenPixel.x / Screen.width, screenPixel.y / Screen.height);
    }

    // ---------- 입력 (New Input System, Mouse.current 폴링) ----------

    private void HandleInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _currentStrokeId++;
            _isPointerDrawing = true;
            _hasStroke = true;

            _currentPlayerLine = GetOrCreatePlayerLine(_currentStrokeId);
            _currentPlayerLine.positionCount = 0;

            AddPoint(mouse.position.ReadValue());
        }
        else if (mouse.leftButton.isPressed && _isPointerDrawing)
        {
            AddPoint(mouse.position.ReadValue());
        }
        else if (mouse.leftButton.wasReleasedThisFrame && _isPointerDrawing)
        {
            _isPointerDrawing = false;
            _strokeEndTime = Time.time;
        }
    }

    private void AddPoint(Vector2 screenPos)
    {
        Vector2 areaLocal = ScreenToAreaLocal(screenPos);

        for (int i = _allPoints.Count - 1; i >= 0; i--)
        {
            if (_allPoints[i].strokeId != _currentStrokeId) break;
            if (Vector2.Distance(_allPoints[i].pos, areaLocal) < 0.005f) return;
            break;
        }

        _allPoints.Add(new SkillPoint(areaLocal, _currentStrokeId));

        Vector3 worldPos = ViewportToDrawWorld(AreaLocalToViewport(areaLocal));
        _currentPlayerLine.positionCount++;
        _currentPlayerLine.SetPosition(_currentPlayerLine.positionCount - 1, worldPos);
    }

    private Vector3 ViewportToDrawWorld(Vector2 viewport)
    {
        return _cam.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, _drawDistance));
    }

    // ---------- 가이드라인 (한번에 전체 표시, 여러 획 지원, 위치/크기 재배치) ----------

    private Vector2 ComputeCentroid(List<SkillPoint> points)
    {
        Vector2 sum = Vector2.zero;
        foreach (var p in points) sum += p.pos;
        return sum / points.Count;
    }

    private void DrawGuideLines()
    {
        ClearGuideLines();

        int strokeCount = 0;
        foreach (var p in _guidePoints) strokeCount = Mathf.Max(strokeCount, p.strokeId + 1);

        List<List<Vector3>> strokeWorldPoints = new List<List<Vector3>>();
        for (int i = 0; i < strokeCount; i++) strokeWorldPoints.Add(new List<Vector3>());

        foreach (var p in _guidePoints)
        {
            Vector2 displayAreaLocal = _guideViewportCenter + (p.pos - _guideRawCentroid) * _guideDisplayScale;
            strokeWorldPoints[p.strokeId].Add(ViewportToDrawWorld(AreaLocalToViewport(displayAreaLocal)));
        }

        for (int i = 0; i < strokeWorldPoints.Count; i++)
        {
            LineRenderer line = GetOrCreateGuideLine(i);
            line.positionCount = strokeWorldPoints[i].Count;
            line.SetPositions(strokeWorldPoints[i].ToArray());
        }
    }

    // ---------- 판정 ----------

    private void FinishDrawing()
    {
        _isActive = false;
        _isPointerDrawing = false;

        float multiplier = 1f;

        if (_allPoints.Count >= 2)
        {
            float score = GestureMatcher.ComputeSimilarityScore(_allPoints, _guidePoints);
            multiplier = ScoreToMultiplier(score);
            Debug.Log($"[SkillDrawController] 유사도 {score:0} / 배율 x{multiplier:0.00}");
        }
        else
        {
            Debug.Log("[SkillDrawController] 그린 게 없음 / 배율 x1");
        }

        if (_timerRoot != null) _timerRoot.SetActive(false);
        ClearGuideLines();
        ClearPlayerLines();

        PlayFadeOut(multiplier);
    }

    /// <summary>
    /// threshold 미만이면 _minDamageMultiplier, threshold~100 구간을
    /// _minDamageMultiplier~_maxDamageMultiplier로 선형 매핑.
    /// </summary>
    private float ScoreToMultiplier(float score)
    {
        if (score < _scoreThreshold) return _minDamageMultiplier;
        float t = Mathf.InverseLerp(_scoreThreshold, 100f, score);
        return Mathf.Lerp(_minDamageMultiplier, _maxDamageMultiplier, t);
    }

    private void UpdateTimerUI(float remaining)
    {
        if (_timerText != null) _timerText.text = remaining.ToString("0.0");
        if (_timerFillImage != null) _timerFillImage.fillAmount = _timeLimit > 0f ? remaining / _timeLimit : 0f;
    }

    // ---------- LineRenderer 풀 관리 ----------

    private LineRenderer GetOrCreateGuideLine(int index)
    {
        while (_guideLines.Count <= index)
        {
            LineRenderer clone = Instantiate(_guideLinePrefab, transform);
            clone.gameObject.SetActive(true);
            _guideLines.Add(clone);
        }
        return _guideLines[index];
    }

    private LineRenderer GetOrCreatePlayerLine(int index)
    {
        while (_playerLines.Count <= index)
        {
            LineRenderer clone = Instantiate(_playerLinePrefab, transform);
            clone.gameObject.SetActive(true);
            _playerLines.Add(clone);
        }
        return _playerLines[index];
    }

    private void ClearGuideLines()
    {
        foreach (var line in _guideLines) if (line != null) Destroy(line.gameObject);
        _guideLines.Clear();
    }

    private void ClearPlayerLines()
    {
        foreach (var line in _playerLines) if (line != null) Destroy(line.gameObject);
        _playerLines.Clear();
    }
}