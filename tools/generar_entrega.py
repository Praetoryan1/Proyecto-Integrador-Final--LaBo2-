"""Genera la entrega preliminar y sus diagramas desde la fuente Markdown.

Dependencias de documentación: reportlab, pypdf, Pillow y Poppler.
No configura ni implementa la aplicación web.
"""
from pathlib import Path
import re
import shutil
import subprocess
from xml.sax.saxutils import escape

from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4, A3, landscape
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import (
    BaseDocTemplate, PageTemplate, Frame, Paragraph, Spacer, Table,
    TableStyle, PageBreak, NextPageTemplate, Flowable,
)
from reportlab.graphics.shapes import Drawing, Rect, String, PolyLine
from reportlab.graphics import renderPDF
from pypdf import PdfReader
from PIL import Image, ImageOps, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / 'docs'
OUT = ROOT / 'output' / 'pdf'
TMP = ROOT / 'tmp' / 'pdfs'
OUT.mkdir(parents=True, exist_ok=True)
TMP.mkdir(parents=True, exist_ok=True)

FONT_DIR = Path('C:/Windows/Fonts')
for name, file in [('Body', 'arial.ttf'), ('BodyBold', 'arialbd.ttf'), ('BodyItalic', 'ariali.ttf')]:
    pdfmetrics.registerFont(TTFont(name, str(FONT_DIR / file)))
pdfmetrics.registerFontFamily('Body', normal='Body', bold='BodyBold', italic='BodyItalic', boldItalic='BodyBold')

INK = colors.HexColor('#172639')
MUTED = colors.HexColor('#526379')
BLUE = colors.HexColor('#215987')
PURPLE = colors.HexColor('#68509C')
GREEN = colors.HexColor('#246B58')
ORANGE = colors.HexColor('#98612B')


def label(d, text, x, y, size=12, color=MUTED, align='middle'):
    width = pdfmetrics.stringWidth(text, 'BodyBold', size) + 8
    left = x - width / 2 if align == 'middle' else x - 4
    d.add(Rect(left, y - 3, width, size + 6, fillColor=colors.white, strokeColor=None))
    d.add(String(x, y, text, fontName='BodyBold', fontSize=size, fillColor=color, textAnchor=align))


def edge(d, points, start, end, color=BLUE, note=None):
    d.add(PolyLine([v for p in points for v in p], strokeColor=color, strokeWidth=1.6, fillColor=None))
    for text, x, y in (start, end):
        label(d, text, x, y, 11, color)
    if note:
        label(d, note[0], note[1], note[2], 10.5, color)


def card(d, title, x, y, w, h, fields, accent=BLUE):
    d.add(Rect(x, y, w, h, rx=7, ry=7, fillColor=colors.HexColor('#F7F9FC'), strokeColor=colors.HexColor('#C8D2DF'), strokeWidth=1))
    d.add(Rect(x, y+h-31, w, 31, rx=5, ry=5, fillColor=accent, strokeColor=None))
    d.add(String(x+12, y+h-21, title, fontName='BodyBold', fontSize=15, fillColor=colors.white))
    for i, field in enumerate(fields):
        d.add(String(x+12, y+h-52-i*17, field, fontName='Body', fontSize=12, fillColor=INK))


