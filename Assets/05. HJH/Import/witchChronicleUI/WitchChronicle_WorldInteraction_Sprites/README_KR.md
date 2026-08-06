# World Interaction Prompt Sprite Pack

모든 PNG는 텍스트가 없는 투명 배경 Sprite입니다. NPC Outline은 셰이더 기능이므로 포함하지 않았습니다.

## Unity Import
- Texture Type: Sprite (2D and UI)
- Mesh Type: Full Rect / Alpha Is Transparency: On / Mip Maps: Off
- Filter Mode: Bilinear / Compression: None
- Prompt BG: Image Type Sliced, Border 16
- Key BG: Image Type Sliced, Border 12
- Frame과 Ornament: Simple 권장

## 권장 World Canvas 구조
RoleRoot 아래에 TMP `상점`과 `shop_role_ornament`를 배치합니다.
PromptRoot에는 Prompt BG → Key BG → Key TMP `E` → Action TMP `상점 이용` → Prompt Frame → 상/하단 CenterDiamond 순서로 배치합니다.
모든 Image와 TMP의 Raycast Target은 끄는 것을 권장합니다.
