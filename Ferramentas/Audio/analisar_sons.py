# Mede cada efeito sonoro (duracao, pico, volume, brilho, ataque, graves) para ajudar a classificar onde usar.
# Uso: python analisar_sons.py <pasta_com_mp3> <saida.json>
import sys, os, json
import numpy as np
import miniaudio

pasta, saida = sys.argv[1], sys.argv[2]
res = []
for nome in sorted(os.listdir(pasta)):
    if not nome.lower().endswith(".mp3"):
        continue
    d = miniaudio.decode_file(os.path.join(pasta, nome), nchannels=1, sample_rate=44100, output_format=miniaudio.SampleFormat.FLOAT32)
    x = np.array(d.samples, dtype=np.float32)
    sr = 44100
    dur = len(x) / sr
    env = np.abs(x)
    jan = int(sr * 0.02)
    rms = np.sqrt(np.convolve(x ** 2, np.ones(jan) / jan, mode="same"))
    pico = float(env.max())
    ipico = int(np.argmax(rms))
    # tempo ate o pico de energia (ataque) e quanto da energia esta no inicio
    ataque = ipico / sr
    ener = rms ** 2
    tot = ener.sum() + 1e-12
    n1 = int(min(len(x), sr * 0.5))
    ini = float(ener[:n1].sum() / tot)
    F = np.abs(np.fft.rfft(x[: sr * 4] if len(x) > sr * 4 else x))
    fr = np.fft.rfftfreq(len(x[: sr * 4] if len(x) > sr * 4 else x), 1 / sr)
    centro = float((F * fr).sum() / (F.sum() + 1e-9))
    graves = float(F[fr < 200].sum() / (F.sum() + 1e-9))
    # silencio no inicio/fim
    ativo = np.where(rms > 0.02 * rms.max())[0]
    ini_s = float(ativo[0] / sr) if len(ativo) else 0.0
    fim_s = float(ativo[-1] / sr) if len(ativo) else dur
    # repeticoes (picos separados) - cadencia de passos
    lim = 0.5 * rms.max()
    picos = []
    ult = -1
    for i in range(1, len(rms) - 1):
        if rms[i] > lim and rms[i] >= rms[i - 1] and rms[i] >= rms[i + 1] and (ult < 0 or i - ult > sr * 0.15):
            picos.append(round(i / sr, 2)); ult = i
    res.append({"arquivo": nome, "duracao": round(dur, 2), "pico": round(pico, 2), "ataque_s": round(ataque, 2),
                "energia_primeiros_0_5s": round(ini, 2), "brilho_hz": round(centro), "graves": round(graves, 2),
                "som_ativo": [round(ini_s, 2), round(fim_s, 2)], "picos": picos[:12]})
json.dump(res, open(saida, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
for r in res:
    print(f'{r["arquivo"][:58]:58} {r["duracao"]:5.1f}s pico={r["pico"]:.2f} atq={r["ataque_s"]:.2f} brilho={r["brilho_hz"]:5d} graves={r["graves"]:.2f} picos={len(r["picos"])}')
