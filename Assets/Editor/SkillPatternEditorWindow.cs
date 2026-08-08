using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 웹 버전 "스킬 좌표 에디터"를 Unity Editor 창으로 그대로 옮긴 툴입니다.
///
/// 사용법:
/// 1. 메뉴에서 Tools > Skill Pattern Editor 실행
/// 2. "이미지 불러오기"로 원본 이미지를 선택
/// 3. 캔버스 위에서 마우스를 누른 채 드래그해서 한 획을 그림. 손을 떼면 획 완성, 다시 누르면 새 획.
/// 4. 필요하면 "마지막 획 되돌리기" / "전체 초기화"
/// 5. 스킬 이름 입력 후 "JSON으로 저장" -> 저장 폴더를 고르면
///    {skillName}.json 과 원본 이미지 사본이 같은 폴더에 함께 저장됩니다.
///
/// 반드시 프로젝트의 "Editor" 폴더 아래(예: Assets/Editor/)에 넣어야 컴파일됩니다.
/// </summary>
public class SkillPatternEditorWindow : EditorWindow
{
    [Serializable]
    private class SkillPatternPoint
    {
        public float x;
        public float y;
        public int strokeId;
    }

    [Serializable]
    private class SkillPatternData
    {
        public string skillName;
        public int strokeCount;
        public List<SkillPatternPoint> points;
    }

    private static readonly Color[] StrokeColors =
    {
        new Color(0.357f, 0.549f, 1f),
        new Color(0.306f, 0.796f, 0.443f),
        new Color(1f, 0.718f, 0.302f),
        new Color(1f, 0.42f, 0.42f),
        new Color(0.780f, 0.490f, 1f),
        new Color(0.302f, 0.816f, 0.882f),
        new Color(0.941f, 0.384f, 0.573f),
        new Color(0.631f, 0.533f, 0.494f),
    };

    private const float MinPointDist = 0.004f; // 정규화 좌표 기준. 너무 촘촘한 점 스킵.
    private const float MaxCanvasSize = 640f;

    private Texture2D _texture;
    private byte[] _sourceImageBytes;
    private string _sourceImageExtension = ".png";

    private string _skillName = "";

    // 모든 점은 캔버스 기준 정규화 좌표 (0~1, 좌상단 원점, y는 아래로 증가). 저장 시점에만 y를 뒤집습니다.
    private readonly List<List<Vector2>> _strokes = new List<List<Vector2>>();
    private List<Vector2> _currentStroke;
    private bool _isDrawing;

    private string _statusMessage = "";
    private MessageType _statusType = MessageType.None;

    [MenuItem("Tools/Skill Pattern Editor")]
    private static void Open()
    {
        SkillPatternEditorWindow window = GetWindow<SkillPatternEditorWindow>("Skill Pattern Editor");
        window.minSize = new Vector2(760, 520);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawSidebar();
        DrawCanvasArea();
        EditorGUILayout.EndHorizontal();

        HandleGlobalMouseUp();
    }

