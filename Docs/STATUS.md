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
| Regras de equipes | Código local ampliado | Cinco equipes de quatro jogadores; validação de elencos completos, fases WaitingForPlayers/InProgress/Finished, bloqueio de novas inscrições após início e detecção de vencedor único; ainda sem validação no Unity |
| Mapa da comunidade | Gerador procedural detalhado em código | `UrbanCommunityBlockout.cs` cria casas com fachadas, portas, janelas, varandas, ar-condicionado, caixas-d'água, postes, marcações viárias e quadra; ainda precisa ser gerado e avaliado na Unity |
| HUD Android | Código inicial adicionado | `BattleClubsMobileHUD.cs` cria joystick virtual e botões de correr, pular e atacar em runtime; integração ainda precisa ser validada |
| Uniformes 3D | Base de kits por equipe | `ClubKitApplier.cs` suporta cinco paletas, cores e texturas; atribuição automática via `TeamMatchRules`; falta configurar os renderers do modelo humanoide e validar visualmente |
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


## Atualização de regras — 21/11/2026

- A partida local agora possui fases explícitas: aguardando jogadores, em andamento e finalizada.
- O início exige 20 jogadores em cinco elencos completos e coerentes; novas inscrições são bloqueadas depois do início.
- O vencedor é registrado quando resta exatamente uma equipe não eliminada. Empate por eliminação simultânea continua sem regra definida.
- Ataque e reanimação possuem verificações locais adicionais de estado e equipe.
- Essas mudanças foram gravadas no GitHub, mas ainda não foram compiladas ou executadas no Unity Editor. O multiplayer online continua pendente.


## Atualização — pipeline de personagem 3D

Foi adicionada na branch `feature/humanoid-character-integration` uma ferramenta de Editor para integrar um modelo Humanoid importado ao Player_Root existente. Ela conecta Animator, driver de movimento, ataque e aplicador de kits, removendo apenas os visuais provisórios gerados pelo construtor.

O projeto ainda depende de um modelo humanoide licenciado e clipes de animação. A ferramenta foi gravada no GitHub, mas ainda não foi compilada nem executada no Unity Editor. Consulte `Docs/CHARACTER_INTEGRATION.md`.


## Atualização — otimização de materiais do cenário

Foi adicionada `CommunityMaterialLibrary.cs` para reutilizar materiais por cor nos detalhes de escadarias, fiação, varais, mercadinhos e infraestrutura das fachadas. Os geradores `CommunityArtDetails` e `FavelaStreetLifeDetails` agora compartilham essas instâncias, reduzindo a criação repetida de materiais. Isso ainda não substitui a otimização de malhas/draw calls nem uma medição real no Unity; validar memória e desempenho em Android e PC continua pendente.


## Atualização — cache compartilhado de materiais do mapa

O gerador principal do mapa agora compartilha materiais por combinação de cor e textura através de `CommunityMaterialLibrary`. A escala individual de textura é enviada por `MaterialPropertyBlock`, permitindo reutilizar o mesmo material em objetos de dimensões diferentes. A mudança também cobre as quatro malhas do relevo de fundo e complementa a otimização já aplicada aos detalhes de escadarias e fachadas. Ainda falta confirmar a compilação, inspecionar o resultado visual e medir draw calls, memória e FPS no Unity Profiler em Android/PC.


## Atualização — primeira camada de sessão online

Foi adicionada à branch `feature/humanoid-character-integration` a dependência Netcode for GameObjects 1.9.1 com Unity Transport 1.4.0, alinhada à versão Unity 2022.3. O novo `NetworkSessionBootstrap.cs` permite iniciar Host, servidor dedicado ou cliente por IPv4, limita conexões a 20 e valida a versão do protocolo na aprovação de entrada.

