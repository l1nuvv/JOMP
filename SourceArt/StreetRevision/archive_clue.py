from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/JustOneMorePeel/Resources'
FONT = ROOT / 'SourceArt/StreetRevision/Fonts/Caveat.ttf'

def make(language):
    image = Image.new('RGB', (768, 1024), '#e5dbc0')
    draw = ImageDraw.Draw(image)
    draw.rectangle((26, 25, 741, 998), outline='#b8ad91', width=3)
    draw.line((72, 184, 696, 184), fill='#ad9d83', width=2)
    typeface = ImageFont.truetype('C:/Windows/Fonts/times.ttf', 31)
    hand = ImageFont.truetype(str(FONT), 61)
    small = ImageFont.truetype(str(FONT), 49)
    heading = 'ДОМОВОЙ АРХИВ  /  17' if language == 'ru' else 'HOUSE ARCHIVE  /  17'
    draw.text((76, 88), heading, font=typeface, fill='#665c4d')
    lines = (['Стук снова слышен', 'за закрытой дверью.', 'Я проверил — там никого.', '', 'Но кто-то оставил', 'объявления из нашего двора.'] if language == 'ru' else
             ['The knocking came again', 'from behind the closed door.', 'I checked — no one was there.', '', 'But someone left notices', 'from our courtyard.'])
    for i, line in enumerate(lines):
        draw.text((83, 230 + i * 83), line, font=hand, fill='#393e3a', stroke_width=0)
    draw.text((505, 816), '— М.' if language == 'ru' else '— M.', font=small, fill='#393e3a')
    draw.line((75, 919, 340, 919), fill='#b7a98e', width=2)
    image.save(OUT / ('ArchiveClue_' + language + '.png'))

OUT.mkdir(parents=True, exist_ok=True)
for lang in ('ru', 'en'):
    make(lang)
