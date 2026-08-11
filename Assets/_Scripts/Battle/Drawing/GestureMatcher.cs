using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// $P Point-Cloud 방식 궤적 유사도 계산 유틸. SkillRecognizer와 SkillDrawMinigameController가 공유.
///
/// maxAcceptableDist / weightFalloff는 기본값을 기존 그대로 유지해서, 이 파라미터를 넘기지 않는
/// 다른 호출부(SkillRecognizer 등)의 판정 결과는 바뀌지 않습니다. 더 엄격하게 판정하고 싶은
/// 호출부(SkillDrawController 등)만 명시적으로 값을 넘겨서 조절하세요.
/// </summary>
public static class GestureMatcher
{
    /// <param name="maxAcceptableDist">
    /// 정규화된(대각선 길이=1 기준) 궤적에서 허용하는 최대 평균 오차. 작을수록 더 정확하게 그려야
    /// 높은 점수가 나옵니다. 기존 로직과 동일하게 기본값 0.5f.
    /// </param>
    /// <param name="weightFalloff">
    /// 궤적 뒷부분에 대한 가중치 감쇠 강도. 1이면 기존 로직처럼 매칭 시작점에서 멀어질수록(궤적 뒷부분일수록)
    /// 가중치가 선형으로 0까지 떨어집니다. 0이면 궤적 전체가 균일한 가중치로 반영됩니다(대충 그린 뒷부분도
    /// 앞부분만큼 점수에 영향을 줌). 기존 로직과 동일하게 기본값 1f.
    /// </param>
    public static float ComputeSimilarityScore(
        List<SkillPoint> playerRaw,
        List<SkillPoint> templateRaw,
        int resampleCount = 64,
        float maxAcceptableDist = 0.5f,
        float weightFalloff = 1f)
    {
        if (playerRaw == null || templateRaw == null || playerRaw.Count < 2 || templateRaw.Count < 2)
            return 0f;
        List<Vector2> a = Normalize(ResamplePath(playerRaw, resampleCount));
        List<Vector2> b = Normalize(ResamplePath(templateRaw, resampleCount));
        float weightedAvgDist = GreedyCloudMatch(a, b, weightFalloff);
        return Mathf.Clamp01(1f - (weightedAvgDist / maxAcceptableDist)) * 100f;
    }
    private static List<Vector2> Normalize(List<SkillPoint> resampled)
    {
        List<Vector2> positions = resampled.Select(p => p.pos).ToList();
        Vector2 centroid = GetCentroid(positions);
        positions = positions.Select(p => p - centroid).ToList();
        float scale = GetBoundingBoxScale(positions);
        return positions.Select(p => p * scale).ToList();
    }
    private static Vector2 GetCentroid(List<Vector2> points)
    {
        Vector2 sum = Vector2.zero;
        foreach (var p in points) sum += p;
        return sum / points.Count;
    }
    private static float GetBoundingBoxScale(List<Vector2> points)
    {
        float minX = points.Min(p => p.x), maxX = points.Max(p => p.x);
        float minY = points.Min(p => p.y), maxY = points.Max(p => p.y);
        float diagonal = Mathf.Sqrt((maxX - minX) * (maxX - minX) + (maxY - minY) * (maxY - minY));
        return diagonal < 0.0001f ? 1f : 1f / diagonal;
    }
    private static List<SkillPoint> ResamplePath(List<SkillPoint> points, int n)
    {
        float totalLength = GetPathLength(points);
        float interval = totalLength / (n - 1);
        List<SkillPoint> resampled = new List<SkillPoint> { points[0] };
        float accumulated = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].strokeId != points[i - 1].strokeId) continue;
            float segDist = Vector2.Distance(points[i - 1].pos, points[i].pos);
            if (accumulated + segDist >= interval)
            {
                Vector2 segStart = points[i - 1].pos;
                Vector2 segEnd = points[i].pos;
                float remaining = segDist;
                float D = accumulated;
                while (D + remaining >= interval && resampled.Count < n)
                {
                    float t = Mathf.Clamp01((interval - D) / remaining);
                    if (float.IsNaN(t)) t = 0.5f;
                    Vector2 newPos = Vector2.Lerp(segStart, segEnd, t);
                    resampled.Add(new SkillPoint(newPos, points[i].strokeId));
                    remaining = D + remaining - interval;
                    D = 0f;
                    segStart = newPos;
                }
                accumulated = remaining;
            }
            else
            {
                accumulated += segDist;
            }
            if (resampled.Count >= n) break;
        }
        while (resampled.Count < n) resampled.Add(points[points.Count - 1]);
        return resampled;
    }
    private static float GetPathLength(List<SkillPoint> points)
    {
        float length = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].strokeId == points[i - 1].strokeId)
                length += Vector2.Distance(points[i - 1].pos, points[i].pos);
        }
        return length;
    }
    private static float GreedyCloudMatch(List<Vector2> points1, List<Vector2> points2, float weightFalloff)
    {
        int n = points1.Count;
        float eps = 0.5f;
        int step = Mathf.Max(1, Mathf.FloorToInt(Mathf.Pow(n, 1f - eps)));
        float minDistance = float.MaxValue;
        for (int i = 0; i < n; i += step)
        {
            float dist1 = CloudDistance(points1, points2, i, weightFalloff);
            float dist2 = CloudDistance(points2, points1, i, weightFalloff);
            minDistance = Mathf.Min(minDistance, Mathf.Min(dist1, dist2));
        }
        return minDistance;
    }
    private static float CloudDistance(List<Vector2> points1, List<Vector2> points2, int startIndex, float weightFalloff)
    {
        int n = points1.Count;
        bool[] matched = new bool[n];
        float weightedSum = 0f, weightSum = 0f;
        int i = startIndex;
        do
        {
            int bestIndex = -1;
            float bestDist = float.MaxValue;
            for (int j = 0; j < n; j++)
            {
                if (matched[j]) continue;
                float dist = Vector2.Distance(points1[i], points2[j]);
                if (dist < bestDist) { bestDist = dist; bestIndex = j; }
            }
            matched[bestIndex] = true;
            float progress = ((i - startIndex + n) % n) / (float)n;
            // weightFalloff가 1이면 기존과 동일(뒤로 갈수록 가중치 0까지 감쇠),
            // 0이면 감쇠 없이 궤적 전체가 균일한 가중치를 가짐.
            float weight = 1f - progress * weightFalloff;
            weightedSum += weight * bestDist;
            weightSum += weight;
            i = (i + 1) % n;
        } while (i != startIndex);
        return weightedSum / weightSum;
    }
}