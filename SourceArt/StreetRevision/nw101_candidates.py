"""Prepare the two bilingual trial prints from Story & World Bible §16."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from chapter_prints import stock

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/JustOneMorePeel/Art/Notices/StoryCandidates"
FONTS = Path("C:/Windows/Fonts")
HAND = Path(__file__).parent / "Fonts/Caveat.ttf"
INK, RED = "#222f2b", "#9a3f32"


def write(canvas, xy, words, face, size, color=INK, stroke=0):
    font = ImageFont.truetype(str(face), size)
    assert canvas.textbbox(xy, words, font=font, anchor="lt")[2] <= 686, words
    canvas.text(xy, words, font=font, fill=color, anchor="lt", stroke_width=stroke)


def accordion():
    motif = Image.new("RGBA", (580, 275))
    d = ImageDraw.Draw(motif)
    d.polygon([(115, 67), (453, 34), (458, 209), (115, 239)], fill=RED)
    for x in (181, 263, 345):
        y = 67 - (x - 115) // 10
        d.line([(x, y), (x + 21, y + 89), (x, y + 172)], fill="#ead4ab", width=13)
    d.rounded_rectangle((27, 51, 120, 248), 12, fill=INK)
    d.rounded_rectangle((448, 25, 553, 220), 12, fill=INK)
    for x in (55, 84):
        for y in (94, 128, 162, 196):
            d.ellipse((x, y, x + 12, y + 12), fill="#ead4ab")
    for x in (477, 500, 523):
        d.line((x, 66, x, 172), fill="#ead4ab", width=9)
    return motif


def request(ru):
    sheet = stock((720, 1018), "#e9e1ca")
    d = ImageDraw.Draw(sheet)
    write(d, (54, 70), "СЛУЖЕБНЫЙ ВХОД" if ru else "SERVICE ENTRANCE", FONTS / "arialbd.ttf", 52)
    d.line((56, 151, 664, 151), fill=INK, width=5)
    lines = (
        ["Опять стучит снизу.", "Кто будет во дворе,", "помогите расчистить", "дверь. Ручка под", "бумагой."]
        if ru else
        ["It’s knocking", "downstairs again.", "If you’re out in the yard,", "please help clear", "the door. The handle’s", "under the notices."]
    )
    for i, words in enumerate(lines):
        write(d, (54, 210 + 97 * i), words, HAND, 76, stroke=2)
    d.line((55, 831, 665, 831), fill="#a29880", width=2)
    write(d, (55, 879), "Заявка №17" if ru else "Job 17", FONTS / "courbd.ttf", 45)
    return sheet


def flyer(ru, motif):
    sheet = stock((720, 1018), "#e6cc9f")
    d = ImageDraw.Draw(sheet)
    d.rectangle((30, 30, 690, 988), outline=RED, width=4)
    write(d, (65, 65), "МУЗЫКА" if ru else "MUSIC IN", FONTS / "georgiab.ttf", 101)
    write(d, (67, 183), "ВО ДВОРЕ" if ru else "THE COURTYARD", FONTS / "georgiab.ttf", 67 if ru else 57)
    d.line((64, 279, 656, 279), fill=RED, width=5)
    sheet.paste(motif, (70, 317), motif)
    write(d, (65, 643), "10 октября · 18:30" if ru else "10 October · 6:30 pm", FONTS / "arialbd.ttf", 48)
    write(d, (67, 772), "У второго подъезда." if ru else "By entrance 2.", FONTS / "georgia.ttf", 41)
    write(d, (67, 839), "Стулья вынесем." if ru else "We’ll bring chairs.", FONTS / "georgia.ttf", 41)
    return sheet


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    motif = accordion()
    motif.save(OUT / "NW-02_Accordion.png")
    for language in ("RU", "EN"):
        request(language == "RU").save(OUT / f"NW-01_{language}.png")
        flyer(language == "RU", motif).save(OUT / f"NW-02_{language}.png")
