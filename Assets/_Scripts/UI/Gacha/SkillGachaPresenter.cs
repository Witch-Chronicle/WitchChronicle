using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 스킬 가챠 스핀 연출.
/// 결과는 이미 정해진 상태로 들어오며, 여기서는 보여주기만 한다(판정 없음).
/// 아이콘을 빠르게 돌리다가 감속해서 결과 아이콘에서 멈춘다.
/// </summary>
public class SkillGachaPresenter : MonoBehaviour
{
    [Header("스핀 표시")]
    [Tooltip("돌아가는 동안 아이콘이 바뀔 이미지")]
    [SerializeField] private Image _spinIcon;

    [Tooltip("스핀 중 배경 등에 색을 입힐 대상(선택)")]
    [SerializeField] private Image _frameImage;

    [Header("결과 표시")]
    [SerializeField] private GameObject _resultRoot;
    [SerializeField] private Image _resultIcon;
    [SerializeField] private TMP_Text _resultNameText;
    [SerializeField] private TMP_Text _resultDescText;

    [Header("연출 타이밍")]
    [Tooltip("전체 스핀 시간(초)")]
    [SerializeField] private float _spinDuration = 1.8f;

    [Tooltip("아이콘 교체 최소 간격(가장 빠를 때)")]
    [SerializeField] private float _fastInterval = 0.04f;

    [Tooltip("아이콘 교체 최대 간격(멈추기 직전)")]
    [SerializeField] private float _slowInterval = 0.25f;

    [Tooltip("결과를 보여주기 전 잠깐 멈추는 시간")]
    [SerializeField] private float _revealDelay = 0.3f;

    [Tooltip("멈춘 뒤 확정된 느낌을 주는 반짝임 횟수. 0이면 반짝이지 않는다")]
    [SerializeField] private int _settleFlashCount = 3;

    [Tooltip("반짝임 한 번의 간격(초)")]
    [SerializeField] private float _settleFlashInterval = 0.07f;

    [Header("티어별 색상 (0=1티어 ... 3=4티어)")]
    [SerializeField]
    private Color[] _tierColors =
    {
        new Color(1f, 0.85f, 0.3f),   // 1티어 금색
        new Color(0.8f, 0.5f, 1f),    // 2티어 보라
        new Color(0.4f, 0.7f, 1f),    // 3티어 파랑
        new Color(0.8f, 0.8f, 0.8f),  // 4티어 회색
    };

    [Header("사운드")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _spinClip;
    [SerializeField] private AudioClip _revealClip;

    /// <summary>연출이 진행 중인지.</summary>
    public bool IsPlaying { get; private set; }

    private readonly List<Sprite> _iconPool = new List<Sprite>();
    private Coroutine _routine;

    /// <summary>
    /// 스핀 연출을 재생한다.
    /// </summary>
    /// <param name="result">이미 확정된 뽑기 결과</param>
    /// <param name="iconPool">돌아가는 동안 스쳐갈 아이콘들(후보 스킬 아이콘)</param>
    /// <param name="onFinished">연출 종료 콜백</param>
    public void Play(SkillBookResult result, IReadOnlyList<Sprite> iconPool, Action onFinished = null)
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _iconPool.Clear();

        if (iconPool != null)
        {
            for (int i = 0; i < iconPool.Count; i++)
            {
                if (iconPool[i] != null)
                {
                    _iconPool.Add(iconPool[i]);
                }
            }
        }

        _routine = StartCoroutine(SpinRoutine(result, onFinished));
    }

