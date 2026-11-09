"""Validate the web presentation, student IDs, layout and presenter notes."""
import hashlib
import json
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(__file__).resolve().parents[1]
pdf = root / "iCareX-ThuyetTrinh.pdf"
if not pdf.is_file() or not pdf.stat().st_size:
    raise SystemExit("Presentation PDF missing or empty")
raw_text = subprocess.check_output(["pdftotext", "-layout", str(pdf), "-"], encoding="utf-8")
bbox = subprocess.check_output(["pdftotext", "-bbox-layout", str(pdf), "-"], encoding="utf-8")
pages = ET.fromstring(bbox).findall(".//{*}page")
outside = []
wrong_aspect = []
for page_number, page in enumerate(pages, 1):
    width, height = float(page.attrib["width"]), float(page.attrib["height"])
    if abs(width/height - 16/9) > .002:
        wrong_aspect.append(page_number)
    for word in page.findall(".//{*}word"):
        a = word.attrib
        if float(a["xMin"]) < 2 or float(a["xMax"]) > width-2 or float(a["yMin"]) < 2 or float(a["yMax"]) > height-2:
            outside.append({"slide":page_number, "text":"".join(word.itertext()), "bounds":a})
log = (root / "build/main.log").read_text(encoding="utf-8", errors="replace")
tex_errors = re.findall(r"^.*(?:Overfull \\[hv]box|Missing character:|LaTeX Error:|Package .+ Error:|undefined references).*$", log, re.M)
font_warnings = re.findall(r"^LaTeX Font Warning:.*$", log, re.M)
text = re.sub(r"\s+", " ", raw_text)
required = ["iCareX", "PHAN TRUNG HIẾU", "Nguyễn Thanh Tùng", "Ngô Minh Triết", "Nông Thế Vinh",
            "2449106", "2313562", "2115302", "Ứng dụng web", "USER FLOW 01", "USER FLOW 02", "USER FLOW 03", "USER FLOW 04",
            "React 19", "Vite 8", "ASP.NET Core 10", "PostgreSQL", "HttpOnly", "CSRF",
            "8443", "5080", "12.699", "12.700", "13.000", "16.000", "450.000",
            "Cảm ơn thầy và các bạn", "đã lắng nghe!"]
missing = [term for term in required if term not in text]
forbidden = ["Chuẩn bị để chạy một buổi demo", "Demo 1", "Demo 2", "Demo 3", "Demo 4",
             "mobile", "Flutter", "Nhóm đề xuất thống nhất", "sau buổi họp"]
prohibited = [term for term in forbidden if term.casefold() in text.casefold()]
student_ids = {"Nguyễn Thanh Tùng": "2449106", "Ngô Minh Triết": "2313562", "Nông Thế Vinh": "2115302"}
cover_words = [word for word in pages[0].findall(".//{*}word")
               if float(word.attrib["xMin"]) > float(pages[0].attrib["width"]) * .60]
cover_words.sort(key=lambda word: (round(float(word.attrib["yMin"]), 1), float(word.attrib["xMin"])))
cover_text = " ".join("".join(word.itertext()) for word in cover_words)
wrong_student_ids = [name for name, sid in student_ids.items()
                     if not re.search(re.escape(name) + r"\s+MSSV:\s*" + sid + r"\b", cover_text)]
last_page_text = re.sub(r"\s+", " ", raw_text.split("\f")[-2]).strip()
closing_has_only_thanks = last_page_text == "Cảm ơn thầy và các bạn đã lắng nghe!"
font_info = subprocess.check_output(["pdffonts", str(pdf)], encoding="utf-8")
missing_fonts = [name for name in ("BeVietnamPro-Regular", "BeVietnamPro-SemiBold") if name not in font_info]
assets = json.loads((root/"assets-manifest.json").read_text(encoding="utf-8-sig"))
asset_errors = []
for asset in assets:
    path = root/asset["asset"]
    if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != asset["sha256"]:
        asset_errors.append(asset["asset"])
    original = root.parents[1] / asset["source"]
    if asset["operation"] == "exact copy" and (not original.is_file() or original.read_bytes() != path.read_bytes()):
        asset_errors.append(asset["asset"] + ": differs from source")
notes = (root/"Ghi-chu-thuyet-trinh.md").read_text(encoding="utf-8")
note_count = len(re.findall(r"^## \d+\.", notes, re.M))
pdf_copies = [root/"main.pdf", root/"dist/main.pdf"]
copy_mismatch = [str(p) for p in pdf_copies if not p.is_file() or p.read_bytes() != pdf.read_bytes()]
slide_labels = [int(n) for n in re.findall(r"(\d+)\s*/\s*17\b", raw_text)]
result = {
    "pages":len(pages), "aspect_ratio":"16:9", "wrong_aspect_slides":wrong_aspect,
    "pdf_bytes":pdf.stat().st_size, "sha256":hashlib.sha256(pdf.read_bytes()).hexdigest(),
    "missing_required_content":missing, "text_outside_page":outside,
    "prohibited_content":prohibited, "incorrect_student_ids":wrong_student_ids,
    "closing_has_only_thanks":closing_has_only_thanks, "missing_fonts":missing_fonts,
    "tex_errors_or_overfull_or_missing_glyph":tex_errors, "font_warnings":font_warnings,
    "verified_assets":len(assets), "asset_errors":asset_errors,
    "speaker_note_sections":note_count, "pdf_copy_mismatch":copy_mismatch,
    "slide_number_labels":slide_labels,
    "presentation_scope":"Web application; four user flows illustrated with user-provided design screenshots",
    "software_business_tests":"Not run: presentation-only change"
}
(root/"validation.json").write_text(json.dumps(result, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
(root/"build/slides-text.txt").write_text(raw_text, encoding="utf-8")
print(json.dumps(result, ensure_ascii=False, indent=2))
failed = (len(pages) != 17 or wrong_aspect or outside or tex_errors or font_warnings or missing or asset_errors
          or note_count != 17 or copy_mismatch or slide_labels != list(range(1,17)) or prohibited
          or wrong_student_ids or not closing_has_only_thanks or missing_fonts)
raise SystemExit(1 if failed else 0)
