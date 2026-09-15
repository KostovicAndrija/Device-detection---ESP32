"""One-time conversion of table data to complete bullet entries."""
from pathlib import Path
import re
p=Path(__file__).resolve().parent
source=(p/'Zavrsni_rad_ESP32.md').read_text(encoding='utf-8')
source=source.replace('пар уређај–сесија','пар који чине уређај и сесија')
source=source.replace('ток planned–active–completed','прелаз из стања planned у active, а затим у completed')
source=source.replace(' — ','. ').replace('\u00ad','')
lines=source.splitlines();out=[];i=0;table_count=0
while i<len(lines):
    line=lines[i]
    if re.match(r'Табела \d+:',line):
        out.append(re.sub(r'^Табела \d+:\s*','',line));i+=1;continue
    if line.startswith('|'):
        rows=[]
        while i<len(lines) and lines[i].startswith('|'):
            row=[s.strip() for s in lines[i].strip('|').split('|')]
            if not all(re.fullmatch(r'[-: ]+',s) for s in row): rows.append(row)
            i+=1
        table_count+=1
        for row in rows[1:]:
            if table_count==1: txt=f'{row[0]}. Реализација: {row[1]}. Начин провере: {row[2]}.'
            elif table_count==2: txt=f'{row[0]}. Технологија: {row[1]}. Улога: {row[2]}.'
            elif table_count==3: txt=f'{row[0]}. Примери: {row[1]}. Одговорност: {row[2]}.'
            elif table_count==4: txt=f'{row[0]}. Одабрана поља: {row[1]}. Намена: {row[2]}.'
            elif table_count==5: txt=f'{row[0]}. Намена: {row[1]}.'
            elif table_count==6:
                new,allowed=row[2].split(' / ')
                txt=f'RSSI: {row[0]} dBm. Врста сигнала: {row[1]}. Нов уређај: {new}. Дозвољен уређај: {allowed}. Резултат: {row[3]}. Праг 70: {row[4].lower()}.'
            else: txt=f'{row[0]}. Улаз: {row[1]}. Предмет провере: {row[2]}.'
            out.extend(['- '+txt,''])
        continue
    out.append(line);i+=1
(p/'Zavrsni_rad_ESP32.md').write_text('\n'.join(out)+'\n',encoding='utf-8')
print('Converted tables:',table_count)
