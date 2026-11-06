from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[2]
im=Image.new('RGB',(768,480),'#b8b8a5');d=ImageDraw.Draw(im)
d.rectangle((0,55,768,135),fill='#334b47')
d.text((45,194),'17',font=ImageFont.truetype('C:/Windows/Fonts/bahnschrift.ttf',172),fill='#30413d')
for x1,x2,y in [(307,650,238),(307,531,267),(307,600,297)]:d.line((x1,y,x2,y),fill='#75877b',width=9)
d.line((0,392,768,392),fill='#52605a',width=3)
im.save(root/'Assets/JustOneMorePeel/Art/Tools/CardFace.png')
print('Authored card face: universal yard number, no bank details or branding')
