"""Two sides of the expired yard pass, drawn at print scale into a single atlas."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[2]
width, height = 768, 480
atlas = Image.new("RGB", (width * 2, height), "#d5cfaf")
draw = ImageDraw.Draw(atlas)
ink = "#233f3d"
ochre = "#b17b47"
font = "C:/Windows/Fonts/bahnschrift.ttf"

# One-colour courtyard gate stamp, gate number and a narrow validation stripe.
draw.rectangle((0, 0, width - 1, 68), fill=ink)
draw.rectangle((0, 69, width - 1, 78), fill=ochre)
draw.text((43, 84), "17", font=ImageFont.truetype(font, 273), fill=ink, stroke_width=2)
draw.rounded_rectangle((499, 118, 674, 298), radius=17, outline=ink, width=9)
draw.arc((533, 147, 640, 252), 180, 360, fill=ink, width=12)
draw.line([(538, 200), (538, 275), (635, 275), (635, 200)], fill=ink, width=10)
draw.line((586, 164, 586, 273), fill=ink, width=8)
draw.line((36, 392, 731, 392), fill=ink, width=3)
draw.text((47, 399), "017 / 04", font=ImageFont.truetype(font, 34), fill=ink)

# Magnetic stripe and the matching serial on the reverse.
draw.rectangle((width, 54, width * 2 - 1, 146), fill="#383c37")
draw.rectangle((width + 49, 212, width + 577, 282), fill="#e6dfc5")
draw.text((width + 65, 218), "017 / 04", font=ImageFont.truetype(font, 39), fill=ink)
draw.line((width + 49, 374, width * 2 - 49, 374), fill=ochre, width=5)
atlas.save(root / "Assets/JustOneMorePeel/Art/Tools/CardFace.png")
