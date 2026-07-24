using System;
using UnityEngine;

/// <summary>
/// 별자리 단일 박자 데이터
/// 투사체 한 발과 별 하나의 동기화 정보
/// </summary>
[Serializable]
public class ConstellationBeatData
{
    [Header("Projectile Timing")]
    [SerializeField, Min(0f)] private float _projectileLaunchTime;
    [SerializeField, Min(0f)] private float _impactTime = 1f;

    [Header("Star Timing")]
    [SerializeField, Min(0f)] private float _starLeadTime = 0.5f;
    [SerializeField, Min(0f)] private float _perfectWindow = 0.08f;
    [SerializeField, Min(0f)] private float _goodWindow = 0.2f;

    [Header("Star Position")]
    [SerializeField]
    private Vector2 _normalizedStarPosition =
        new Vector2(0.5f, 0.5f);

    public float ProjectileLaunchTime => _projectileLaunchTime;
    public float ImpactTime => _impactTime;
    public float StarLeadTime => _starLeadTime;
    public float PerfectWindow => _perfectWindow;
    public float GoodWindow => _goodWindow;
    public Vector2 NormalizedStarPosition => _normalizedStarPosition;

    public float ProjectileTravelDuration =>
        _impactTime - _projectileLaunchTime;

    public float StarShowTime =>
        Mathf.Max(0f, _impactTime - _starLeadTime);

    /// <summary>
    /// 박자 데이터 유효성 검사
    /// </summary>
    public bool TryValidate(out string errorMessage)
    {
        if (_impactTime <= 0f)
        {
            errorMessage = "ImpactTime은 0보다 커야 합니다.";
            return false;
        }

        if (_projectileLaunchTime < 0f)
        {
            errorMessage = "ProjectileLaunchTime은 0 이상이어야 합니다.";
            return false;
        }

        if (_projectileLaunchTime >= _impactTime)
        {
            errorMessage = "ProjectileLaunchTime은 ImpactTime보다 빨라야 합니다.";
            return false;
        }

        if (_starLeadTime <= 0f)
        {
            errorMessage = "StarLeadTime은 0보다 커야 합니다.";
            return false;
        }

        if (_perfectWindow <= 0f)
        {
            errorMessage = "PerfectWindow는 0보다 커야 합니다.";
            return false;
        }

        if (_goodWindow < _perfectWindow)
        {
            errorMessage = "GoodWindow는 PerfectWindow보다 크거나 같아야 합니다.";
            return false;
        }

        if (_normalizedStarPosition.x < 0f ||
            _normalizedStarPosition.x > 1f ||
            _normalizedStarPosition.y < 0f ||
            _normalizedStarPosition.y > 1f)
        {
            errorMessage = "NormalizedStarPosition은 0~1 범위여야 합니다.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}