"""Fixed editorial compositions, local raster typography and licensed photography.
No image generation, random poster templates or bundled fonts. Source layouts are below.
"""
from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageEnhance, ImageFilter
from pathlib import Path
import argparse, json, re
parser=argparse.ArgumentParser();parser.add_argument('--language',choices=['ru','en'],default='ru');args=parser.parse_args()
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
OUT=ROOT/'Assets/JustOneMorePeel/Art/Notices'
if args.language=='en':OUT=OUT/'English'
OUT.mkdir(parents=True,exist_ok=True)
translations=json.loads((HERE/'notice-en.json').read_text(encoding='utf-8')) if args.language=='en' else {}
INK='#2e332c'; RED='#8a3829'; PAPER='#d6ceb2'; BLUE='#283f46'
def font(size,face='arialbd.ttf'):return ImageFont.truetype('C:/Windows/Fonts/'+face,size)
def canvas(color=PAPER,size=(768,1024)):
    im=Image.new('RGB',size,color);return im,ImageDraw.Draw(im)
def text(d,xy,t,size=35,color=INK,face='arialbd.ttf'):
    if args.language=='en':
        entry=translations.get(t,t)
        if isinstance(entry,dict):t=entry['text'];size=entry.get('size',size)
        else:t=entry
        if re.search('[А-Яа-яЁё]',t):raise ValueError('Missing English text: '+t)
    d.text(xy,t,font=font(size,face),fill=color,spacing=6)
def rule(d,y,color=INK,width=2):d.line((44,y,724,y),fill=color,width=width)
def photo(im,name,box,tint=None,crop=None):
    p=Image.open(HERE/'Photos'/name).convert('RGB')
    if crop:p=p.crop(crop)
    p=ImageOps.fit(p,(box[2]-box[0],box[3]-box[1]))
    if tint:p=ImageOps.colorize(ImageOps.grayscale(p),tint[0],tint[1])
    im.paste(p,box[:2])
