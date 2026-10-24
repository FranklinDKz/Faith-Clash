# Junta os quadros <prefixo>_NN_<frame>.png em uma folha. Uso: python montar_quadros.py <prefixo> [colunas]
import sys, glob, os, re
from PIL import Image, ImageDraw
pref = sys.argv[1]; cols = int(sys.argv[2]) if len(sys.argv) > 2 else 6
fs = sorted(glob.glob(pref + "_*_*.png"))
W, H = 170, 220
rows = (len(fs) + cols - 1) // cols
folha = Image.new("RGB", (W * cols, H * rows), (30, 30, 30)); d = ImageDraw.Draw(folha)
for i, p in enumerate(fs):
    fr = re.search(r"_(\d+)\.png$", p).group(1)
    folha.paste(Image.open(p).convert("RGB"), ((i % cols) * W, (i // cols) * H))
    d.rectangle([(i % cols) * W, (i // cols) * H, (i % cols) * W + 40, (i // cols) * H + 12], fill=(0, 0, 0))
    d.text(((i % cols) * W + 3, (i // cols) * H + 1), fr, fill=(255, 255, 0))
    os.remove(p)
folha.save(pref + "_folha.png")
