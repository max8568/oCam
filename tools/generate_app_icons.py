"""Rebuild FrameCam's vector sources, PNGs, Windows ICO, and design sheet.

Development dependency: Pillow (python -m pip install Pillow).
Run from any directory: python tools/generate_app_icons.py
"""

from io import BytesIO
from pathlib import Path
import struct

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "src/FrameCam/Assets/Icons"
SIZES = (16, 20, 24, 32, 40, 48, 64, 128, 256)
BLUE = "#2563EB"
RED = "#EF4444"


def geometry(size):
    # At 16–24 px, slightly heavier corners and a larger recording light.
    return (4.8, 7.5) if size <= 24 else (4.2, 7)


def svg(mark=False):
    color = BLUE if mark else "#FFFFFF"
    background = "" if mark else f'<rect x="2" y="2" width="60" height="60" rx="14" fill="{BLUE}"/>'
    ring = "" if mark else '<circle cx="32" cy="32" r="9.5" fill="#FFFFFF"/>'
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" fill="none">
  <title>FrameCam — capture frame and recording light</title>
  {background}
  <path d="M23 18H15V26 M41 18H49V26 M15 38V46H23 M49 38V46H41"
        stroke="{color}" stroke-width="4.2" stroke-linecap="round" stroke-linejoin="round"/>
  {ring}
  <circle cx="32" cy="32" r="7" fill="{RED}"/>
</svg>
'''


def icon(size, mark=False):
    scale = 8
    image = Image.new("RGBA", (size * scale, size * scale))
    draw = ImageDraw.Draw(image)
    unit = size * scale / 64
    stroke, radius = geometry(size)

    def box(coords):
        return tuple(round(c * unit) for c in coords)

    def circle(x, y, r, color):
        draw.ellipse(box((x-r, y-r, x+r, y+r)), fill=color)

    if not mark:
        draw.rounded_rectangle(box((2, 2, 62, 62)), radius=round(14*unit), fill=BLUE)
    for path in (((23,18),(15,18),(15,26)), ((41,18),(49,18),(49,26)),
                 ((15,38),(15,46),(23,46)), ((49,38),(49,46),(41,46))):
        color = BLUE if mark else "#FFFFFF"
        draw.line([(round(x*unit), round(y*unit)) for x,y in path], fill=color,
                  width=round(stroke*unit), joint="curve")
        for x,y in path:
            circle(x,y,stroke/2,color)
    if not mark:
        circle(32,32,radius+2.5,"#FFFFFF")
    circle(32,32,radius,RED)
    image = image.resize((size,size), Image.Resampling.LANCZOS)
    # Remove near-transparent resampling halos outside the rounded silhouette.
    image.putalpha(image.getchannel("A").point(lambda a: 0 if a <= 2 else a))
    return image


def write_ico(images):
    # PNG-backed ICO entries preserve full alpha on the supported Windows versions.
    chunks = []
    for size, image in images.items():
        data = BytesIO()
        image.save(data, format="PNG")
        chunks.append((size,data.getvalue()))
    offset = 6 + 16 * len(chunks)
    directory = bytearray(struct.pack("<HHH",0,1,len(chunks)))
    payload = bytearray()
    for size,data in chunks:
        directory.extend(struct.pack("<BBBBHHII",size % 256,size % 256,0,0,1,32,len(data),offset))
        payload.extend(data)
        offset += len(data)
    (OUT / "FrameCam.ico").write_bytes(directory + payload)


def preview():
    sheet = Image.new("RGB", (1280,900), "#F1F5F9")
    d = ImageDraw.Draw(sheet)
    regular = Path("C:/Windows/Fonts/msjh.ttc")
    bold = Path("C:/Windows/Fonts/msjhbd.ttc")

    def label(text, x, y, size=18, color="#64748B", strong=False):
        d.text((x,y),text,font=ImageFont.truetype(str(bold if strong else regular),size),fill=color)

    def paste(size,x,y,mark=False):
        sheet.paste(icon(size,mark),(x,y),icon(size,mark))

    label("FrameCam",48,32,32,"#0F172A",True)
    label("應用程式圖示  /  擷取框 × 錄影圓點",48,79,18)
    d.rounded_rectangle((48,132,776,430),22,fill="white")
    d.rounded_rectangle((800,132,1232,430),22,fill="white")
    paste(208,82,174)
    label("EXE / Windows 工作列",330,192,23,"#0F172A",True)
    label("藍色圓角底，搭配白色擷取框",330,237)
    label("紅色錄影圓點，快速辨識工具用途",330,268)
    for x,c,name in ((330,BLUE,"#2563EB"),(474,RED,"#EF4444")):
        d.rounded_rectangle((x,325,x+22,347),6,fill=c)
        label(name,x+32,323,15)
    paste(132,946,158,True)
    label("工具列識別圖示",900,315,22,"#0F172A",True)
    label("透明背景 · 同一套造型",906,353,17)
    label("實際像素大小",48,461,22,"#0F172A",True)
    label("16 / 20 / 24 / 32 / 40 / 48 / 64 px",810,465,18)
    for top,fill,ink in ((512,"#FFFFFF","#64748B"),(658,"#172033","#CBD5E1")):
        d.rounded_rectangle((48,top,1232,top+126),18,fill=fill)
        label("淺色背景" if top==512 else "深色背景",70,top+49,17,ink)
        for i,size in enumerate(SIZES[:7]):
            cx=270+i*125
            paste(size,cx-size//2,top+46-size//2)
            label(f"{size} px",cx-23,top+88,14,ink)
    label("可編輯 SVG  ·  透明 PNG  ·  9 種解析度 Windows ICO",48,821,18,"#334155")
    label("小尺寸已加粗線條與圓點",48,853,15)
    sheet.save(OUT / "preview.png")


def main():
    OUT.mkdir(parents=True,exist_ok=True)
    (OUT / "FrameCam.svg").write_text(svg(),encoding="utf-8")
    (OUT / "FrameCam-mark.svg").write_text(svg(True),encoding="utf-8")
    images = {size:icon(size) for size in SIZES}
    for size,image in images.items():
        image.save(OUT / f"FrameCam-{size}.png")
        icon(size,True).save(OUT / f"FrameCam-mark-{size}.png")
    write_ico(images)
    preview()
    with Image.open(OUT / "FrameCam.ico") as saved:
        assert saved.ico.sizes() == {(s,s) for s in SIZES}
        for size in SIZES:
            frame = saved.ico.getimage((size,size)).convert("RGBA")
            assert frame.tobytes() == images[size].tobytes()
            assert frame.getpixel((0,0))[3] == 0
    print(f"Generated and verified {len(SIZES)} ICO sizes in {OUT}")


if __name__ == "__main__":
    main()
