"""Promote reviewed render drafts without changing filenames, masks or GUIDs."""
from pathlib import Path
import shutil
from PIL import Image, ImageChops
from revise_selected_notices import CAT, SELECTED, OUT, DRAFT, HERE, ROOT

backup=ROOT/'Logs/PosterRevision/before'
if backup.exists():
    raise SystemExit('Backup already exists; stop to avoid overwriting earlier work.')
for language in ('ru','en'):
    for number in SELECTED:
        name=CAT[number-1]['name']+'.png'
        target=OUT/('English' if language=='en' else '')/name
        draft=DRAFT/language/name
        a=Image.open(target).convert('RGBA'); b=Image.open(draft).convert('RGBA')
        if a.size!=b.size or ImageChops.difference(a.getchannel('A'),b.getchannel('A')).getbbox():
            raise SystemExit(f'Dimensions or alpha changed: {language}/{name}')
for language in ('ru','en'):
    for number in SELECTED:
        name=CAT[number-1]['name']+'.png'
        target=OUT/('English' if language=='en' else '')/name
        dst=backup/language/name;dst.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(target,dst)
        shutil.copy2(DRAFT/language/name,target)
    shutil.copy2(HERE/('notice-30-'+language+'.jpg'),backup/('notice-30-'+language+'.jpg'))
    shutil.copy2(DRAFT/('notice-review-'+language+'.jpg'),HERE/('notice-30-'+language+'.jpg'))
print(f'Promoted {len(SELECTED)*2} PNGs; original masks and dimensions verified; backups at {backup}')