    private IEnumerator SpinRoutine(SkillBookResult result, Action onFinished)
    {
        IsPlaying = true;

        if (_resultRoot != null)
        {
            _resultRoot.SetActive(false);
        }

        PlayClip(_spinClip);

        // 스핀: 시간이 갈수록 교체 간격이 길어져 감속처럼 보인다.
        // 아이콘과 함께 티어 색도 계속 갈아끼워 릴이 돌아가는 느낌을 준다.
        float elapsed = 0f;
        int index = 0;
        int colorIndex = 0;

        while (elapsed < _spinDuration)
        {
            if (_iconPool.Count > 0 && _spinIcon != null)
            {
                _spinIcon.sprite = _iconPool[index % _iconPool.Count];
                _spinIcon.enabled = true;
                index++;
            }

            ApplyReelColor(GetTierColorAt(colorIndex));
            colorIndex++;

            float t = _spinDuration <= 0f ? 1f : elapsed / _spinDuration;
            float interval = Mathf.Lerp(_fastInterval, _slowInterval, t * t);

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        // 결과 아이콘에서 정지
        Sprite finalIcon = result.LearnedSkill != null ? result.LearnedSkill.SkillIcon : null;
        Color finalColor = GetTierColor(result.RolledTier);

        if (_spinIcon != null)
        {
            _spinIcon.sprite = finalIcon;

            // 아이콘이 없는 스킬이면 색만 칠한 사각형이 크게 보이므로 아예 숨긴다
            _spinIcon.enabled = finalIcon != null;
        }

        ApplyReelColor(finalColor);

        // 멈춘 자리에서 짧게 반짝여 "확정" 느낌을 준다
        for (int i = 0; i < _settleFlashCount; i++)
        {
            ApplyReelColor(Color.white);
            yield return new WaitForSeconds(_settleFlashInterval);

            ApplyReelColor(finalColor);
            yield return new WaitForSeconds(_settleFlashInterval);
        }

        yield return new WaitForSeconds(_revealDelay);

        ShowResult(result, finalIcon);
        PlayClip(_revealClip);

        IsPlaying = false;
        _routine = null;

        onFinished?.Invoke();
    }

    /// <summary>결과 패널 내용 채우기.</summary>
    private void ShowResult(SkillBookResult result, Sprite icon)
    {
        if (_resultRoot != null)
        {
            _resultRoot.SetActive(true);
        }

        if (_resultIcon != null)
        {
            _resultIcon.sprite = icon;
            _resultIcon.enabled = icon != null;
            _resultIcon.color = GetTierColor(result.RolledTier);
        }

        if (result.LearnedSkill != null)
        {
            if (_resultNameText != null)
            {
                _resultNameText.text = result.LearnedSkill.SkillName;
                _resultNameText.color = GetTierColor(result.RolledTier);
            }

            if (_resultDescText != null)
            {
                _resultDescText.text =
                    $"{result.RolledTier}티어 스킬을 습득했습니다.\n\n{result.LearnedSkill.Description}";
            }
        }
        else
        {
            // 중복(더 배울 스킬 없음) → 골드 보상
            if (_resultNameText != null)
            {
                _resultNameText.text = $"+{result.RewardGold} G";
                _resultNameText.color = GetTierColor(result.RolledTier);
            }

            if (_resultDescText != null)
            {
                _resultDescText.text = "이미 모두 익힌 지식입니다. 대가를 받았습니다.";
            }
        }
    }

    /// <summary>스핀 중인 틀·아이콘에 같은 색을 입힌다.</summary>
    private void ApplyReelColor(Color color)
    {
        if (_frameImage != null)
        {
            _frameImage.color = color;
        }

        if (_spinIcon != null)
        {
            _spinIcon.color = color;
        }
    }

    /// <summary>티어 색을 순서대로 돌려쓴다(스핀 연출용).</summary>
    private Color GetTierColorAt(int index)
    {
        if (_tierColors == null || _tierColors.Length == 0)
        {
            return Color.white;
        }

        return _tierColors[index % _tierColors.Length];
    }

    /// <summary>티어(1이 최상)에 대응하는 색.</summary>
    private Color GetTierColor(int tier)
    {
        if (_tierColors == null || _tierColors.Length == 0)
        {
            return Color.white;
        }

        int idx = Mathf.Clamp(tier - 1, 0, _tierColors.Length - 1);
        return _tierColors[idx];
    }

    private void PlayClip(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
}
