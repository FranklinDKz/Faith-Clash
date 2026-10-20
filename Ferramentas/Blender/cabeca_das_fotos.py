# Gera a cabeca do protagonista a partir da folha de referencia (fotos frente/perfil/costas/3-4).
# Saida: <pasta>/cabeca.npz (vertices, faces, uvs) e <pasta>/Rosto.png (textura projetada das fotos).
# Uso: python cabeca_das_fotos.py <folha_referencia> <pasta_saida> [--debug]
import sys, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

FOLHA, SAIDA = sys.argv[1], sys.argv[2]
DEBUG = "--debug" in sys.argv
os.makedirs(SAIDA, exist_ok=True)

img = np.asarray(Image.open(FOLHA).convert("RGB")).astype(np.float32)

# paineis da linha superior da folha (x0, x1) e angulo de rotacao da cabeca em cada foto
Y0, Y1 = 64, 345
PAINEIS = {
    "frente": ((14, 251), 0.0),
    "tres_quartos_esq": ((256, 489), -45.0),
    "esquerda": ((494, 726), -90.0),
    "costas": ((731, 954), 180.0),
    "direita": ((959, 1192), 90.0),
    "tres_quartos_dir": ((1197, 1435), 45.0),
}


def mascara(p):
    (x0, x1), _ = PAINEIS[p]
    a = img[Y0:Y1, x0:x1]
    borda = np.concatenate([a[:, :6], a[:, -6:]], axis=1).mean(axis=1, keepdims=True)  # fundo por linha
    d = np.sqrt(((a - borda) ** 2).sum(axis=2))
    m = d > 30
    # limpa: abre/fecha com filtros do PIL
    mi = Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MedianFilter(5))
    m = np.asarray(mi) > 127
    return a, m


def extremos(m):
    H = m.shape[0]
    esq = np.full(H, np.nan)
    dirt = np.full(H, np.nan)
    for r in range(H):
        c = np.nonzero(m[r])[0]
        if len(c) > 3:
            esq[r], dirt[r] = c[0], c[-1]
    return esq, dirt


dados = {p: mascara(p) for p in PAINEIS}
ext = {p: extremos(dados[p][1]) for p in PAINEIS}

# ---------------------------------------------------------------- linhas-chave
def topo(p):
    e, _ = ext[p]
    return int(np.nonzero(~np.isnan(e))[0][0])


fe, fd = ext["frente"]
le, ld = ext["esquerda"]
t_f, t_l = topo("frente"), topo("esquerda")
H = Y1 - Y0

# queixo: no perfil (rosto para a esquerda), a borda frontal recua bruscamente abaixo do queixo
fr = le.copy()
lo, hi = t_l + int(0.62 * (H - t_l)), t_l + int(0.92 * (H - t_l))
deriv = np.diff(fr[lo:hi])
queixo = lo + int(np.nanargmax(deriv))
altura_px = queixo - t_l
ESCALA = 0.255 / altura_px          # metros por pixel (topo do cabelo ao queixo ~25,5 cm)
# fim do pescoco: largura minima na frente abaixo do queixo
larg = fd - fe
q_f = t_f + altura_px
pesc_lo, pesc_hi = q_f + 5, min(H - 2, q_f + int(0.35 * altura_px))
fim = pesc_lo + int(np.nanargmin(larg[pesc_lo:pesc_hi])) + 6
fim = min(fim, H - 2)
print("topo", t_f, t_l, "queixo", queixo, "fim", fim, "escala", ESCALA)

# ---------------------------------------------------------------- casco (visual hull aproximado)
NR = fim - t_f            # aneis (1 por pixel de altura da foto de frente)
NA = 96                   # pontos por anel
cx = np.nanmean(((fe + fd) / 2)[t_f + int(0.45 * altura_px): t_f + int(0.8 * altura_px)])

linhas_f = np.arange(t_f, fim)
linhas_l = linhas_f - t_f + t_l
xr = (fd[linhas_f] - cx) * ESCALA           # lado +X (esquerda do personagem)
xl = (fe[linhas_f] - cx) * ESCALA           # lado -X
pf = le[np.clip(linhas_l, 0, H - 1)]
pb = ld[np.clip(linhas_l, 0, H - 1)]


