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
| Personagem 3D | Estrutura de integração criada; asset pendente | Driver de Animator e telemetria do motor adicionados; falta importar o modelo Humanoid, criar o Animator Controller e validar clipes no Unity |
| Uniformes | Base de kits criada; integração no personagem pendente | `ClubKitApplier.cs` aplica cores/texturas por equipe com `MaterialPropertyBlock`; configurar renderers e chamar `ApplyKit` na atribuição da equipe; escudos oficiais somente com autorização |
| HUD e controles Android | Botão de resgate conectado; validação pendente | HUD agora inicia resgate ao pressionar e cancelar ao soltar, mostrando progresso; testar multitoque e interação no Unity/Android |
| Regras de equipes | Em correção de consistência | Formato canônico: 5 equipes de 4 jogadores (20 no total); alinhar capacidade e cinco pontos de nascimento com `GAME_DESIGN.md` |
| Multiplayer | Pendente | Ainda sem servidor autoritativo, sincronização, lobby ou matchmaking |

## Registro de trabalho

### 21/11/2026 — Fundação da produção visual
- Adicionado ao `UrbanCommunityBlockout` um relevo de morro em malhas 3D procedurais nas quatro bordas externas do mapa, com variação de altura e silhueta irregular; integrado à geração principal do cenário. Ainda precisa de inspeção visual no Unity para ajustar escala, normais e enquadramento.
- Criada a branch `production/3d-art-foundation`, separando a produção original da linha principal.
- Adicionado o gerador de cinco texturas tileáveis originais: tijolo aparente, reboco pintado, concreto, asfalto e piso de quadra.
- Integradas as texturas ao gerador procedural do cenário por tipo de superfície.
- Habilitados no manifest os módulos Unity de Física e Animação que faltaram na compilação anterior.
- Próxima ação: importar e executar a cena no Unity, verificar as escadas/cabos/drenagem e corrigir sobreposições; depois iniciar a integração do personagem humanoide com rig e animações.
- Adicionado `CommunityArtDetails.cs`: escadarias em vielas selecionadas, fiação aérea, canais de drenagem e muros de contenção; conectado ao construtor da cena Unity.
- Corrigida a capacidade de equipe para cinco jogadores e inicializadas quatro equipes padrão (20 vagas no total); o construtor da cena agora inclui o gerenciador de regras de partida.
- Corrigidos os pontos de nascimento do mapa para quatro âncoras, uma por equipe.
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
