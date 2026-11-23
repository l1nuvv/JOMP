"""Paint the temporary pavilion reveal plate with the project's existing Pillow workflow."""

from pathlib import Path
from random import Random

from PIL import Image, ImageDraw, ImageFilter


WIDTH, HEIGHT = 1600, 400
INK = '#283e46'
TEAL = '#477a7e'
PALE = '#d9dac0'
OCHRE = '#e2ae5f'
RUST = '#ad624d'
CREAM = '#f1d69b'


def paint():
    mural = Image.new('RGB', (WIDTH, HEIGHT), '#9dbab1')
    brush = ImageDraw.Draw(mural)

    # Twilight across the same paved courtyard and its residential windows.
    brush.ellipse((1070, 20, 1216, 166), fill=OCHRE)
    brush.polygon([(0, 72), (258, 91), (258, 242), (0, 279)], fill='#728c88')
    brush.polygon([(205, 103), (517, 74), (517, 245), (205, 255)], fill='#b3b398')
    brush.polygon([(540, 123), (786, 111), (786, 247), (540, 235)], fill='#78928d')
    brush.polygon([(1261, 100), (1600, 54), (1600, 278), (1261, 236)], fill='#9b9e86')
    for x, y in [(30, 109), (88, 114), (146, 121), (270, 126), (327, 119),
                 (389, 113), (449, 105), (582, 151), (635, 147), (691, 143),
                 (1340, 117), (1402, 106), (1466, 97), (1534, 86)]:
        for offset in (0, 55):
            brush.rectangle((x, y + offset, x + 21, y + offset + 31), fill=INK)
            brush.rectangle((x + 3, y + offset + 4, x + 18, y + offset + 26), fill=CREAM)
            brush.line((x + 11, y + offset + 3, x + 11, y + offset + 29), fill=INK, width=2)

    brush.polygon([(0, 239), (352, 216), (690, 232), (1060, 210), (1600, 242),
                   (1600, 400), (0, 400)], fill='#ad9779')
    brush.polygon([(0, 321), (593, 255), (703, 250), (1540, 400), (0, 400)], fill='#c6ad83')
    brush.line([(0, 346), (455, 307), (845, 303), (1600, 373)], fill='#916f5d', width=3)
    brush.line([(89, 400), (536, 273), (818, 263), (1374, 400)], fill='#916f5d', width=3)
    brush.polygon([(0, 271), (69, 209), (89, 95), (76, 0), (150, 0), (120, 121),
                   (120, 239), (180, 278)], fill='#536a62')
    brush.polygon([(1426, 270), (1477, 227), (1493, 120), (1474, 48), (1506, 19),
                   (1536, 120), (1542, 249), (1600, 289)], fill='#596e63')
    for bounds in [(-120, -65, 174, 128), (6, -72, 248, 77), (-41, 31, 114, 141),
                   (1410, -87, 1710, 97), (1383, -19, 1522, 100), (1508, 42, 1676, 158)]:
        brush.ellipse(bounds, fill=TEAL)

    # A light string leads from the performer to the rest of the gathering.
    brush.arc((105, -280, 1535, 133), 9, 171, fill=INK, width=3)
    for x, y in [(195, 34), (310, 74), (428, 100), (554, 118), (684, 128),
                 (820, 133), (955, 128), (1089, 117), (1213, 98), (1335, 69), (1452, 32)]:
        brush.line((x, y - 2, x, y + 7), fill=INK, width=3)
        brush.ellipse((x - 5, y + 4, x + 5, y + 17), fill=CREAM)

    # The accordion's bellows sit under the narrow opening in the second bay.
    brush.ellipse((294, 306, 583, 342), fill='#8b7967')
    brush.polygon([(382, 212), (474, 220), (493, 304), (471, 319), (432, 274),
                   (394, 266), (377, 313), (347, 312)], fill=INK)
    brush.line((351, 315, 327, 322, 375, 323), fill='#39484b', width=10)
    brush.line((472, 316, 501, 326, 519, 325), fill='#39484b', width=10)
    brush.polygon([(402, 147), (448, 147), (468, 171), (471, 227),
                   (383, 249), (377, 173)], fill=RUST)
    brush.ellipse((395, 99, 451, 158), fill='#ba805e')
    brush.polygon([(392, 132), (389, 108), (402, 96), (426, 92), (449, 101),
                   (456, 121), (432, 115), (416, 129)], fill=INK)
    brush.line((428, 135, 439, 134), fill=INK, width=3)
    brush.line((385, 180, 372, 214, 398, 224), fill=CREAM, width=12)
    brush.line((456, 170, 483, 192, 477, 211), fill=CREAM, width=11)
    brush.polygon([(397, 183), (471, 170), (482, 216), (406, 235)], fill=INK)
    for x in range(413, 471, 9):
        brush.line((x, 181 - (x - 413) // 6, x + 10, 229 - (x - 413) // 4),
                   fill=OCHRE if x % 2 else CREAM, width=4)
    brush.polygon([(380, 182), (400, 178), (412, 235), (389, 242)], fill=TEAL)
    brush.polygon([(464, 168), (483, 163), (497, 216), (479, 222)], fill=TEAL)
    for y in range(185, 231, 8):
        brush.line((385 + (y - 185) // 6, y, 398 + (y - 185) // 6, y - 3), fill=CREAM, width=3)

    # Dancers in the middle and neighbours watching from the right-hand bench.
    brush.ellipse((612, 300, 1067, 351), fill='#8b7967')
    brush.line([(684, 215), (705, 259), (675, 312), (647, 326)], fill=INK, width=16)
    brush.line([(733, 211), (751, 267), (792, 311)], fill=INK, width=16)
    brush.polygon([(680, 146), (730, 149), (746, 220), (682, 231), (664, 209)], fill=TEAL)
    brush.line([(675, 160), (644, 190), (623, 155)], fill=TEAL, width=15)
    brush.line([(727, 159), (780, 183), (818, 158)], fill=TEAL, width=15)
    brush.ellipse((683, 106, 726, 152), fill='#cb9471')
    brush.polygon([(679, 128), (683, 106), (704, 99), (728, 110), (724, 120), (695, 115)], fill=INK)
    brush.line([(875, 236), (869, 277), (830, 320)], fill=INK, width=14)
    brush.line([(916, 236), (933, 274), (971, 297)], fill=INK, width=13)
    brush.polygon([(877, 144), (914, 145), (920, 190), (958, 244), (863, 262),
                   (838, 235), (876, 193)], fill=RUST)
    brush.line([(880, 153), (853, 182), (818, 158)], fill=RUST, width=14)
    brush.line([(910, 153), (953, 136), (974, 107)], fill=RUST, width=14)
    brush.ellipse((877, 105, 918, 149), fill='#cb9471')
    brush.polygon([(867, 134), (874, 106), (889, 96), (919, 105), (922, 121),
                   (900, 114), (878, 140), (870, 154)], fill=INK)
    brush.ellipse((1038, 287, 1402, 328), fill='#8b7967')
    brush.line((1074, 261, 1388, 271), fill=INK, width=10)
    brush.line((1089, 271, 1087, 319), fill=INK, width=8)
    brush.line((1365, 271, 1368, 322), fill=INK, width=8)
    for x, y, coat, skin in [(1110, 195, TEAL, '#c89069'), (1226, 205, OCHRE, '#b27b59'),
                             (1318, 188, '#687e66', '#c89069')]:
        brush.polygon([(x, y + 20), (x + 45, y + 16), (x + 59, y + 64),
                       (x - 6, y + 64)], fill=coat)
        brush.ellipse((x + 6, y - 15, x + 39, y + 22), fill=skin)
        brush.arc((x + 2, y - 20, x + 40, y + 19), 172, 343, fill=INK, width=9)
        brush.line([(x + 5, y + 62), (x - 13, y + 76), (x - 19, y + 108)], fill=INK, width=10)
        brush.line([(x + 37, y + 63), (x + 27, y + 79), (x + 24, y + 109)], fill=INK, width=10)

    # A little tooth in the paint, consistent across all four panes.
    mural = mural.filter(ImageFilter.GaussianBlur(0.55))
    random = Random(17)
    pixels = mural.load()
    for y in range(HEIGHT):
        for x in range(WIDTH):
            grain = random.randrange(-8, 9)
            pixels[x, y] = tuple(max(0, min(255, channel + grain)) for channel in pixels[x, y])
    return mural


if __name__ == '__main__':
    output = (Path(__file__).resolve().parents[2] / 'Assets/JustOneMorePeel/Art/Integrated'
              / 'PavilionMural' / 'Courtyard concert.png')
    output.parent.mkdir(exist_ok=True)
    paint().save(output, optimize=True)
