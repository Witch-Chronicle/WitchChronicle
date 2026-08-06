using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 스킬 확정 시, Header의 예시 이미지(DrawImg)로 그려야 할 모양을 보여주고
/// 플레이어가 마우스로 따라 그리게 한 뒤, 그 궤적을 SkillData.DrawGuideJson과 GestureMatcher로 판정,
/// 데미지 배율을 콜백으로 돌려줌.
///
/// * 라인은 Fill(얇음, 그리는 동안 계속 보임)과 Outline(두꺼움, 뒤에 깔림, 그리는 동안엔 숨김) 두 개를
///   같은 궤적으로 동시에 그림. 판정 후 결과 색 와이프가 시작될 때 Outline이 알파 0->1로 드러나면서
///   같이 물듦 ("색이 입혀지며 나타나는" 느낌). Fill도 같이 물들지는 _applyColorWipeToFill로 선택 가능.
///
/// * TimerFillImage도 판정 결과 색(resultColor)으로 같은 타이밍에 부드럽게 색이 바뀜.
///
/// * 그리기 영역 제한(_drawAreaRect): 지정하면 화면 전체가 아니라 그 UI Image(RectTransform)의
///   사각형 안에서만 좌표를 정규화(0~1)해서 사용.
///
/// * 진행 흐름:
///   Play() -> SkillDrawCanvas 활성화 + Header의 DrawImg에 예시 이미지 표시 -> DrawingPlace(패널)
///   CanvasGroup Fade In -> Fade In 완료 시점부터 타이머(_timeLimit) 카운트 시작 + 입력 활성화.
///   시간 종료(또는 스트로크 타임아웃) -> 판정 -> 배율 텍스트 표시 + _resultHoldDuration 유지
///   -> Header 비활성화
///   -> Outline이 그린 순서대로 알파 0->1로 드러나며 resultColor로 물듦 (Fill은 _applyColorWipeToFill에 따라)
///      + TimerFillImage도 같은 시간에 걸쳐 resultColor로 색이 바뀜
///   -> 다 채워지면 두께 펄스(살짝 커졌다 작아짐)
///   -> Fill/Outline 모두 각자의 색을 유지한 채로 알파+두께 페이드아웃, TimerFillImage도 알파 페이드아웃
///   -> DrawingPlace Fade Out -> Fade Out 완료 후 SkillDrawCanvas 비활성화 + 콜백.
///
/// 좌표는 지정된 영역(또는 화면) 기준 0~1로 저장 - JSON 포맷과 동일한 좌표계라 판정에 그대로 사용됨.
/// 실제 플레이어가 그린 라인의 화면 표시는 Camera.ViewportToWorldPoint로 카메라 앞 고정 거리 평면에 투영.
///
/// New Input System 기준(Mouse.current)으로 폴링.
/// </summary>
public class SkillDrawController : MonoBehaviour
{
    public static SkillDrawController Instance { get; private set; }

    /// <summary>
    /// Play() 호출 후 실제 그리기 프로세스가 시작된 시점부터
    /// FinalizeFinish()로 콜백이 나가기 직전까지 true.
    /// 이 구간 동안은 BattleUIInputReader가 Esc(취소) 입력을 무시해야 함.
    /// </summary>
    public bool IsDrawing { get; private set; }

    [Header("Timer UI (DrawingCanvas 하위, 캔버스 자체가 켜지고/꺼질 때 같이 보임)")]
    [SerializeField] private TMPro.TMP_Text _timerText;
    [SerializeField] private UnityEngine.UI.Image _timerFillImage;
    [Tooltip("스트로크 타임아웃으로 조기 종료됐을 때, Fill이 중간값에서 멈추지 않고 100%까지 마저 채워지는 시간")]
    [SerializeField] private float _timerCompleteFillDuration = 0.2f;
    [Tooltip("판정 후 TimerFillImage 원래 색상")]
    [SerializeField] private Color _timerFillDefaultColor = Color.white;

    [Header("Header (점수 나온 뒤 비활성화)")]
    [SerializeField] private GameObject _headerObject;

