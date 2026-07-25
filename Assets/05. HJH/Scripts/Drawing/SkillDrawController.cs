using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 스킬 확정 시, 그 스킬의 JSON 궤적을 화면(뷰포트) 기준으로 가이드라인으로 보여주고
/// 플레이어가 마우스로 따라 그리게 한 뒤 GestureMatcher로 판정, 데미지 배율을 콜백으로 돌려줌.
///
/// ShapeTracing과 동일하게 Canvas 없이 순수 LineRenderer + Camera.main 기준으로 동작.
/// SkillDrawCamera가 활성화된 동안 Camera.main이 그 시점을 렌더링하므로(Cinemachine Brain),
/// 별도로 SkillDrawCamera를 직접 참조하지 않고 Camera.main만 사용.
/// 좌표는 화면 뷰포트(0~1) 기준으로 저장 - JSON 포맷과 동일한 좌표계라 가이드/플레이어 입력이
/// 그대로 정합됨. 실제 화면 표시는 Camera.ViewportToWorldPoint로 카메라 앞 고정 거리 평면에 투영.
///
/// 가이드라인은 표시된 지 _guideVisibleDuration이 지나면 사라지고(전체 제한시간 _timeLimit과 별개),
/// 그 이후에도 플레이어는 계속 그릴 수 있음. 가이드 표시 위치/크기는 판정에 영향 없이 순수 표시용으로만
/// _guideViewportCenter / _guideDisplayScale로 조정 가능.
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

    [Header("Draw Plane")]
    [Tooltip("Camera.main 기준, 그리기 도형을 투영할 평면까지의 거리")]
    [SerializeField] private float _drawDistance = 5f;

    [Header("가이드라인 표시 설정")]
    [Tooltip("가이드라인이 사라지기까지 걸리는 시간 (_timeLimit과 별개, 이 시간이 지나면 가이드만 사라지고 그리기는 계속 가능)")]
    [SerializeField] private float _guideVisibleDuration = 1.5f;
    [Tooltip("가이드 도형이 화면 어디에 표시될지 (뷰포트 기준, 0.5,0.5 = 화면 중앙)")]
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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (_timerRoot != null) _timerRoot.SetActive(false);
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

        _onComplete = onComplete;

        _allPoints.Clear();
        ClearPlayerLines();
        _currentStrokeId = -1;
        _isPointerDrawing = false;
        _hasStroke = false;
        _guideHidden = false;
        _gestureStartTime = Time.time;
        _isActive = true;

        if (_timerRoot != null) _timerRoot.SetActive(true);

        _guideRawCentroid = ComputeCentroid(_guidePoints);
        DrawGuideLines();
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
        Vector2 viewport = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);

        for (int i = _allPoints.Count - 1; i >= 0; i--)
        {
            if (_allPoints[i].strokeId != _currentStrokeId) break;
            if (Vector2.Distance(_allPoints[i].pos, viewport) < 0.005f) return;
            break;
        }

        _allPoints.Add(new SkillPoint(viewport, _currentStrokeId));

        Vector3 worldPos = ViewportToDrawWorld(viewport);
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
            Vector2 displayViewport = _guideViewportCenter + (p.pos - _guideRawCentroid) * _guideDisplayScale;
            strokeWorldPoints[p.strokeId].Add(ViewportToDrawWorld(displayViewport));
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

        var callback = _onComplete;
        _onComplete = null;
        callback?.Invoke(multiplier);
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