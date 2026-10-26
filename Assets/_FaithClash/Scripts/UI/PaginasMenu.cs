using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FaithClash.Combat;
using FaithClash.Core;
using FaithClash.Player;

namespace FaithClash.UI
{
    /// <summary>Páginas de opções e de personalização compartilhadas pelo menu principal (OPÇÕES / COFRE) e pelo menu de pausa. Cada uma preenche uma lista rolável.</summary>
    public static class PaginasMenu
    {
        public static readonly Color[] Peles =
        {
            new Color(0.97f, 0.82f, 0.72f), new Color(0.93f, 0.74f, 0.6f), new Color(0.85f, 0.64f, 0.46f), new Color(0.73f, 0.51f, 0.35f),
            new Color(0.58f, 0.4f, 0.27f), new Color(0.43f, 0.29f, 0.2f), new Color(0.3f, 0.2f, 0.15f), new Color(0.8f, 0.86f, 0.92f),
        };

        public static void Graficos(Transform c, List<Action> atualizadores)
        {
            var d = PerfilJogo.D;
            Action mudou = PerfilJogo.Alterado;

            UIKit.Titulo(c, "Gráficos", UIKit.Ciano);
            UIKit.Opcoes(c, "Qualidade", ConfigRuntime.NomesQualidade, () => d.qualidade, v => { d.qualidade = v; mudou(); }, atualizadores, "Sombras, suavização e distância de detalhes");
            var resolucoes = ConfigRuntime.ResolucoesDisponiveis();
            UIKit.Opcoes(c, "Resolução", resolucoes, () => ConfigRuntime.IndiceResolucao(resolucoes), v => { ConfigRuntime.DefinirResolucao(resolucoes[v]); }, atualizadores);
            UIKit.Alternador(c, "Tela cheia", () => d.telaCheia, v => { d.telaCheia = v; mudou(); }, atualizadores);
            UIKit.Alternador(c, "VSync", () => d.vsync, v => { d.vsync = v; mudou(); }, atualizadores);
            UIKit.Opcoes(c, "Limite de FPS", ConfigRuntime.NomesFps, () => ConfigRuntime.IndiceFps(d.limiteFps), v => { d.limiteFps = ConfigRuntime.ValoresFps[v]; mudou(); }, atualizadores);
            UIKit.Deslizante(c, "Escala de renderização", 0.5f, 1.5f, () => d.escalaRender, v => { d.escalaRender = v; mudou(); }, atualizadores, "0%", "Menor = mais FPS, maior = mais nítido");
            UIKit.Alternador(c, "Sombras", () => d.sombras, v => { d.sombras = v; mudou(); }, atualizadores);
            UIKit.Alternador(c, "Pós-processamento", () => d.posProcessamento, v => { d.posProcessamento = v; mudou(); }, atualizadores, "Brilho (bloom), cores, vinheta");
            UIKit.Alternador(c, "Neblina", () => d.neblina, v => { d.neblina = v; mudou(); }, atualizadores);
            UIKit.Deslizante(c, "Brilho", 0.5f, 1.5f, () => d.brilho, v => { d.brilho = v; mudou(); }, atualizadores);
            UIKit.Deslizante(c, "Campo de visão", 50f, 95f, () => d.fov, v => { d.fov = Mathf.Round(v); mudou(); }, atualizadores, "0");
        }

        public static void Jogabilidade(Transform c, List<Action> atualizadores)
        {
            var d = PerfilJogo.D;
            Action mudou = PerfilJogo.Alterado;

            UIKit.Titulo(c, "Jogabilidade", UIKit.Ouro);
            UIKit.Alternador(c, "Parkour automático", () => d.parkourAutomatico, v => { d.parkourAutomatico = v; mudou(); }, atualizadores, "Pula obstáculos correndo, sem apertar SPACE");
            UIKit.Alternador(c, "Deslize (slide)", () => d.deslize, v => { d.deslize = v; mudou(); }, atualizadores);
            UIKit.Alternador(c, "Descer degraus sozinho", () => d.descidaAutomatica, v => { d.descidaAutomatica = v; mudou(); }, atualizadores);
            UIKit.Alternador(c, "Assistência de combate", () => d.assistenciaCombate, v => { d.assistenciaCombate = v; mudou(); }, atualizadores, "Segue o inimigo durante os combos");
            UIKit.Alternador(c, "Mira assistida", () => d.miraAssistida, v => { d.miraAssistida = v; mudou(); }, atualizadores, "Vira para o inimigo mais próximo ao atacar");
            UIKit.Alternador(c, "Dicas na tela", () => d.dicasNaTela, v => { d.dicasNaTela = v; mudou(); }, atualizadores);
            UIKit.Deslizante(c, "Velocidade do jogo", 0.5f, 1.5f, () => d.velocidadeJogo, v => { d.velocidadeJogo = Mathf.Round(v * 20f) / 20f; mudou(); }, atualizadores, "0.00x");
        }