def finish(im,name,old=False,tearoffs=False):
    # Subtle fixed folds and rubbed edges, without random placement.
    im=im.convert('RGBA');w,h=im.size
    shade=Image.new('RGBA',im.size);d=ImageDraw.Draw(shade)
    d.line((w//2,0,w//2,h),fill=(73,59,32,17),width=3)
    d.line((w//2+3,0,w//2+3,h),fill=(255,246,214,22),width=2)
    d.line((0,int(h*.62),w,int(h*.62)),fill=(79,65,38,14),width=3)
    if old:d.rectangle((2,2,w-3,h-3),outline=(79,62,34,30),width=7)
    im=Image.alpha_composite(im,shade)
    mask=Image.new('L',im.size,255);d=ImageDraw.Draw(mask)
    # Deliberate old tears, different from a uniformly shredded perimeter.
    if name.startswith(('04','07','10')):
        d.polygon([(0,0),(15,0),(5,63),(12,147),(0,212)],fill=0)
        d.polygon([(w,h),(w-36,h),(w-8,h-21),(w-12,h-77),(w,h-105)],fill=0)
    elif name.startswith(('03','05','09')):
        d.polygon([(w,0),(w-8,0),(w-4,92),(w-11,156),(w,220)],fill=0)
    elif name.startswith('06'):
        d.polygon([(0,h-15),(59,h-8),(117,h-14),(159,h-4),(w,h-8),(w,h),(0,h)],fill=0)
    if old:
        d.polygon([(0,h),(0,h-150),(41,h-119),(76,h-141),(110,h-107),(169,h-96),(203,h-123),(245,h-78),(w,h-42),(w,h)],fill=0)
    if tearoffs:
        for x in range(112,w-30,90):d.rectangle((x,h-165,x+2,h),fill=0)
        d.rectangle((204,h-126,291,h),fill=0)
    im.putalpha(mask);im.save(OUT/(name+'.png'))
    return im
items=[]
# 01: photocopied lost-cat notice, broad photograph and handwritten correction.
im,d=canvas('#e3dfce');text(d,(48,35),'ПРОПАЛ КОТ',77);rule(d,137,width=5)
photo(im,'cat-portrait.png',(52,166,716,620),crop=(0,710,828,1260));d=ImageDraw.Draw(im)
text(d,(52,644),'РЫЖИЙ. ЗОВУТ БАТОН.',37);text(d,(52,703),'ушёл гулять и не вернулся',30,face='comici.ttf')
text(d,(52,754),'Если видели — зайдите в кв. 8.',30,face='arial.ttf')
text(d,(52,805),'Дети очень ждут.',30,RED,'segoepr.ttf');rule(d,846)
for x in range(37,700,90):
    strip=Image.new('RGBA',(130,70));sd=ImageDraw.Draw(strip);text(sd,(0,9),'кв. 8 • БАТОН',16)
    im.alpha_composite(strip.rotate(90,expand=True)) if im.mode=='RGBA' else im.paste(strip.rotate(90,expand=True),(x,865),strip.rotate(90,expand=True))
items.append(finish(im,'01_LostCat',tearoffs=True))
# 02: one photographic film-club print; small mixed-case serif title, no handbill hierarchy.
im,d=canvas('#172128');photo(im,'earthrise.jpg',(0,0,768,784));d=ImageDraw.Draw(im)
text(d,(38,34),'Киноклуб во дворе',25,'#e3e1d4','times.ttf')
text(d,(39,327),'Далеко',93,'#e6e0d1','timesi.ttf');text(d,(146,431),'от дома',93,'#e6e0d1','timesi.ttf')
text(d,(39,818),'пятница, 21:00',41,'#ddd6c7','times.ttf');text(d,(39,885),'Стул принеси с собой.',26,'#cccac2','times.ttf')
text(d,(39,936),'Фото: NASA / Apollo 8',16,'#949d9a','times.ttf');items.append(finish(im,'02_Cinema'))
# 03: a neighbour's small-band flyer, marker title and typewritten line-up.
im,d=canvas('#ddd8c7');text(d,(52,27),'в субботу играем во дворе',27,RED,'cour.ttf')
text(d,(30,115),'тише',112,RED,'segoeprb.ttf');text(d,(176,270),'двора',112,RED,'segoeprb.ttf')
d.line([(70,300),(338,315),(453,307)],fill=RED,width=4)
d.line([(182,452),(510,467),(699,452)],fill=RED,width=5)
text(d,(62,540),'Миша — пианино',33,face='cour.ttf');text(d,(62,598),'Лёша — бас',33,face='cour.ttf');text(d,(62,656),'соседи — всё остальное',33,face='cour.ttf')
d.rectangle((53,767,293,879),outline=RED,width=3);text(d,(77,783),'19:30',58,RED,'courbd.ttf')
text(d,(340,788),'за мастерской',27,face='cour.ttf');text(d,(340,831),'бесплатно',27,face='cour.ttf')
text(d,(61,943),'если дождь — под навесом',26,face='cour.ttf');items.append(finish(im,'03_QuietYard'))
# 04: ordinary Word-era office printout. Centred Times, dense useful information, no logo.
im,d=canvas('#e2e0d6');text(d,(165,43),'Уважаемые жители!',34,face='times.ttf')
text(d,(106,113),'Ремонт обуви',74,face='timesbd.ttf');text(d,(174,204),'во дворе дома № 17',34,face='times.ttf')
d.rectangle((58,285,710,620),outline=INK,width=2)
text(d,(82,307),'• Замена набоек и подошвы',34,face='times.ttf');text(d,(82,376),'• Замена молний',34,face='times.ttf');text(d,(82,445),'• Прошивка, растяжка',34,face='times.ttf');text(d,(82,514),'• Ремонт сумок и ремней',34,face='times.ttf')
text(d,(94,658),'Среда — воскресенье, 10–18 ч.',35,face='times.ttf');text(d,(134,720),'Вход рядом с подвалом.',35,face='times.ttf')
text(d,(119,789),'пн. и вт. — выходной',30,BLUE,'segoepr.ttf');d.line((82,864,688,864),fill=INK,width=2)
for x in range(35,700,90):text(d,(x,904),'№ 17',21,face='times.ttf')
items.append(finish(im,'04_ShoeRepair',tearoffs=True))
# 05: landscape garage advertisement, contrasting red handwritten amendment.
im,d=canvas('#d1cfbd',(768,512));text(d,(38,25),'СДАМ ГАРАЖ',58,face='courbd.ttf');d.line((40,113,724,113),fill=INK,width=4)
text(d,(41,148),'СУХОЙ. БЕЗ ЯМЫ.',37,face='cour.ttf');text(d,(41,218),'Спросить у сторожа за аркой.',27,face='cour.ttf')
text(d,(320,283),'уже сдан!',49,RED,'comici.ttf');d.line((42,84,710,192),fill=RED,width=5)
for x in range(42,700,90):text(d,(x,414),'№ 17',20)
items.append(finish(im,'05_Garage',old=True,tearoffs=True))
# 06: handwritten neighbour note on ruled paper, landscape format.
im,d=canvas('#ddd5bc',(768,640))
for y in range(88,620,55):d.line((16,y,752,y),fill='#a1b0a0',width=1)
d.line((89,0,89,640),fill='#bb7c72',width=2)
text(d,(117,28),'Соседи!',57,BLUE,'segoepr.ttf');text(d,(117,133),'Не закрывайте',37,BLUE,'segoepr.ttf');text(d,(117,204),'кота в подвале.',37,BLUE,'segoepr.ttf');text(d,(117,312),'Он там не живёт,',32,BLUE,'segoepr.ttf');text(d,(117,377),'он там работает :)',32,BLUE,'segoepr.ttf');text(d,(462,506),'кв. 8',34,BLUE,'segoepr.ttf');items.append(finish(im,'06_Neighbours'))
# 07: older municipal sign, deliberately broad empty border and broken lower edge.
im,d=canvas('#b9b39a');d.rectangle((47,44,720,942),outline=RED,width=6);text(d,(92,109),'ОБЪЯВЛЕНИЯ',55,RED,'impact.ttf');text(d,(103,204),'НЕ КЛЕИТЬ',76,RED,'impact.ttf');d.line((99,323,665,323),fill=RED,width=4)
text(d,(106,410),'СТЕНА ОКРАШЕНА',37);text(d,(106,481),'ОТВЕТСТВЕННЫЙ:',32,face='arial.ttf');text(d,(106,540),'ДВОРНИК',43);text(d,(106,756),'1987',78,RED,'timesbd.ttf');items.append(finish(im,'07_OldWarning',old=True))
# 08: cheap laser-printed service card with a large handwritten local contact.
im,d=canvas('#e2e2db');text(d,(45,35),'ЭЛЕКТРИК',92,'#1d211f','arialbd.ttf')
text(d,(51,168),'по соседству',45,'#1d211f','arial.ttf');d.line((52,243,718,243),fill=INK,width=5)
text(d,(52,284),'Выбивает автомат?',35,face='arial.ttf');text(d,(52,345),'Не работает розетка?',35,face='arial.ttf');text(d,(52,406),'Повесить люстру?',35,face='arial.ttf')
d.rectangle((49,514,718,741),outline='#3c514c',width=3)
text(d,(92,523),'Павел',69,BLUE,'segoeprb.ttf');text(d,(325,630),'кв. 12',53,BLUE,'segoepr.ttf')
text(d,(52,775),'После 18:00. Звоните в дверь.',29,face='arial.ttf');rule(d,850)
for x in range(37,700,90):
    tab=Image.new('RGBA',(140,70));td=ImageDraw.Draw(tab);text(td,(0,12),'Павел / кв.12',18,face='arial.ttf');tab=tab.rotate(90,expand=True);im.paste(tab,(x,868),tab)
items.append(finish(im,'08_Electrician',tearoffs=True))
# 09: small community exchange card, deliberately no headline template.
im,d=canvas('#d1c9c4',(768,640));text(d,(47,20),'Отдам',75,RED,'segoesc.ttf');text(d,(47,136),'цветы',83,RED,'segoesc.ttf')
text(d,(49,300),'фикус / алоэ / традесканция',29,face='arial.ttf');text(d,(49,368),'в хорошие руки, с горшками',29,face='arial.ttf');text(d,(49,478),'Вечером у подъезда №2',36,face='comici.ttf');items.append(finish(im,'09_Plants'))
# 10: a photocopied programme from a previous event, landscape and compact serif italics.
im,d=canvas('#c7c4b4',(768,512));photo(im,'earthrise.jpg',(31,40,304,377),('#3e4139','#c3c2b2'));d=ImageDraw.Draw(im)
text(d,(331,40),'Ночь короткого',37,face='georgiai.ttf');text(d,(331,96),'метра',64,face='georgiai.ttf')
text(d,(336,203),'7 июля, с 22 часов',27,face='times.ttf');text(d,(336,253),'Четыре фильма.',27,face='times.ttf');text(d,(336,293),'Один проектор.',27,face='times.ttf');text(d,(336,333),'Двор за аркой.',27,face='times.ttf')
text(d,(35,399),'Киноклуб / фото: NASA, Apollo 8',16,face='times.ttf');items.append(finish(im,'10_OldCinema',old=True))
sheet=Image.new('RGB',(5*230,2*335),'#343c38')
for i,item in enumerate(items):
    thumb=item.copy();thumb.thumbnail((214,302));sheet.paste(thumb,(i%5*230+8,i//5*335+8),thumb)
sheet.save(HERE/('notice-contact-sheet-en.jpg' if args.language=='en' else 'notice-contact-sheet.jpg'))
print('COMPOSED: 10 distinct '+args.language+' notices; contact sheet saved')
