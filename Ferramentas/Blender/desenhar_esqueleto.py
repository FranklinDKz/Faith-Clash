# Desenha o JSON de esqueleto_dump.py: linha de cima = vista lateral (frente do personagem para a esquerda), baixo = frontal.
# Uso: python desenhar_esqueleto.py <entrada.json> <saida.png>
import sys, json
from PIL import Image, ImageDraw
d = json.load(open(sys.argv[1]))
W, H, S = 200, 260, 120   # S = pixels por metro
n = len(d["frames"])
img = Image.new("RGB", (W * n, H * 2), (28, 30, 36)); dr = ImageDraw.Draw(img)
def cor(nome):
    if nome.startswith("Left"): return (90, 170, 255)
    if nome.startswith("Right"): return (255, 110, 90)
    return (235, 235, 235)
for i, fr in enumerate(d["frames"]):
    p = fr["p"]
    hips = p["Hips"]
    for linha in (0, 1):
        ox, oy = i * W + W // 2, linha * H + H - 28
        dr.line([(i * W + 6, oy), (i * W + W - 6, oy)], fill=(80, 80, 90))
        def proj(v):
            # frente do personagem = -Y do mundo; direita dele = -X
            if linha == 0: return (ox + (v[1] - hips[1]) * S, oy - v[2] * S)      # lateral: frente (-Y) vai para a esquerda
            return (ox + (v[0] - hips[0]) * S, oy - v[2] * S)                      # frontal: esquerda do personagem (+X) à direita da imagem
        for nome, pai in d["bones"].items():
            if pai is None: continue
            dr.line([proj(p[pai]), proj(p[nome])], fill=cor(nome), width=3)
        for nome in ("Head", "LeftHand", "RightHand", "LeftFoot", "RightFoot"):
            x, y = proj(p[nome]); dr.ellipse([x - 4, y - 4, x + 4, y + 4], outline=cor(nome))
    dr.text((i * W + 6, 4), str(fr["f"]), fill=(255, 255, 0))
img.save(sys.argv[2])
