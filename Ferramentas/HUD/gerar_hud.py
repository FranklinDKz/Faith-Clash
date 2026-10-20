# Gera as imagens do HUD (molduras, barras, icones).
# Os icones de elementos/itens/arma sao recortados das telas de referencia do HUD.
# Uso: python gerar_hud.py <tela_referencia.webp> <retrato.png> <pasta_Resources_HUD>
import sys, os, math, random
from PIL import Image, ImageDraw, ImageFilter, ImageChops

REF, RETRATO, OUT = sys.argv[1:4]
os.makedirs(OUT, exist_ok=True)
random.seed(3)

OURO = (214, 178, 108, 255)
OURO_CLARO = (246, 222, 160, 255)
OURO_ESC = (140, 108, 58, 255)
S = 4  # supersampling


def salvar(img, nome, pasta=None):
    img.save(os.path.join(pasta or OUT, nome + ".png"))


def reduz(img, w, h):
    return img.resize((w, h), Image.LANCZOS)


def losango(d, cx, cy, r, cor, contorno=None, w=1):
    pts = [(cx, cy - r), (cx + r * 0.7, cy), (cx, cy + r), (cx - r * 0.7, cy)]
    d.polygon(pts, fill=cor, outline=contorno, width=w)


def estrela(d, cx, cy, r, cor, pontas=4, fino=0.22):
    pts = []
    for i in range(pontas * 2):
        a = math.pi * i / pontas - math.pi / 2
        rr = r if i % 2 == 0 else r * fino
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    d.polygon(pts, fill=cor)


# ---------------------------------------------------------------- moldura circular (retrato, minimapa, arma)
def moldura_circular(tam, espinhos=True, nome="moldura_circular"):
    T = tam * S
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = T / 2
    r = T * 0.40
    d.ellipse([c - r, c - r, c + r, c + r], outline=OURO, width=int(5 * S * tam / 256))
    r2 = r * 0.93
    d.ellipse([c - r2, c - r2, c + r2, c + r2], outline=OURO_ESC, width=int(2 * S * tam / 256))
    r3 = r * 1.06
    d.ellipse([c - r3, c - r3, c + r3, c + r3], outline=OURO_ESC, width=int(2 * S * tam / 256))
    # filigrana: pequenos arcos e pontos
    for i in range(48):
        a = 2 * math.pi * i / 48
        x, y = c + r * 1.0 * math.cos(a), c + r * 1.0 * math.sin(a)
        if i % 4 == 0:
            d.ellipse([x - 3 * S, y - 3 * S, x + 3 * S, y + 3 * S], fill=OURO_CLARO)
    if espinhos:
        for i in range(4):
            a = math.pi / 2 * i - math.pi / 2
            x, y = c + r * 1.12 * math.cos(a), c + r * 1.12 * math.sin(a)
            estrela(d, x, y, T * 0.04, OURO_CLARO, 4, 0.25)
            losango(d, c + r * 1.3 * math.cos(a), c + r * 1.3 * math.sin(a), T * 0.025, OURO)
        for i in range(4):
            a = math.pi / 2 * i + math.pi / 4
            x, y = c + r * 1.05 * math.cos(a), c + r * 1.05 * math.sin(a)
            losango(d, x, y, T * 0.03, OURO)
    salvar(reduz(im, tam, tam), nome)


def mascara_circulo(tam=256):
    T = tam * S
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    ImageDraw.Draw(im).ellipse([0, 0, T - 1, T - 1], fill=(255, 255, 255, 255))
    salvar(reduz(im, tam, tam), "circulo")


# ---------------------------------------------------------------- painel (9-slice)
def painel():
    W, H = 128 * S, 128 * S
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([6 * S, 6 * S, W - 6 * S, H - 6 * S], fill=(14, 12, 10, 215))
    d.rectangle([6 * S, 6 * S, W - 6 * S, H - 6 * S], outline=OURO_ESC, width=2 * S)
    d.rectangle([11 * S, 11 * S, W - 11 * S, H - 11 * S], outline=(120, 96, 56, 160), width=1 * S)
    for (x, y) in [(6, 6), (122, 6), (6, 122), (122, 122)]:
        losango(d, x * S, y * S, 6 * S, OURO_CLARO)
    salvar(reduz(im, 128, 128), "painel")


def barra_fundo():
    W, H = 256 * S, 32 * S
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([10 * S, 7 * S, W - 14 * S, H - 7 * S], fill=(8, 8, 8, 225), outline=OURO_ESC, width=2 * S)
    # ponteira em seta na direita
    d.polygon([(W - 16 * S, 4 * S), (W - 2 * S, H / 2), (W - 16 * S, H - 4 * S), (W - 11 * S, H / 2)], fill=OURO)
    losango(d, 9 * S, H / 2, 7 * S, OURO)
    salvar(reduz(im, 256, 32), "barra_fundo")