**Limite desta etapa:** ainda não existe partida online pronta. O prefab de jogador precisa ser configurado com `NetworkObject` no NetworkManager; equipes, spawn, movimento, dano, reanimação e pontuação ainda não foram migrados para autoridade do servidor. A implementação precisa ser importada, compilada e testada no Unity Editor. Consulte `Docs/NETWORK_IMPLEMENTATION.md`.


## Atualização — combate online autoritativo inicial

Na branch `feature/humanoid-character-integration` foi criado `NetworkCombatantAuthority.cs`. Em jogadores com `NetworkObject` ativo, `BatAttack` agora envia a solicitação ao servidor em vez de aplicar dano local. O servidor aplica cooldown, procura alvos por colisão, rejeita aliados e aplica o acerto no `Combatant`; quantidade de acertos e estado de vida são replicados por `NetworkVariable`. A integração exige adicionar o componente ao prefab do jogador, configurar o ponto de impacto e um `NetworkTransform`, além de registrar o prefab no `NetworkManager`. Reanimação, movimento replicado, equipes autoritativas, matchmaking e pontuação online continuam pendentes. Código ainda não compilado/testado no Unity Editor.


## Atualização — reanimação online autoritativa inicial

Foi criado `NetworkReviveAuthority.cs`. Em sessão NGO, a interação existente encaminha início e cancelamento ao servidor. O servidor procura um companheiro derrubado, valida estado de vida, equipe e distância, conta quatro segundos e restaura o jogador; o progresso é replicado. O prefab online precisa receber `NetworkReviveAuthority` além de `NetworkCombatantAuthority`. A atribuição de equipes ainda não é replicada/automatizada, e o código ainda não foi compilado nem testado no Unity Editor.


## Atualização — importador automático de humanoide 3D CC0

Foi adicionado `HumanoidAssetBootstrap.cs` ao Editor. O menu `BATTLE CLUBS/Characters/Download CC0 Humanoid + Animations` baixa diretamente a release oficial KinemIQ, confere SHA-256 e extrai o FBX + clipes para `Assets/ThirdParty/KinemIQ`, sem exigir download manual pelo usuário. O pacote publicado declara 35 animações humanoides CC0 e inclui `KinemIQ_Mannequin.fbx`. O pacote ainda não foi efetivamente importado por uma instância do Unity Editor nesta sessão; o personagem é um mannequin 3D rigged, não um modelo fotorrealista final. Depois da importação, o Avatar precisa ser validado como Humanoid e integrado ao `Player_Root` pelo wizard existente. Nenhuma compilação/teste Unity foi executado.


## 22/11/2026 — Integração do espaço central da favela

- Revisada a composição dos três geradores existentes: `UrbanCommunityBlockout`, `CommunityArtDetails` e `FavelaStreetLifeDetails` continuam sendo usados, sem duplicar sistemas.
- Reservado o bloco central do layout 5x5 como praça/quadra aberta. O gerador de casas não constrói fachadas nesse bloco; os geradores de escadarias, varais, mercadinhos e medidores também deixam essa área livre.
- A quadra central passa a ocupar uma área sem casas nem objetos de rua atravessando o piso, reduzindo interseções visuais e obstáculos indevidos na principal área de combate.
- Alterações gravadas na branch `feature/humanoid-character-integration`. Ainda precisam ser importadas e verificadas no Unity Editor: colisões, navegação a pé, alinhamento dos geradores e aparência em execução não foram testados nesta sessão.


### 22/11/2026 — Limpeza de recursos dos geradores do cenário
- `UrbanCommunityBlockout` agora rastreia e libera as meshes de relevo procedural quando o mapa é regenerado ou destruído, evitando acumular meshes órfãs durante recriações no Editor/Play Mode.
- `CommunityArtDetails` agora reutiliza `CommunityMaterialLibrary` nos cabos de energia em vez de criar um Material exclusivo para cada trecho de LineRenderer.
- São correções de gerenciamento de recursos e redução de materiais duplicados; não representam benchmark de FPS nem validação visual. A compilação e o teste de regeneração ainda dependem do Unity Editor.
