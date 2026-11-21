# BATTLE CLUBS — Andamento do projeto

**Atualizado em:** 21/11/2026  
**Repositório:** https://github.com/binhozer09/BATTLE-CLUBS  
**Branch:** `main`

## Estado confirmado no repositório

| Área | Estado | O que existe |
|---|---|---|
| Estrutura do projeto | Parcial | Documentação e pastas iniciais do Unity |
| Movimento do jogador | Código inicial | `ThirdPersonMotor.cs` |
| Câmera em terceira pessoa | Código inicial atualizado | `ThirdPersonCameraRig.cs`; o método `AddAttackOrbit()` gira a câmera suavemente 90° por acionamento do ataque, mantendo o controle manual |
| Combate com taco | Código inicial atualizado | `BatAttack.cs` aciona o giro da câmera, dispara o gatilho opcional `Attack` no Animator e prioriza o adversário vivo mais próximo dentro do alcance |
| Reanimação | Código inicial | `ReviveInteraction.cs` |
| Regras de equipes | Código inicial | `TeamMatchRules.cs` |
| Mapa da comunidade | Blockout procedural | `UrbanCommunityBlockout.cs` |
| HUD Android | Código inicial adicionado | `BattleClubsMobileHUD.cs` cria joystick virtual e botões de correr, pular e atacar em runtime; integração ainda precisa ser validada no Unity |
| Uniformes 3D oficiais | Pendente de integração | Identificação/cadastro inicial; modelos e texturas não confirmados |
| Multiplayer online | Pendente | Não há partida online funcional confirmada |
| APK Android / build PC | Pendente | Nenhuma compilação testada foi confirmada |

## Próximas etapas

1. Configurar no Animator Controller o Trigger `Attack` e validar a animação, o direcionamento ao adversário mais próximo e o giro da câmera no Unity.
2. Ajustar a HUD à resolução e proporção de telas Android.
3. Criar/configurar a cena Unity e um personagem provisório.
4. Compilar e testar primeiro no Android e depois no PC.
5. Evoluir o blockout da comunidade para cenário detalhado.
6. Integrar uniformes autorizados e, depois, o multiplayer.

## Limitação importante

Os arquivos listados são código-fonte inicial. A presença deles no GitHub **não comprova** que o jogo compila, que a HUD funciona, nem que existe APK ou multiplayer pronto. Cada etapa só deve ser marcada como concluída depois de compilar e testar.
