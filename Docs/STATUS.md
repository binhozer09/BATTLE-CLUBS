# BATTLE CLUBS — Andamento do projeto

**Atualizado em:** 21/11/2026  
**Repositório:** https://github.com/binhozer09/BATTLE-CLUBS  
**Branch de produção consultada:** `production/3d-art-foundation` (a branch principal `main` ainda não contém todas as alterações abaixo).

## Estado confirmado no repositório

| Área | Estado | O que existe |
|---|---|---|
| Projeto Unity | Estrutura inicial criada | Versão-base Unity 2022.3 LTS e dependência UGUI em `Packages/manifest.json`; ainda precisa ser aberto/importado no Editor |
| Cena de teste | Ferramenta de criação adicionada | Menu `BATTLE CLUBS > Create Community Test Scene` em `Assets/Editor/BattleClubsSceneBuilder.cs`; cria câmera, personagem provisório, alvos de treino, iluminação e liga os scripts |
| Movimento do jogador | Código inicial | `ThirdPersonMotor.cs` |
| Câmera em terceira pessoa | Código inicial atualizado | `ThirdPersonCameraRig.cs`; `AddAttackOrbit()` gira a câmera suavemente 90° ao atacar |
| Combate com taco | Código inicial atualizado | `BatAttack.cs` procura o adversário vivo mais próximo e sincroniza o dano com `ApplyAttackHit()`; fallback após 0,35 s |
| Reanimação | Código inicial | `ReviveInteraction.cs` |
| Regras de equipes | Código atualizado na branch de produção | Cinco equipes de quatro jogadores (20 no total); integração local de kits ao adicionar jogador; ainda sem validação no Unity |
| Mapa da comunidade | Gerador procedural detalhado em código | `UrbanCommunityBlockout.cs` cria casas com fachadas, portas, janelas, varandas, ar-condicionado, caixas-d'água, postes, marcações viárias e quadra; ainda precisa ser gerado e avaliado na Unity |
| HUD Android | Código inicial adicionado | `BattleClubsMobileHUD.cs` cria joystick virtual e botões de correr, pular e atacar em runtime; integração ainda precisa ser validada |
| Uniformes 3D | Base de kits por equipe | `ClubKitApplier.cs` suporta cinco paletas, cores e texturas; atribuição automática via `TeamMatchRules`; falta configurar os renderers do modelo humanoide |
| Multiplayer online | Pendente | Não há partida online funcional confirmada |
| APK Android / build PC | Pendente | Nenhuma compilação testada foi confirmada |

## Próximas etapas

1. Importar a branch de produção no Unity 2022.3.62f1 e corrigir erros de compilação/importação.
2. Executar a cena de verificação no Editor para validar mapa, colisões, cinco spawns, câmera e controles existentes.
3. Integrar o modelo humanoide final, rig e clipes ao `HumanoidAnimationDriver` e `BatAttack`.
4. Configurar os renderers do uniforme e validar a atribuição automática de kits.
5. Implementar servidor autoritativo, sincronização, salas, lobby e matchmaking para 20 jogadores.
6. Validar desempenho e estabilidade em Android e PC antes de qualquer build final.

## Limitação importante

O código e a ferramenta de criação de cena foram gravados no GitHub, mas a cena ainda não foi executada no Unity Editor. A branch de produção possui correções recentes de regras (5x4), cinco pontos de nascimento e atribuição de kits; essas alterações ainda não foram compiladas nem validadas no Editor. Portanto, não há captura real, compilação validada, APK ou multiplayer pronto. A qualidade visual final só poderá ser avaliada depois de abrir a cena e renderizá-la na Unity.
