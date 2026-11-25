# Agente remoto do BATTLE CLUBS

## Serviço
- Painel: https://battle-clubs-remote-agent-production.up.railway.app/
- Health: https://battle-clubs-remote-agent-production.up.railway.app/health
- Serviço Railway: `battle-clubs-remote-agent`
- Dados persistidos em volume Railway montado em `/data`.

## Pareamento do Android
1. Instale uma versão do APK que contenha `BattleClubsRemoteAgent`.
2. Abra o jogo e toque em **Conectar** no painel do agente dentro do jogo.
3. Digite o código administrativo privado no campo de pareamento. O código não deve ser publicado nem salvo no repositório.
4. O servidor emite um token individual do dispositivo, salvo localmente em PlayerPrefs.

## Painel web
Abra o endereço do serviço, informe o código administrativo e use **Enviar configuração ao jogo**. O cliente consulta a fila a cada 8 segundos e confirma a aplicação.

## Comandos permitidos
- `cameraFov`: 40–90
- `targetFrameRate`: 30–90
- `moveSpeed`: 1–12
- `sprintSpeed`: 1–16
- `cameraSensitivity`: 0.2–5
- `shadowDistance`: 0–150
- `masterVolume`: 0–1

O protocolo não executa C#, scripts, shell ou código arbitrário recebido pela rede. Alterações em scripts e mecânicas precisam passar pelo GitHub Actions e por uma nova compilação do APK. O sistema remoto só aplica parâmetros explicitamente permitidos.

## Diagnósticos
O cliente envia os arquivos de evidência do diretório local de diagnóstico após uma ocorrência, se o dispositivo estiver pareado e conectado. O servidor mantém os arquivos no volume persistente. O painel lista metadados de recebimento; a inspeção/análise automática por IA ainda é uma etapa separada.

## Limites atuais
- Atualizações de parâmetros são aplicadas durante a sessão.
- Não há ainda entrega remota de texturas/modelos/AssetBundles.
- Não há captura de vídeo MP4 no Android; são enviadas imagens e logs.
- O APK atualmente instalado precisa ser atualizado para incluir o cliente remoto.
