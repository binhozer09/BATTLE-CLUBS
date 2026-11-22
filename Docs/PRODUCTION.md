# BATTLE CLUBS — Painel de produção

Este painel acompanha a produção do jogo original. A compilação do antigo protótipo não é uma meta de entrega.

**Projeto Unity:** 2022.3.62f1  
**Repositório:** https://github.com/binhozer09/BATTLE-CLUBS  
**Branch de produção:** `production/3d-art-foundation`

## Etapas e situação

| Frente | Situação | Evidência / próximo passo |
|---|---|---|
| Base técnica Unity | Configuração ajustada; teste pendente | Módulos internos de Física e Animação adicionados ao manifest; falta reimportar/validar no Editor Unity |
| Materiais e texturas originais | Integrados no código; aguardando renderização | Gerador procedural cria tijolo, reboco, concreto, asfalto e piso da quadra; o gerador do mapa agora atribui esses materiais por tipo de objeto |
| Cenário 3D da comunidade | Geometria procedural ampliada; validação visual pendente | Além das construções, vielas e detalhes urbanos, o mapa agora gera quatro faixas de relevo em malha 3D ao redor da comunidade para compor o morro no horizonte; conferir no Unity se a escala e o enquadramento estão corretos |
| Personagem 3D | Ferramenta de integração criada; asset e clipes pendentes | `HumanoidAnimationDriver` lê telemetria/estado; `HumanoidAnimationSetupWizard` cria o controller-base e valida Avatar Humanoid; importar modelo/clipes e validar no Editor |
| Uniformes | Base de kits criada; integração no personagem pendente | `ClubKitApplier.cs` aplica cores/texturas por equipe com `MaterialPropertyBlock`; configurar renderers e chamar `ApplyKit` na atribuição da equipe; escudos oficiais somente com autorização |
| HUD e controles Android | Botão de resgate conectado; validação pendente | HUD agora inicia resgate ao pressionar e cancelar ao soltar, mostrando progresso; testar multitoque e interação no Unity/Android |
| Regras e spawn de equipes | Integração local criada; validação Unity pendente | `TeamMatchRules` limita cinco equipes de quatro e chama `TeamSpawnManager`, que posiciona os quatro membros em formação ao redor de cada um dos cinco marcadores |
| Multiplayer | Pendente | Ainda sem servidor autoritativo, sincronização, lobby ou matchmaking |

## Registro de trabalho

### 21/11/2026 — Spawn local por equipe
- Criado `TeamSpawnManager.cs`: localiza os cinco marcadores gerados pelo mapa, distribui até quatro jogadores por equipe em uma formação de quatro posições e orienta os personagens para o centro da comunidade.
- `TeamMatchRules.TryAddPlayer` agora chama o gerenciador de spawn ao registrar um jogador; `UrbanCommunityBlockout.Generate` atualiza o cache dos marcadores depois de criar o mapa.
- O construtor da cena de teste adiciona `TeamSpawnManager` ao objeto do mapa. Essa lógica é local e provisória; no multiplayer o servidor deverá validar e autorizar os pontos de nascimento.

### 21/11/2026 — Correção do HUD e diagnóstico de compilação
- O log da execução Unity mais recente no `main` apontou erro C# `CS1022` em `BattleClubsMobileHUD.cs`, causado por uma chave extra após `TouchLookPad`. A estrutura de namespace foi corrigida no `main`.
- Na revisão seguinte da branch de produção, foi detectado e corrigido outro problema estrutural: `TouchLookPad` estava declarado dentro de `HoldSprint`, impedindo sua resolução pelo construtor do HUD. Agora os dois componentes são classes independentes no namespace `BattleClubs.UI`.
- A versão de produção mantém câmera por toque no lado direito e resgate por pressão contínua; o HUD existente não foi recriado.
- A execução anterior falhou antes de gerar APK. A correção ainda precisa de uma nova compilação Unity; não publicar APK nesta fase.

### 21/11/2026 — Ferramenta de integração humanoide
- Criado `HumanoidAnimationSetupWizard.cs` com comandos no menu BATTLE CLUBS para gerar/atualizar o Animator Controller base (`Assets/Animation/BC_Humanoid.controller`) e validar o Avatar Humanoid selecionado. A ferramenta configura referências do `HumanoidAnimationDriver`, desativa root motion e verifica os componentes de gameplay no objeto raiz.
- Os estados do controller são criados sem clipes intencionalmente; os movimentos finais precisam vir de assets humanoides licenciados e ser atribuídos no Editor.
- O construtor da cena de teste agora inclui `Combatant` e `HumanoidAnimationDriver` no objeto raiz do jogador, preparando a hierarquia para substituição do visual provisório.
- Documentado o procedimento em `Docs/HUMANOID_ANIMATION_SETUP.md`. Não foi possível compilar ou validar no Unity Editor nesta sessão.

