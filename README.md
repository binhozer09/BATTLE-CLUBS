# BATTLE CLUBS

Jogo multiplayer 3D de combate em equipes inspirado na cultura do futebol brasileiro.

## Visão do projeto
- Plataformas-alvo: Android e PC.
- Câmera: terceira pessoa.
- Partida: 5 equipes de 4 jogadores (20 jogadores).
- Combate principal: taco de beisebol; 4 acertos derrubam um adversário.
- Jogador derrubado pode ser reanimado por um companheiro.
- Uma equipe é eliminada quando os quatro integrantes são eliminados.
- A equipe vencedora recebe pontuação competitiva conforme as equipes eliminadas.
- Cross-play Android/PC planejado.

## Estrutura
- `Docs/`: visão do jogo, regras e planejamento técnico.
- `Unity/Assets/Scripts/`: código C# do cliente Unity.
- `Unity/Assets/ClubKits/`: materiais e configurações de uniformes autorizados.
- `Server/`: especificações e código futuro dos serviços multiplayer.

## Estado atual
O repositório contém scripts Unity de movimentação, câmera, combate, reanimação, HUD mobile e geração procedural da comunidade, além de documentação de integração e produção. A branch `production/3d-art-foundation` concentra a produção visual mais recente. Ainda não há build Android/PC validada, modelo humanoide final com rig/animações, servidor multiplayer operacional ou uniformes oficiais importados.

## Próximos marcos
1. Validar a cena e os sistemas locais já implementados no Unity Editor.
2. Integrar personagem humanoide final, rig e animações.
3. Integrar kits de equipe aos renderers do personagem.
4. Finalizar e otimizar o cenário da comunidade.
5. Implementar servidor autoritativo, salas e partidas online de 20 jogadores.
6. Implementar lobby, matchmaking e cross-play Android/PC.
7. Validar desempenho, estabilidade e builds finais em Android e PC.

## Requisitos
- Unity LTS compatível com Android e Windows.
- Android Build Support (SDK, NDK e OpenJDK).
- Git LFS recomendado para arquivos grandes (modelos, texturas e áudio).

## Licenciamento
Escudos, marcas, uniformes oficiais e patrocínios de clubes podem exigir autorização dos respectivos titulares. Até que haja autorização, usar materiais provisórios originais.