    [Header("예시 이미지 (Header/DrawImg)")]
    [Tooltip("이번에 그릴 모양을 미리 보여주는 이미지. SkillData.DrawExampleSprite가 여기에 세팅됨.")]
    [SerializeField] private UnityEngine.UI.Image _drawExampleImage;

    [Header("Tooltip (그리기 전에만 표시, 첫 라인을 그리는 순간 즉시 숨김)")]
    [SerializeField] private GameObject _tooltipObject;

    [Header("Result (배율 표시, DrawCanvas 하위)")]
    [SerializeField] private TMPro.TMP_Text _multiplierText;
    [Tooltip("판정 후 라인이 사라지기 시작하기 전 잠깐 멈춰있는 시간 (더 이상 배율 텍스트 표시용이 아님)")]
    [SerializeField] private float _resultHoldDuration = 1f;

    [Header("Multiplier Result Area (마법진이 다 사라진 뒤 배율을 보여주는 영역)")]
    [Tooltip("HeaderTxt/MultiplierTxt/MultiplierSlider를 담은 영역의 CanvasGroup. 평소 alpha 0으로 숨겨둠. " +
         "MultiplierArea 오브젝트에 CanvasGroup 컴포넌트를 추가해서 연결할 것.")]
    [SerializeField] private CanvasGroup _multiplierAreaCanvasGroup;
    [SerializeField] private float _multiplierAreaFadeDuration = 0.25f;
    [SerializeField] private Ease _multiplierAreaFadeEase = Ease.OutQuad;

    [Tooltip("0~_maxDamageMultiplier 범위로 채워지는 슬라이더 Fill 이미지 (MultiplierSlider/Fill)")]
    [SerializeField] private UnityEngine.UI.Image _multiplierSliderFillImage;

    [Tooltip("슬라이더/텍스트가 0에서 목표 배율까지 차오르는 데 걸리는 시간 (둘이 같은 시간 사용, 동시에 끝남)")]
    [SerializeField] private float _multiplierRevealDuration = 0.6f;
    [SerializeField] private Ease _multiplierRevealEase = Ease.OutQuad;

    [Tooltip("배율이 다 채워진 뒤 MultiplierArea가 사라지기 전 유지되는 시간")]
    [SerializeField] private float _multiplierHoldDuration = 0.6f;

    [Header("Disappear (라인/FilledImg 페이드아웃)")]
    [Tooltip("점수 확인 후 라인/FilledImg가 서서히 사라지는 데 걸리는 시간")]
    [SerializeField] private float _disappearDuration = 1f;

    [Header("Result Color Wipe (그린 순서대로 결과 색이 채워짐)")]
    [Tooltip("점수(0~100)를 이 Gradient로 매핑해서 라인/아웃라인/TimerFillImage가 물드는 색상 결정. 왼쪽=낮은 점수, 오른쪽=높은 점수.")]
    [SerializeField] private Gradient _resultColorGradient;

    [Tooltip("색이 채워지는 데 걸리는 시간 (그린 순서대로 진행, TimerFillImage 색 전환도 같은 시간 사용)")]
    [SerializeField] private float _colorWipeDuration = 0.6f;

    [Tooltip("true: Fill과 Outline 모두 Color Wipe 적용 / false: Outline에만 적용")]
    [SerializeField] private bool _applyColorWipeToFill = false;

    [Header("Pulse (색 다 채워진 뒤 두께가 잠깐 커졌다 작아짐)")]
    [SerializeField] private float _pulseWidthMultiplier = 1.4f;
    [Tooltip("커졌다가 원래대로 돌아오는 전체 시간 (반씩 나눠서 사용)")]
    [SerializeField] private float _pulseDuration = 0.25f;

    [Header("Line Renderer Prefabs")]
    [Tooltip("얇은 Fill 라인 - 그리는 동안 계속 보임")]
    [SerializeField] private LineRenderer _playerLinePrefab;
    [Tooltip("두꺼운 Outline 라인 - 그리는 동안엔 숨겨져 있다가, 판정 후 색 와이프 시작 시 알파 0->1로 드러남. " +
             "Fill보다 뒤에 그려지도록 Sorting Order를 더 낮게 잡아둘 것.")]
    [SerializeField] private LineRenderer _outlineLinePrefab;

