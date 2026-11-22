# BATTLE CLUBS — Arquitetura de multiplayer online

**Status:** especificação técnica inicial; nenhuma rede online foi implementada ou validada.  
**Escopo:** Android + PC, cross-play, 20 jogadores por partida (5 equipes de 4).

Este documento define os contratos que a camada de rede deverá cumprir antes de escolher e instalar um SDK. Não representa um servidor existente, lobby funcional ou partida online pronta.

## 1. Decisão de arquitetura

A partida deve usar **servidor dedicado autoritativo**. O cliente envia intenções de entrada (movimento, ataque, pulo, interação); o servidor valida e publica o estado resultante. Não usar um cliente jogador como autoridade da partida competitiva.

A escolha do provedor/SDK de transporte e hospedagem fica em aberto até uma prova técnica comparar:
- suporte real a servidor dedicado/headless em Unity 2022.3 LTS;
- Android e Windows no mesmo protocolo e versão de jogo;
- 20 jogadores simultâneos por sala;
- sincronização de transform, animação e eventos de combate;
- reconexão, migração/queda do servidor e encerramento de sala;
- latência nas regiões relevantes para jogadores do Brasil;
- custo por sala/hora, tráfego, CCU e operação;
- licença, limites de uso, suporte e caminho de migração.

**Não instalar um SDK nem criar contas/serviços de produção antes dessa prova.** Evita acoplar gameplay a um fornecedor sem teste de latência, custo e compatibilidade.

## 2. Limites de autoridade

| Sistema | Autoridade | Regra |
|---|---|---|
| Equipes e vagas | Servidor | Exatamente 5 equipes, até 4 jogadores por equipe |
| Entrada/saída da partida | Servidor | Validar sala, capacidade, versão e estado da partida |
| Spawn | Servidor | Selecionar um dos 5 spawns e validar posição/colisão |
| Movimento | Servidor | Validar velocidade, aceleração, salto e deslocamento plausíveis |
| Ataque com taco | Servidor | Validar cooldown, alcance, direção, alvo e janela de contato |
| Dano e estado Downed | Servidor | O cliente nunca define dano, vida ou eliminação |
| Reanimação | Servidor | Validar aliado, equipe, distância, duração e cancelamento |
| Eliminação e fim da equipe | Servidor | Equipe só é eliminada quando os 4 membros estão eliminados |
| Pontuação e vencedor | Servidor | Resultado calculado e persistido pelo serviço da partida |
| HUD e câmera | Cliente | Apresentação local; não são fonte de verdade |

O servidor deve rejeitar comandos fora de ordem, repetidos, com identificadores inválidos ou incompatíveis com o estado atual do jogador.

## 3. Modelo mínimo de estado replicado

### Estado da sala
- ID da partida, versão do protocolo e fase (Waiting, Starting, InProgress, Finished);
- limite de 20 participantes;
- cinco equipes com quatro posições cada;
- relógio autoritativo e resultado final.

### Estado do jogador
- identificador de sessão e equipe (0–4);
- posição, rotação e velocidade;
- estado de vida (Alive, Downed, Eliminated);
- estado de movimento necessário para apresentação;
- cooldown/estado do ataque e progresso de reanimação, quando aplicável.

O cliente deve interpolar estados recebidos para suavizar movimento. Predição local pode ser adicionada depois, mas a reconciliação deve aceitar o estado do servidor como definitivo.

## 4. Fluxos de gameplay

### Ataque
1. Cliente envia intenção de ataque com sequência/timestamp controlado pelo protocolo.
2. Servidor verifica se o jogador está Alive, se o cooldown terminou e se o ataque é permitido.
3. Servidor calcula janela, alcance e alvo usando estado autoritativo.
4. Servidor aplica no máximo um acerto válido por ataque e replica o resultado.
5. Clientes reproduzem animação/efeitos; efeitos visuais não aplicam dano.

