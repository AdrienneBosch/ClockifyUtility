from docx.shared import Pt, RGBColor
from docx.enum.text import WD_PARAGRAPH_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

def hex_to_rgb_color(hexstr):
    hexstr = hexstr.strip().lstrip("#")
    return RGBColor(int(hexstr[0:2], 16), int(hexstr[2:4], 16), int(hexstr[4:6], 16))

class DocumentFormatter:
    def __init__(self, doc, config):
        self.doc = doc
        self.title_font_size = int(config["TITLE_FONT_SIZE"])
        self.title_color = hex_to_rgb_color(config["TITLE_COLOR"])
        self.heading_sizes = config["HEADING_FONT_SIZES"]
        self.body_font_size = int(config["BODY_FONT_SIZE"])
        self.separator_color = config["SEPARATOR_COLOR"]
        self.separator_size = int(config["SEPARATOR_SIZE"])
        self.header_spacing_before = Pt(int(config["HEADER_SPACING_BEFORE"]))
        self.header_spacing_after = Pt(int(config["HEADER_SPACING_AFTER"]))
        self.body_spacing_before = Pt(int(config["BODY_SPACING_BEFORE"]))
        self.body_spacing_after = Pt(int(config["BODY_SPACING_AFTER"]))


    def add_title(self, text):
        p = self.doc.add_paragraph()
        run = p.add_run(text)
        run.bold = True
        run.font.size = Pt(self.title_font_size)
        run.font.color.rgb = self.title_color
        p.alignment = WD_PARAGRAPH_ALIGNMENT.LEFT
        p.paragraph_format.space_before = self.header_spacing_before
        p.paragraph_format.space_after = self.header_spacing_after
        self._add_separator(p)

    def add_heading(self, text, level=1):
        size = self.heading_sizes.get(level, self.body_font_size)
        p = self.doc.add_paragraph()
        run = p.add_run(text)
        run.bold = True
        run.font.size = Pt(size)
        p.alignment = WD_PARAGRAPH_ALIGNMENT.LEFT
        p.paragraph_format.space_before = self.header_spacing_before
        p.paragraph_format.space_after = self.header_spacing_after
        self._add_separator(p)

    def add_heading_level(self, level, text):
        self.add_heading(text, level)

    def add_body(self, text):
        p = self.doc.add_paragraph(text)
        p.alignment = WD_PARAGRAPH_ALIGNMENT.LEFT
        p.paragraph_format.space_before = self.body_spacing_before
        p.paragraph_format.space_after = self.body_spacing_after

    def add_paragraph(self, text):
        self.add_body(text)

    def _add_separator(self, paragraph):
        p = paragraph._p
        pPr = p.get_or_add_pPr()
        pBdr = OxmlElement("w:pBdr")
        bottom = OxmlElement("w:bottom")
        bottom.set(qn("w:val"), "single")
        bottom.set(qn("w:sz"), str(self.separator_size))
        bottom.set(qn("w:space"), "1")
        bottom.set(qn("w:color"), self.separator_color)
        pBdr.append(bottom)
        pPr.append(pBdr)