        public static void Camera(Transform c, List<Action> atualizadores)
        {
            var d = PerfilJogo.D;
            Action mudou = PerfilJogo.Alterado;

            UIKit.Titulo(c, "Câmera", UIKit.Magenta);
            UIKit.Deslizante(c, "Sensibilidade do mouse", 0.3f, 2.5f, () => d.sensibilidade, v => { d.sensibilidade = v; mudou(); }, atualizadores);
            UIKit.Deslizante(c, "Tremor de câmera", 0f, 2f, () => d.tremorCamera, v => { d.tremorCamera = v; mudou(); }, atualizadores, "0.00", "Golpes, quedas e explosões");
            UIKit.Deslizante(c, "Efeitos de velocidade", 0f, 2f, () => d.efeitoVelocidade, v => { d.efeitoVelocidade = v; mudou(); }, atualizadores, "0.00", "Linhas de velocidade, FOV e inclinação");
        }

        public static void Audio(Transform c, List<Action> atualizadores)
        {
            var d = PerfilJogo.D;
            Action mudou = PerfilJogo.Alterado;

            UIKit.Titulo(c, "Áudio", UIKit.Verde);
            UIKit.Deslizante(c, "Volume geral", 0f, 1.5f, () => d.volMestre, v => { d.volMestre = v; mudou(); }, atualizadores, "0%", null, UIKit.Verde);
            UIKit.Deslizante(c, "Música", 0f, 1.5f, () => d.volMusica, v => { d.volMusica = v; mudou(); }, atualizadores, "0%", null, UIKit.Verde);
            UIKit.Deslizante(c, "Efeitos", 0f, 1.5f, () => d.volEfeitos, v => { d.volEfeitos = v; mudou(); }, atualizadores, "0%", null, UIKit.Verde);
            UIKit.Deslizante(c, "Graves (impacto)", 0f, 1.5f, () => d.volGraves, v => { d.volGraves = v; mudou(); }, atualizadores, "0%", "Reforça o grave de golpes, quedas e poderes", UIKit.Verde);
            UIKit.Deslizante(c, "Passos", 0f, 1.5f, () => d.volPassos, v => { d.volPassos = v; mudou(); }, atualizadores, "0%", null, UIKit.Verde);
            UIKit.Deslizante(c, "Combate", 0f, 1.5f, () => d.volCombate, v => { d.volCombate = v; mudou(); }, atualizadores, "0%", null, UIKit.Verde);
            UIKit.Deslizante(c, "Poderes", 0f, 1.5f, () => d.volPoderes, v => { d.volPoderes = v; mudou(); }, atualizadores, "0%", null, UIKit.Verde);
            UIKit.Deslizante(c, "Ambiente", 0f, 1.5f, () => d.volAmbiente, v => { d.volAmbiente = v; mudou(); }, atualizadores, "0%", null, UIKit.Verde);
            UIKit.Deslizante(c, "Interface", 0f, 1.5f, () => d.volInterface, v => { d.volInterface = v; mudou(); }, atualizadores, "0%", null, UIKit.Verde);
        }

        public static void Controles(Transform c, List<Action> atualizadores)
        {
            UIKit.Titulo(c, "Controles", UIKit.Ouro);
            string[,] teclas =
            {
                { "W A S D", "Mover" }, { "Shift", "Correr" }, { "Alt", "Andar devagar" }, { "SPACE", "Pular / transpor / escalar / deslizar por baixo" }, { "Ctrl", "Rolar / deslizar correndo" },
                { "C", "Dash" }, { "Mouse esq.", "Golpe leve (segurar = pesado)" }, { "Q", "Travar alvo" }, { "F", "Habilidade do poder" },
                { "R", "Roxo (carga espiritual)" }, { "Scroll", "Aproxima / volta a câmera" }, { "1", "Sem poder (taijutsu)" }, { "2 – 5", "Poderes equipados" }, { "G", "Katana ⇄ poder" }, { "H", "Katana: uma ou duas mãos" },
                { "E (segurar)", "Concentrar Fé" }, { "Z X B V", "Itens" }, { "I / P / Esc", "Poderes / Personagem / Pausa" }, { "Tab", "Alterna as dicas" }, { "'", "Mostra/esconde o mouse" },
            };
            for (int i = 0; i < teclas.GetLength(0); i++)
            {
                var l = UIKit.NovaLinha(c, teclas[i, 0], 44f);
                l.rotulo.color = UIKit.Ouro; l.rotulo.fontStyle = FontStyle.Bold; l.rotulo.fontSize = 22;
                var t = UIKit.Rotulo(l.controle, teclas[i, 1], 21, UIKit.Texto, TextAnchor.MiddleLeft);
                UIKit.Esticar(t.rectTransform);
            }
        }