def suaviza(v, k):
    v = np.array(v, dtype=float)
    idx = np.arange(len(v))
    ok = ~np.isnan(v)
    v = np.interp(idx, idx[ok], v[ok])
    ker = np.ones(k) / k
    return np.convolve(np.pad(v, k // 2, mode="edge"), ker, mode="valid")[: len(v)]


xr, xl, pb = (suaviza(suaviza(v, 15), 9) for v in (xr, xl, pb))
pf = suaviza(pf, 3)
# centro de profundidade: meio do perfil na altura das orelhas
r_or = int(0.5 * altura_px)
cy_px = (pf[r_or] + pb[r_or]) / 2
yf_raw = (pf - cy_px) * ESCALA               # frente (negativo)
yb = (pb - cy_px) * ESCALA
# rosto sem nariz/labios: maximo deslizante (menos saliente) para a base; o resto vira relevo central
base = np.array([np.max(yf_raw[max(0, i - 9): i + 10]) for i in range(len(yf_raw))])
base = suaviza(suaviza(base, 15), 9)
relevo = suaviza(np.minimum(0, suaviza(yf_raw, 5) - base), 5)  # <= 0 (para frente)
z = -np.arange(NR) * ESCALA                  # 0 no topo

N_EXP = 2.15
r_olho = int(0.54 * altura_px)   # altura dos olhos (proporcao da foto de frente)
ang = np.linspace(0, 2 * np.pi, NA, endpoint=False)
P = np.zeros((NR, NA, 3))
for r in range(NR):
    for a_i, th in enumerate(ang):
        c, s = math.cos(th), math.sin(th)
        ax = xr[r] if c >= 0 else -xl[r]
        ay = -base[r] if s < 0 else yb[r]
        x = ax * math.copysign(abs(c) ** (2 / N_EXP), c)
        y = ay * math.copysign(abs(s) ** (2 / N_EXP), s)
        if s < 0:  # face frontal: aplica nariz/labios perto do centro
            y += relevo[r] * math.exp(-(x / 0.016) ** 2)
            # cavidade dos olhos
            dz = (r - r_olho) * ESCALA
            for ox in (-0.033, 0.033):
                y += 0.006 * math.exp(-((x - ox) / 0.014) ** 2 - (dz / 0.012) ** 2)
        P[r, a_i] = (x, y, z[r])
# fecha o topo: achata o primeiro anel para o centro
topo_c = P[0].mean(axis=0)
P[0] = topo_c + (P[0] - topo_c) * 0.15

def borra_linhas(G, k):
    """Media movel ao longo dos aneis (eixo 0), mantendo as bordas."""
    pad = np.pad(G, ((k // 2, k // 2), (0, 0), (0, 0)), mode="edge")
    out = np.zeros_like(G)
    for i in range(k):
        out += pad[i:i + G.shape[0]]
    return out / k


def borra_angulo(G, k):
    out = np.zeros_like(G)
    for i in range(-(k // 2), k // 2 + 1):
        out += np.roll(G, i, axis=1)
    return out / k


# geometria levemente suavizada (tira degraus das bordas dos cachos)
P[1:] = borra_linhas(P, 7)[1:]

# normais por diferencas finitas, calculadas numa versao bem suavizada (evita faixas na textura)
Ps = borra_angulo(borra_linhas(P, 21), 5)
dA = np.roll(Ps, -1, axis=1) - np.roll(Ps, 1, axis=1)
dR = np.zeros_like(Ps)
dR[1:-1] = Ps[2:] - Ps[:-2]
dR[0] = Ps[1] - Ps[0]
dR[-1] = Ps[-1] - Ps[-2]
Nrm = np.cross(dR, dA)
Nrm /= np.linalg.norm(Nrm, axis=2, keepdims=True) + 1e-9
# garante normais para fora (sinal unico para a malha toda)
cent = P.mean(axis=(0, 1))
if ((P - cent) * Nrm).sum() < 0:
    Nrm *= -1

# ---------------------------------------------------------------- alinhamento de cada foto ao casco
def projeta(p_xyz, graus):
    t = math.radians(graus)
    x = p_xyz[..., 0] * math.cos(t) - p_xyz[..., 1] * math.sin(t)
    return x


offs = {}
for p, ((x0, x1), g) in PAINEIS.items():
    e, d = ext[p]
    t_p = topo(p)
    xs = projeta(P, g) / ESCALA
    amostras = []
    for r in range(int(0.15 * NR), int(0.75 * NR), 3):
        lr = t_p + r
        if lr >= H or np.isnan(e[lr]):
            continue
        amostras.append(((e[lr] + d[lr]) / 2) - (xs[r].min() + xs[r].max()) / 2)
    offs[p] = (float(np.median(amostras)), t_p)
    print(p, "offset", offs[p])

# ---------------------------------------------------------------- textura (u = angulo, v = anel)
TW, TH = 2048, 2048


def amplia(a):
    im = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    im = im.resize((im.width * 2, im.height * 2), Image.LANCZOS)
    im = im.filter(ImageFilter.UnsharpMask(radius=2, percent=90, threshold=2))
    return np.asarray(im).astype(np.float32)


dados_hd = {p: amplia(dados[p][0]) for p in PAINEIS}
uu = (np.arange(TW) + 0.5) / TW * NA
vv = (np.arange(TH) + 0.5) / TH * (NR - 1)
U, V = np.meshgrid(uu, vv)
a0 = np.floor(U).astype(int) % NA
a1 = (a0 + 1) % NA
fa = (U - np.floor(U))[..., None]
r0 = np.floor(V).astype(int)
r1 = np.minimum(r0 + 1, NR - 1)
fr_ = (V - r0)[..., None]


def bil(G):
    return (G[r0, a0] * (1 - fa) * (1 - fr_) + G[r0, a1] * fa * (1 - fr_) +
            G[r1, a0] * (1 - fa) * fr_ + G[r1, a1] * fa * fr_)


PT, NT = bil(P), bil(Nrm)
acc = np.zeros((TH, TW, 3))
wsum = np.zeros((TH, TW, 1))
acc_m = np.zeros((TH, TW, 3))   # mistura suave, usada onde nenhuma foto ve bem (topo da cabeca)
wsum_m = np.zeros((TH, TW, 1))
for p, ((x0, x1), g) in PAINEIS.items():
    if p.startswith("tres_quartos"):
        continue  # as fotos 3/4 geram fantasmas (olhos duplicados); frente + perfis + costas bastam
    t = math.radians(g)
    ny = NT[..., 0] * math.sin(t) + NT[..., 1] * math.cos(t)   # normal girada, eixo para a camera (-Y)
    w = np.clip(-ny, 0, 1) ** 10
    if p == "frente":
        w *= 4.0
    off, t_p = offs[p]
    px = projeta(PT, g) / ESCALA + off
    py = -PT[..., 2] / ESCALA + t_p
    # amostra numa versao 2x mais nitida da foto
    foto = dados_hd[p]
    px, py = px * 2.0, py * 2.0
    hh, ww = foto.shape[:2]
    pxi = np.clip(px, 0, ww - 1.001)
    pyi = np.clip(py, 0, hh - 1.001)
    x0i, y0i = np.floor(pxi).astype(int), np.floor(pyi).astype(int)
    fx, fy = (pxi - x0i)[..., None], (pyi - y0i)[..., None]
    cor = (foto[y0i, x0i] * (1 - fx) * (1 - fy) + foto[y0i, x0i + 1] * fx * (1 - fy) +
           foto[y0i + 1, x0i] * (1 - fx) * fy + foto[y0i + 1, x0i + 1] * fx * fy)
    dentro = dados[p][1][np.clip(y0i // 2, 0, dados[p][1].shape[0] - 1), np.clip(x0i // 2, 0, dados[p][1].shape[1] - 1)][..., None]
    wm = (np.clip(-ny, 0, 1)[..., None] + 0.02) * dentro
    w = w[..., None] * dentro
    acc += cor * w
    wsum += w
    acc_m += cor * wm
    wsum_m += wm
tex = acc / np.maximum(wsum, 1e-6)
mix = acc_m / np.maximum(wsum_m, 1e-6)
peso = np.clip(wsum / 0.05, 0, 1)
tex = tex * peso + mix * (1 - peso)
Image.fromarray(np.clip(tex, 0, 255).astype(np.uint8)).save(os.path.join(SAIDA, "Rosto.png"))

# ---------------------------------------------------------------- malha (grade com costura em u)
# pescoco extra: prolonga o ultimo anel para baixo (entra na gola da jaqueta)
EXTRA = 12
base_anel = P[-1]
cent_anel = base_anel.mean(axis=0)
ext_aneis = []
for k in range(1, EXTRA + 1):
    anel = cent_anel + (base_anel - cent_anel) * (1 - 0.012 * k)
    anel[:, 2] = base_anel[:, 2] - 0.008 * k
    ext_aneis.append(anel)
P = np.concatenate([P, np.array(ext_aneis)], axis=0)
NT_TOTAL = NR + EXTRA

verts, uvs, faces = [], [], []
idx = {}
for r in range(NT_TOTAL):
    for a_i in range(NA + 1):
        idx[(r, a_i)] = len(verts)
        verts.append(P[r, a_i % NA])
        uvs.append((a_i / NA, max(0.002, 1 - r / (NR - 1))))
for r in range(NT_TOTAL - 1):
    for a_i in range(NA):
        faces.append((idx[(r, a_i)], idx[(r + 1, a_i)], idx[(r + 1, a_i + 1)], idx[(r, a_i + 1)]))
# tampa do topo
c_i = len(verts)
verts.append(topo_c)
uvs.append((0.5, 1.0))
tampa = [(c_i, idx[(0, a_i + 1)], idx[(0, a_i)]) for a_i in range(NA)]
np.savez(os.path.join(SAIDA, "cabeca.npz"), verts=np.array(verts), uvs=np.array(uvs),
         quads=np.array(faces), tris=np.array(tampa), queixo_z=-(altura_px) * ESCALA)
print("vertices", len(verts), "queixo_z", -(altura_px) * ESCALA, "altura total", NR * ESCALA)

if DEBUG:
    for p in PAINEIS:
        Image.fromarray((dados[p][1] * 255).astype(np.uint8)).save(os.path.join(SAIDA, f"mask_{p}.png"))