def core_diagram():
    d = Drawing(1000, 615)
    # Relaciones primero, para que las cajas delimiten las terminaciones.
    edge(d, [(290,480),(365,480)], ('1',305,488), ('1',350,488), note=('tiene',328,462))
    edge(d, [(655,480),(730,480)], ('1',669,488), ('0..N',710,488))
    edge(d, [(150,395),(150,322)], ('1',139,377), ('0..N',130,337), note=('crea',175,355))
    edge(d, [(320,200),(365,200)], ('1',330,210), ('0..N',346,182))
    edge(d, [(150,72),(150,31),(850,31),(850,72)], ('1',135,53), ('0..N',875,51), GREEN, ('clasificación N:M',500,21))
    edge(d, [(850,224),(850,178)], ('1',835,207), ('0..N',878,186), GREEN)
    card(d, 'Usuario', 20,395,270,165, [
        'PK  Id', 'UQ  EmailNormalizado', 'UQ  NombreUsuarioNormalizado',
        'PasswordHash · EmailConfirmado', 'Estado · FechaAlta'])
    card(d, 'Perfil', 365,355,290,230, [
        'PK/FK  UsuarioId', 'NombreVisible · Biografia', 'Especialidad · DisponibleParaTrabajar',
        'AvatarClave · Tema · ColorAcento', 'Disposicion · Visibilidad',
        'EmailContacto?', 'VisibilidadContacto'])
    card(d, 'EnlacePerfil',730,395,250,165,[
        'PK  Id', 'FK  PerfilUsuarioId', 'Etiqueta · Url · Orden'])
    card(d, 'Publicacion',20,72,300,250,[
        'PK  Id    FK  AutorId', 'Tipo · Titulo · Texto',
        'EstadoEditorial · EstadoModeracion', 'Visibilidad · OrdenDestacado?',
        'CreadaUtc · PublicadaUtc?', 'ActualizadaUtc · Version'])
    card(d, 'RecursoMultimedia',365,72,315,250,[
        'PK  Id    FK  PublicacionId', 'Tipo · EstadoProcesamiento',
        'ClaveAlmacenamiento? / YouTubeId?', 'Mime? · Bytes? · Orden',
        'TextoAlternativo · Creditos', 'UQ  (PublicacionId, Orden)'])
    card(d, 'Etiqueta',730,224,250,115,[
        'PK  Id    Nombre', 'UQ  NombreNormalizado', 'Activa'], GREEN)
    card(d, 'PublicacionEtiqueta',730,72,250,106,[
        'PK/FK  PublicacionId', 'PK/FK  EtiquetaId'], GREEN)
    return d


def social_diagram():
    d = Drawing(1000,615)
    edge(d,[(155,455),(155,370)],('1',139,430),('0..N',129,385),BLUE)
    edge(d,[(380,490),(310,490),(310,325),(280,325)],('1',358,501),('0..N',300,308),BLUE)
    edge(d,[(500,435),(500,365)],('1',486,416),('0..N',475,381),PURPLE)
    edge(d,[(735,490),(713,490),(713,395),(650,395),(650,315),(625,315)],('1',724,503),('0..N',648,296),PURPLE)
    edge(d,[(415,435),(340,435),(340,185),(325,185)],('1',327,405),('0..N',374,183),PURPLE,('SeguidorId',410,213))
    edge(d,[(443,435),(443,422),(355,422),(355,136),(325,136)],('1',368,400),('0..N',381,125),PURPLE,('SeguidoId',416,154))
    edge(d,[(625,529),(680,529),(680,251),(730,251)],('1',643,538),('0..N',708,260),ORANGE,('denunciante',655,574))
    edge(d,[(625,554),(665,554),(665,190),(730,190)],('0..1',648,558),('0..N',708,198),ORANGE,('resolutor',652,595))
    edge(d,[(860,455),(860,300)],('1',846,430),('0..N',834,317),ORANGE)
    card(d,'Rol',30,455,250,100,['PK  Id','UQ  NombreNormalizado'])
    card(d,'UsuarioRol',30,275,250,95,['PK/FK  UsuarioId','PK/FK  RolId'])
    card(d,'Usuario (misma tabla)',380,435,245,130,['PK  Id','Datos de identidad y estado','Ver vista de portafolios'])
    card(d,'Publicacion (misma tabla)',735,455,245,100,['PK  Id','FK  AutorId'])
    card(d,'Favorito',380,260,245,105,['PK/FK  UsuarioId','PK/FK  PublicacionId','CreadoUtc'],PURPLE)
    card(d,'Seguimiento',30,75,295,140,['PK/FK  SeguidorId -> Usuario','PK/FK  SeguidoId -> Usuario','CreadoUtc','SeguidorId distinto de SeguidoId'],PURPLE)
    card(d,'Reporte',730,70,250,230,['PK  Id','FK  PublicacionId','FK  DenuncianteId','FK  ResueltoPorId?','Motivo · Detalle · Estado','CreadoUtc · ResueltoUtc?','Resolucion?'],ORANGE)
    label(d,'Cada FK apunta a una sola fila; 0..N expresa la cantidad de asociaciones.',500,35,12)
    return d


DIAGRAMS = {'CORE':core_diagram(), 'SOCIAL':social_diagram()}
for key,drawing in DIAGRAMS.items():
    renderPDF.drawToFile(drawing, str(TMP/f'der-{key.lower()}.pdf'), title=f'Trazo - DER {key}')


