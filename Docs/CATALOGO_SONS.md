# Catálogo de efeitos sonoros

Os 38 sons ficam em `Assets/_FaithClash/Resources/Sons` e são tocados pelo nome (`SomJogo.Tocar("Espada_Corte1")`).

**Como foram classificados:** não dá para "ouvir" automaticamente, então cada arquivo foi *medido* (`Ferramentas/Audio/analisar_sons.py`):
duração, tempo até o pico (ataque), brilho (agudo = corte/estalo, grave = explosão/impacto), quantos picos tem (passos, ecos) e onde o som realmente começa/termina.
O resultado está em `Ferramentas/Audio/analise_sons.json`. Junto com o nome do arquivo, isso definiu onde cada um serve.
Sons longos (explosões de 16–22 s, power-down de 8 s) são **cortados com fade-out** no momento de tocar.

| Nome no jogo | Arquivo original | Onde toca |
|---|---|---|
| Espada_Corte1 / Corte2 | sword-slice / sword-slice-2 | Cortes 1 e 2 do combo (sai 0,18 s antes do impacto) |
| Espada_CortePesado | violent-sword-slice | Golpe pesado, corte giratório |
| Espada_Estocada | knife-slice | Ataque correndo |
| Espada_Sacar | katana | Sacar a espada (G) |
| Espada_Impacto | sword-clashhit | Corte que acerta; Faísca Negra |
| Soco_Leve1/2/3 | punch / punch-02 / punch-04 | Socos 1, 2, 3 |
| Soco_Forte | power-punch | Golpe pesado do Poder; pouso rolando |
| Soco_Impacto | fist-fight | Soco que acerta |
| Defesa_Bloqueio | sfx32-blocked | Começar a defender (botão direito) |
| Fogo_Lancar | big-fire-blast-whoosh | Katon: Goukakyuu; Daitoppa (mais agudo) |
| Fogo_Impacto | fire-spell-impact | Explosão do Goukakyuu / Housenka |
| Fogo_Magia | fire-magic-5 | Housenka; frasco de fúria |
| Agua_Splash | water-splash | Suiton: Suiryuudan |
| Agua_Impacto | large-underwater-explosion (3,3 s) | Impacto do Suiryuudan |
| Agua_Explosao | large-underwater-explosion (6,6 s) | Suiton: Suirou |
| Terra_Doryuudan | explosion-36210 | Doton: Doryuudan |
| Terra_Muralha | explosion-sfx-43814 | Doton: Doryuuheki |
| Ar_Reppushou | power-down 8 s | Fuuton: Reppushou |
| Azul_Atrair | sci-fi circuits hum | Técnica Maldita: Azul |
| Roxo_Carga / Feixe / Explosão | tense-close-up / beam / boom | Roxo (R), em três tempos |
| Explosao_Elemental | explosion-2 | Água → Fogo → Terra → Ar |
| Poder_Ativar / Poder_Desativar | power-up / power-down | Trocar de modo (G); cristal de Fé |
| UI_Clique / UI_Elemento / UI_Travar / UI_Aviso | button-ui, ui-button-click, selectconfirm, new-notification | Trocar alvo, escolher estilo (1–4), travar alvo (Q), avisos e poção |
| Passos_Andar / Passos_Correr | pasos-steps / running-on-the-floor | Passos: o jogo corta cada pisada no tempo medido |
| Movimento_Vento | running-in-grass | Reserva (superfície de grama) |
| Ambiente_Treino / Ambiente_Drone | ambient-game / static-drone | Ambiente em loop (volume baixo) |
| Morte_Jogador | game-over | Quando a vida zera |