    [Header("Draw Area (지정 시 이 UI Image 안에서만 그려짐, 미지정 시 화면 전체)")]
    [Tooltip("SkillDrawCanvas 루트 오브젝트. 평소 비활성화 상태로 두고, 그리는 동안만 켬.")]
    [SerializeField] private GameObject _skillDrawCanvasRoot;
    [Tooltip("그리기를 국한시킬 UI Image의 RectTransform (예: DrawPanel/DrawingRect)")]
    [SerializeField] private RectTransform _drawAreaRect;
    [Tooltip("_drawAreaRect가 속한 Canvas의 Render Camera. Screen Space-Overlay면 비워둬도 됨.")]
    [SerializeField] private Camera _drawAreaUICamera;

    [Header("Draw Plane")]
    [Tooltip("Camera.main 기준, 그리기 도형을 투영할 평면까지의 거리")]
    [SerializeField] private float _drawDistance = 5f;

    [Header("DrawingPlace Fade (패널 페이드 인/아웃)")]
    [Tooltip("DrawingPlace(BG+DrawingRect 전체)에 붙은 CanvasGroup. " +
             "Fade In이 끝난 시점부터 타이머가 흐르고 입력이 활성화됨.")]
    [SerializeField] private CanvasGroup _drawingPlaceCanvasGroup;
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private Ease _fadeEase = Ease.OutQuad;

    [Header("공통 판정 설정 (모든 스킬 동일 적용)")]
    [SerializeField] private float _timeLimit = 3f;
    [SerializeField] private float _interStrokeTimeout = 0.45f;
    [Range(0f, 100f)]
    [SerializeField] private float _scoreThreshold = 70f;

    [Header("데미지 배율 (0~threshold를 min~1로, threshold~100을 1~max로 선형 매핑)")]
    [SerializeField] private float _minDamageMultiplier = 0f;
    [SerializeField] private float _maxDamageMultiplier = 2f;

    private Camera _cam;

    private bool _isActive;
    private bool _isPointerDrawing;
    private bool _hasStroke;

    private readonly List<SkillPoint> _allPoints = new List<SkillPoint>();
    private List<SkillPoint> _guidePoints; // 화면에 그리진 않지만 판정용으로는 계속 사용

    private readonly List<LineRenderer> _playerLines = new List<LineRenderer>();
    private readonly List<LineRenderer> _outlineLines = new List<LineRenderer>();
    private LineRenderer _currentPlayerLine;
    private LineRenderer _currentOutlineLine;
    private int _currentStrokeId = -1;
    private bool _pendingNewStroke;

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
        if (_skillDrawCanvasRoot != null) _skillDrawCanvasRoot.SetActive(false);

        if (_drawingPlaceCanvasGroup != null)
        {
            _drawingPlaceCanvasGroup.alpha = 0f;
            _drawingPlaceCanvasGroup.interactable = false;
            _drawingPlaceCanvasGroup.blocksRaycasts = false;
        }

