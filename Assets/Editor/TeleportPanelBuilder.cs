using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 빠른 이동 패널을 메뉴 한 번으로 만들어주는 에디터 도구.
///
/// 메뉴: Tools > Witch Chronicle > 빠른 이동 패널 생성
///
/// 하는 일
///  - 선택한 Canvas(없으면 씬의 첫 Canvas) 아래에 패널 계층을 만든다
///  - 목적지 버튼 프리팹을 Assets/_Prefabs/UI/ 에 저장한다
///  - TeleportPanel의 슬롯 4개를 자동으로 연결한다
///
/// 만들고 나면 배경색·크기 같은 건 인스펙터에서 취향대로 조정하면 된다.
/// </summary>
public static class TeleportPanelBuilder
{
    private const string PrefabFolder = "Assets/_Prefabs/UI";
    private const string EntryPrefabPath = PrefabFolder + "/TeleportEntryButton.prefab";

    // 한글이 포함된 프로젝트 기본 폰트
    private const string KoreanFontPath = "Assets/05. HJH/Import/NEXON_Lv2_Gothic/TTF/NEXON Lv2 Gothic SDF.asset";

    private static TMP_FontAsset _koreanFont;

    /// <summary>
    /// 한글 폰트를 적용한다. 못 찾으면 기본 폰트를 그대로 둔다.
    /// </summary>
    /// <param name="text">대상 텍스트</param>
    private static void ApplyFont(TMP_Text text)
    {
        if (_koreanFont == null)
        {
            _koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        }

        if (_koreanFont == null)
        {
            Debug.LogWarning($"[TeleportPanelBuilder] 한글 폰트를 찾지 못했습니다: {KoreanFontPath}");
            return;
        }

        text.font = _koreanFont;
    }

    [MenuItem("Tools/Witch Chronicle/빠른 이동 패널 생성")]
    private static void Build()
    {
        Canvas canvas = FindCanvas();

        if (canvas == null)
        {
            EditorUtility.DisplayDialog("빠른 이동 패널", "씬에서 Canvas를 찾지 못했습니다.\nCanvas를 먼저 만들거나 선택한 뒤 다시 실행하세요.", "확인");
            return;
        }

        // ── 패널 루트 ────────────────────────────────
        GameObject panel = NewUI("TeleportPanel", canvas.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        Stretch(panelRect, 0.5f, 0.5f, new Vector2(560f, 620f));

        panel.AddComponent<CanvasGroup>();
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.05f, 0.09f, 0.94f);

        UIPanelAnimator animator = panel.AddComponent<UIPanelAnimator>();
        TeleportPanel teleportPanel = panel.AddComponent<TeleportPanel>();

        // ── 제목 ────────────────────────────────────
        GameObject title = NewUI("TitleText", panel.transform);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(-40f, 56f);

        TMP_Text titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "빠른 이동";
        titleText.fontSize = 34f;
        titleText.alignment = TextAlignmentOptions.Center;
        ApplyFont(titleText);

        // ── 목록 (Scroll View) ──────────────────────
        GameObject scroll = NewUI("ScrollView", panel.transform);
        RectTransform scrollRect = scroll.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(24f, 88f);
        scrollRect.offsetMax = new Vector2(-24f, -84f);

        scroll.AddComponent<RectMask2D>();
        ScrollRect scrollComponent = scroll.AddComponent<ScrollRect>();
        scrollComponent.horizontal = false;

        GameObject content = NewUI("Content", scroll.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollComponent.content = contentRect;
        scrollComponent.viewport = scrollRect;

        // ── 닫기 버튼 ───────────────────────────────
        GameObject close = NewUI("CloseButton", panel.transform);
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 20f);
        closeRect.sizeDelta = new Vector2(200f, 54f);

        Image closeBg = close.AddComponent<Image>();
        closeBg.color = new Color(0.25f, 0.22f, 0.32f, 1f);
        Button closeButton = close.AddComponent<Button>();

        GameObject closeLabel = NewUI("Text", close.transform);
        Stretch(closeLabel.GetComponent<RectTransform>(), 0.5f, 0.5f, Vector2.zero, true);
        TMP_Text closeText = closeLabel.AddComponent<TextMeshProUGUI>();
        closeText.text = "닫기";
        closeText.fontSize = 26f;
        closeText.alignment = TextAlignmentOptions.Center;
        ApplyFont(closeText);

        // ── 목적지 버튼 프리팹 ──────────────────────
        Button entryPrefab = CreateEntryPrefab();

        // ── TeleportPanel 슬롯 연결 ─────────────────
        SerializedObject so = new SerializedObject(teleportPanel);
        so.FindProperty("_panelAnimator").objectReferenceValue = animator;
        so.FindProperty("_entryPrefab").objectReferenceValue = entryPrefab;
        so.FindProperty("_entryRoot").objectReferenceValue = contentRect;
        so.FindProperty("_closeButton").objectReferenceValue = closeButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(panel, "빠른 이동 패널 생성");
        Selection.activeGameObject = panel;
        EditorGUIUtility.PingObject(panel);

        Debug.Log("[TeleportPanelBuilder] 빠른 이동 패널을 만들었습니다. TeleportPortal의 Teleport Panel 슬롯에 연결하세요.");
    }

    /// <summary>
    /// 목적지 하나를 표시할 버튼 프리팹을 만들어 저장한다.
    /// </summary>
    private static Button CreateEntryPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath) != null)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath).GetComponent<Button>();
        }

        if (AssetDatabase.IsValidFolder(PrefabFolder) == false)
        {
            AssetDatabase.CreateFolder("Assets/_Prefabs", "UI");
        }

        GameObject temp = NewUI("TeleportEntryButton", null);
        RectTransform rect = temp.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 64f);

        Image bg = temp.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.16f, 0.24f, 1f);
        temp.AddComponent<Button>();

        LayoutElement element = temp.AddComponent<LayoutElement>();
        element.preferredHeight = 64f;

        GameObject label = NewUI("Text", temp.transform);
        Stretch(label.GetComponent<RectTransform>(), 0.5f, 0.5f, Vector2.zero, true);
        TMP_Text text = label.AddComponent<TextMeshProUGUI>();
        text.text = "목적지";
        text.fontSize = 26f;
        text.alignment = TextAlignmentOptions.Center;
        ApplyFont(text);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, EntryPrefabPath);
        Object.DestroyImmediate(temp);

        return saved.GetComponent<Button>();
    }

    private static Canvas FindCanvas()
    {
        if (Selection.activeGameObject != null)
        {
            Canvas selected = Selection.activeGameObject.GetComponentInParent<Canvas>();

            if (selected != null)
            {
                return selected.rootCanvas;
            }
        }

        return Object.FindFirstObjectByType<Canvas>();
    }

    private static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));

        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        return go;
    }

    private static void Stretch(RectTransform rect, float pivotX, float pivotY, Vector2 size, bool fill = false)
    {
        if (fill)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return;
        }

        rect.anchorMin = new Vector2(pivotX, pivotY);
        rect.anchorMax = new Vector2(pivotX, pivotY);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }
}
