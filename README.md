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
Este repositório começa com documentação e estrutura-base. Ainda não contém uma build Android/PC, modelos 3D finais, servidor multiplayer operacional ou uniformes oficiais importados.

## Próximos marcos
1. Criar projeto Unity e cena de protótipo.
2. Implementar movimentação, câmera e HUD Android/PC.
3. Implementar combate, estado derrubado e reanimação.
4. Construir o mapa de teste.
5. Integrar rede, salas e partidas de 20 jogadores.
6. Importar personagens e uniformes com direitos de uso confirmados.
7. Testar desempenho em Android e PC e gerar builds.

## Requisitos
- Unity LTS compatível com Android e Windows.
- Android Build Support (SDK, NDK e OpenJDK).
- Git LFS recomendado para arquivos grandes (modelos, texturas e áudio).

## Licenciamento
Escudos, marcas, uniformes oficiais e patrocínios de clubes podem exigir autorização dos respectivos titulares. Até que haja autorização, usar materiais provisórios originais.
