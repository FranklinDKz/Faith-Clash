# Junta as fotos do teste de combate (Ferramentas/analise/fotos/<golpe>_NN.png) em uma folha por golpe: <golpe>_folha.png
import glob, os, re, sys
from PIL import Image, ImageDraw
pasta = os.path.join(os.path.dirname(os.path.abspath(__file__)), "fotos")
nomes = sorted({re.sub(r"_\d+\.png$", "", os.path.basename(p)) for p in glob.glob(os.path.join(pasta, "*_[0-9][0-9].png"))})
W, H, COLS = 220, 280, 8
for n in nomes:
    fs = sorted(glob.glob(os.path.join(pasta, n + "_[0-9][0-9].png")))
    rows = (len(fs) + COLS - 1) // COLS
    folha = Image.new("RGB", (W * COLS, H * rows), (20, 20, 24)); d = ImageDraw.Draw(folha)
    for i, p in enumerate(fs):
        folha.paste(Image.open(p).convert("RGB"), ((i % COLS) * W, (i // COLS) * H))
        d.text(((i % COLS) * W + 4, (i // COLS) * H + 3), f"{n} {i}", fill=(255, 255, 0))
    folha.save(os.path.join(pasta, n + "_folha.png"))
print(len(nomes), "folhas")