class Diagram(Flowable):
    def __init__(self, drawing, width):
        super().__init__()
        self.drawing=drawing
        self.width=width
        self.scale=width/drawing.width
        self.height=drawing.height*self.scale
    def draw(self):
        self.canv.saveState()
        self.canv.scale(self.scale,self.scale)
        renderPDF.draw(self.drawing,self.canv,0,0)
        self.canv.restoreState()


styles = {
    'body':ParagraphStyle('body',fontName='Body',fontSize=10.1,leading=13.8,textColor=INK,spaceAfter=7),
    'h1':ParagraphStyle('h1',fontName='BodyBold',fontSize=18,leading=22,textColor=INK,spaceAfter=13),
    'h2':ParagraphStyle('h2',fontName='BodyBold',fontSize=12.2,leading=16,textColor=BLUE,spaceBefore=6,spaceAfter=8),
    'bullet':ParagraphStyle('bullet',fontName='Body',fontSize=10.1,leading=13.8,textColor=INK,leftIndent=11,firstLineIndent=-9,spaceAfter=6),
    'table':ParagraphStyle('table',fontName='Body',fontSize=9.6,leading=12.7,textColor=INK,spaceAfter=0),
    'th':ParagraphStyle('th',fontName='BodyBold',fontSize=9.6,leading=12.7,textColor=colors.white),
    'caption':ParagraphStyle('caption',fontName='Body',fontSize=9,leading=12,textColor=MUTED,spaceAfter=4),
}


def inline(text):
    text=escape(text)
    text=re.sub(r'\*\*(.+?)\*\*',r'<b>\1</b>',text)
    text=re.sub(r'`([^`]+)`',r'\1',text)
    return text


