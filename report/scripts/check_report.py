"""Check PDF structure, resolved references, and text bounds using TeX Live Poppler."""
import json
import csv
import hashlib
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
PDF=ROOT/"dist/main.pdf"
if not PDF.is_file() or not PDF.stat().st_size:
    raise SystemExit("PDF missing/empty")
info=subprocess.check_output(["pdfinfo",str(PDF)],text=True,encoding="utf-8",errors="replace")
text=subprocess.check_output(["pdftotext","-layout",str(PDF),"-"],text=True,encoding="utf-8",errors="strict")
bounding=subprocess.check_output(["pdftotext","-bbox-layout",str(PDF),"-"],text=True,encoding="utf-8",errors="strict")
xml=ET.fromstring(bounding)
pages=xml.findall(".//{*}page")
outside=[]
for n,page in enumerate(pages,1):
    width,height=float(page.attrib['width']),float(page.attrib['height'])
    for word in page.findall(".//{*}word"):
        a=word.attrib
        if float(a['xMin']) < 8 or float(a['xMax']) > width-8 or float(a['yMin'])<8 or float(a['yMax'])>height-8:
            outside.append({"page":n,"word":"".join(word.itertext()),"bounds":a})
log=(ROOT/"build/main.log").read_text(encoding="utf-8",errors="replace")
bad=re.findall(r"^.*(?:Overfull \\[hv]box|Missing character:|Citation .+undefined|Reference .+undefined|There were undefined references|LaTeX Error:|Package .+ Error:).*$",log,re.M)
font_warnings=re.findall(r"^LaTeX Font Warning:.*$",log,re.M)
underfull_boxes=re.findall(r"^Underfull \\[hv]box.*$",log,re.M)
active_files = set()
def collect(path):
    path = path.resolve()
    if path in active_files:
        return ""
    active_files.add(path)
    content = path.read_text(encoding="utf-8")
    children = re.findall(r"\\(?:input|include)\{([^}]+)\}", content)
    children += ["figures/" + name for name in re.findall(r"\\diagram\{([^}]+)\}", content)]
    parts = [content]
    for name in children:
        if "#" in name:
            continue
        child = ROOT / name
        if not child.suffix:
            child = child.with_suffix(".tex")
        parts.append(collect(child))
    return "\n".join(parts)
source = collect(ROOT / "main.tex")
labels=re.findall(r"\\label\{([^}]+)\}",source)
refs=re.findall(r"\\(?:ref|eqref)\{([^}]+)\}",source)
with (ROOT/"evidence/test-cases.csv").open(encoding="utf-8-sig",newline="") as f:
    test_count=sum(1 for _ in csv.DictReader(f))
required = ["iCareX", "PHAN TRUNG HIẾU", "Nhóm 2", "Nguyễn Thanh Tùng", "Ngô Minh Triết", "Thế Vinh", "2449106", "DEC07", "HttpOnly", "CSRF", "450.000", "Tài liệu tham khảo", "Nguyên lý thiết kế", "Inter", "F4F8F1", "2F6B4A", "13.000", "16.000"]
forbidden = ["Wiki", "JWT", "Kiểm soát tài liệu", "Khai báo sử dụng AI", "Nghiên cứu người dùng dự kiến", "Contract phương thức đề xuất", "Đặc tả operation đích", "Điểm cần chốt trước triển khai", "wireframe", "LaTeX", "TikZ", "Báo cáo lấy từ workspace"]
compact_text = re.sub(r"\s+", " ", text)
missing = [value for value in required if value not in compact_text]
prohibited = [value for value in forbidden if value.casefold() in compact_text.casefold()]
fabricated_assets = sorted(str(p.relative_to(ROOT)) for p in active_files if "brand" in p.parts or p.name.startswith("ui-"))
design_images = set(re.findall(r"\\includegraphics(?:\[[^\]]*\])?\{(figures/design/[^}]+)\}", source))
manifest = json.loads((ROOT/"evidence/design-assets.json").read_text(encoding="utf-8-sig"))
manifest_assets = {item['asset']: item for item in manifest['assets']}
asset_failures = []
for name in sorted(design_images):
    item = manifest_assets.get(name)
    if item is None:
        asset_failures.append(name + ': no source provenance')
        continue
    original = ROOT.parent / item['source']
    target = ROOT / name
    if not original.is_file() or not target.is_file():
        asset_failures.append(name + ': missing original or asset')
        continue
    original_hash = hashlib.sha256(original.read_bytes()).hexdigest()
    target_hash = hashlib.sha256(target.read_bytes()).hexdigest()
    if original_hash != item['source_sha256'] or target_hash != item['asset_sha256']:
        asset_failures.append(name + ': source or asset hash changed')
    if item['operation'] == 'byte-for-byte copy' and original_hash != target_hash:
        asset_failures.append(name + ': screenshot copy differs from original')
    if 'crop_xywh' in item:
        x, y, width, height = item['crop_xywh']
        if min(x, y) < 0 or width <= 0 or height <= 0 or x+width > item['source_width'] or y+height > item['source_height']:
            asset_failures.append(name + ': invalid crop bounds')
required_design_pages = {'figures/design/' + name + '.png' for name in ('landing', 'login', 'register', 'vehicle-overview', 'fuel-create', 'notifications')}
asset_failures += [name + ': page not included' for name in sorted(required_design_pages - design_images)]
table_ids = set(re.findall(r"\bTC\d{2}\b", (ROOT/"chapters/06-testcase-details.tex").read_text(encoding="utf-8")))
with (ROOT/"evidence/test-cases.csv").open(encoding="utf-8-sig", newline="") as f:
    csv_ids = {row['id'] for row in csv.DictReader(f)}
result={"pdf_bytes":PDF.stat().st_size,"physical_pages":len(pages),"editable_tikz_diagrams":sum(p.parent == ROOT/"figures" for p in active_files),"use_case_cards":source.count(r"\subsubsection{UC"),"business_test_cases":test_count,"required_text_present":not missing,"missing_required_text":missing,"prohibited_text_present":prohibited,"unverified_brand_or_wireframe_assets":fabricated_assets,"design_images_used":len(design_images),"design_asset_provenance_failures":asset_failures,"test_plan_id_mismatch":sorted(table_ids.symmetric_difference(csv_ids)),"text_outside_page_safe_bounds":outside,"tex_errors_or_overfull_or_missing_glyph":bad,"font_warnings":font_warnings,"underfull_box_count":len(underfull_boxes),"unresolved_source_labels":sorted(set(refs)-set(labels)),"pdfinfo":info,"note":"Business tests are Planned/Not run. Design screenshots and exact logo/icon crops are user-provided source material; screenshot fallback was explicitly authorized. No direct Figma file read is claimed."}
(ROOT/"evidence/document-validation.json").write_text(json.dumps(result,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
(ROOT/"build/report-text.txt").write_text(text,encoding="utf-8")
print(json.dumps({k:v for k,v in result.items() if k not in {"pdfinfo"}},ensure_ascii=False,indent=2))
raise SystemExit(1 if bad or font_warnings or outside or missing or prohibited or fabricated_assets or asset_failures or result['unresolved_source_labels'] or result['test_plan_id_mismatch'] else 0)
