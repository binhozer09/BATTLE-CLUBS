# BATTLE CLUBS — Documento de design

## Formato da partida
- 20 jogadores por partida.
- 5 equipes com 4 jogadores cada.
- Perspectiva em terceira pessoa.
- Objetivo: sobreviver e eliminar as equipes adversárias.

## Combate
- Arma inicial principal: taco de beisebol.
- Quatro acertos válidos derrubam o jogador.
- O jogador derrubado entra em estado de reanimação, não é eliminado imediatamente.
- Um aliado pode reanimá-lo dentro das condições definidas pela partida.
- A equipe só é eliminada quando todos os quatro integrantes forem eliminados.

## Pontuação
- A equipe vencedora recebe 3 pontos por equipe adversária eliminada.
- A regra exata de atribuição dos pontos por eliminação será validada no protótipo competitivo.

## Plataformas e controles
### Android
HUD adaptável com joystick virtual, ataque, corrida, pulo, agachar, interação e reanimação contextual. Elementos devem permitir ajuste de posição, tamanho e transparência.

### PC
Teclado e mouse, com remapeamento planejado.

## Identidade visual
- Cenário inicial: comunidade/favela brasileira fictícia, com vielas, escadarias, casas, comércio, quadra e pontos de referência urbanos.
- Uniformes de clubes: sistema de kits por equipe, com variações casa/fora.
- Usar escudos, nomes, marcas e patrocínios oficiais somente após confirmar as licenças. Até lá, kits genéricos de teste.

## Requisitos técnicos
- Cliente Unity 3D.
- Arquitetura de rede autoritativa no servidor para validar dano, estado de jogador, reanimação e resultado.
- Cross-play Android/PC.
- Alvo inicial de 20 jogadores por partida; desempenho e estabilidade devem ser medidos em testes reais.

## Fora do protótipo inicial
Loja, passe de temporada, cosméticos pagos, chat de voz e torneios ficam para fases posteriores.
