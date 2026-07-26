using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// $P Point-Cloud 방식 궤적 유사도 계산 유틸. SkillRecognizer와 SkillDrawMinigameController가 공유.
/// </summary>
public static class GestureMatcher
{
    public static float ComputeSimilarityScore(List<SkillPoint> playerRaw, List<SkillPoint> templateRaw, int resampleCount = 64)
    {
        if (playerRaw == null || templateRaw == null || playerRaw.Count < 2 || templateRaw.Count < 2)
            return 0f;

        List<Vector2> a = Normalize(ResamplePath(playerRaw, resampleCount));
        List<Vector2> b = Normalize(ResamplePath(templateRaw, resampleCount));

        float weightedAvgDist = GreedyCloudMatch(a, b);
        float maxAcceptableDist = 0.5f;

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

    private static float GreedyCloudMatch(List<Vector2> points1, List<Vector2> points2)
    {
        int n = points1.Count;
        float eps = 0.5f;
        int step = Mathf.Max(1, Mathf.FloorToInt(Mathf.Pow(n, 1f - eps)));

        float minDistance = float.MaxValue;

        for (int i = 0; i < n; i += step)
        {
            float dist1 = CloudDistance(points1, points2, i);
            float dist2 = CloudDistance(points2, points1, i);
            minDistance = Mathf.Min(minDistance, Mathf.Min(dist1, dist2));
        }

        return minDistance;
    }

    private static float CloudDistance(List<Vector2> points1, List<Vector2> points2, int startIndex)
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
            float weight = 1f - progress;

            weightedSum += weight * bestDist;
            weightSum += weight;

            i = (i + 1) % n;
        } while (i != startIndex);

        return weightedSum / weightSum;
    }
}