def parse_chunk(chunk, wide=False):
    lines=chunk.strip().splitlines()
    flows=[]
    i=0
    width=1000 if wide else A4[0]-88
    while i<len(lines):
        line=lines[i].strip()
        if not line:
            i+=1; continue
        if line.startswith('<!-- DIAGRAM'):
            key='CORE' if 'CORE' in line else 'SOCIAL'
            flows.append(Diagram(DIAGRAMS[key],width))
            flows.append(Spacer(1,6))
            i+=1; continue
        if line.startswith('|'):
            rows=[]
            while i<len(lines) and lines[i].strip().startswith('|'):
                cells=[c.strip() for c in lines[i].strip().strip('|').split('|')]
                if not all(re.fullmatch(r'[:\- ]+',c) for c in cells):
                    st='th' if not rows else 'table'
                    rows.append([Paragraph(inline(c),styles[st]) for c in cells])
                i+=1
            tab=Table(rows,colWidths=[125,width-125],repeatRows=1,hAlign='LEFT')
            tab.setStyle(TableStyle([
                ('BACKGROUND',(0,0),(-1,0),BLUE),('VALIGN',(0,0),(-1,-1),'TOP'),
                ('LEFTPADDING',(0,0),(-1,-1),8),('RIGHTPADDING',(0,0),(-1,-1),8),
                ('TOPPADDING',(0,0),(-1,-1),7),('BOTTOMPADDING',(0,0),(-1,-1),7),
                ('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.HexColor('#F2F6FA'),colors.white]),
                ('LINEBELOW',(0,0),(-1,-1),.4,colors.HexColor('#D9E1EA')),
            ]))
            flows.extend([tab,Spacer(1,10)])
            continue
        if line.startswith('# '):
            flows.append(Paragraph(inline(line[2:]),styles['h1']));i+=1;continue
        if line.startswith('## '):
            flows.append(Paragraph(inline(line[3:]),styles['h2']));i+=1;continue
        if line.startswith('- '):
            flows.append(Paragraph('- '+inline(line[2:]),styles['bullet']));i+=1;continue
        para=[]
        while i<len(lines) and lines[i].strip() and not lines[i].strip().startswith(('#','|','<!--','- ')):
            s=lines[i]
            para.append(inline(s.strip())+('<br/>' if s.endswith('  ') else ''))
            i+=1
        flows.append(Paragraph(' '.join(para),styles['caption'] if wide else styles['body']))
    return flows


def decoration(canvas,doc):
    w,h=canvas._pagesize
    canvas.saveState()
    canvas.setStrokeColor(colors.HexColor('#BFD3E2'))
    canvas.setLineWidth(.7)
    canvas.line(40,h-34,w-40,h-34)
    canvas.setFont('BodyBold',8)
    canvas.setFillColor(BLUE)
    canvas.drawString(40,h-26,'TRAZO / LABORATORIO II')
    canvas.setFont('Body',8)
    canvas.setFillColor(MUTED)
    canvas.drawRightString(w-40,h-26,'PROPUESTA PRELIMINAR · 15/10/2026')
    canvas.line(40,32,w-40,32)
    canvas.drawString(40,20,'Muñoz, Jonathan · Modelo inicial sujeto a validación docente')
    canvas.drawRightString(w-40,20,str(doc.page))
    canvas.restoreState()


pdf=OUT/'entrega-preliminar-trazo.pdf'
doc=BaseDocTemplate(str(pdf),pagesize=A4,title='Trazo - Entrega preliminar de Laboratorio II',author='Muñoz, Jonathan')
portrait_frame=Frame(44,44,A4[0]-88,A4[1]-96,leftPadding=0,rightPadding=0,topPadding=0,bottomPadding=0)
lw,lh=landscape(A3)
landscape_frame=Frame(40,42,lw-80,lh-88,leftPadding=0,rightPadding=0,topPadding=0,bottomPadding=0)
doc.addPageTemplates([
    PageTemplate(id='P',frames=[portrait_frame],pagesize=A4,onPage=decoration),
    PageTemplate(id='L',frames=[landscape_frame],pagesize=landscape(A3),onPage=decoration),
])
chunks=(DOCS/'entrega-preliminar.md').read_text(encoding='utf-8').split('<!-- PAGEBREAK -->')
story=[]
for i,chunk in enumerate(chunks):
    wide='<!-- DIAGRAM' in chunk
    if i:
        story.extend([NextPageTemplate('L' if wide else 'P'),PageBreak()])
    story.extend(parse_chunk(chunk,wide))
doc.build(story)

pdftoppm=shutil.which('pdftoppm')
if not pdftoppm:
    raise RuntimeError('Poppler pdftoppm es necesario para renderizar y revisar el resultado.')
for key in DIAGRAMS:
    stem=f'der-{key.lower()}'
    subprocess.run([pdftoppm,'-png','-scale-to','2400','-singlefile',str(TMP/f'{stem}.pdf'),str(DOCS/stem)],check=True)
imgs=[Image.open(DOCS/f'der-{key.lower()}.png').convert('RGB') for key in DIAGRAMS]
full=Image.new('RGB',(imgs[0].width,sum(im.height for im in imgs)+140),'white')
draw=ImageDraw.Draw(full)
from PIL import ImageFont
heading=ImageFont.truetype(str(FONT_DIR/'arialbd.ttf'),30)
y=0
for im,title in zip(imgs,['TRAZO · DER 1/2 · Portafolios y publicaciones','TRAZO · DER 2/2 · Comunidad y permisos (Usuario y Publicacion son las mismas tablas)']):
    draw.text((40,y+15),title,font=heading,fill='#172639')
    y+=70
    full.paste(im,(0,y));y+=im.height
full.save(DOCS/'der-preliminar.png')  # El DER actual se compone con tools/Generar-Der.mjs.

subprocess.run([pdftoppm,'-png','-r','105',str(pdf),str(TMP/'entrega')],check=True)
pages=[TMP/f'entrega-{i}.png' for i in range(1,len(PdfReader(pdf).pages)+1)]
thumbs=[]
for page in pages:
    im=Image.open(page).convert('RGB');im.thumbnail((470,620))
    cell=Image.new('RGB',(490,660),'#DCE2E9')
    cell.paste(im,((490-im.width)//2,15))
    ImageDraw.Draw(cell).text((15,637),page.stem,fill='#172639')
    thumbs.append(cell)
sheet=Image.new('RGB',(490*3,660*((len(thumbs)+2)//3)),'#DCE2E9')
for i,im in enumerate(thumbs):sheet.paste(im,((i%3)*490,(i//3)*660))
sheet.save(TMP/'contact-sheet.png')
reader=PdfReader(pdf)
print(f'PDF: {pdf}\nPáginas: {len(reader.pages)}')
for i,p in enumerate(reader.pages,1):
    t=p.extract_text() or ''
    print(f'Página {i}: {int(p.mediabox.width)}x{int(p.mediabox.height)}, {len(t)} caracteres; {t[:65]!r}')
alltext='\n'.join(p.extract_text() or '' for p in reader.pages)
assert 'jonathanezequielm20@gmail.com' in alltext
assert 'Muñoz' in alltext
assert 'PublicacionEtiqueta' in alltext
assert len(reader.pages)==6, 'Revisar desbordes: se esperan seis páginas.'
