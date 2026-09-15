"""Create a paginated PDF with linked contents independently of desktop Word."""
from pathlib import Path
import re, sys
from html import escape
from tipografija import segments
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'.tmp_doc_tools'))
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_JUSTIFY,TA_LEFT,TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import cm
from reportlab.platypus import BaseDocTemplate,PageTemplate,Frame,Paragraph,Spacer,PageBreak,PageBreakIfNotEmpty,Table,TableStyle,Image,KeepTogether,Flowable
from reportlab.platypus.tableofcontents import TableOfContents

for name,file in [('TNR','times.ttf'),('TNR-B','timesbd.ttf'),('TNR-I','timesi.ttf'),('TNR-BI','timesbi.ttf'),('Mono','consola.ttf'),('Mono-I','consolai.ttf')]:
    pdfmetrics.registerFont(TTFont(name,'C:/Windows/Fonts/'+file))
pdfmetrics.registerFontFamily('TNR',normal='TNR',bold='TNR-B',italic='TNR-I',boldItalic='TNR-BI')
pdfmetrics.registerFontFamily('Mono',normal='Mono',bold='Mono',italic='Mono-I',boldItalic='Mono-I')
W,H=A4
LEFT=3*cm;RIGHT=2.5*cm;TOP=2.5*cm;BOTTOM=2.3*cm
WIDTH=W-LEFT-RIGHT
styles={
    'body':ParagraphStyle('body',fontName='TNR',fontSize=12,leading=18,alignment=TA_JUSTIFY,spaceAfter=7,allowWidows=0,allowOrphans=0),
    'h1':ParagraphStyle('h1',fontName='TNR-B',fontSize=16,leading=20,spaceAfter=14,keepWithNext=True),
    'h2':ParagraphStyle('h2',fontName='TNR-B',fontSize=14,leading=17,spaceBefore=10,spaceAfter=7,keepWithNext=True),
    'h3':ParagraphStyle('h3',fontName='TNR-B',fontSize=12,leading=15,spaceBefore=10,spaceAfter=7,keepWithNext=True),
    'caption':ParagraphStyle('caption',fontName='TNR-I',fontSize=10,leading=12,alignment=TA_CENTER,spaceBefore=5,spaceAfter=9),
    'cell':ParagraphStyle('cell',fontName='TNR',fontSize=10,leading=12,spaceAfter=0),
    'cellhead':ParagraphStyle('cellhead',fontName='TNR-B',fontSize=10,leading=12),
    'code':ParagraphStyle('code',fontName='Mono',fontSize=9,leading=12,backColor=colors.HexColor('#F2F3F5'),borderPadding=8,spaceBefore=7,spaceAfter=12),
    'ref':ParagraphStyle('ref',fontName='TNR',fontSize=11,leading=14,spaceAfter=10,splitLongWords=True),
    'bullet':ParagraphStyle('bullet',fontName='TNR',fontSize=12,leading=14,alignment=TA_JUSTIFY,spaceAfter=7,leftIndent=14,firstLineIndent=0,bulletIndent=0,bulletFontName='TNR',bulletFontSize=11,allowWidows=0,allowOrphans=0),
}

class ThesisDoc(BaseDocTemplate):
    def afterFlowable(self,f):
        if isinstance(f,Paragraph) and getattr(f,'heading_level',None) is not None:
            level=f.heading_level;key=f.heading_key;title=f.getPlainText()
            self.canv.bookmarkPage(key)
            self.canv.addOutlineEntry(title,key,level=level,closed=False)
            if level<2:self.notify('TOCEntry',(level,f.text,self.page,key))

def page(c,d):
    if d.page>1:
        c.saveState();c.setFont('TNR',11);c.drawCentredString(W/2,1.2*cm,str(d.page));c.restoreState()

