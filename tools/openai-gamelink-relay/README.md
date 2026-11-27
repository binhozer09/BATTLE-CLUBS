# OpenAI GameLink Relay

Serviço HTTPS intermediário para o plugin Unity Editor em `Unity/Assets/Editor/OpenAIGameLink`.

## O que faz
- Autentica o Unity com `GAMELINK_TOKEN` (Bearer).
- Recebe relatórios de cena e imagens PNG/JPEG/WebP.
- Envia o conteúdo à OpenAI Responses API usando `OPENAI_API_KEY` guardada somente no servidor.
- Devolve um diagnóstico em português diretamente ao Editor.
- Mantém o último diagnóstico em memória e expõe `GET /v1/unity/latest-analysis`.
- O endpoint de comandos responde sempre com uma lista vazia. Esta primeira versão não executa alterações remotas.

## Implantação
Use um serviço Node.js 20+ que aceite deploy de uma subpasta do repositório:
- Root directory: `tools/openai-gamelink-relay`
- Build command: nenhum
- Start command: `npm start`

Configure estas variáveis secretas no provedor:
- `GAMELINK_TOKEN`: token longo e aleatório, que será informado no campo Token do plugin Unity.
- `OPENAI_API_KEY`: chave da conta API OpenAI do usuário.
- `OPENAI_MODEL` (opcional): padrão `gpt-4.1-mini`.
- `PORT`: normalmente fornecida automaticamente pelo provedor.

Depois do deploy, copie a URL HTTPS pública para o campo URL do relay no plugin.

## Rotas
- `GET /health`: estado básico do serviço (sem expor segredos).
- `POST /v1/unity/ping`
- `POST /v1/unity/scene-report`
- `POST /v1/unity/scene-image` (multipart, campo `image`)
- `GET /v1/unity/latest-analysis`
- `GET /v1/unity/commands` → `{"commands":[]}`

## Segurança e limites
- Nunca coloque a chave OpenAI em Unity, APK, GitHub ou variáveis públicas.
- Use HTTPS e um token exclusivo para este relay.
- Limite de 30 solicitações/minuto por IP, relatório de 2 MB e imagem de 12 MB.
- Diagnósticos não são comandos. Não há execução remota de código nem edição automática nesta versão.
- O último diagnóstico fica apenas na memória do processo; reiniciar ou escalar para múltiplas instâncias perde/fragmenta esse histórico.
- O uso da OpenAI API é cobrado separadamente da assinatura ChatGPT.
