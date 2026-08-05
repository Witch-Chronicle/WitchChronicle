Witch Chronicle - Main HUD Quest Tracker Sprite Pack

포함 PNG
- QuestTracker_Panel_BG.png
- QuestTracker_Panel_Frame.png
- QuestTracker_Slot_BG.png
- QuestTracker_Slot_Frame.png
- QuestTracker_Slot_Selected_BG.png
- QuestTracker_Slot_Selected_Frame.png
- QuestTracker_Status_Diamond.png
- QuestTracker_Title_Divider.png

Unity 공통 Import
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Mesh Type: Full Rect
- Wrap Mode: Clamp
- Mip Maps: Off
- Compression: None 또는 High Quality

권장 Hierarchy
QuestTrackerPanel
├─ Background
├─ TitleText
├─ TitleDivider
├─ QuestListRoot (Vertical Layout Group)
│  ├─ QuestSlot_01
│  │  ├─ Background
│  │  ├─ QuestNameText
│  │  ├─ ObjectiveText
│  │  ├─ StatusText
│  │  └─ Frame
│  ├─ QuestSlot_02
│  └─ QuestSlot_03
└─ Frame

Panel Frame
- Image Type: Sliced
- Sprite Editor Border: Left 64 / Right 64 / Top 64 / Bottom 64
- Panel Frame은 가장 마지막 Sibling, Raycast Target Off

Quest Slot Frame
- Image Type: Sliced
- Sprite Editor Border: Left 48 / Right 48 / Top 48 / Bottom 48
- Background와 Frame은 동일 RectTransform에 겹쳐 사용

권장 UI 크기 (1920x1080 기준)
- QuestTrackerPanel: Width 430~470 / Height 500~560
- QuestSlot: Width 390~430 / Height 110~125
- Panel 우측 여백 35~50 / 상단 여백 35~50

권장 TMP 설정
- Panel Title: 28~32px, #E9E7DD
- Quest Name: 21~24px, #E9E7DD
- Objective: 16~18px, #C5C2B9
- Status: 15~17px, #E9E7DD 또는 #A2A09F
- 완료 슬롯은 CanvasGroup Alpha 0.62~0.72 권장
