# Witch Chronicle 레이어형 타이틀 화면 설정

## 포함 파일

| 파일 | 용도 |
|---|---|
| `title_background_base.png` | 구름과 캐릭터가 제거된 1920×1080 배경 |
| `title_clouds_far.png` | 멀리 있는 얇은 구름 레이어, 1920×1080 투명 PNG |
| `title_clouds_near.png` | 화면 가장자리를 지나는 가까운 구름 레이어, 1920×1080 투명 PNG |
| `title_characters_group.png` | 네 캐릭터의 뒷모습 그룹, 투명 PNG |
| `title_logo.png` | 타이틀 로고, 투명 PNG |
| `title_menu_selected.png` | 선택 메뉴 아래에 놓는 발광 장식선, 투명 PNG |
| `TitleBackgroundMotionController.cs` | 구름 루프, 마우스 패럴랙스, 캐릭터 미세 움직임 |

## 1. Canvas 생성

- Canvas: `Screen Space - Overlay`
- Canvas Scaler: `Scale With Screen Size`
- Reference Resolution: `1920 × 1080`
- Screen Match Mode: `Match Width Or Height`
- Match: `0.5`

## 2. 권장 Hierarchy

```text
Canvas_Title
├─ MotionController                 (TitleBackgroundMotionController)
├─ BackgroundRoot                  (Stretch, 화면보다 좌우/상하 12px 크게)
│  └─ BaseBackground               (title_background_base.png)
├─ FarCloudRoot                    (Stretch)
│  ├─ FarCloud_A                   (title_clouds_far.png)
│  └─ FarCloud_B                   (title_clouds_far.png)
├─ NearCloudRoot                   (Stretch)
│  ├─ NearCloud_A                  (title_clouds_near.png)
│  └─ NearCloud_B                  (title_clouds_near.png)
├─ Characters                      (title_characters_group.png)
└─ UI_Root
   ├─ Logo                         (title_logo.png)
   └─ MenuRoot
      ├─ ContinueButton
      ├─ NewGameButton
      │  └─ SelectedOrnament       (title_menu_selected.png)
      ├─ SettingsButton
      └─ ExitButton
```

Hierarchy 순서가 곧 렌더링 순서입니다. 구름은 배경보다 위, 캐릭터보다 아래에 두고, 로고와 메뉴는 항상 마지막에 두세요.

## 3. RectTransform 설정

### BackgroundRoot / BaseBackground

- Anchor: Stretch / Stretch
- Left, Right, Top, Bottom: `-12`
- 패럴랙스 이동 시 화면 가장자리가 비치지 않도록 원본 화면보다 조금 크게 둡니다.
- Image: `Preserve Aspect OFF`, `Raycast Target OFF`

### FarCloudRoot / NearCloudRoot

- Root Anchor: Stretch / Stretch, Offset 전부 `0`
- A/B Anchor Min/Max: `(0, 0.5)`
- A/B Pivot: `(0, 0.5)`
- A/B Size: `1920 × 1080`
- A Position X: `0`
- B Position X: 스크립트가 자동으로 설정
- Image: `Preserve Aspect OFF`, `Raycast Target OFF`
- A와 B에는 반드시 같은 Sprite를 넣습니다.

### Characters

- Anchor/Pivot: Bottom Right `(1, 0)`
- 권장 Width: `900~1050`
- Height: `Set Native Size` 후 비율 유지
- Anchored Position: `X -20`, `Y -5`
- Image: `Preserve Aspect ON`, `Raycast Target OFF`

### Logo

- Anchor/Pivot: Top Left `(0, 1)`
- 권장 Width: `520~620`
- Anchored Position: `X 65`, `Y -70`
- Image: `Preserve Aspect ON`, `Raycast Target OFF`

### SelectedOrnament

- 선택된 TMP 텍스트의 자식으로 둡니다.
- Anchor/Pivot: Bottom Center `(0.5, 0)`
- 권장 Size: `360 × 43`
- Anchored Position: `X 0`, `Y -18`
- Image Color Alpha: `0.75~0.9`

## 4. Sprite Import Settings

모든 PNG 공통:

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Filter Mode: `Bilinear`
- Wrap Mode: `Clamp`
- Compression: `High Quality`
- Generate Mip Maps: `OFF`

구름/캐릭터/로고/장식선은 `Alpha Is Transparency`를 켭니다.

## 5. MotionController Inspector 연결

`MotionController` 오브젝트에 `TitleBackgroundMotionController`를 추가합니다.

| 필드 | 연결 대상 / 권장값 |
|---|---|
| Far Clouds > Root | `FarCloudRoot` |
| Far Clouds > First | `FarCloud_A` |
| Far Clouds > Second | `FarCloud_B` |
| Far Clouds > Speed | `4` |
| Far Clouds > Gap | `-80` |
| Far Clouds > Parallax | `X 4, Y 2` |
| Near Clouds > Root | `NearCloudRoot` |
| Near Clouds > First | `NearCloud_A` |
| Near Clouds > Second | `NearCloud_B` |
| Near Clouds > Speed | `8` |
| Near Clouds > Gap | `-100` |
| Near Clouds > Parallax | `X 8, Y 4` |
| Background Layer | `BackgroundRoot` |
| Background Parallax | `X 2, Y 1` |
| Character Layer | `Characters` |
| Character Parallax | `X 14, Y 6` |
| Parallax Follow Speed | `3.5` |
| Character Breath Amount | `2` |
| Character Breath Speed | `0.75` |

## 6. 연출 방향

- 먼 구름은 느리게, 가까운 구름은 약 두 배 빠르게 흐르게 합니다.
- 마우스 이동량은 매우 작게 유지하여 배경 이미지라는 인상을 해치지 않습니다.
- 네 캐릭터는 정면 일러스트 대신 어두운 뒷모습을 사용해 게임의 핵심 자원을 암시합니다.
- 메뉴 글자는 TMP로 직접 배치하세요. 이미지 텍스트보다 다국어와 해상도 대응이 쉽습니다.
- 선택 메뉴만 장식선의 Alpha를 올리고, 나머지 메뉴에서는 비활성화합니다.

## 7. 경계가 보일 때

- 구름 A/B 사이가 비면 `Gap`을 `-120`까지 낮춥니다.
- 화면 가장자리가 보이면 각 Cloud Root를 좌우로 약 `20px` 크게 만들거나 구름 자식 Width를 `1960`으로 늘립니다.
- 구름이 너무 강하면 Image Color의 Alpha를 먼 구름 `0.25~0.4`, 가까운 구름 `0.35~0.55`로 조정합니다.
