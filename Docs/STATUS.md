# BATTLE CLUBS — Andamento do projeto

**Atualizado em:** 21/11/2026  
**Repositório:** https://github.com/binhozer09/BATTLE-CLUBS  
**Branch:** `main`

## Estado confirmado no repositório

| Área | Estado | O que existe |
|---|---|---|
| Projeto Unity | Estrutura inicial criada | Versão-base Unity 2022.3 LTS e dependência UGUI em `Packages/manifest.json`; ainda precisa ser aberto/importado no Editor |
| Cena de teste | Ferramenta de criação adicionada | Menu `BATTLE CLUBS > Create Community Test Scene` em `Assets/Editor/BattleClubsSceneBuilder.cs`; cria câmera, personagem provisório, alvos de treino, iluminação e liga os scripts |
| Movimento do jogador | Código inicial | `ThirdPersonMotor.cs` |
| Câmera em terceira pessoa | Código inicial atualizado | `ThirdPersonCameraRig.cs`; `AddAttackOrbit()` gira a câmera suavemente 90° ao atacar |
| Combate com taco | Código inicial atualizado | `BatAttack.cs` procura o adversário vivo mais próximo e sincroniza o dano com `ApplyAttackHit()`; fallback após 0,35 s |
| Reanimação | Código inicial | `ReviveInteraction.cs` |
| Regras de equipes | Código inicial | `TeamMatchRules.cs` |
| Mapa da comunidade | Gerador procedural detalhado em código | `UrbanCommunityBlockout.cs` cria casas com fachadas, portas, janelas, varandas, ar-condicionado, caixas-d'água, postes, marcações viárias e quadra; ainda precisa ser gerado e avaliado na Unity |
| HUD Android | Código inicial adicionado | `BattleClubsMobileHUD.cs` cria joystick virtual e botões de correr, pular e atacar em runtime; integração ainda precisa ser validada |
| Uniformes 3D | Pendente de integração | Cadastro inicial de kits; modelos e texturas não confirmados |
| Multiplayer online | Pendente | Não há partida online funcional confirmada |
| APK Android / build PC | Pendente | Nenhuma compilação testada foi confirmada |

## Próximas etapas

1. Abrir a pasta `Unity` no Unity Hub com Unity 2022.3 LTS e aguardar a importação dos pacotes.
2. Executar `BATTLE CLUBS > Create Community Test Scene`; a ferramenta salva `Assets/Scenes/BattleClubs_Community_Test.unity`.
3. Pressionar Play para gerar o mapa, observar a câmera e testar movimento e HUD; corrigir erros que aparecerem no Console.
4. Substituir o personagem provisório por modelo 3D com rig e animações reais; configurar o Trigger `Attack` e o Animation Event `ApplyAttackHit`.
5. Fazer uma primeira build Android de teste e medir desempenho em aparelho real.
6. Integrar uniformes autorizados e, depois, desenvolver e testar o multiplayer.

## Limitação importante

O código e a ferramenta de criação de cena foram gravados no GitHub, mas a cena ainda não foi executada no Unity Editor. Portanto, não há captura real, compilação validada, APK ou multiplayer pronto. A qualidade visual final só poderá ser avaliada depois de abrir a cena e renderizá-la na Unity.