        if (_multiplierAreaCanvasGroup != null)
        {
            _multiplierAreaCanvasGroup.alpha = 1f;
            _multiplierAreaCanvasGroup.interactable = false;
            _multiplierAreaCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (_isActive == false) return;

        HandleInput();

        float elapsed = Time.time - _gestureStartTime;
        UpdateTimerUI(Mathf.Max(0f, _timeLimit - elapsed), elapsed);

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
        StopAllCoroutines();

        if (_skillDrawCanvasRoot != null) _skillDrawCanvasRoot.SetActive(true);

        _drawAreaScreenRect = CalculateDrawAreaScreenRect();

        _onComplete = onComplete;

        IsDrawing = true;

        _allPoints.Clear();
        ClearPlayerLines();
        _currentStrokeId = -1;
        _isPointerDrawing = false;
        _hasStroke = false;
        _pendingNewStroke = false;

        // Fade In 도중에 이전 판정의 잔여 Fill 값/색이 잠깐 보이는 것 방지 (미리 리셋)
        if (_timerFillImage != null)
        {
            _timerFillImage.DOKill();
            _timerFillImage.fillAmount = 0f;

            Color resetColor = _timerFillDefaultColor;
            resetColor.a = 1f; // 지난 판정의 사라짐 연출에서 0으로 페이드아웃했을 수 있으니 복원
            _timerFillImage.color = resetColor;
        }
        if (_timerText != null) _timerText.text = _timeLimit.ToString("0.0");

        UpdateExampleImage(skillData.DrawExampleSprite);

        if (_tooltipObject != null) _tooltipObject.SetActive(true);
        if (_headerObject != null) _headerObject.SetActive(true);

        // MultiplierArea는 매 판정마다 깨끗한 상태(x0.00, Fill 0)로 리셋, alpha는 항상 1 유지
        if (_multiplierAreaCanvasGroup != null)
        {
            _multiplierAreaCanvasGroup.DOKill();
            _multiplierAreaCanvasGroup.alpha = 1f;
            _multiplierAreaCanvasGroup.interactable = false;
            _multiplierAreaCanvasGroup.blocksRaycasts = false;
        }

        if (_multiplierText != null)
        {
            _multiplierText.DOKill();
            _multiplierText.text = "x 0.00";
        }

        if (_multiplierSliderFillImage != null)
        {
            _multiplierSliderFillImage.DOKill();
            _multiplierSliderFillImage.fillAmount = 0f;
        }

        // 타이머/입력은 아직 시작하지 않음 (Fade In 완료 후 시작)
        _isActive = false;

        PlayFadeIn();
    }