        public static void Estilos(Transform c, List<Action> atualizadores)
        {
            var d = PerfilJogo.D;
            Action mudou = PerfilJogo.Alterado;

            UIKit.Titulo(c, "Estilos prontos", UIKit.Magenta);
            var estilos = new (string nome, int pele, int cab, int roupa, int acento, int calca, int calcado)[]
            {
                ("Ninja", d.tomPele, 0, 0, 3, 0, 0), ("Samurai", d.tomPele, 0, 3, 7, 1, 1), ("Monge", d.tomPele, 1, 9, 8, 8, 8), ("Sombra", d.tomPele, 0, 1, 6, 0, 1), ("Dragão", d.tomPele, 2, 4, 7, 4, 1),
            };
            var linhaPre = UIKit.NovaLinha(c, "Combinações", 64f);
            for (int i = 0; i < estilos.Length; i++)
            {
                var e = estilos[i];
                var b = UIKit.Botao(linhaPre.controle, e.nome, UIKit.CartaoClaro, () =>
                {
                    d.corRoupa = e.roupa; d.corAcento = e.acento; d.corCalca = e.calca; d.corCalcado = e.calcado; if (e.cab >= 0) d.estiloCabelo = e.cab; mudou();
                }, 19);
                float w = 1f / estilos.Length;
                UIKit.Ancorar(b.GetComponent<RectTransform>(), new Vector2(i * w, 0), new Vector2((i + 1) * w, 1), new Vector2(i == 0 ? 0 : 3, 4), new Vector2(i == estilos.Length - 1 ? 0 : -3, -4));
            }
        }

        public static void Corpo(Transform c, List<Action> atualizadores)
        {
            var d = PerfilJogo.D;
            Action mudou = PerfilJogo.Alterado;

            UIKit.Titulo(c, "Corpo", UIKit.Ciano);
            UIKit.Amostras(c, "Tom de pele", Peles, () => d.tomPele, v => { d.tomPele = v; mudou(); }, atualizadores);
            UIKit.Opcoes(c, "Cabelo", Personalizacao.NomesCabelo, () => d.estiloCabelo, v => { d.estiloCabelo = v; mudou(); }, atualizadores);
            UIKit.Amostras(c, "Cor do cabelo", Personalizacao.CoresCabelo, () => d.corCabelo, v => { d.corCabelo = v; mudou(); }, atualizadores);
            UIKit.Alternador(c, "Barba", () => d.barba, v => { d.barba = v; mudou(); }, atualizadores);
        }

        public static void Roupas(Transform c, List<Action> atualizadores)
        {
            var d = PerfilJogo.D;
            Action mudou = PerfilJogo.Alterado;

            UIKit.Titulo(c, "Roupas (peça por peça)", UIKit.Ouro);
            UIKit.Amostras(c, "Roupa principal", Personalizacao.CoresRoupa, () => d.corRoupa, v => { d.corRoupa = v; mudou(); }, atualizadores);
            UIKit.Amostras(c, "Detalhes / faixas", Personalizacao.CoresRoupa, () => d.corAcento, v => { d.corAcento = v; mudou(); }, atualizadores);
            UIKit.Amostras(c, "Calça", Personalizacao.CoresRoupa, () => d.corCalca, v => { d.corCalca = v; mudou(); }, atualizadores);
            UIKit.Amostras(c, "Calçado", Personalizacao.CoresRoupa, () => d.corCalcado, v => { d.corCalcado = v; mudou(); }, atualizadores);
            UIKit.Alternador(c, "Capuz", () => d.capuz, v => { d.capuz = v; mudou(); }, atualizadores);
            UIKit.Alternador(c, "Luvas", () => d.luvas, v => { d.luvas = v; mudou(); }, atualizadores);
            UIKit.Alternador(c, "Acessórios", () => d.acessorios, v => { d.acessorios = v; mudou(); }, atualizadores);
        }

        public static void Katana(Transform c, List<Action> atualizadores)
        {
            var d = PerfilJogo.D;
            Action mudou = PerfilJogo.Alterado;

            UIKit.Titulo(c, "Katana", UIKit.Verde);
            UIKit.Opcoes(c, "Tipo de katana", Personalizacao.NomesKatana, () => d.katana, v => { d.katana = v; mudou(); }, atualizadores);
            UIKit.Alternador(c, "Duas katanas", () => d.duasKatanas, v => { d.duasKatanas = v; mudou(); }, atualizadores, "Duas lâminas no estilo uma mão (tecla H)");
        }

    }
}
