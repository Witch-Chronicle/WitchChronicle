Witch Chronicle - Dialogue UI Sprite Pack

구성
- Dialogue_Panel_BG / Frame: 대화창 본체. Frame은 투명 이미지이며 BG 위에 겹칩니다.
- Dialogue_NamePlate_BG / Frame: 캐릭터 이름판.
- Dialogue_ChoiceButton_BG_Normal / Frame_Normal: 일반 선택지.
- Dialogue_ChoiceButton_BG_Selected / Frame_Selected: 현재 선택된 선택지.
- Dialogue_Portrait_BG / Frame: 패널 내부용 직사각형 캐릭터 초상화 슬롯. Character Image는 BG와 Frame 사이에 배치합니다.
- Dialogue_Next_Indicator: 다음 대사 진행 표시. Alpha 또는 Scale 애니메이션을 적용합니다.

Unity 권장 설정
1. Texture Type: Sprite (2D and UI)
2. Mesh Type: Full Rect
3. Compression: None
4. Filter Mode: Bilinear
5. Panel / NamePlate / Button BG와 Frame: Image Type = Sliced
6. PNG는 SVG 원본의 2배 크기로 출력되어 있습니다.
   - Panel Border: Left/Right/Top/Bottom 48 px
   - NamePlate Border: 40 px
   - Choice Button Border: 40 px
   - Portrait BG / Frame은 Simple 권장(비율 유지)
   - CharacterImage는 Preserve Aspect를 켜고 PortraitFrame 안쪽으로 12~18 px 여백을 둡니다.

권장 Hierarchy
DialoguePanel
  Background
  DialogueText (TMP)
  ChoiceRoot
    ChoiceButton_01
      BG
      Label (TMP)
      Frame
    ChoiceButton_02
      BG
      Label (TMP)
      Frame
  PortraitRoot
    PortraitBG
    CharacterImage
    PortraitFrame
    NamePlate
      BG
      NameText (TMP)
      Frame
  NextIndicator
  PanelFrame

주의: 대사, 캐릭터명, 선택지 문자는 Sprite에 포함하지 않았습니다. TextMeshPro로 구성하세요.