    /// <summary>
    /// Header의 DrawImg에 이번 스킬의 예시 이미지를 세팅. 없으면 이미지 비활성화.
    /// </summary>
    private void UpdateExampleImage(Sprite exampleSprite)
    {
        if (_drawExampleImage == null) return;

        _drawExampleImage.sprite = exampleSprite;
        _drawExampleImage.enabled = exampleSprite != null;
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
        IsDrawing = false;
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
            _isPointerDrawing = true;

            // 아직 실제 스트로크(라인)를 시작하지 않음 - Rect 안에 첫 유효한 점이 찍힐 때
            // AddPoint 내부에서 지연 시작함. 이래야 Rect 바깥 클릭이 스트로크로 인정돼서
            // 손 뗀 뒤 _interStrokeTimeout 카운트다운이 도는 것을 방지할 수 있음.
            _pendingNewStroke = true;

            AddPoint(mouse.position.ReadValue());
        }
        else if (mouse.leftButton.isPressed && _isPointerDrawing)
        {
            AddPoint(mouse.position.ReadValue());
        }
        else if (mouse.leftButton.wasReleasedThisFrame && _isPointerDrawing)
        {
            _isPointerDrawing = false;

            // 실제로 유효한 점이 한 번이라도 찍힌 경우에만 스트로크 종료 시각을 기록
            // (Rect 바깥에서만 클릭했다 뗀 경우는 _hasStroke가 여전히 false라 타임아웃 대상이 아님)
            if (_hasStroke)
            {
                _strokeEndTime = Time.time;
            }
        }
    }

    private void AddPoint(Vector2 screenPos)
    {
        // 그리기 영역(Rect) 바깥이면 아예 점을 추가하지 않음 (가장자리에 라인이 눌어붙는 것 방지)
        // + 아직 스트로크가 시작되지 않은 상태라면, 스트로크 시작 자체도 보류됨.
        if (_drawAreaScreenRect.Contains(screenPos) == false)
        {
            return;
        }

        if (_pendingNewStroke)
        {
            _pendingNewStroke = false;
            _currentStrokeId++;
            _hasStroke = true;

            // 플레이어가 실제로 라인을 그리기 시작하는 순간 즉시 숨김 (이후 스트로크에도 계속 숨김 유지)
            if (_tooltipObject != null) _tooltipObject.SetActive(false);

            _currentPlayerLine = GetOrCreatePlayerLine(_currentStrokeId);
            _currentPlayerLine.positionCount = 0;

            _currentOutlineLine = GetOrCreateOutlineLine(_currentStrokeId);
            _currentOutlineLine.positionCount = 0;
            _currentOutlineLine.enabled = false; // 그리는 동안엔 숨김, 판정 후 와이프 시작 시 드러남
        }

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

        _currentOutlineLine.positionCount++;
        _currentOutlineLine.SetPosition(_currentOutlineLine.positionCount - 1, worldPos);
    }

    private Vector3 ViewportToDrawWorld(Vector2 viewport)
    {
        return _cam.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, _drawDistance));
    }

    // ---------- 판정 ----------

    private void FinishDrawing()
    {
        _isActive = false;
        _isPointerDrawing = false;

        CompleteTimerFill();

        float score = 0f;
        float multiplier;

        if (_allPoints.Count >= 2)
        {
            score = GestureMatcher.ComputeSimilarityScore(_allPoints, _guidePoints);
            multiplier = ScoreToMultiplier(score);
            Debug.Log($"[SkillDrawController] 유사도 {score:0} / 배율 x{multiplier:0.00}");
        }
        else
        {
            multiplier = ScoreToMultiplier(score); // score는 0이므로 _minDamageMultiplier로 자연스럽게 매핑됨
            Debug.Log($"[SkillDrawController] 그린 게 없음 / 배율 x{multiplier:0.00}");
        }

        ShowMultiplierResult(score, multiplier);
    }

    /// <summary>
    /// TimerFillImage를 짧은 시간에 걸쳐 100%까지 마저 채움. 조기 종료(스트로크 타임아웃) 시
    /// 중간값에서 뚝 멈춘 것처럼 보이는 걸 방지.
    /// </summary>
    private void CompleteTimerFill()
    {
        if (_timerFillImage == null) return;

        _timerFillImage.DOKill();
        _timerFillImage.DOFillAmount(1f, _timerCompleteFillDuration).SetEase(Ease.OutQuad);
    }

    private void ShowMultiplierResult(float score, float multiplier)
    {
        StartCoroutine(HoldResultThenDisappear(score, multiplier));
    }

    private IEnumerator HoldResultThenDisappear(float score, float multiplier)
    {
        yield return new WaitForSeconds(_resultHoldDuration);

        if (_headerObject != null) _headerObject.SetActive(false);
        // ↑ 여기서 _multiplierText.gameObject.SetActive(false) 줄은 삭제 (더 이상 여기서 안 다룸)

        Color resultColor = _resultColorGradient.Evaluate(Mathf.Clamp01(score / 100f));

        if (_timerFillImage != null)
        {
            _timerFillImage.DOKill();

            Color targetColor = resultColor;
            targetColor.a = _timerFillImage.color.a;

            _timerFillImage.DOColor(targetColor, _colorWipeDuration);
        }

        // 1. Outline이 그린 순서대로 알파 0->1 드러나며 resultColor로 물듦
        yield return StartCoroutine(ColorWipeRoutine(resultColor));

        // 2. 다 채워지면 두께가 살짝 커졌다가 되돌아옴
        yield return StartCoroutine(PulseLinesRoutine());

        // 3. 최종 페이드아웃 (마법진 라인)
        yield return StartCoroutine(FinalFadeOutRoutine(resultColor, _disappearDuration));

        if (_timerFillImage != null)
        {
            _timerFillImage.DOKill();
            _timerFillImage.DOFade(0f, _disappearDuration);
        }

        ClearPlayerLines();

        // 4. 마법진이 완전히 사라진 뒤에야 MultiplierArea 등장 + 슬라이더/텍스트 카운트업
        yield return StartCoroutine(RevealMultiplierArea(multiplier));

        // 5. 다 채워진 배율을 잠깐 보여준 뒤
        yield return new WaitForSeconds(_multiplierHoldDuration);

        // 6. MultiplierArea 페이드아웃
        yield return StartCoroutine(FadeOutMultiplierArea());

        PlayFadeOut(multiplier);
    }

    /// <summary>
    /// MultiplierArea를 페이드 인하고, 슬라이더 Fill과 텍스트를 x0.0에서 targetMultiplier까지
    /// 같은 시간(_multiplierRevealDuration)에 걸쳐 동시에 채움 - 둘이 정확히 같은 타이밍에 끝남.
    /// </summary>
    private IEnumerator RevealMultiplierArea(float targetMultiplier)
    {
        if (_multiplierSliderFillImage != null) _multiplierSliderFillImage.fillAmount = 0f;
        if (_multiplierText != null) _multiplierText.text = "x 0.00";

        float maxRange = Mathf.Max(0.0001f, _maxDamageMultiplier);
        float targetFill = Mathf.Clamp01(targetMultiplier / maxRange);

        if (_multiplierSliderFillImage != null)
        {
            _multiplierSliderFillImage.DOKill();
            _multiplierSliderFillImage.DOFillAmount(targetFill, _multiplierRevealDuration).SetEase(_multiplierRevealEase);
        }

        float value = 0f;

        Tween textTween = DOTween.To(
            () => value,
            x =>
            {
                value = x;
                if (_multiplierText != null) _multiplierText.text = $"x {value:0.00}";
            },
            targetMultiplier,
            _multiplierRevealDuration).SetEase(_multiplierRevealEase);

        yield return textTween.WaitForCompletion();

        if (_multiplierText != null) _multiplierText.text = $"x {targetMultiplier:0.00}";
        if (_multiplierSliderFillImage != null) _multiplierSliderFillImage.fillAmount = targetFill;
    }

    /// <summary>
    /// 배율 확인이 끝난 뒤 MultiplierArea를 페이드아웃.
    /// </summary>
    private IEnumerator FadeOutMultiplierArea()
    {
        if (_multiplierAreaCanvasGroup == null) yield break;

        _multiplierAreaCanvasGroup.DOKill();

        yield return _multiplierAreaCanvasGroup
            .DOFade(0f, _multiplierAreaFadeDuration)
            .SetEase(_multiplierAreaFadeEase)
            .WaitForCompletion();
    }

    /// <summary>
    /// Fill은 _applyColorWipeToFill이 true일 때만 resultColor로 그린 순서대로 와이프.
    /// Outline은 항상 cutoff 진행도에 맞춰 알파 0(숨김) -> 1(resultColor로 드러남)로 변함.
    /// </summary>
    private IEnumerator ColorWipeRoutine(Color resultColor)
    {
        if (_outlineLines.Count == 0 && _playerLines.Count == 0)
            yield break;

        // Fill의 원래 색상을 Color Wipe 시작 전에 저장 (ColorGradient 적용 시 startColor도 바뀔 수 있어 미리 보관)
        List<Color> originalFillColors = new List<Color>(_playerLines.Count);

        for (int i = 0; i < _playerLines.Count; i++)
        {
            LineRenderer fillLine = _playerLines[i];

            originalFillColors.Add(
                fillLine != null
                    ? fillLine.startColor
                    : Color.white);
        }

        // Outline은 와이프 시작 시 렌더러를 켜서 드러나기 시작
        for (int i = 0; i < _outlineLines.Count; i++)
        {
            if (_outlineLines[i] != null)
                _outlineLines[i].enabled = true;
        }

        float elapsed = 0f;

        while (elapsed < _colorWipeDuration)
        {
            elapsed += Time.deltaTime;

            float cutoff = _colorWipeDuration > 0f
                ? Mathf.Clamp01(elapsed / _colorWipeDuration)
                : 1f;

            if (_applyColorWipeToFill)
            {
                for (int i = 0; i < _playerLines.Count; i++)
                {
                    LineRenderer fillLine = _playerLines[i];
                    if (fillLine == null) continue;

                    ApplyWipeGradient(
                        fillLine,
                        cutoff,
                        originalFillColors[i],
                        resultColor,
                        1f);
                }
            }

            for (int i = 0; i < _outlineLines.Count; i++)
            {
                LineRenderer outlineLine = _outlineLines[i];
                if (outlineLine == null) continue;

                ApplyOutlineWipeGradient(
                    outlineLine,
                    cutoff,
                    resultColor);
            }

            yield return null;
        }

        // 마지막 프레임에서 정확하게 100% 상태로 고정
        if (_applyColorWipeToFill)
        {
            for (int i = 0; i < _playerLines.Count; i++)
            {
                LineRenderer fillLine = _playerLines[i];
                if (fillLine == null) continue;

                ApplyWipeGradient(
                    fillLine,
                    1f,
                    originalFillColors[i],
                    resultColor,
                    1f);
            }
        }

        for (int i = 0; i < _outlineLines.Count; i++)
        {
            LineRenderer outlineLine = _outlineLines[i];
            if (outlineLine == null) continue;

            ApplyOutlineWipeGradient(
                outlineLine,
                1f,
                resultColor);
        }
    }

    /// <summary>
    /// cutoff(0~1) 지점을 경계로, 그 이전(포인트 인덱스 기준 앞쪽)은 resultColor, 이후는 originalColor로
    /// 칠해지는 Gradient를 라인에 적용. alpha는 균일하게 적용(페이드아웃 단계에서 별도로 사용).
    /// </summary>
    private void ApplyWipeGradient(LineRenderer line, float cutoff, Color originalColor, Color resultColor, float alpha)
    {
        const float epsilon = 0.001f;

        GradientColorKey[] colorKeys;

        if (cutoff <= 0f)
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(originalColor, 0f),
                new GradientColorKey(originalColor, 1f)
            };
        }
        else if (cutoff >= 1f)
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(resultColor, 0f),
                new GradientColorKey(resultColor, 1f)
            };
        }
        else
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(resultColor, 0f),
                new GradientColorKey(resultColor, Mathf.Max(0f, cutoff - epsilon)),
                new GradientColorKey(originalColor, Mathf.Min(1f, cutoff + epsilon)),
                new GradientColorKey(originalColor, 1f)
            };
        }

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(alpha, 0f),
            new GradientAlphaKey(alpha, 1f)
        };

        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        line.colorGradient = gradient;
    }

    /// <summary>
    /// Outline 전용: 색은 항상 resultColor 고정, cutoff 이전(그린 순서 기준 앞쪽)은 알파 1(보임),
    /// 이후는 알파 0(숨김)으로 - 와이프가 지나가는 부분만 드러나 보이게 함.
    /// </summary>
    private void ApplyOutlineWipeGradient(LineRenderer line, float cutoff, Color resultColor)
    {
        const float epsilon = 0.001f;

        GradientColorKey[] colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(resultColor, 0f),
            new GradientColorKey(resultColor, 1f)
        };

        GradientAlphaKey[] alphaKeys;

        if (cutoff <= 0f)
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0f, 1f)
            };
        }
        else if (cutoff >= 1f)
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
        }
        else
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, Mathf.Max(0f, cutoff - epsilon)),
                new GradientAlphaKey(0f, Mathf.Min(1f, cutoff + epsilon)),
                new GradientAlphaKey(0f, 1f)
            };
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        line.colorGradient = gradient;
    }

    /// <summary>
    /// 색이 다 채워진 뒤, Fill+Outline 모든 라인의 두께(widthMultiplier)가 잠깐 커졌다가 원래 크기로 돌아오는 펄스.
    /// </summary>
    private IEnumerator PulseLinesRoutine()
    {
        int totalCount = _playerLines.Count + _outlineLines.Count;
        if (totalCount == 0 || _pulseDuration <= 0f) yield break;

        float halfDuration = _pulseDuration * 0.5f;

        List<LineRenderer> allLines = new List<LineRenderer>();
        allLines.AddRange(_playerLines);
        allLines.AddRange(_outlineLines);

        List<float> originalWidths = new List<float>();
        for (int i = 0; i < allLines.Count; i++)
        {
            originalWidths.Add(allLines[i] != null ? allLines[i].widthMultiplier : 1f);
        }

        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < allLines.Count; i++)
        {
            LineRenderer line = allLines[i];
            if (line == null) continue;

            sequence.Join(DOTween.To(() => line.widthMultiplier, x => line.widthMultiplier = x, originalWidths[i] * _pulseWidthMultiplier, halfDuration));
        }

        yield return sequence.WaitForCompletion();

        Sequence sequenceBack = DOTween.Sequence();

        for (int i = 0; i < allLines.Count; i++)
        {
            LineRenderer line = allLines[i];
            if (line == null) continue;

            sequenceBack.Join(DOTween.To(() => line.widthMultiplier, x => line.widthMultiplier = x, originalWidths[i], halfDuration));
        }

        yield return sequenceBack.WaitForCompletion();
    }

    /// <summary>
    /// Fill은 각자의 색(_applyColorWipeToFill에 따라 resultColor 또는 원래 색)을 유지한 채로,
    /// Outline은 resultColor를 유지한 채로, 둘 다 알파와 두께를 duration에 걸쳐 0으로 페이드아웃.
    /// </summary>
    private IEnumerator FinalFadeOutRoutine(Color resultColor, float duration)
    {
        int totalCount = _playerLines.Count + _outlineLines.Count;
        if (totalCount == 0 || duration <= 0f) yield break;

        // Fill은 지금 유지 중인 색을 그대로 페이드아웃에 사용
        List<Color> fillColors = new List<Color>();
        for (int i = 0; i < _playerLines.Count; i++)
        {
            fillColors.Add(_playerLines[i] != null ? _playerLines[i].startColor : Color.white);
        }

        List<LineRenderer> allLines = new List<LineRenderer>();
        allLines.AddRange(_playerLines);
        allLines.AddRange(_outlineLines);

        List<float> startWidths = new List<float>();
        for (int i = 0; i < allLines.Count; i++)
        {
            startWidths.Add(allLines[i] != null ? allLines[i].widthMultiplier : 1f);
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = 1f - t;

            for (int i = 0; i < allLines.Count; i++)
            {
                LineRenderer line = allLines[i];
                if (line == null) continue;

                bool isFill = i < _playerLines.Count;

                Color lineColor;

                if (isFill)
                {
                    lineColor = _applyColorWipeToFill
                        ? resultColor
                        : fillColors[i];
                }
                else
                {
                    lineColor = resultColor;
                }

                ApplyWipeGradient(
                    line,
                    1f,
                    lineColor,
                    lineColor,
                    alpha);

                line.widthMultiplier = Mathf.Lerp(
                    startWidths[i],
                    0f,
                    t);
            }

            yield return null;
        }
    }

    /// <summary>
    /// 0~threshold 구간은 _minDamageMultiplier~1로, threshold~100 구간은 1~_maxDamageMultiplier로
    /// 각각 선형 매핑 (threshold 지점이 배율 1.0이 되는 경계).
    /// </summary>
    private float ScoreToMultiplier(float score)
    {
        if (score < _scoreThreshold)
        {
            float t = _scoreThreshold > 0f
                ? Mathf.InverseLerp(0f, _scoreThreshold, score)
                : 1f;

            return Mathf.Lerp(_minDamageMultiplier, 1f, t);
        }

        float tHigh = Mathf.InverseLerp(_scoreThreshold, 100f, score);
        return Mathf.Lerp(1f, _maxDamageMultiplier, tHigh);
    }

    private void UpdateTimerUI(float remaining, float elapsed)
    {
        if (_timerText != null) _timerText.text = remaining.ToString("0.0");

        // Fill은 "남은 시간"이 아니라 "경과 시간" 기준 0 -> 1로 채워지도록 (반대 방향)
        if (_timerFillImage != null) _timerFillImage.fillAmount = _timeLimit > 0f ? Mathf.Clamp01(elapsed / _timeLimit) : 0f;
    }

    // ---------- LineRenderer 풀 관리 (Fill + Outline) ----------

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

    private LineRenderer GetOrCreateOutlineLine(int index)
    {
        while (_outlineLines.Count <= index)
        {
            LineRenderer clone = Instantiate(_outlineLinePrefab, transform);
            clone.gameObject.SetActive(true);
            clone.enabled = false; // 그리는 동안엔 숨김
            _outlineLines.Add(clone);
        }
        return _outlineLines[index];
    }

    private void ClearPlayerLines()
    {
        foreach (var line in _playerLines) if (line != null) Destroy(line.gameObject);
        _playerLines.Clear();

        foreach (var line in _outlineLines) if (line != null) Destroy(line.gameObject);
        _outlineLines.Clear();
    }
}