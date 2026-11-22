"""Working documents from the basement rounds and the house archive."""
from pathlib import Path
from random import Random
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
OUT = ROOT.parents[1] / "Assets/JustOneMorePeel/Art/Notices"
FONTS = Path("C:/Windows/Fonts")
INK, BLUE, RED = "#343d39", "#394e68", "#914e3e"


def stock(size, colour):
    rng = Random(17)
    grain = Image.frombytes("L", size, rng.randbytes(size[0] * size[1]))
    return Image.blend(Image.new("RGB", size, colour), grain.convert("RGB"), .055)


def line(draw, xy, words, font, size, colour=INK, limit=0):
    face = ImageFont.truetype(str(font), size)
    assert not limit or draw.textlength(words, font=face) <= limit, words
    draw.text(xy, words, font=face, fill=colour, anchor="lt")


def rounds(ru):
    sheet = stock((640, 896), "#e0ddc7")
    d = ImageDraw.Draw(sheet)
    labels = ("ПОДВАЛ   /   УЗЕЛ 02", "Лист обхода", "ДАТА", "ОТМЕТКА ДЕЖУРНОГО",
              "Сухо у ввода", "Кран закрыт", "Ключ в щитовой", "Смена Б") if ru else (
              "BASEMENT   /   UNIT 02", "Rounds log", "DATE", "DUTY NOTES",
              "Inlet dry", "Valve shut", "Key in switch room", "Shift B")
    d.rectangle((0, 0, 640, 78), fill="#475e54")
    line(d,(38,24),labels[0],FONTS/"arialbd.ttf",29,"#f0ebd5")
    line(d,(36,114),labels[1],FONTS/"timesbd.ttf",83,limit=565)
    line(d,(42,237),labels[2],FONTS/"arial.ttf",20)
    line(d,(199,237),labels[3],FONTS/"arial.ttf",20)
    for y in range(279, 765, 80):
        d.line((36,y,603,y),fill="#868f82",width=2)
    d.line((178,228,178,760),fill="#899184",width=2)
    for i, date in enumerate(("12.X","13.X","14.X")):
        y = 306 + i*80
        line(d,(48,y),date,ROOT/"Fonts/Caveat.ttf",42,BLUE)
        line(d,(198,y-3),labels[4+i],ROOT/"Fonts/Caveat.ttf",42,BLUE,399)
    line(d,(43,808),labels[7],FONTS/"timesi.ttf",28)
    return sheet


def archive(ru):
    sheet = stock((896, 544), "#dac7a2")
    d = ImageDraw.Draw(sheet)
    labels = ("ДОМОВОЙ АРХИВ", "Лист выдачи", "ФОНД", "ДЕЛО", "Папка заявок", "Вернуть на полку Б") if ru else (
        "HOUSE ARCHIVE", "Loan slip", "FONDS", "FILE", "Maintenance requests", "Return to shelf B")
    d.rectangle((0,0,22,544),fill=RED)
    line(d,(60,39),labels[0],FONTS/"courbd.ttf",30)
    line(d,(61,112),labels[1],FONTS/"times.ttf",83,limit=570)
    d.rectangle((695,34,850,177),outline=RED,width=4)
    line(d,(715,49),labels[2],FONTS/"cour.ttf",22,RED)
    line(d,(724,86),"04",FONTS/"timesbd.ttf",67,RED)
    d.line((60,225,850,225),fill=INK,width=3)
    line(d,(63,258),labels[3]+"  17 / Б" if ru else labels[3]+"  17 / B",FONTS/"courbd.ttf",31)
    line(d,(62,321),labels[4],FONTS/"timesi.ttf",45)
    d.line((63,411,848,411),fill="#96896f",width=2)
    line(d,(211,439),labels[5],ROOT/"Fonts/Caveat.ttf",49,BLUE)
    return sheet


def remnants():
    sheet = stock((512,256), "#bbaa90").convert("RGBA")
    mask = Image.new("L",sheet.size)
    d = ImageDraw.Draw(mask)
    rng = Random(23)
    # Three islands left along the gummed edge of a twice-torn sheet.
    for left,right,top,bottom in ((8,183,26,67),(201,274,27,44),(398,486,176,228)):
        lower = [(x,round(bottom-(bottom-top)*((x-left)/(right-left))**1.5)+rng.randrange(-5,6))
                 for x in range(left,right,3)]
        d.polygon([(left,top),(right,top+2),*reversed(lower)],fill=255)
    sheet.putalpha(mask)
    return sheet


if __name__ == "__main__":
    for russian in (True,False):
        directory = OUT if russian else OUT/"English"
        rounds(russian).save(directory/"31_RoundsLog.png")
        archive(russian).save(directory/"32_ArchiveLoan.png")
    remnants().save(OUT/"GummedEdge.png")
