"""Hand-composed replacements for the owner's selected notice numbers.

Renders review drafts only. Existing Unity textures are untouched until reviewed.
All text is fictional. Source images and fonts are recorded in ASSET_SOURCES.md.
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont, ImageOps, ImageEnhance

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
OUT=ROOT/'Assets/JustOneMorePeel/Art/Notices'
DRAFT=HERE/'RevisionDraft'
CAT=json.loads((HERE/'notice-catalogue.json').read_text(encoding='utf8'))['notices']
SELECTED=[2,9,11,12,13,14,15,17,18,19,20,22,23,24,28,30]
LABEL={2:'КОНЦЕРТ',9:'СНИМУ КОМНАТУ',11:'ВЕЛОРЕМОНТ',12:'ВАКАНСИЯ',13:'ЧАЙ',14:'КЛЮЧИ',15:'ОТКЛЮЧЕНИЕ ВОДЫ',17:'ОРИЕНТИРОВКА',18:'АТЕЛЬЕ',19:'АУДИОМАСТЕРСКАЯ',20:'ВАКАНСИЯ ПЕКАРНИ',22:'ОБМЕН КАССЕТ',23:'МУЗЫКАЛЬНАЯ АФИША',24:'БАРАХОЛКА',28:'КЕПКА',30:'СТАРАЯ АФИША'}
PHOTOS=HERE/'Photos'
FONTS=Path('C:/Windows/Fonts')
INK='#292b28'; RED='#9e3d32'; BLUE='#284b5e'; CREAM='#f2ecda'

def font(name,size):
    path=HERE/'Fonts/Caveat.ttf' if name=='hand' else FONTS/name
    return ImageFont.truetype(str(path),int(size))

def render(number,lang):
    spec=CAT[number-1]; name=spec['name']
    src=OUT/('English' if lang=='en' else '')/(name+'.png')
    original=Image.open(src).convert('RGBA'); w,h=original.size
    # Work at original proportions; source alpha remains exactly unchanged.
    W=768; H=round(W*h/w)
    bg={2:'#1e2527',9:'#ddd8c9',11:'#d6c6a4',12:'#dfddd2',13:'#e8dfc6',14:'#e8e4d9',15:'#eeede6',17:'#ded8c7',18:'#e2d8c7',19:'#d4cbb3',20:'#eadcbf',22:'#d6ccb7',23:'#a83e33',24:'#d9ceae',28:'#e7e2d3',30:'#d5c6a4'}[number]
    im=Image.new('RGB',(W,H),bg);d=ImageDraw.Draw(im)
    def tr(ru,en):return en if lang=='en' else ru
    def text(x,y,ru,en,size=38,face='arial.ttf',fill=INK,maxw=None):
        s=tr(ru,en);px=int(H*y);size=int(size)
        f=font(face,size)
        while maxw and d.multiline_textbbox((x,px),s,font=f,spacing=7)[2]>x+maxw and size>14:
            size-=1;f=font(face,size)
        bounds=d.multiline_textbbox((x,px),s,font=f,spacing=7)
        if bounds[0]<0 or bounds[2]>W-8 or bounds[3]>H-5:raise ValueError((number,lang,s,bounds,(W,H)))
        d.multiline_text((x,px),s,font=f,fill=fill,spacing=7)
    def rule(y,fill=INK,width=2,x=48,x2=720):d.line((x,int(H*y),x2,int(H*y)),fill=fill,width=width)
    def photo(filename,box,mono=False,crop=None):
        nonlocal im,d
        x0,y0,x1,y1=box;y0=int(y0*H);y1=int(y1*H)
        p=Image.open(PHOTOS/filename).convert('RGB')
        if crop:p=p.crop(tuple(int(v) for v in crop))
        p=ImageOps.fit(p,(x1-x0,y1-y0),Image.Resampling.LANCZOS)
        if mono:p=ImageOps.colorize(ImageOps.grayscale(p),'#202321','#e4dfd0')
        im.paste(p,(x0,y0));d=ImageDraw.Draw(im)
    if number==2:
        photo('concert-crowd.jpg',(0,.0,768,.67),True)
        d.rectangle((0,int(H*.62),W,H),fill='#222528')
        text(45,.05,'ЖИВАЯ МУЗЫКА','LIVE MUSIC',35,'arialbd.ttf',CREAM)
        text(40,.65,'ПОСЛЕДНИЙ\nТРАМВАЙ','LAST\nTRAM HOME',82,'arialbd.ttf',CREAM,680)
        rule(.85,'#b75b4c',5);text(44,.87,'СУББОТА  /  19:30','SATURDAY  /  7:30 PM',36,'arialbd.ttf',CREAM)
        text(46,.94,'КЛУБ «ПОДВАЛ»  ·  ВХОД СО ДВОРА','BASEMENT CLUB  ·  YARD ENTRANCE',22,'arial.ttf',CREAM)
    elif number==9:
        for y in range(0,H,39):d.line((0,y,W,y),fill='#c4c1b5',width=1)
        text(52,.06,'Сниму комнату','Room wanted',67,'hand',BLUE,660)
        text(55,.27,'Недалеко от работы.\nОдин человек, без животных.','Near my work.\nOne person, no pets.',43,'hand',BLUE,655)
        text(56,.68,'Звонить после 18:00','Call after 6 pm',39,'hand',BLUE)
        text(56,.82,'555-01-82  /  Илья','555-01-82  /  Ilya',40,'hand',BLUE)
    elif number==11:
        text(40,.035,'РЕМОНТ ВЕЛОСИПЕДОВ','BICYCLE REPAIRS',57,'arialbd.ttf',INK,690);rule(.12,INK,5,40,728)
        text(44,.16,'КАМЕРЫ  •  ЦЕПИ\nТОРМОЗА  •  КОЛЁСА','TYRES  •  CHAINS\nBRAKES  •  WHEELS',54,'arialbd.ttf',INK,680)
        rule(.43,INK,2);text(48,.47,'Принимаю во дворе у арки.\nБез записи, кроме воскресенья.','Workshop by the courtyard arch.\nWalk-ins welcome, except Sunday.',35,'arial.ttf',INK,660)
        text(48,.72,'Алексей   555-01-26','Alex   555-01-26',42,'arialbd.ttf')
        for x in range(48,700,105):text(x,.90,'555-01','555-01',17,'arial.ttf')
    elif number==12:
        text(45,.04,'ТРЕБУЕТСЯ','HELP WANTED',77,'arialbd.ttf',INK,680);rule(.15,INK,8)
        text(49,.21,'ПОМОЩНИК\nВ МАГАЗИН','SHOP\nASSISTANT',64,'arialbd.ttf',INK,650)
        text(49,.47,'Приём товара и работа в зале.\nМожно без опыта — научим.','Stock deliveries and shop floor.\nNo experience needed; training given.',36,'arial.ttf',INK,675)
        text(49,.69,'СМЕНЫ 2 / 2   •   РЯДОМ С ДОМОМ','TWO DAYS ON / TWO DAYS OFF',28,'arialbd.ttf',BLUE,675)
        rule(.78,INK,2);text(49,.82,'Спросить администратора\nс 10:00 до 18:00','Ask for the manager\nfrom 10 am to 6 pm',31,'arial.ttf')
    elif number==13:
        for y in range(16,H,36):d.line((0,y,W,y),fill='#c3c5b6',width=1)
        for x in range(16,W,36):d.line((x,0,x,H),fill='#d4d1c2',width=1)
        text(48,.03,'Чай во дворе','Tea in the yard',77,'hand',BLUE,680)
        text(62,.24,'В воскресенье в пять\nпринесу самовар.','Sunday at five\nI will bring the samovar.',58,'hand',BLUE,675)
        text(53,.61,'Захватите кружки!','Bring your mugs!',55,'hand',RED,650)
        text(467,.82,'Нина, кв. 6','Nina, flat 6',40,'hand',BLUE,250)
    elif number==14:
        text(42,.025,'НАЙДЕНЫ КЛЮЧИ','KEYS FOUND',69,'arialbd.ttf',INK,680)
        photo('keys-on-table.jpg',(46,.17,722,.69),True,(1950,900,3450,2250))
        text(47,.73,'Нашёл у арки вечером.','Found by the arch in the evening.',34,'arial.ttf',INK,675)
        text(47,.83,'Забрать: подъезд 1, кв. 3','Collect: stairwell 1, flat 3',31,'arialbd.ttf',INK,675)
    elif number==15:
        text(47,.025,'УПРАВЛЯЮЩАЯ ОРГАНИЗАЦИЯ','PROPERTY MANAGEMENT',29,'timesbd.ttf',INK,675)
        text(47,.075,'ЖИЛИЩНЫЙ УЧАСТОК № 3','HOUSING SERVICE OFFICE 3',28,'times.ttf',INK,675);rule(.13,INK,2)
        text(48,.17,'УВАЖАЕМЫЕ ЖИТЕЛИ!','NOTICE TO RESIDENTS',51,'timesbd.ttf',INK,670)
        text(48,.28,'14 июля с 09:00 до 13:00','14 July, 09:00 to 13:00',43,'timesbd.ttf',INK,670)
        text(48,.37,'будет отключена холодная вода\nв связи с ремонтом трубопровода.','cold water will be switched off\nfor essential pipe repairs.',36,'times.ttf',INK,675)
        text(48,.54,'Дом № 17, подъезды 1 и 2.\nПросим заранее сделать запас воды\nи закрыть водоразборные краны.','Building 17, entrances 1 and 2.\nPlease store water in advance\nand keep taps closed.',33,'times.ttf',INK,675)
        rule(.76,INK,1);text(48,.79,'Диспетчерская: 555-01-17','Service desk: 555-01-17',31,'timesbd.ttf')
        text(48,.89,'Администрация жилищного участка','Housing service administration',26,'timesi.ttf',INK,675)
    elif number==17:
        text(42,.025,'ОРИЕНТИРОВКА','POLICE NOTICE',67,'arialbd.ttf',INK,680);rule(.13,INK,6)
        text(44,.16,'РОЗЫСК','WANTED',108,'arialbd.ttf',RED,680)
        text(47,.32,'Разыскивается подозреваемый\nпо делу о краже у станции.','Seeking a suspect in a\ntheft near the station.',39,'arial.ttf',INK,670)
        text(47,.49,'Тёмная куртка, светлая сумка.\nПоследний раз замечен\nу выхода на площадь.','Dark coat, light-coloured bag.\nLast seen near the exit\nto the square.',34,'arial.ttf',INK,670)
        rule(.73,INK,2);text(47,.77,'Сведения сообщать дежурному','Report information to the desk',30,'arialbd.ttf',INK,670)
        text(47,.85,'555-01-02   /   отдел № 4','555-01-02   /   station 4',41,'arialbd.ttf',INK,670)
    elif number==18:
        text(38,.025,'АТЕЛЬЕ','TAILOR',78,'timesbd.ttf',RED,680)
        text(40,.13,'РЕМОНТ И ПОДГОНКА ОДЕЖДЫ','CLOTHING REPAIRS & ALTERATIONS',29,'arialbd.ttf',INK,680)
        photo('sewing-buttons.jpg',(42,.21,726,.54),False)
        text(42,.58,'Пуговицы  •  молнии  •  подшив','Buttons  •  zips  •  hems',32,'timesbd.ttf',INK,680)
        text(42,.70,'Принесите вещь на примерку.\nПодъезд 2, цокольный этаж.','Bring your garment for fitting.\nEntrance 2, lower ground floor.',31,'times.ttf',INK,675)
        text(42,.81,'Вт–сб  10:00–18:00  /  555-01-45','Tue–Sat  10:00–18:00  /  555-01-45',25,'arial.ttf',INK,675)
    elif number==19:
        text(45,.03,'МАСТЕРСКАЯ ЗВУКА','AUDIO REPAIR',58,'arialbd.ttf',BLUE,675)
        text(47,.12,'РАДИО  /  МАГНИТОФОНЫ','RADIOS  /  TAPE PLAYERS',28,'arialbd.ttf',INK,675)
        photo('cassette.jpg',(48,.20,720,.53),True)
        text(48,.58,'Не крутит кассету?\nХрипит динамик?','Tape not playing?\nSpeaker crackling?',43,'timesbd.ttf',INK,665)
        text(48,.75,'Диагностика и ремонт.\nСергей, мастерская у арки.','Inspection and repair.\nSergei, workshop by the arch.',33,'arial.ttf',INK,670)
        text(48,.9,'555-01-39  ·  после 12:00','555-01-39  ·  after noon',30,'arialbd.ttf',BLUE)
    elif number==20:
        text(48,.035,'В ПЕКАРНЮ','BAKERY',38,'arialbd.ttf',RED)
        text(45,.13,'ТРЕБУЕТСЯ\nПРОДАВЕЦ','COUNTER\nSTAFF WANTED',72,'arialbd.ttf',INK,680)
        rule(.40,RED,6);text(48,.45,'Утренние смены.\nОпыт не обязателен.','Morning shifts.\nNo experience necessary.',39,'arial.ttf',INK,665)
        text(48,.67,'Пекарня у остановки\nСпросить Ольгу','Bakery by the bus stop\nAsk for Olga',36,'times.ttf',INK,665)
        text(48,.86,'555-01-20','555-01-20',45,'arialbd.ttf',RED)
    elif number==22:
        photo('cassette.jpg',(43,.08,725,.49),True)
        text(47,.53,'ОБМЕН КАССЕТ','TAPE SWAP',59,'arialbd.ttf',INK,675)
        text(47,.67,'Рок, джаз, записи с радио.\nПринеси свои — послушаем.','Rock, jazz and radio recordings.\nBring yours and have a listen.',34,'arial.ttf',INK,675)
        text(47,.84,'СУББОТА  /  15:00  /  У АРКИ','SATURDAY  /  3 PM  /  BY THE ARCH',28,'arialbd.ttf',RED,675)
    elif number==23:
        text(42,.025,'КЛУБ «СМЕНА»  ПРЕДСТАВЛЯЕТ','SMENA CLUB PRESENTS',28,'arialbd.ttf',CREAM,680)
        text(38,.15,'ШУМ\nГОРОДА','CITY\nNOISE',118,'arialbd.ttf',CREAM,690)
        rule(.50,CREAM,6);text(45,.54,'ТРИ ГРУППЫ  /  ОДИН ВЕЧЕР','THREE BANDS  /  ONE NIGHT',30,'arialbd.ttf',CREAM,675)
        text(46,.65,'ПЯТНИЦА  19:00','FRIDAY  7 PM',54,'arialbd.ttf',CREAM,675)
        text(46,.78,'Клуб у трамвайного кольца\nБилеты у входа','Club by the tram terminus\nTickets at the door',30,'arial.ttf',CREAM,675)
    elif number==24:
        text(44,.03,'БАРАХОЛКА','FLEA MARKET',76,'arialbd.ttf',INK,680)
        text(47,.17,'ВО ДВОРЕ У АРКИ','IN THE COURTYARD',43,'arialbd.ttf',RED,675)
        rule(.27,INK,5);text(48,.32,'КНИГИ  /  ПОСУДА\nОДЕЖДА  /  ПЛАСТИНКИ','BOOKS  /  DISHES\nCLOTHES  /  RECORDS',40,'arialbd.ttf',INK,675)
        text(48,.54,'Воскресенье, с 11 до 15.\nСвои вещи можно принести.','Sunday, 11 am to 3 pm.\nBring things you no longer need.',35,'arial.ttf',INK,675)
        rule(.76,INK,2);text(48,.80,'Место для стола бесплатно.','Table space is free.',31,'timesi.ttf',INK,675)
    elif number==28:
        for y in range(30,H,40):d.line((0,y,W,y),fill='#c9c9bd',width=1)
        text(50,.03,'ПОТЕРЯЛАСЬ\nКЕПКА','LOST\nA CAP',71,'hand',BLUE,670)
        text(52,.37,'Синяя, с белой полоской.\nЕсли нашли — напишите!','Blue with a white stripe.\nPlease let me know if found!',49,'hand',BLUE,675)
        text(51,.70,'Оставил на скамейке\nу детской площадки.','Left on the bench\nby the playground.',38,'hand',RED,675)
        text(53,.88,'кв. 8  /  спасибо','flat 8  /  thank you',36,'hand',BLUE)
    elif number==30:
        text(43,.025,'ДОМ КУЛЬТУРЫ  /  АФИША','COMMUNITY HALL  /  PROGRAMME',27,'timesbd.ttf',RED,680)
        rule(.12,RED,3);text(44,.16,'ВЕЧЕР\nПЛАСТИНОК','RECORD\nNIGHT',85,'timesbd.ttf',RED,680)
        text(46,.49,'Танцевальная музыка\nиз домашней коллекции','Dance music from\nlocal record collections',38,'times.ttf',INK,675)
        text(46,.70,'СУББОТА  /  18:00','SATURDAY  /  6 PM',43,'timesbd.ttf',INK)
        text(46,.82,'Вход со двора.\nБилет — у дежурного.','Enter from the courtyard.\nTickets at the desk.',32,'times.ttf',INK,675)
    im=im.resize((w,h),Image.Resampling.LANCZOS).convert('RGBA')
    im.putalpha(original.getchannel('A'))
    dest=DRAFT/lang;dest.mkdir(parents=True,exist_ok=True);im.save(dest/(name+'.png'),optimize=True)

def make_sheet(lang):
    sheet=Image.new('RGB',(1500,6*350),'#303b36');d=ImageDraw.Draw(sheet)
    for i,spec in enumerate(CAT):
        n=i+1;name=spec['name'];p=(DRAFT/lang/(name+'.png')) if n in SELECTED else OUT/('English' if lang=='en' else '')/(name+'.png')
        im=Image.open(p).convert('RGBA');im.thumbnail((276,305),Image.Resampling.LANCZOS)
        x=i%5*300;y=i//5*350;sheet.paste(im,(x+(300-im.width)//2,y+6),im)
        title=(str(n).zfill(2)+'  '+LABEL[n]) if n in SELECTED and lang=='ru' else name
        d.text((x+9,y+318),title,font=font('arial.ttf',17),fill='#f0ead8')
    path=DRAFT/('notice-review-'+lang+'.jpg');sheet.save(path,quality=94)
    return path

if __name__=='__main__':
    for language in ('ru','en'):
        for number in SELECTED:render(number,language)
        print(language,make_sheet(language))
    print('Drafts:',len(SELECTED)*2,'dimensions and alpha retained; source textures unchanged.')
