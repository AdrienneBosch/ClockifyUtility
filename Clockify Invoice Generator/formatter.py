from docx.shared import Pt, RGBColor
from docx.enum.text import WD_PARAGRAPH_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

TITLE_FONT_SIZE = 26
TITLE_COLOR = RGBColor(0xC0, 0x50, 0x4D)
HEADING_FONT_SIZE = 18
HEADING_LEVEL_SIZES = {1: 20, 2: 18, 3: 16, 4: 14}
BODY_FONT_SIZE = 12
SEPARATOR_COLOR = "c0504d"
SEPARATOR_SIZE_TITLE = 8
SEPARATOR_SIZE_HEADING = 6
DEFAULT_SEPARATOR_COLOR = "666666"
DEFAULT_SEPARATOR_SIZE = 6

class DocumentFormatter:
    def __init__(self, doc):
        self.doc = doc

    def add_title(self, text):
        p = self.doc.add_paragraph()
        run = p.add_run(text)
        run.bold = True
        run.font.size = Pt(TITLE_FONT_SIZE)
        run.font.color.rgb = TITLE_COLOR
        p.alignment = WD_PARAGRAPH_ALIGNMENT.LEFT
        p.paragraph_format.space_after = Pt(6)
        self._add_separator(p, color=SEPARATOR_COLOR, size=SEPARATOR_SIZE_TITLE)

    def add_heading(self, text):
        p = self.doc.add_paragraph()
        run = p.add_run(text)
        run.bold = True
        run.font.size = Pt(HEADING_FONT_SIZE)
        p.alignment = WD_PARAGRAPH_ALIGNMENT.LEFT
        self._add_separator(p, color=SEPARATOR_COLOR, size=SEPARATOR_SIZE_HEADING)

    def add_heading_level(self, level, text):
        font_size = HEADING_LEVEL_SIZES.get(level, BODY_FONT_SIZE)
        p = self.doc.add_paragraph()
        run = p.add_run(text)
        run.bold = True
        run.font.size = Pt(font_size)
        p.alignment = WD_PARAGRAPH_ALIGNMENT.LEFT
        self._add_separator(p, color=SEPARATOR_COLOR, size=SEPARATOR_SIZE_HEADING)

    def add_body(self, text):
        p = self.doc.add_paragraph(text)
        p.alignment = WD_PARAGRAPH_ALIGNMENT.LEFT

    def add_paragraph(self, text):
        p = self.doc.add_paragraph(text)
        p.alignment = WD_PARAGRAPH_ALIGNMENT.LEFT

    def _add_separator(self, paragraph, color=DEFAULT_SEPARATOR_COLOR, size=DEFAULT_SEPARATOR_SIZE):
        p = paragraph._p
        pPr = p.get_or_add_pPr()
        pBdr = OxmlElement("w:pBdr")
        bottom = OxmlElement("w:bottom")
        bottom.set(qn("w:val"), "single")
        bottom.set(qn("w:sz"), str(size))
        bottom.set(qn("w:space"), "1")
        bottom.set(qn("w:color"), color)
        pBdr.append(bottom)
        pPr.append(pBdr)