### Derrubada e reanimação
1. Quatro acertos válidos levam o alvo a Downed, conforme o design atual.
2. Servidor mantém o jogador derrubado e valida tentativas de reanimação de aliados.
3. A reanimação dura quatro segundos na regra atual e é cancelada se distância, alvo ou condição deixar de ser válida.
4. A conclusão altera o estado no servidor e é replicada.
5. A equipe só é eliminada quando todos os quatro integrantes estiverem Eliminated.

**Ponto de design ainda aberto:** o documento de gameplay não define tempo de sangramento/eliminação automática de um jogador derrubado nem uma ação de finalização. Não implementar uma dessas regras por suposição; fechar a regra de produto antes de codificar.

## 5. Ciclo de vida da partida

1. Criar sala e reservar até 20 vagas.
2. Aguardar a condição de início definida pelo matchmaking.
3. Travar equipes e atribuir os cinco spawns no servidor.
4. Iniciar relógio e simulação autoritativa.
5. Tratar desconexão com janela de reconexão configurável; o servidor conserva o estado durante essa janela.
6. Encerrar quando houver uma equipe vencedora ou condição de encerramento definida.
7. Publicar resultado final e liberar recursos da sala.

A política para iniciar com menos de 20 jogadores, preencher vagas com bots ou cancelar sala precisa ser definida antes do matchmaking público.

## 6. Requisitos de desempenho e segurança

- Medir latência de ida e volta, perda de pacotes, jitter, taxa de correção e uso de CPU/memória no servidor.
- Separar frequência de simulação, envio de estado e atualização visual.
- Replicar apenas dados necessários; não transmitir objetos de cena inteiros ou dados cosméticos a cada atualização.
- Usar identificadores de sessão e validação de mensagens; nunca confiar em valores de dano, equipe, cooldown ou posição enviados pelo cliente.
- Limitar frequência/tamanho de comandos e registrar rejeições suspeitas.
- Testar com 20 clientes simulados e, depois, dispositivos Android reais e PCs.
- Garantir compatibilidade de versão e impedir que builds incompatíveis entrem na mesma sala.

## 7. Plano de implementação

| Fase | Entrega | Critério de saída |
|---|---|---|
| A. Prova de rede | Protótipo descartável com servidor dedicado e 2–4 clientes | Android e PC conectam; latência e custo medidos |
| B. Sessões e salas | Criar, entrar, sair, reconectar e fechar sala | Testes automatizados de ciclo de vida |
| C. Estado de partida | Equipes 5x4, spawn e relógio autoritativos | Servidor impede equipe/vaga/spawn inválidos |
| D. Gameplay em rede | Movimento, taco, Downed e revive | Dois clientes observam o mesmo resultado validado |
| E. Escala e segurança | Teste com 20 clientes e abuso de mensagens | Metas de desempenho definidas e atingidas |
| F. Cross-play de produção | Android + PC em ambiente hospedado | Teste prolongado, reconexão e encerramento aprovados |

## 8. Estado atual do repositório

A implementação atual contém sistemas locais de movimento, câmera, ataque, estado de combate, reanimação e distribuição de spawn. O manifest Unity consultado contém apenas módulos internos de Animation, Physics e UGUI; não há SDK de rede declarado nele. A documentação de status também registra que multiplayer online, lobby, matchmaking e servidor autoritativo ainda estão pendentes.

Portanto:
- não existe confirmação de partida online funcional;
- spawn local não substitui alocação de spawn no servidor;
- dano e reanimação locais precisam ser migrados para comandos validados;
- nenhuma build ou teste de rede é considerado concluído até ser executado e registrado.

## 9. Referências internas

- Docs/GAME_DESIGN.md — regras e formato de partida.
- Docs/MAP_AND_REVIVE_SETUP.md — mapa, spawns e reanimação local.
- Docs/STATUS.md — estado confirmado do repositório.
- Docs/PRODUCTION.md — painel de produção.
