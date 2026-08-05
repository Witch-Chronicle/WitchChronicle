Witch Chronicle - Dungeon Entry UI Sprite Pack

[생성형 이미지]
- Dungeon_Map.png: UI와 글자가 없는 4지역 월드맵 원본.
- Dungeon_Icon_Wild / Underground / Cursed / Fire: 투명 배경 던전 아이콘.
- Monster_Icon_FireSlime / AshWolf / LavaGolem: 불의 던전 몬스터 아이콘.

[UI Sprite]
- Dungeon_Fullscreen_Dim_BG: 화면 뒤 어둡게 처리.
- Dungeon_Map_Frame: 지도 외곽 프레임.
- Dungeon_DetailPanel_BG / Frame: 우측 정보 패널.
- Dungeon_Node_BG / Frame_Normal: 일반 지도 노드.
- Dungeon_Node_BG / Frame_Selected: 선택된 지도 노드.
- Dungeon_Label_BG_Normal / Selected: 지도 내 던전명 뒤 배경. 글자는 TMP로 배치.
- Dungeon_MonsterSlot_BG / Frame: 몬스터 목록 슬롯.
- Dungeon_MonsterIcon_Frame: 몬스터 아이콘 외곽 프레임.
- Dungeon_EnterButton_BG / Frame: 입장하기 버튼.
- Dungeon_Section_Divider: 출현 몬스터 등 섹션 구분선.
- Dungeon_CloseButton_Frame: 닫기 버튼.

[Unity Import]
1. Texture Type: Sprite (2D and UI)
2. Mesh Type: Full Rect
3. Compression: None
4. Filter Mode: Bilinear
5. BG/Frame을 같은 RectTransform에 겹쳐 사용합니다.
6. Map Frame, Detail Panel, Label BG, Monster Slot, Enter Button은 Image Type=Sliced 권장.

[2배 PNG 기준 Border 권장값]
- Dungeon_Map_Frame: 80
- Dungeon_DetailPanel_BG / Frame: 64
- Dungeon_Label_BG: Left/Right 120, Top/Bottom 16
- Dungeon_MonsterSlot_BG / Frame: 28
- Dungeon_EnterButton_BG / Frame: 48

[지도 노드 권장 Hierarchy]
DungeonNode
  SelectedGlow (선택 시에만 활성화)
  NodeBG
  DungeonIcon
  NodeFrame
  LabelBG
  LabelText (TMP)

[몬스터 슬롯 권장 Hierarchy]
MonsterSlot
  SlotBG
  MonsterIcon
  MonsterIconFrame
  MonsterName (TMP)
  SlotFrame

주의: 던전명, 설명, 몬스터명, 버튼 문자는 PNG에 포함하지 않았습니다.