def draw_mixed(c,x,y,text,size=12,bold=False,center=True):
    runs=[(part,('TNR-BI' if bold else 'TNR-I') if english else ('TNR-B' if bold else 'TNR')) for part,english in segments(text)]
    if center:x-=sum(pdfmetrics.stringWidth(part,font,size) for part,font in runs)/2
    for part,font in runs:
        c.setFont(font,size);c.drawString(x,y,part);x+=pdfmetrics.stringWidth(part,font,size)

class Cover(Flowable):
    def __init__(self):super().__init__();self.width=WIDTH;self.height=H-TOP-BOTTOM-4
    def draw(self):
        c=self.canv;cx=WIDTH/2;y=self.height
        c.drawImage(str(OUT/'assets/grb_pmf.png'),cx-1.7*cm,y-3.4*cm,width=3.4*cm,height=3.4*cm)
        c.setFont('TNR',12)
        for k,t in enumerate(['ИНСТИТУТ ЗА МАТЕМАТИКУ И ИНФОРМАТИКУ','ПРИРОДНО-МАТЕМАТИЧКИ ФАКУЛТЕТ','УНИВЕРЗИТЕТ У КРАГУЈЕВЦУ']):
            c.drawCentredString(cx,y-4*cm-k*16,t)
        c.setFont('TNR',14);c.drawCentredString(cx,y-6.7*cm,'ЗАВРШНИ РАД')
        c.setLineWidth(1);c.line(0,y-7.55*cm,WIDTH,y-7.55*cm)
        c.setFont('TNR-B',17)
        for k,t in enumerate(['СИСТЕМ ЗА ДЕТЕКЦИЈУ БЕЖИЧНИХ','УРЕЂАЈА ТОКОМ ИСПИТНИХ СЕСИЈА','ПРИМЕНОМ ESP32 ПЛАТФОРМЕ']):
            draw_mixed(c,cx,y-8.35*cm-k*23,t,17,True)
        c.line(0,y-10.4*cm,WIDTH,y-10.4*cm)
        c.setFont('TNR-B',11)
        for x,ls in [(0,['Ментор','др Ана Капларевић-Малишић']),(9*cm,['Студент','Андрија Костовић, 51/2021'])]:
            for k,t in enumerate(ls):c.drawString(x,2.7*cm-k*15,t)
        c.setFont('TNR',12);c.drawCentredString(cx,1*cm,'Крагујевац, септембар 2026.')

def text_html(text):
    parts=[]
    for part,english in segments(text):
        value=escape(part)
        if english:value='<i>'+value+'</i>'
        if part.startswith(('https://','http://')):
            value=f'<link href="{escape(part,quote=True)}" color="#244C77">{value}</link>'
        parts.append(value)
    return ''.join(parts)

class PictureSpace(Flowable):
    def __init__(self, instruction):
        super().__init__();self.width=WIDTH;self.height=8.5*cm;self.instruction=instruction
    def draw(self):
        c=self.canv;c.setStrokeColor(colors.HexColor('#9CA6B0'));c.rect(0,0,self.width,self.height)
        p=Paragraph(text_html('МЕСТО ЗА СЛИКУ. '+self.instruction),styles['caption'])
        w,h=p.wrap(self.width-30,self.height);p.drawOn(c,15,(self.height-h)/2)

