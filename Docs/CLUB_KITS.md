# Sistema de uniformes por equipe

## Componente
`BattleClubs.Characters.ClubKitApplier` aplica kits originais aos renderers do personagem usando `MaterialPropertyBlock`. Isso evita criar cópias de materiais compartilhados e permite que jogadores da mesma cena tenham cores diferentes.

## Integração
1. Adicionar `ClubKitApplier` ao objeto visual raiz do personagem (o mesmo que contém o modelo/Animator).
2. Se necessário, preencher `Targets` com os renderers da camisa/shorts; por padrão o componente coleta os renderers filhos.
3. Configurar até quatro `KitDefinition` no Inspector, com nome, cor primária, cor secundária e textura opcional.
4. Quando o servidor/gerenciador de partida atribuir a equipe, chamar `ApplyKit(teamIndex, shirtNumber)`. Os índices são zero-based (0 a 3).
5. Para evitar colorir pele, cabelo ou acessórios, configurar `Primary Material Slots` e `Secondary Material Slots` ou atribuir somente os renderers das peças do uniforme em `Targets`.

## Limites desta etapa
- As cinco paletas incluídas são placeholders originais, não representam clubes reais.
- Não há escudos, marcas ou uniformes oficiais incorporados.
- O número é armazenado em `PlayerNumber` para futura integração com UI/textura de costas; ainda não é renderizado automaticamente.
- A integração com a atribuição de equipe e a validação visual precisam ser feitas no Unity Editor.