### 21/11/2026 — Correção do formato de partida e kits\n- Corrigida a divergência entre a especificação original (cinco equipes de quatro jogadores) e a implementação anterior (quatro equipes de cinco). `TeamMatchRules` agora fixa cinco equipes, quatro vagas por equipe e 20 jogadores no total.\n- `UrbanCommunityBlockout` agora cria cinco marcadores de nascimento, um por equipe.\n- `ClubKitApplier` agora possui cinco paletas provisórias e `TeamMatchRules.TryAddPlayer` aplica automaticamente o kit correspondente quando encontra o componente no personagem.\n- Atualizados README e documentos de mapa/kits. Ainda falta validar os pontos de nascimento, os renderers e a atribuição de equipe no Unity Editor.\n\n### 21/11/2026 — Fundação da produção visual
- Adicionado ao `UrbanCommunityBlockout` um relevo de morro em malhas 3D procedurais nas quatro bordas externas do mapa, com variação de altura e silhueta irregular; integrado à geração principal do cenário. Ainda precisa de inspeção visual no Unity para ajustar escala, normais e enquadramento.
- Criada a branch `production/3d-art-foundation`, separando a produção original da linha principal.
- Adicionado o gerador de cinco texturas tileáveis originais: tijolo aparente, reboco pintado, concreto, asfalto e piso de quadra.
- Integradas as texturas ao gerador procedural do cenário por tipo de superfície.
- Habilitados no manifest os módulos Unity de Física e Animação que faltaram na compilação anterior.
- Próxima ação: importar e executar a cena no Unity, verificar as escadas/cabos/drenagem e corrigir sobreposições; depois iniciar a integração do personagem humanoide com rig e animações.
- Adicionado `CommunityArtDetails.cs`: escadarias em vielas selecionadas, fiação aérea, canais de drenagem e muros de contenção; conectado ao construtor da cena Unity.
- A implementação inicial tinha quatro equipes de cinco jogadores; essa configuração foi corrigida posteriormente para respeitar o design original de cinco equipes de quatro.
- A versão anterior tinha quatro âncoras; a implementação atual cria cinco âncoras, uma por equipe.
- Integrado ao HUD Android o botão de resgate por toque contínuo: procura aliado caído, exibe progresso e cancela se o jogador soltar ou sair do botão. Continua sendo resgate local, sem autoridade/replicação de rede.
- Criado `HumanoidAnimationDriver.cs` para ligar velocidade, salto/queda e estados de vida ao Animator Humanoid; `ThirdPersonMotor` agora expõe telemetria de movimento para animação.
- Documentado o contrato de rig, hierarquia, parâmetros do Animator e critérios de validação em `Docs/HUMANOID_ANIMATION_SETUP.md`. O modelo 3D e os clipes finais ainda não foram adicionados.
- Ainda não há personagem final, captura renderizada, APK de produção ou validação visual no Unity Editor. O código precisa ser importado e executado no Unity para confirmar escala, colisões e desempenho.

## Como acompanhar
- Commits da produção: https://github.com/binhozer09/BATTLE-CLUBS/commits/production/3d-art-foundation
- Arquivos da produção: https://github.com/binhozer09/BATTLE-CLUBS/tree/production/3d-art-foundation
- Compilações (somente quando uma build de validação for necessária): https://github.com/binhozer09/BATTLE-CLUBS/actions

**Builds Android:** automação de APK no branch `main` foi pausada para não gerar builds durante a fase de desenvolvimento. Não executar nem publicar APK até solicitação explícita e conclusão do jogo.

**Regra de transparência:** uma tarefa só muda para concluída quando o arquivo foi gravado e a integração/teste correspondente foi confirmado. Plano não é asset pronto; código de geração não é captura de jogo.


### 21/11/2026 — Contrato técnico do multiplayer
- Criado `Docs/ONLINE_MULTIPLAYER_ARCHITECTURE.md` com arquitetura dedicada autoritativa, responsabilidades do servidor, estado mínimo replicado, fluxos de ataque/derrubada/reanimação, ciclo de vida da sala, requisitos de segurança e fases de validação.
- O SDK/provedor permanece deliberadamente em aberto até uma prova técnica Android + PC com 2–4 clientes, medição de latência/custo e confirmação de suporte a servidor dedicado. Nenhuma dependência de rede foi instalada e nenhuma partida online foi declarada pronta.
- Registrada a regra pendente de design sobre o destino de jogadores derrubados; não implementar bleed-out ou finalização sem decisão explícita no GAME_DESIGN.
