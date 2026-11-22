# BATTLE CLUBS — primeira camada de sessão online

**Status:** implementação inicial no código; ainda não validada no Unity nem em rede real.

## SDK e compatibilidade

O projeto usa Unity 2022.3 LTS. Foi adicionada a base oficial:
- Netcode for GameObjects 1.9.1;
- Unity Transport 1.4.0 (dependência exigida pelo NGO 1.9.1).

A documentação da Unity lista NGO 1.9.1 como versão lançada para Unity 2022.3. O changelog do pacote registra Unity Transport 1.4.0 como dependência. Isso estabelece compatibilidade de pacote, mas não substitui a compilação e os testes no Editor.

## Componente criado

`Unity/Assets/Scripts/Networking/NetworkSessionBootstrap.cs`

Responsabilidades atuais:
- iniciar sessão como Host;
- iniciar processo como servidor dedicado;
- conectar cliente por endereço IPv4 e porta;
- limitar a capacidade configurada a no máximo 20 conexões/jogadores;
- exigir versão de protocolo compatível no payload de conexão;
- rejeitar conexões quando a sala estiver cheia ou a versão for incompatível;
- expor encerramento da sessão e registrar conexão/desconexão.

## Configuração obrigatória no Unity

1. Aguardar o Package Manager resolver NGO e Unity Transport.
2. Criar um GameObject de sessão na cena.
3. Adicionar `NetworkManager`, `UnityTransport` e `NetworkSessionBootstrap`.
4. No `NetworkManager`, atribuir o prefab de jogador em `Player Prefab`. O prefab precisa conter `NetworkObject`.
5. Configurar o transporte/porta de acordo com a hospedagem.
6. Executar primeiro Host + Client no Editor/PC; depois validar Android + PC.

## O que esta entrega ainda NÃO faz

- Não cria matchmaking, lista pública de salas ou convites.
- Não distribui automaticamente jogadores nas cinco equipes.
- Não implementa movimento, dano, derrubada, reanimação ou pontuação autoritativos pela rede.
- Não configura Relay, lobby cloud ou hospedagem de servidores.
- Não comprova suporte a 20 jogadores simultâneos até executar teste de carga.

A camada de sessão é o primeiro degrau de rede. A próxima entrega deve ligar o prefab do jogador à autoridade do servidor e implementar estado replicado de equipe/vida, seguido de movimento e combate validados no servidor.

## Referências

- Unity NGO 1.9.1 para Unity 2022.3: https://docs.unity.cn/Manual/com.unity.netcode.gameobjects.html
- NGO 1.9.1 changelog e dependência UTP 1.4.0: https://docs.unity.cn/Packages/com.unity.netcode.gameobjects@1.9/changelog/CHANGELOG.html
