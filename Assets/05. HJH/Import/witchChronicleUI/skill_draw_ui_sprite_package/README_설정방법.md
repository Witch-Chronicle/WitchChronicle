# Skill Draw UI Sprite Package

## 포함 파일

| 파일 | 크기 | 용도 |
|---|---:|---|
| `header_panel_background.png` | 720×128 | 중앙 상단 안내 패널 배경 |
| `header_panel_frame.png` | 720×128 | 중앙 상단 안내 패널 프레임 |
| `multiplier_slider_background.png` | 760×52 | 스킬 위력 보정 슬라이더 배경 |
| `multiplier_slider_frame.png` | 760×52 | 스킬 위력 보정 슬라이더 프레임/구간선 |
| `multiplier_slider_fill_full.png` | 760×52 | 스킬 위력 보정 슬라이더 전체 Fill |
| `multiplier_header_divider.png` | 600×52 | 스킬 위력 보정 제목 앞쪽 Divider |
| `example_panel_background.png` | 300×360 | 예시 문양 패널 배경 |
| `example_panel_frame.png` | 300×360 | 예시 문양 패널 프레임 |
| `drawing_fullscreen_background_overlay.png` | 1920×1080 | 화면 Dimmed/Vignette/모서리 별자리 오버레이 |
| `mouse_drag_icon.png` | 128×128 | 드래그 안내 마우스 아이콘 |

## Unity Import

모든 파일 공통:

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Alpha Is Transparency: `ON`
- Wrap Mode: `Clamp`
- Compression: `None` 또는 `High Quality`

## Slider 구조

```text
MultiplierSlider
├─ Background
├─ Fill
└─ Frame
```

세 오브젝트는 같은 RectTransform 크기 `760×52`로 겹칩니다.

`Fill` Image 설정:

- Source Image: `multiplier_slider_fill_full.png`
- Image Type: `Filled`
- Fill Method: `Horizontal`
- Fill Origin: `Left`
- Fill Amount: 코드에서 0~1로 제어
- Preserve Aspect: `OFF`

현재 `SkillDrawController`의 `_multiplierSliderFillImage`에는 이 `Fill` Image를 연결합니다.

## 패널 구조

Background와 Frame은 동일한 RectTransform으로 겹쳐서 사용합니다.

```text
HeaderPanel
├─ Background
├─ Frame
└─ Texts...

ExamplePanel
├─ Background
├─ Frame
├─ TitleTxt
└─ DrawImg
```

`Frame` Image는 반드시 Background보다 뒤가 아닌 앞쪽 Sibling에 배치합니다.

## 전체 배경

`drawing_fullscreen_background_overlay.png`는 게임 화면을 포함하지 않는 반투명 RGBA 이미지입니다.

- Anchor: Stretch / Stretch
- Left / Right / Top / Bottom: `0`
- Color: White
- Raycast Target: `OFF`
- Canvas의 가장 첫 번째 UI Image로 배치

## 권장 Hierarchy

```text
SkillDrawCanvas
├─ FullscreenBackground
├─ DrawingPlace
│  ├─ MagicCircleFrame
│  ├─ Tooltip
│  │  ├─ MouseIcon
│  │  └─ TooltipTxt
│  └─ PlayerLines (LineRenderer는 기존 코드에서 생성)
├─ Header
│  ├─ HeaderPanel
│  │  ├─ Background
│  │  ├─ Frame
│  │  ├─ HeaderTxt
│  │  └─ InstructionTxt
│  ├─ Timer
│  └─ ExamplePanel
└─ MultiplierArea
   ├─ Divider
   ├─ HeaderTxt
   ├─ MultiplierTxt
   └─ MultiplierSlider
```
