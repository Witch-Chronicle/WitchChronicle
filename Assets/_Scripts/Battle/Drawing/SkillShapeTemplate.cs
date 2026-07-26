using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// fire_ball.json 같은 스킬 궤적 JSON(TextAsset)을 SkillPoint 리스트로 파싱.
/// SkillRecognizer의 템플릿 로딩과 동일한 포맷.
/// </summary>
public static class SkillShapeTemplate
{
    [System.Serializable]
    private class TemplateJson
    {
        public string skillName;
        public List<PointJson> points;
    }

    [System.Serializable]
    private class PointJson
    {
        public float x;
        public float y;
        public int strokeId;
    }

    public static List<SkillPoint> ParsePoints(TextAsset json)
    {
        if (json == null) return null;

        TemplateJson data = JsonUtility.FromJson<TemplateJson>(json.text);

        if (data?.points == null || data.points.Count < 2) return null;

        return data.points
            .Select(p => new SkillPoint(new Vector2(p.x, p.y), p.strokeId))
            .ToList();
    }
}