    // ───────────────────────── Sidebar ─────────────────────────
    private void DrawSidebar()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(280));

        EditorGUILayout.LabelField("스킬 좌표 에디터", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "이미지를 불러오고, 그 위에서 마우스를 드래그해서 실제로 따라 그리세요. " +
            "손을 떼면 한 획이 완성되고, 다시 누르면 새 획이 시작됩니다.",
            MessageType.None);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("이미지 불러오기"))
        {
            LoadImage();
        }

        if (_texture != null)
        {
            EditorGUILayout.LabelField($"이미지 크기: {_texture.width} x {_texture.height}", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("스킬 이름 (skillName)");
        _skillName = EditorGUILayout.TextField(_skillName);

        EditorGUILayout.Space(8);
        DrawStatsBox();

        EditorGUILayout.Space(4);
        DrawStrokeLegend();

        EditorGUILayout.Space(8);

        EditorGUI.BeginDisabledGroup(_strokes.Count == 0);
        if (GUILayout.Button("마지막 획 되돌리기"))
        {
            _strokes.RemoveAt(_strokes.Count - 1);
            Repaint();
        }
        EditorGUI.EndDisabledGroup();

        Color prevColor = GUI.color;
        GUI.color = new Color(1f, 0.6f, 0.6f);
        EditorGUI.BeginDisabledGroup(_strokes.Count == 0 && _currentStroke == null);
        if (GUILayout.Button("전체 초기화"))
        {
            _strokes.Clear();
            _currentStroke = null;
            Repaint();
        }
        EditorGUI.EndDisabledGroup();
        GUI.color = prevColor;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        bool canExport = _texture != null && _strokes.Count > 0;
        EditorGUI.BeginDisabledGroup(!canExport);
        GUI.backgroundColor = canExport ? new Color(0.357f, 0.549f, 1f) : GUI.backgroundColor;
        if (GUILayout.Button("JSON으로 저장", GUILayout.Height(30)))
        {
            ExportToJson();
        }
        GUI.backgroundColor = Color.white;
        EditorGUI.EndDisabledGroup();

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStatsBox()
    {
        int totalPoints = 0;
        foreach (List<Vector2> stroke in _strokes)
        {
            totalPoints += stroke.Count;
        }

        EditorGUILayout.LabelField($"획 개수: {_strokes.Count}개   |   전체 점: {totalPoints}개", EditorStyles.miniBoldLabel);
    }

    private void DrawStrokeLegend()
    {
        for (int i = 0; i < _strokes.Count; i++)
        {
            Color color = StrokeColors[i % StrokeColors.Length];
            EditorGUILayout.BeginHorizontal();

            Rect swatchRect = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f));
            EditorGUI.DrawRect(swatchRect, color);

            EditorGUILayout.LabelField($"{i + 1}번 획 ({_strokes[i].Count}점)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    }

    // ───────────────────────── Canvas ─────────────────────────
    private void DrawCanvasArea()
    {
        EditorGUILayout.BeginVertical();

        if (_texture == null)
        {
            EditorGUILayout.HelpBox("이미지를 불러오면 여기에 표시됩니다.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        Rect canvasRect = GetCanvasRect();
        Handles.BeginGUI();

        GUI.DrawTexture(canvasRect, _texture, ScaleMode.StretchToFill);
        DrawGridBorder(canvasRect);

        for (int i = 0; i < _strokes.Count; i++)
        {
            DrawStroke(_strokes[i], StrokeColors[i % StrokeColors.Length], canvasRect);
        }

        if (_currentStroke != null)
        {
            DrawStroke(_currentStroke, StrokeColors[_strokes.Count % StrokeColors.Length], canvasRect);
        }

        Handles.EndGUI();

        HandleCanvasMouseInput(canvasRect);

        // GUILayout이 실제로 이 영역을 차지하도록 자리만 예약합니다.
        GUILayoutUtility.GetRect(canvasRect.width, canvasRect.height);

        EditorGUILayout.EndVertical();
    }

    private Rect GetCanvasRect()
    {
        float availableWidth = position.width - 300f;
        float availableHeight = position.height - 40f;

        float aspect = (float)_texture.width / _texture.height;
        float w = Mathf.Min(MaxCanvasSize, availableWidth);
        float h = w / aspect;

        if (h > availableHeight)
        {
            h = availableHeight;
            w = h * aspect;
        }

        Vector2 origin = GUILayoutUtility.GetLastRect().position;
        return new Rect(300f, 10f, w, h);
    }

    private void DrawGridBorder(Rect rect)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.15f);
        Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(1f, 1f, 1f, 0.25f));
    }

    private void DrawStroke(List<Vector2> points, Color color, Rect canvasRect)
    {
        if (points.Count < 2)
        {
            return;
        }

        Vector3[] screenPoints = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            screenPoints[i] = NormalizedToScreen(points[i], canvasRect);
        }

        Handles.color = color;
        Handles.DrawAAPolyLine(3f, screenPoints);

        DrawMarker(screenPoints[0], new Color(0.306f, 0.796f, 0.443f));
        DrawMarker(screenPoints[screenPoints.Length - 1], new Color(1f, 0.42f, 0.42f));
    }

    private void DrawMarker(Vector3 position, Color fillColor)
    {
        Handles.color = fillColor;
        Handles.DrawSolidDisc(position, Vector3.forward, 4f);
        Handles.color = Color.white;
        Handles.DrawWireDisc(position, Vector3.forward, 4f);
    }

    private Vector3 NormalizedToScreen(Vector2 normalized, Rect canvasRect)
    {
        return new Vector3(
            canvasRect.x + normalized.x * canvasRect.width,
            canvasRect.y + normalized.y * canvasRect.height,
            0f);
    }

    // ───────────────────────── Mouse Input ─────────────────────────
    private void HandleCanvasMouseInput(Rect canvasRect)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && canvasRect.Contains(e.mousePosition))
        {
            Vector2 local = ScreenToNormalized(e.mousePosition, canvasRect);
            _isDrawing = true;
            _currentStroke = new List<Vector2> { local };
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && _isDrawing)
        {
            Vector2 local = ScreenToNormalized(e.mousePosition, canvasRect);
            Vector2 last = _currentStroke[_currentStroke.Count - 1];

            if (Vector2.Distance(local, last) >= MinPointDist)
            {
                _currentStroke.Add(local);
                Repaint();
            }

            e.Use();
        }
    }

    /// <summary>
    /// 캔버스 밖에서 손을 떼도 획이 마무리되도록, MouseUp은 창 전체 기준으로 처리합니다.
    /// (웹 버전의 window.addEventListener('mouseup', endStroke)와 동일한 이유)
    /// </summary>
    private void HandleGlobalMouseUp()
    {
        Event e = Event.current;

        if (e.type != EventType.MouseUp || e.button != 0 || !_isDrawing)
        {
            return;
        }

        _isDrawing = false;

        if (_currentStroke != null && _currentStroke.Count >= 2)
        {
            _strokes.Add(_currentStroke);
        }

        _currentStroke = null;
        Repaint();
    }

    private Vector2 ScreenToNormalized(Vector2 screenPos, Rect canvasRect)
    {
        return new Vector2(
            Mathf.Clamp01((screenPos.x - canvasRect.x) / canvasRect.width),
            Mathf.Clamp01((screenPos.y - canvasRect.y) / canvasRect.height));
    }

    // ───────────────────────── 이미지 불러오기 ─────────────────────────
    private void LoadImage()
    {
        string path = EditorUtility.OpenFilePanel("이미지 선택", "", "png,jpg,jpeg");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        byte[] bytes;

        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            ShowStatus($"이미지를 읽지 못했습니다: {ex.Message}", MessageType.Error);
            return;
        }

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (tex.LoadImage(bytes) == false)
        {
            ShowStatus("이미지 형식을 해석하지 못했습니다.", MessageType.Error);
            return;
        }

        _texture = tex;
        _sourceImageBytes = bytes;
        _sourceImageExtension = Path.GetExtension(path);

        if (string.IsNullOrEmpty(_sourceImageExtension))
        {
            _sourceImageExtension = ".png";
        }

        _strokes.Clear();
        _currentStroke = null;
        _statusMessage = "";

        Repaint();
    }

    // ───────────────────────── 저장 ─────────────────────────
    private void ExportToJson()
    {
        string name = _skillName.Trim();

        if (string.IsNullOrEmpty(name))
        {
            ShowStatus("스킬 이름을 입력해주세요.", MessageType.Error);
            return;
        }

        if (_strokes.Count == 0)
        {
            ShowStatus("최소 한 획 이상 그려주세요.", MessageType.Error);
            return;
        }

        string folder = EditorUtility.SaveFolderPanel("저장할 폴더 선택", Application.dataPath, "");

        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        SkillPatternData data = BuildPatternData(name);
        string json = JsonUtility.ToJson(data, true);

        string jsonPath = Path.Combine(folder, $"{name}.json");
        string imagePath = Path.Combine(folder, $"{name}{_sourceImageExtension}");

        try
        {
            File.WriteAllText(jsonPath, json);

            if (_sourceImageBytes != null)
            {
                File.WriteAllBytes(imagePath, _sourceImageBytes);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"저장 실패: {ex.Message}", MessageType.Error);
            return;
        }

        // 저장 위치가 프로젝트(Assets) 안이라면 Project 창에 바로 보이도록 갱신합니다.
        if (folder.StartsWith(Application.dataPath))
        {
            AssetDatabase.Refresh();
        }

        ShowStatus($"저장 완료: {name}.json ({_strokes.Count}획, {data.points.Count}점) + 원본 이미지", MessageType.Info);
    }

    private SkillPatternData BuildPatternData(string skillName)
    {
        List<SkillPatternPoint> points = new List<SkillPatternPoint>();

        for (int strokeIndex = 0; strokeIndex < _strokes.Count; strokeIndex++)
        {
            List<Vector2> stroke = _strokes[strokeIndex];

            foreach (Vector2 p in stroke)
            {
                points.Add(new SkillPatternPoint
                {
                    x = Mathf.Round(p.x * 10000f) / 10000f,
                    y = Mathf.Round((1f - p.y) * 10000f) / 10000f, // 이미지 좌표 -> Unity 좌표계로 y 반전
                    strokeId = strokeIndex,
                });
            }
        }

        return new SkillPatternData
        {
            skillName = skillName,
            strokeCount = _strokes.Count,
            points = points,
        };
    }

    private void ShowStatus(string message, MessageType type)
    {
        _statusMessage = message;
        _statusType = type;
        Repaint();
    }
}