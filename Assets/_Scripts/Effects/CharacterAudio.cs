using UnityEngine;

public class CharacterAudio : MonoBehaviour
{
    [Header("Footstep")]
    [SerializeField] private AudioClip[] footstepClips;
    [Range(0f, 1f)]
    [SerializeField] private float footstepVolume = 0.5f;

    [Header("Attack")]
    [SerializeField] private AudioClip[] attackClips;

    [Header("Skill")]
    [SerializeField] private AudioClip[] skillClips;

    [Header("Hit")]
    [SerializeField] private AudioClip[] hitClips;

    [Header("Status")]
    [SerializeField] private AudioClip[] statusClips;

    [Header("Parry")]
    [SerializeField] private AudioClip[] parryClips;

    [Header("Death")]
    [SerializeField] private AudioClip[] deathClips;

    [Header("Victory")]
    [SerializeField] private AudioClip[] victoryClips;

    [Header("Joint Attack")]
    [SerializeField] private AudioClip[] jointAttackClips;

    /// <summary>
    /// SoundManager를 통해 재생 (전역 마스터/SFX 볼륨이 자동 반영됨).
    /// </summary>
    private void PlayRandom(AudioClip[] clips, float volume = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        if (SoundManager.Instance == null) return;

        int index = Random.Range(0, clips.Length);
        SoundManager.Instance.PlaySfxOneShot(clips[index], volume);
    }

    // ---------- 배틀 이벤트(Binder)에서 직접 호출 ----------

    public void PlayAttack() => PlayRandom(attackClips);

    public void PlaySkill() => PlayRandom(skillClips);

    public void PlayHit() => PlayRandom(hitClips);

    public void PlayParry() => PlayRandom(parryClips);

    public void PlayDeath() => PlayRandom(deathClips);

    // ---------- Animation Events ----------

    public void OnFootstep(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(footstepClips, footstepVolume);
    }

    public void OnAttack(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(attackClips);
    }

    public void OnSkill(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(skillClips);
    }

    public void OnHit(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(hitClips);
    }

    public void OnStatus(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(statusClips);
    }

    public void OnParry(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(parryClips);
    }

    public void OnDeath(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(deathClips);
    }

    public void OnVictory(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(victoryClips);
    }

    public void OnJointAttack(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(jointAttackClips);
    }

    public void OnLand(AnimationEvent e)
    {
        // 착지 사운드가 필요하면 추가
    }
}