"""Merge substantive additions into the saved pre-expansion text."""
from pathlib import Path
import re
OUT=Path(__file__).resolve().parent
base=(OUT/'verzija-pre-prosirenja/Zavrsni_rad_ESP32.md').read_text(encoding='utf-8')
additions=(OUT/'dopune_rada.md').read_text(encoding='utf-8')
for section in additions.split('@@BEFORE ')[1:]:
    marker,content=section.split('\n',1)
    assert base.count(marker+'\n')==1,marker
    base=base.replace(marker+'\n',content.strip()+'\n\n'+marker+'\n')
(OUT/'Zavrsni_rad_ESP32.md').write_text(base,encoding='utf-8')
print('Words:',len(base.split()),'Picture spaces:',base.count('{{SLIKA|'))
