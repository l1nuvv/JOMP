"""Twenty individually art-directed local documents; no generators or external assets.
Render EN/RU, validate text bounds, and emit the shared import catalogue.
Existing 01-10 PNGs remain unchanged. Installed fonts are rasterized, not bundled.
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont, ImageOps

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUT = ROOT / 'Assets/JustOneMorePeel/Art/Notices'
INK, RED, BLUE, GREEN = '#32382f', '#8c4237', '#344c59', '#4c6655'
NAMES = ['01_LostCat','02_Cinema','03_QuietYard','04_ShoeRepair','05_Garage',
         '06_Neighbours','07_OldWarning','08_Electrician','09_Plants','10_OldCinema',
         '11_BicycleWorkshop','12_BookExchange','13_TeaInTheYard','14_FoundKeys',
         '15_WaterNotice','16_ChessClub','17_SeedExchange','18_Alterations',
         '19_RadioRepair','20_BreadWindow','21_DogWalker','22_PhotoWalk',
         '23_YardTheatre','24_FleaMarket','25_PianoLessons','26_Stargazing',
         '27_CleanUp','28_LostMitten','29_ParcelNote','30_OldSummerFair']

def render(lang):
    dest = OUT / ('English' if lang == 'en' else '')
    dest.mkdir(parents=True, exist_ok=True)
    items=[]
    def tr(ru,en): return en if lang=='en' else ru
    def start(col='#e0dbc8',size=(768,1024)):
        nonlocal im,d
        im=Image.new('RGBA',size,col);d=ImageDraw.Draw(im)
    def txt(x,y,ru,en,size=34,face='arial.ttf',color=INK,width=None):
        content=tr(ru,en);limit=width or (im.width-x-36)
        while size>=14:
            f=ImageFont.truetype('C:/Windows/Fonts/'+face,size)
            box=d.multiline_textbbox((x,y),content,font=f,spacing=9)
            if box[2]<=x+limit and box[3]<=im.height-18:break
            size-=1
        assert box[0]>=0 and box[2]<=im.width-18 and box[3]<=im.height-18,(lang,content,box)
        d.multiline_text((x,y),content,font=f,fill=color,spacing=9)
    def line(points,col=INK,w=3):d.line(points,fill=col,width=w)
    def save(n,tabs=False,remnant=False):
        nonlocal im,d
        w,h=im.size
        # A restrained fold; content and illustration carry the design.
        fold=Image.new('RGBA',im.size);fd=ImageDraw.Draw(fold)
        fd.line((w//2,0,w//2,h),fill=(74,60,36,15),width=2)
        fd.line((w//2+2,0,w//2+2,h),fill=(255,255,242,24),width=2)
        im=Image.alpha_composite(im,fold)
        mask=Image.new('L',im.size,255);md=ImageDraw.Draw(mask)
        if tabs:
            for x in range(108,w-24,91):md.rectangle((x,h-140,x+3,h),fill=0)
            md.rectangle((202,h-103,288,h),fill=0)
        if remnant:
            md.polygon([(0,h),(0,h-96),(72,h-60),(123,h-86),(218,h-36),(331,h-72),(417,h-28),(w,h-56),(w,h)],fill=0)
            md.polygon([(0,0),(44,0),(14,92),(0,148)],fill=0)
        im.putalpha(mask)
        # Keep 768 px authoring coordinates, export economical 512 px prints.
        im=im.resize((512,round(h*512/w)),Image.Resampling.LANCZOS)
        im.save(dest/(NAMES[n-1]+'.png'),optimize=True)
        items.append(im.copy())
    im=d=None

    # 11. Workshop lithograph: bicycle dominates, narrow service footer.
    start('#c5d0c2');txt(46,32,'ВЕЛОМАСТЕРСКАЯ','CYCLE WORKSHOP',50,'arialbd.ttf',GREEN)
    txt(49,112,'снова в путь','back on the road',50,'timesi.ttf',GREEN)
    for cx in (203,559):d.ellipse((cx-133,319,cx+133,585),outline=GREEN,width=10)
    line([(203,452),(325,267),(431,452),(203,452),(294,320),(485,320),(559,452)],GREEN,10)
    line([(431,452),(485,273),(537,273)],GREEN,9);line([(284,264),(356,264)],GREEN,12)
    d.ellipse((413,434,449,470),outline=GREEN,width=5)
    txt(47,655,'КАМЕРЫ / ТОРМОЗА / ЦЕПИ','TYRES / BRAKES / CHAINS',30,'courbd.ttf')
    txt(47,730,'Приноси, даже если не едет.','Bring it in. We will get it rolling.',29)
    line((47,816,719,816),GREEN);txt(47,850,'За аркой · вт–сб, 11–19','Through the arch · Tue–Sat, 11–7',29)
    save(11)
    # 12. Library catalogue / bookplate, no advertising headline hierarchy.
    start('#dfd4b7');d.rectangle((36,34,731,988),outline=INK,width=2)
    txt(75,70,'ИЗ ДВОРОВОЙ БИБЛИОТЕКИ','THE COURTYARD LIBRARY',24,'times.ttf')
    txt(76,169,'Книга\nза книгу','A book\nfor a book',88,'georgia.ttf')
    txt(78,448,'Прочитал? Передай соседу.','Finished it? Pass it to a neighbour.',30,'timesi.ttf')
    for y,label in [(566,tr('Романы','Novels')),(636,tr('Сказки','Stories')),(706,tr('Путешествия','Travels'))]:
        line((79,y+46,685,y+46),'#8d8875',1);txt(80,y,label,label,32,'times.ttf')
    txt(80,840,'Полка в первом подъезде.\nБез записи и залога.','Shelf in the first stairwell.\nNo cards. No deposit.',31,'times.ttf')
    save(12)
    # 13. Kitchen invitation on squared notepaper, asymmetric handwritten body.
    start('#e5dfc8',(768,640))
    for y in range(20,640,40):line((0,y,768,y),'#b6c2b3',1)
    for x in range(20,768,40):line((x,0,x,640),'#c3cbbc',1)
    txt(66,42,'Чай во дворе','Tea in the yard',60,'segoeprb.ttf',BLUE)
    txt(68,176,'в воскресенье, в пять.\nЧашку возьми свою.','Sunday, at five.\nBring your own cup.',40,'segoepr.ttf',BLUE)
    txt(68,380,'Варенье уже есть :)','Jam is taken care of :)',37,'segoepr.ttf',RED)
    txt(449,523,'Нина, кв. 6','Nina, flat 6',30,'segoepr.ttf',BLUE);save(13)
    # 14. Found item card; original key drawing with local contact only.
    start('#ddd6ac',(768,768));txt(45,31,'ЧЬИ КЛЮЧИ?','WHOSE KEYS?',66,'arialbd.ttf')
    d.ellipse((196,184,397,385),outline=INK,width=11)
    line([(342,353),(504,530),(528,509),(550,533),(529,556)],INK,18)
    line([(251,379),(251,567),(286,567),(286,536)],INK,17)
    txt(47,614,'Нашлись у арки. Спросить в кв. 3.','Found by the arch. Ask at flat 3.',31);save(14)
    # 15. Official maintenance memo, explicit date and a plain table.
    start('#e0e0d6');txt(48,34,'СЛУЖБА ДОМА №17','HOUSE 17 / MAINTENANCE',24,'arialbd.ttf')
    line((48,94,718,94),BLUE,7);txt(48,142,'Отключение\nводы','Water supply\ninterruption',65,'arial.ttf',BLUE)
    d.rectangle((48,388,717,571),outline=BLUE,width=2)
    txt(74,409,'14 июля','14 July',37,'arialbd.ttf');txt(74,480,'09:00 — 13:00','09:00 — 13:00',51,'arialbd.ttf',BLUE)
    txt(48,636,'Проверяем трубы в подвале.\nПожалуйста, закройте краны.','Pipe inspection in the basement.\nPlease turn off your taps.',32)
    txt(48,825,'Работы: подъезды 1 и 2\nСпасибо за терпение.','Affected: stairwells 1 and 2\nThank you for your patience.',28)
    save(15)
    # 16. Chess correspondence club: purposeful board, little title.
    start('#c8c2aa',(768,896));txt(47,30,'ШАХМАТЫ ПО ЧЕТВЕРГАМ','CHESS ON THURSDAYS',38,'timesbd.ttf')
    for row in range(8):
        for col in range(8):d.rectangle((128+col*64,145+row*64,192+col*64,209+row*64),fill=INK if (row+col)%2 else '#e4ddc5')
    txt(46,704,'Сильный ход — зайти.','Your move. Come along.',38,'timesi.ttf')
    txt(46,788,'18:30 · у мастерской · любой уровень','6:30 pm · workshop · all levels',28,'times.ttf');save(16)
    # 17. Seed packet with original botanical ink illustration.
    start('#d9d6b9');txt(51,31,'ОБМЕН СЕМЕНАМИ','SEED EXCHANGE',44,'timesbd.ttf',GREEN)
    line([(380,661),(371,536),(389,408),(370,251)],GREEN,6)
    for x,y,side in [(373,550,-1),(383,458,1),(380,374,-1),(376,290,1)]:
        box=(x-140,y-83,x+8,y+5) if side<0 else (x-8,y-83,x+140,y+5)
        d.ellipse(box,outline=GREEN,width=4);line((x,y,x+side*105,y-50),GREEN,3)
    txt(48,699,'Бархатцы / укроп / фасоль','Marigold / dill / beans',37,'times.ttf')
    txt(48,784,'Подпишите свои пакетики.\nСуббота, 12:00, у подъезда.','Please label your packets.\nSaturday, noon, by the front door.',30,'times.ttf')
    txt(48,944,'Вырастим лето вместе.','Let us grow a summer together.',24,'timesi.ttf',GREEN);save(17)
    # 18. Tailor's narrow receipt / work order.
    start('#dfdbce',(640,1024));txt(44,33,'АТЕЛЬЕ','ALTERATIONS',65,'timesbd.ttf',RED)
    txt(45,130,'Мелкий ремонт одежды','Small repairs, neatly done',30,'times.ttf')
    line((44,204,596,204),RED,2)
    for y,ru,en in [(253,'Подшить брюки','Trouser hems'),(358,'Заменить молнию','Zip replacement'),(463,'Пришить пуговицы','Buttons sewn'),(568,'Укоротить шторы','Curtains shortened')]:
        txt(45,y,ru,en,34,'times.ttf');line((44,y+65,596,y+65),'#a49c87',1)
    txt(44,735,'НЕ ВЫБРАСЫВАЙТЕ.\nПОЧИНИМ.','KEEP IT.\nWE CAN MEND IT.',36,'timesbd.ttf',RED)
    txt(44,914,'кв. 5 · после 17:00','flat 5 · after 5 pm',28,'times.ttf');save(18,tabs=True)
    # 19. Repair technician's monospaced diagnostic sheet.
    start('#cccfc1');txt(44,33,'РАДИО / РЕМОНТ','RADIO / REPAIR',49,'courbd.ttf')
    txt(44,112,'НЕ СПЕШИТЕ ВЫБРАСЫВАТЬ','DO NOT THROW IT AWAY YET',26,'cour.ttf')
    d.rounded_rectangle((103,240,662,563),radius=34,outline=INK,width=6)
    for y in range(293,510,29):line((144,y,371,y),GREEN,4)
    d.ellipse((473,355,594,476),outline=INK,width=6);d.rectangle((432,270,617,305),outline=INK,width=3)
    line((571,240,628,160),INK,5)
    txt(46,634,'Приёмники, лампы, проигрыватели.\nДиагностика по договорённости.','Radios, lamps, record players.\nInspection by arrangement.',29,'cour.ttf')
    txt(46,804,'Сергей / мастерская у арки','Sergei / workshop by the arch',29,'courbd.ttf')
    txt(46,927,'Сохраним хороший звук.','Keep a good sound alive.',26,'cour.ttf');save(19)
    # 20. Bakery wrapper advert: ochre, oval seal and oversized serif word.
    start('#cfb784',(768,896));d.ellipse((193,34,575,199),outline=RED,width=3)
    txt(244,76,'ОКНО № 2','WINDOW 2',39,'timesbd.ttf',RED,width=300)
    txt(53,260,'Тёплый\nхлеб','Warm\nbread',109,'georgia.ttf',RED)
    txt(57,575,'каждое утро с 8:00','every morning from 8',36,'timesi.ttf')
    line((57,685,709,685),RED,2);txt(57,731,'Со стороны переулка.\nКогда кончится — закроемся.','Around the corner in the lane.\nOpen until the last loaf goes.',30,'times.ttf');save(20)
    # 21. Dog walker: economical tear-off offer, large time and informal promise.
    start('#dad9cf');txt(45,35,'ВЫГУЛЯЮ\nСОБАКУ','DOG\nWALKING',84,'arialbd.ttf',BLUE)
    txt(47,286,'пока вы на работе','while you are at work',35,'segoepr.ttf',BLUE)
    txt(47,415,'УТРОМ / ВЕЧЕРОМ','MORNINGS / EVENINGS',32,'arialbd.ttf')
    txt(47,514,'Больших не боюсь.\nЗа маленькими успеваю.','Big dogs welcome.\nSmall dogs kept up with.',35,'segoepr.ttf')
    txt(47,740,'Аня, кв. 9 · после школы','Anna, flat 9 · after school',31)
    for x in range(30,730,91):txt(x,914,'кв.9','flat 9',18,width=74)
    save(21,tabs=True)
    # 22. Local photo walk, contact-sheet framing and an existing licensed cat crop.
    start('#303e3e');txt(41,31,'Свет между домами','Light between houses',49,'timesi.ttf','#e2deca')
    p=Image.open(HERE/'Photos/cat-portrait.png').convert('RGB').crop((0,710,828,1260))
    p=ImageOps.colorize(ImageOps.grayscale(ImageOps.fit(p,(644,421))),'#273c3a','#d9d2b7')
    im.paste(p,(62,177));d=ImageDraw.Draw(im)
    txt(63,615,'01 / ДВОР №17','01 / COURTYARD 17',22,'cour.ttf','#bcbfae')
    txt(44,728,'Фотопрогулка · воскресенье, 10:00','Photo walk · Sunday, 10 am',32,'times.ttf','#e2deca')
    txt(44,824,'Подойдёт любой фотоаппарат.\nСобираемся под номером дома.','Any camera will do.\nMeet beneath the house number.',28,'times.ttf','#e2deca')
    txt(44,957,'Фото кота: Faust002 / CC0','Cat photo: Faust002 / CC0',18,'cour.ttf','#bcbfae');save(22)
    # 23. Amateur theatre playbill, bold split typography and a ticket band.
    start('#a95443');txt(47,29,'ДВОРЯНЕ / домашний театр','THE YARD PLAYERS / home theatre',23,'times.ttf','#eee0bf')
    txt(43,140,'ОКНО\nНАПРОТИВ','THE OTHER\nWINDOW',98,'timesbd.ttf','#eee0bf')
    txt(47,438,'Одна комедия. Четыре соседа.','One comedy. Four neighbours.',32,'timesi.ttf','#eee0bf')
    line((48,548,720,548),'#eee0bf',2)
    txt(49,605,'ПЯТНИЦА\n20:00','FRIDAY\n8 PM',56,'timesbd.ttf','#eee0bf',width=265)
    txt(346,627,'Во дворе за аркой.\nВход — улыбка.','Through the arch.\nAdmission: a smile.',29,'times.ttf','#eee0bf')
    d.rectangle((0,852,768,1024),fill='#dbc9a5');txt(49,887,'БИЛЕТ НА ДВОИХ / № 017','ADMIT TWO / NO. 017',38,'courbd.ttf');save(23)
    # 24. Flea market classified page, intentionally dense two-column listings.
    start('#d6d1bd');txt(40,24,'ДВОР БЕЗ ЛИШНИХ ВЕЩЕЙ','THE COURTYARD CLEAR-OUT',37,'timesbd.ttf')
    line((40,94,726,94),INK,5);txt(40,116,'БАРАХОЛКА / 16 ИЮЛЯ / С 11:00','FLEA MARKET / 16 JULY / FROM 11',25,'timesbd.ttf')
    line((384,207,384,760),INK,1)
    txt(43,224,'ПОСУДА\n\nЧашки без пары.\nСервиз с историей.\n\nКНИГИ\n\nДетективы, атлас,\nлюбимые сказки.','KITCHEN\n\nLonely teacups.\nA storied tea set.\n\nBOOKS\n\nMysteries, maps,\nbedtime stories.',28,'times.ttf',width=312)
    txt(414,224,'ДЛЯ ДОМА\n\nЛампа с бахромой.\nРамки для фото.\n\nМУЗЫКА\n\nПластинки, кассеты.\nПриходи слушать.','FOR THE HOME\n\nA fringed lamp.\nPhotograph frames.\n\nMUSIC\n\nRecords and tapes.\nCome and listen.',28,'times.ttf',width=312)
    line((40,804,726,804),INK,3);txt(42,851,'Ненужное тебе — находка соседу.','Your clutter. A neighbour\u2019s treasure.',33,'timesi.ttf')
    txt(42,949,'Стол принеси свой. Место найдётся.','Bring your table. There is room for all.',25,'times.ttf');save(24)
    # 25. Piano teacher: musical staff is the central image, generous white space.
    start('#dfdccd');txt(50,39,'Уроки\nфортепиано','Piano\nlessons',76,'timesi.ttf',BLUE)
    for y in range(365,486,30):line((52,y,716,y),BLUE,2)
    for x,y in [(178,454),(298,424),(418,394),(538,424)]:
        d.ellipse((x,y,x+36,y+23),fill=BLUE);line((x+35,y+10,x+35,y-83),BLUE,4)
    txt(51,607,'Для детей и взрослых.\nНачать никогда не поздно.','For children and grown-ups.\nIt is never too late to begin.',35,'times.ttf')
    txt(51,782,'Мария Ивановна\nПодъезд 1, кв. 4','Maria Ivanovna\nStairwell 1, flat 4',34,'times.ttf')
    save(25,tabs=True)
    # 26. Astronomy club: original star chart, muted midnight ink.
    start('#334b56');txt(47,36,'Небо над крышей','Above the rooftops',52,'georgiai.ttf','#dfd9bf')
    stars=[(130,388),(250,324),(381,357),(458,495),(568,531),(642,441),(624,291)]
    line(stars,'#a9b6ae',2)
    for x,y in stars:
        d.ellipse((x-6,y-6,x+6,y+6),fill='#e7e0be')
        line((x-12,y,x+12,y),'#e7e0be',1);line((x,y-12,x,y+12),'#e7e0be',1)
    txt(48,653,'Смотрим на звёзды\nв субботу после 22:00.','An evening of stargazing.\nSaturday, after 10 pm.',35,'times.ttf','#dfd9bf')
    txt(48,811,'Встреча у арки. Телескоп есть.\nЕсли облачно — переносим.','Meet at the arch. Telescope provided.\nCloudy skies? We will try next week.',28,'times.ttf','#dfd9bf')
    txt(48,957,'Кружок любопытных соседей','The curious neighbours club',22,'timesi.ttf','#b9c1b2');save(26)
    # 27. Volunteer rota, office table with a marker amendment.
    start('#d7ddcc');txt(45,32,'СУББОТНИК','YARD CLEAN-UP',66,'arialbd.ttf',GREEN)
    txt(46,137,'22 июля / 10:00 / наш двор','22 July / 10 am / our yard',32)
    d.rectangle((45,252,721,720),outline=GREEN,width=3);line((250,252,250,720),GREEN,2)
    for y,ru,en,task,entask in [(285,'10:00','10:00','Собираемся','Meet up'),(433,'10:15','10:15','Убираем двор','Tidy the yard'),(581,'12:00','12:00','Пьём чай','Have some tea')]:
        txt(68,y,ru,en,32);txt(278,y,task,entask,34);line((45,y+105,721,y+105),GREEN,1)
    txt(46,782,'Перчатки выдадим!','Gloves provided!',41,'segoepr.ttf',RED)
    txt(46,928,'Даже полчаса — уже помощь.','Even half an hour makes a difference.',26);save(27)
    # 28. One small child's lost mitten note, softly coloured pencil-like outline.
    start('#ddd1c6',(768,768));txt(56,33,'Потерялась варежка','Lost: one mitten',53,'segoepr.ttf',RED)
    mitten=[(284,478),(270,336),(249,295),(263,269),(294,284),(316,319),(319,220),(337,186),(380,174),(425,190),(443,226),(451,456),(435,490),(284,502)]
    d.polygon(mitten,fill='#b6c3bb');line(mitten+[mitten[0]],BLUE,6)
    line((291,449,447,437),BLUE,3);line((302,472,436,461),BLUE,3)
    txt(53,551,'Синяя. Левая. Очень любимая.','Blue. Left hand. Much loved.',33,'segoepr.ttf',BLUE)
    txt(53,660,'Если нашли — кв. 8. Спасибо!','If found, please ask at flat 8. Thanks!',29,'segoepr.ttf',BLUE);save(28)
    # 29. Quick parcel note on the back of an envelope, landscape miniature.
    start('#cdbb96',(768,512));line((0,0,384,260,768,0),'#9d8b69',2)
    txt(56,54,'Кв. 12!','Flat 12!',58,'segoeprb.ttf',BLUE)
    txt(58,182,'Посылка у меня.\nЗайди вечером.','Your parcel is with me.\nDrop by this evening.',40,'segoepr.ttf',BLUE)
    txt(476,376,'Нина, кв. 6','Nina, flat 6',30,'segoepr.ttf',BLUE);save(29)
    # 30. Earlier courtyard celebration; broken bottom is part of the composition.
    start('#c7bba1');txt(48,34,'ДОМ №17 / АРХИВ ЛЕТА','HOUSE 17 / SUMMER ARCHIVE',24,'cour.ttf',RED)
    txt(46,151,'НАШЕ\nЛЕТО','OUR\nSUMMER',122,'timesbd.ttf',RED)
    d.ellipse((560,51,684,175),outline=RED,width=5)
    txt(49,485,'праздник двора','a courtyard celebration',42,'timesi.ttf')
    txt(49,601,'Музыка • пироги • длинный вечер','Music • pies • a long summer evening',28,'times.ttf')
    txt(49,724,'28 АВГУСТА / 1998','28 AUGUST / 1998',43,'courbd.ttf',RED)
    txt(49,828,'Здесь были все свои.','Everyone belonged here.',29,'timesi.ttf');save(30,remnant=True)
    return items

if __name__=='__main__':
    for lang in ('ru','en'):
        render(lang)
        sheet=Image.new('RGB',(1500,6*342),'#303b36')
        for i,name in enumerate(NAMES):
            p=Image.open(OUT/('English' if lang=='en' else '')/(name+'.png')).convert('RGBA')
            p.thumbnail((276,302),Image.Resampling.LANCZOS)
            sheet.paste(p,(i%5*300+(300-p.width)//2,i//5*342+8),p)
            ImageDraw.Draw(sheet).text((i%5*300+12,i//5*342+316),name,font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17),fill='#e7e1cd')
        sheet.save(HERE/('notice-30-'+lang+'.jpg'),quality=92)
    entries=[]
    for name in NAMES:
        ru=Image.open(OUT/(name+'.png'));en=Image.open(OUT/'English'/(name+'.png'))
        assert ru.size==en.size,name
        opaque=ru.convert('RGBA').getchannel('A').getextrema()==(255,255)
        assert opaque==(en.convert('RGBA').getchannel('A').getextrema()==(255,255)),name
        entries.append(dict(name=name,opaque=opaque,width=ru.width,height=ru.height))
    (HERE/'notice-catalogue.json').write_text(json.dumps(dict(notices=entries),indent=2),encoding='utf-8')
    print('30 EN + 30 RU; text bounds and paired dimensions/opacity verified; catalogue and contact sheets written.')
