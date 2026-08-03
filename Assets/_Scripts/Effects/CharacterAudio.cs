using UnityEngine;

public class CharacterAudio : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Footstep")]
    [SerializeField] private AudioClip[] footstepClips;
    [Range(0f, 1f)]
    [SerializeField] private float footstepVolume = 0.5f;

    [Header("Attack")]
    [SerializeField] private AudioClip[] attackClips;
    [Range(0f, 1f)]
    [SerializeField] private float attackVolume = 0.3f;

    [Header("Skill")]
    [SerializeField] private AudioClip[] skillClips;
    [Range(0f, 1f)]
    [SerializeField] private float skillVolume = 0.3f;

    [Header("Hit")]
    [SerializeField] private AudioClip[] hitClips;
    [Range(0f, 1f)]
    [SerializeField] private float hitVolume = 0.15f;

    [Header("Status")]
    [SerializeField] private AudioClip[] statusClips;
    [Range(0f, 1f)]
    [SerializeField] private float statusVolume = 0.3f;

    [Header("Parry")]
    [SerializeField] private AudioClip[] parryClips;
    [Range(0f, 1f)]
    [SerializeField] private float parryVolume = 0.3f;

    [Header("Death")]
    [SerializeField] private AudioClip[] deathClips;
    [Range(0f, 1f)]
    [SerializeField] private float deathVolume = 0.3f;

    [Header("Victory")]
    [SerializeField] private AudioClip[] victoryClips;
    [Range(0f, 1f)]
    [SerializeField] private float victoryVolume = 0.3f;

    [Header("Joint Attack")]
    [SerializeField] private AudioClip[] jointAttackClips;
    [Range(0f, 1f)]
    [SerializeField] private float jointAttackVolume = 0.3f;


    private void PlayRandom(AudioClip[] clips, float volume = 1f)
    {
        if (audioSource == null || clips == null || clips.Length == 0)
            return;

        int index = Random.Range(0, clips.Length);
        audioSource.PlayOneShot(clips[index], volume);
    }

    // ---------- 배틀 이벤트(Binder)에서 직접 호출 ----------

    public void PlayAttack() => PlayRandom(attackClips, attackVolume);

    public void PlaySkill() => PlayRandom(skillClips, skillVolume);

    public void PlayHit() => PlayRandom(hitClips, hitVolume);

    public void PlayParry() => PlayRandom(parryClips, parryVolume);

    public void PlayDeath() => PlayRandom(deathClips, deathVolume);

    // ---------- Animation Events ----------

    public void OnFootstep(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(footstepClips, footstepVolume);
    }

    public void OnAttack(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(attackClips, attackVolume);
    }

    public void OnSkill(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(skillClips, skillVolume);
    }

    public void OnHit(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(hitClips, hitVolume);
    }

    public void OnStatus(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(statusClips, statusVolume);
    }

    public void OnParry(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(parryClips, parryVolume);
    }

    public void OnDeath(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(deathClips, deathVolume);
    }

    public void OnVictory(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(victoryClips, victoryVolume);
    }

    public void OnJointAttack(AnimationEvent e)
    {
        if (e.animatorClipInfo.weight < 0.5f) return;
        PlayRandom(jointAttackClips, jointAttackVolume);
    }

    public void OnLand(AnimationEvent e)
    {
        // 착지 사운드가 필요하면 추가
    }
}
