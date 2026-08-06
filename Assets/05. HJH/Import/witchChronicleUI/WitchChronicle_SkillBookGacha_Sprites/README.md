# Witch Chronicle — 마도서 스킬 추첨 Sprite Pack

## 추첨 전제
티어를 먼저 추첨하고, 해당 티어의 **전체 스킬** 중 하나를 동일 확률로 선택합니다. 이미 보유한 스킬이면 뽑힌 스킬을 먼저 보여준 뒤 골드 보상으로 전환합니다.

## Unity Import
- Texture Type: Sprite (2D and UI), Mesh Type: Full Rect, Filter Mode: Bilinear, Compression: None 권장
- Books / Effects / Particles: Sprite Mode `Single`, Image Type `Simple`, Preserve Aspect 켜기
- `*_bg`, `*_frame`, 버튼: Image Type `Sliced`; Border 값은 `manifest.json` 참고
- Glow, magic circle, rays, burst, particles: Additive UI Material 권장. 기본 Sprite는 흰색/아이보리라 `Image.color`로 티어 색 적용 가능
- 효과 Image의 Raycast Target은 끄고, 버튼과 최상위 InputBlocker만 켭니다.

## 권장 레이어(뒤→앞)
DimmedBackground → MagicCircleBack → GlowBook → Book → MagicCircleFront → GatherRays → Flash → ResultPanel → ResultHalo/Burst → SkillIcon 또는 GoldParticle → Text/Button

## 3프레임 책 애니메이션
Closed 0.16s → HalfOpen 0.12s → Open 0.18s. 각 교체 구간에 Scale/Rotation을 아주 작게 섞으면 두 장 사이도 자연스럽게 연결됩니다. 책 Sprite 자체에는 광원을 굽지 않았으므로 Glow와 Circle을 별도 Tween할 수 있습니다.

## 연출 팁
- UI가 Pause 중에도 재생되어야 하면 DOTween에 `SetUpdate(true)` 사용
- 신규: `halo_new_skill`, 티어색 `result_burst_neutral`, 밝은 별 입자
- 중복: 실제 스킬을 0.45초 보여준 뒤 Desaturate → `halo_duplicate_gold` → Coin/Spark 입자와 보상 텍스트
- Skip 입력은 결과가 이미 확정된 뒤 연출만 즉시 완료하도록 구현