story=[Cover(),PageBreak(),Paragraph('Садржај',styles['h1'])]
toc=TableOfContents();toc.dotsMinLevel=0
toc.tableStyle=TableStyle([('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),0),('RIGHTPADDING',(0,0),(-1,-1),0),('TOPPADDING',(0,0),(-1,-1),0),('BOTTOMPADDING',(0,0),(-1,-1),0)])
toc.levelStyles=[ParagraphStyle('toc0',fontName='TNR',fontSize=11,leading=14,spaceBefore=3,leftIndent=0,firstLineIndent=0),ParagraphStyle('toc1',fontName='TNR',fontSize=10.5,leading=13,spaceBefore=1,leftIndent=14,firstLineIndent=0)]
story.append(toc)
lines=(OUT/'Zavrsni_rad_ESP32.md').read_text(encoding='utf-8').replace('\u00ad','').splitlines()
i=0;counter=0;picture_number=4
while i<len(lines):
    line=lines[i].strip()
    if not line:i+=1;continue
    if line.startswith('{{SLIKA|'):
        _,key,caption,instruction=line[2:-2].split('|',3);picture_number+=1
        story.append(KeepTogether([PictureSpace(instruction),Paragraph(text_html(f'Слика {picture_number}. {caption}'),styles['caption'])]));i+=1;continue
    if line.startswith('#'):
        n=len(line)-len(line.lstrip('#'))
        if n==1 or line.startswith('## 10.3.'):story.append(PageBreakIfNotEmpty())
        p=Paragraph(text_html(line[n:].strip()),styles['h'+str(n)])
        p.heading_level=n-1;p.heading_key='section_'+str(counter);counter+=1
        story.append(p);i+=1;continue
    if line.startswith('!['):
        m=re.match(r'!\[(.*?)\]\((.*?)\)',line)
        im=Image(str(OUT/m[2]));im.drawHeight=WIDTH*im.imageHeight/im.imageWidth;im.drawWidth=WIDTH
        story.append(KeepTogether([Spacer(1,5),im,Paragraph(text_html(m[1]),styles['caption'])]));i+=1;continue
    if line.startswith('```'):
        block=[];i+=1
        while i<len(lines) and not lines[i].startswith('```'):block.append(lines[i]);i+=1
        txt='<br/>'.join(text_html(x).replace(' ','&#160;') for x in block)
        story.append(KeepTogether([Paragraph(txt,styles['code'])]));i+=1;continue
    if line.startswith('|'):
        rows=[]
        while i<len(lines) and lines[i].strip().startswith('|'):
            row=[s.strip() for s in lines[i].strip().strip('|').split('|')]
            if not all(re.fullmatch(r'[-: ]+',s) for s in row):rows.append(row)
            i+=1
        widths=([7.8,7.7] if len(rows[0])==2 else [4,5.7,5.8] if len(rows[0])==3 else [2,4.2,4,2.5,2.8])
        data=[[Paragraph(escape(cell),styles['cellhead' if ri==0 else 'cell']) for cell in row] for ri,row in enumerate(rows)]
        t=Table(data,colWidths=[w*cm for w in widths],repeatRows=1,hAlign='LEFT')
        t.setStyle(TableStyle([('GRID',(0,0),(-1,-1),.5,colors.HexColor('#9CA6B0')),('BACKGROUND',(0,0),(-1,0),colors.HexColor('#E9EDF2')),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),6),('RIGHTPADDING',(0,0),(-1,-1),6),('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7)]))
        t.spaceAfter=10;story.append(t);continue
    if re.match(r'Табела \d+:',line):
        p=Paragraph(escape(line),styles['caption']);p.keepWithNext=True;story.append(p);i+=1;continue
    if line.startswith('- '):
        story.append(KeepTogether([Paragraph(text_html(line[2:]),styles['bullet'],bulletText='•')]))
        i+=1;continue
    style=styles['ref'] if re.match(r'\[\d+\]',line) else styles['body']
    story.append(Paragraph(text_html(line),style));i+=1

target=OUT/'Zavrsni_rad_Andrija_Kostovic_51_2021_ESP32_Prosireni.pdf'
d=ThesisDoc(str(target),pagesize=A4,leftMargin=LEFT,rightMargin=RIGHT,topMargin=TOP,bottomMargin=BOTTOM,title='Систем за детекцију бежичних уређаја током испитних сесија применом ESP32 платформе',author='Андрија Костовић')
d.addPageTemplates(PageTemplate(id='main',frames=[Frame(LEFT,BOTTOM,WIDTH,H-TOP-BOTTOM,leftPadding=0,rightPadding=0,topPadding=0,bottomPadding=0)],onPage=page))
d.multiBuild(story)
print(target)
