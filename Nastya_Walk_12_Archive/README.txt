Nastya walk animation — 12-frame pack

Contents:
- Nastya_Walk_12_SpriteSheet.png — one-row sprite sheet, transparent background
- frames/ — 12 separate transparent PNG frames

Frame size: 222 x 570 px
Spacing between frames in sprite sheet: 20 px
Canvas size: 2884 x 570 px

Note:
The uploaded source contained 8 distinct poses. To make a 12-frame sequence without
blurring or ghosting the pixel art, 4 additional timing frames repeat selected poses.
They are not newly drawn in-between poses.

Unity import:
1. Select Nastya_Walk_12_SpriteSheet.png.
2. Set Texture Type = Sprite (2D and UI).
3. Set Sprite Mode = Multiple.
4. Open Sprite Editor and slice by Grid By Cell Size:
   Cell Size X = 222, Y = 570
   Offset = 0,0
   Padding = 20,0
5. Apply. Alternatively, import the individual PNGs from frames/.
