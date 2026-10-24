# Junta as 6 poses de cada clipe em linhas de uma imagem. Uso: python montar_folha.py <saida.png> clipe1 clipe2 ...
import sys, os
from PIL import Image, ImageDraw
saida, clipes = sys.argv[1], sys.argv[2:]
pasta = os.path.join(os.path.dirname(os.path.abspath(__file__)), "previews")
W, H = 150, 195
folha = Image.new("RGB", (W * 6, H * len(clipes)), (30, 30, 30))
d = ImageDraw.Draw(folha)
for r, c in enumerate(clipes):
    for q in range(6):
        p = os.path.join(pasta, f"{c}_{q}.png")
        if os.path.exists(p):
            folha.paste(Image.open(p).convert("RGB").resize((W, H)), (q * W, r * H))
    d.rectangle([0, r * H, 230, r * H + 14], fill=(0, 0, 0))
    d.text((3, r * H + 1), c, fill=(255, 255, 0))
folha.save(saida)