def barra_preenchimento():
    W, H = 64, 16
    im = Image.new("RGBA", (W, H))
    for y in range(H):
        t = y / (H - 1)
        v = int(255 * (1.0 - 0.45 * abs(t - 0.35)))
        for x in range(W):
            im.putpixel((x, y), (v, v, v, 255))
    salvar(im, "barra_preenchimento")


def linha_ornamental():
    W, H = 512 * S, 24 * S
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.line([(20 * S, H / 2), (W - 20 * S, H / 2)], fill=OURO_ESC, width=2 * S)
    estrela(d, W / 2, H / 2, 11 * S, OURO_CLARO, 4, 0.25)
    losango(d, 14 * S, H / 2, 6 * S, OURO)
    losango(d, W - 14 * S, H / 2, 6 * S, OURO)
    salvar(reduz(im, 512, 24), "linha_ornamental")


def tecla():
    T = 40 * S
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([3 * S, 3 * S, T - 3 * S, T - 3 * S], fill=(12, 12, 12, 220), outline=(170, 150, 120, 255), width=2 * S)
    salvar(reduz(im, 40, 40), "tecla")


def slot_item():
    T = 72 * S
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle([3 * S, 3 * S, T - 3 * S, T - 3 * S], fill=(10, 10, 10, 170), outline=(110, 100, 86, 255), width=2 * S)
    salvar(reduz(im, 72, 72), "slot")


def pip(cheio, nome):
    T = 24 * S
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    losango(d, T / 2, T / 2, 10 * S, OURO_CLARO if cheio else (0, 0, 0, 0), OURO_CLARO, 2 * S)
    salvar(reduz(im, 24, 24), nome)


def brilho():
    T = 128
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    for y in range(T):
        for x in range(T):
            r = math.hypot(x - T / 2, y - T / 2) / (T / 2)
            a = max(0.0, 1 - r) ** 2
            im.putpixel((x, y), (255, 255, 255, int(255 * a)))
    salvar(im, "brilho")


def seta_jogador():
    T = 64 * S
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon([(T / 2, 6 * S), (T - 16 * S, T - 10 * S), (T / 2, T - 22 * S), (16 * S, T - 10 * S)],
              fill=(238, 230, 210, 255), outline=(60, 50, 30, 255))
    salvar(reduz(im, 64, 64), "seta_jogador")


def marcador_inimigo():
    T = 48 * S
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = T / 2
    g = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse([4 * S, 4 * S, T - 4 * S, T - 4 * S], fill=(255, 40, 40, 120))
    g = g.filter(ImageFilter.GaussianBlur(5 * S))
    im.alpha_composite(g)
    # caveira simples
    d.ellipse([c - 11 * S, c - 13 * S, c + 11 * S, c + 7 * S], fill=(220, 30, 30, 255))
    d.rectangle([c - 7 * S, c + 2 * S, c + 7 * S, c + 12 * S], fill=(220, 30, 30, 255))
    d.ellipse([c - 7 * S, c - 6 * S, c - 1 * S, c + 1 * S], fill=(30, 0, 0, 255))
    d.ellipse([c + 1 * S, c - 6 * S, c + 7 * S, c + 1 * S], fill=(30, 0, 0, 255))
    salvar(reduz(im, 48, 48), "marcador_inimigo")


def icone_circulo_pequeno(nome, desenho):
    T = 48 * S
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    desenho(d, T)
    salvar(reduz(im, 48, 48), nome)


def chama(d, T):
    c = T / 2
    d.polygon([(c, 4 * S), (c + 14 * S, 26 * S), (c + 10 * S, 42 * S), (c - 10 * S, 42 * S), (c - 14 * S, 26 * S)],
              fill=(255, 150, 60, 255))
    d.polygon([(c, 18 * S), (c + 7 * S, 32 * S), (c, 42 * S), (c - 7 * S, 32 * S)], fill=(255, 235, 170, 255))


def escudo(d, T):
    c = T / 2
    d.polygon([(c - 15 * S, 8 * S), (c + 15 * S, 8 * S), (c + 15 * S, 24 * S), (c, 42 * S), (c - 15 * S, 24 * S)],
              fill=(200, 200, 205, 255), outline=(90, 90, 95, 255))
    d.line([(c, 10 * S), (c, 38 * S)], fill=(90, 90, 95, 255), width=3 * S)


