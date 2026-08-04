Witch Chronicle - 강화 UI 스프라이트 패키지 v1

구성 (모두 투명 배경 PNG)
- Enhance_Slot_BG_Normal.png : 기본 슬롯 배경
- Enhance_Slot_BG_Selected.png : 선택 슬롯 배경
- Enhance_Slot_Frame_Normal.png : 기본 슬롯 프레임
- Enhance_Slot_Frame_Selected.png : 선택 슬롯 이중 프레임
- Enhance_Divider_Vertical.png : 세로 구분선
- Enhance_Divider_Horizontal.png : 가로 구분선
- Enhance_Arrow_BeforeAfter.png : 강화 전 → 강화 후 화살표
- Enhance_Carousel_Prev.png : 하단 캐러셀 이전
- Enhance_Carousel_Next.png : 하단 캐러셀 다음
- Enhance_SelectedItemPanel_BG.png : 선택 장비 대형 패널 배경
- Enhance_SelectedItemPanel_Frame.png : 선택 장비 대형 패널 이중 프레임
- Enhance_ProgressBar_Background.png : 강화 포인트 바 배경
- Enhance_ProgressBar_Fill.png : 강화 포인트 바 Fill

Unity 권장 설정
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single / Mesh Type: Full Rect
- Wrap Mode: Clamp / Mip Maps: Off
- 슬롯은 BG와 Frame을 서로 다른 Image로 겹쳐 사용합니다.
- Normal: BG_Normal + Frame_Normal
- Selected: BG_Selected + Frame_Selected
- 슬롯을 크게 늘릴 경우 Image Type을 Sliced로 설정하고 Border는 12px을 권장합니다.
- Divider와 Arrow는 Image Type Simple, Preserve Aspect 활성화를 권장합니다.
- 대형 패널은 BG와 Frame을 겹치고 Image Type Sliced를 사용합니다. Border는 24px을 권장합니다.
- Progress Bar Background는 Simple 또는 Sliced, Fill은 Image Type Filled / Fill Method Horizontal을 권장합니다.

색상 기준
- Ivory: #E9E7DD
- Gray: #A2A09F
- Normal panel: #48434B
- Selected panel: #5B5660
