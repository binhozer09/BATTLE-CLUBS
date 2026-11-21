# BATTLE CLUBS — Andamento do projeto

**Atualizado em:** 21/11/2026  
**Repositório:** https://github.com/binhozer09/BATTLE-CLUBS  
**Branch:** `main`

## Estado confirmado no repositório

| Área | Estado | O que existe |
|---|---|---|
| Estrutura do projeto | Parcial | Documentação e scripts iniciais da Unity |
| Movimento do jogador | Código inicial | `ThirdPersonMotor.cs` |
| Câmera em terceira pessoa | Código inicial atualizado | `ThirdPersonCameraRig.cs`; `AddAttackOrbit()` gira a câmera suavemente 90° ao atacar, mantendo o controle manual |
| Combate com taco | Código inicial atualizado | `BatAttack.cs` procura o adversário vivo mais próximo, vira o personagem e sincroniza o dano com `ApplyAttackHit()`; fallback após 0,35 s |
| Reanimação | Código inicial | `ReviveInteraction.cs` |
| Regras de equipes | Código inicial | `TeamMatchRules.cs` |
| Mapa da comunidade | Gerador procedural detalhado em código | `UrbanCommunityBlockout.cs` cria casas com fachadas, portas, janelas, varandas, ar-condicionado, caixas-d'água, postes, marcações viárias e quadra; ainda precisa ser gerado e avaliado na Unity |
| HUD Android | Código inicial adicionado | `BattleClubsMobileHUD.cs` cria joystick virtual e botões de correr, pular e atacar em runtime; integração ainda precisa ser validada |
| Uniformes 3D | Pendente de integração | Cadastro inicial de kits; modelos e texturas não confirmados |
| Multiplayer online | Pendente | Não há partida online funcional confirmada |
| APK Android / build PC | Pendente | Nenhuma compilação testada foi confirmada |

## Próximas etapas

1. Abrir o projeto na Unity, gerar o mapa e corrigir eventuais avisos ou incompatibilidades de render pipeline.
2. Configurar um personagem 3D provisório com Animator, Trigger `Attack` e Animation Event `ApplyAttackHit`.
3. Ajustar câmera, HUD e colisões; validar a movimentação dentro das vielas e na quadra.
4. Fazer uma primeira build Android de teste e medir desempenho em aparelho real.
5. Substituir os elementos provisórios por modelos otimizados e texturas de melhor qualidade.
6. Integrar uniformes autorizados e, depois, desenvolver e testar o multiplayer.

## Limitação importante

Os arquivos listados são código-fonte. A presença deles no GitHub **não comprova** que o jogo compila, que o mapa já foi renderizado, que a HUD funciona, nem que existe APK ou multiplayer pronto. Cada etapa só deve ser marcada como concluída depois de compilar e testar na Unity.