def sol(d, T):
    c = T / 2
    for i in range(12):
        a = 2 * math.pi * i / 12
        d.line([(c + 12 * S * math.cos(a), c + 12 * S * math.sin(a)), (c + 21 * S * math.cos(a), c + 21 * S * math.sin(a))],
               fill=OURO_CLARO, width=3 * S)
    d.ellipse([c - 9 * S, c - 9 * S, c + 9 * S, c + 9 * S], fill=OURO_CLARO)


def objetivo(d, T):
    c = T / 2
    losango(d, c, c, 18 * S, (0, 0, 0, 0), OURO_CLARO, 3 * S)
    losango(d, c, c, 9 * S, (0, 0, 0, 0), OURO_CLARO, 2 * S)


def mira_alvo():
    T = 96 * S
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = T / 2
    r = T * 0.3
    d.ellipse([c - r, c - r, c + r, c + r], outline=OURO_CLARO, width=3 * S)
    for i in range(4):
        a = math.pi / 2 * i + math.pi / 4
        x, y = c + r * 1.35 * math.cos(a), c + r * 1.35 * math.sin(a)
        losango(d, x, y, 7 * S, OURO_CLARO)
    d.ellipse([c - 5 * S, c - 5 * S, c + 5 * S, c + 5 * S], fill=(255, 90, 60, 255))
    salvar(reduz(im, 96, 96), "mira_alvo")


# ---------------------------------------------------------------- recortes da tela de referencia
ref = Image.open(REF).convert("RGBA")


def recorte_circular(cx, cy, r, nome, tam=128, borda=0.97):
    c = ref.crop((cx - r, cy - r, cx + r, cy + r)).resize((tam, tam), Image.LANCZOS)
    m = Image.new("L", (tam * S, tam * S), 0)
    ImageDraw.Draw(m).ellipse([0, 0, tam * S * borda, tam * S * borda], fill=255)
    m = m.resize((tam, tam), Image.LANCZOS)
    c.putalpha(m)
    salvar(c, nome)


def recorte_quadrado(cx, cy, r, nome, tam=96):
    c = ref.crop((cx - r, cy - r, cx + r, cy + r)).resize((tam, tam), Image.LANCZOS)
    salvar(c, nome)


for nome, x in (("elemento_fogo", 655), ("elemento_agua", 782), ("elemento_terra", 902), ("elemento_ar", 1023)):
    recorte_circular(x, 734, 33, nome)
for nome, x in (("item_pocao", 1328), ("item_cristal", 1418), ("item_lanterna", 1506), ("item_frasco", 1592)):
    recorte_quadrado(x - 4, 808, 21, nome)
recorte_circular(118, 778, 64, "arma_espada", 192)


# moldura das orbes de elemento
def moldura_orbe(tam=160):
    T = tam * S
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = T / 2
    r = T * 0.36
    d.ellipse([c - r, c - r, c + r, c + r], outline=(40, 34, 28, 255), width=7 * S)
    d.ellipse([c - r * 1.04, c - r * 1.04, c + r * 1.04, c + r * 1.04], outline=OURO_ESC, width=2 * S)
    for i in range(8):
        a = math.pi / 4 * i - math.pi / 2
        rr = 1.18 if i % 2 == 0 else 1.1
        estrela(d, c + r * rr * math.cos(a), c + r * rr * math.sin(a), T * (0.06 if i % 2 == 0 else 0.035), OURO_ESC, 4, 0.3)
    salvar(reduz(im, tam, tam), "moldura_orbe")


# ---------------------------------------------------------------- retrato recortado em circulo
def retrato():
    im = Image.open(RETRATO).convert("RGBA")
    w, h = im.size
    box = (int(w * 0.17), int(h * 0.13), int(w * 0.83), int(h * 0.79))
    c = im.crop(box).resize((256, 256), Image.LANCZOS)
    fundo = Image.new("RGBA", (256, 256), (24, 22, 26, 255))
    fundo.alpha_composite(c)
    m = Image.new("L", (256 * S, 256 * S), 0)
    ImageDraw.Draw(m).ellipse([0, 0, 256 * S - 1, 256 * S - 1], fill=255)
    fundo.putalpha(m.resize((256, 256), Image.LANCZOS))
    salvar(fundo, "retrato")


moldura_circular(256)
mira_alvo()
moldura_circular(256, False, "anel_simples")
mascara_circulo()
painel()
barra_fundo()
barra_preenchimento()
linha_ornamental()
tecla()
slot_item()
pip(True, "pip_cheio")
pip(False, "pip_vazio")
brilho()
seta_jogador()
marcador_inimigo()
moldura_orbe()
icone_circulo_pequeno("icone_chama", chama)
icone_circulo_pequeno("icone_escudo", escudo)
icone_circulo_pequeno("icone_sol", sol)
icone_circulo_pequeno("icone_objetivo", objetivo)
retrato()
print("ok")
