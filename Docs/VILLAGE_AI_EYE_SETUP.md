# OLHO DA IA — inspeção visual do vilarejo

## O que foi adicionado
- Componente Unity `VillageAIEye`: botão no jogo, captura da tela atual do jogador, redução da imagem para até 1024 px e envio JPEG.
- A análise volta para um painel dentro do jogo em português.
- O construtor da cena adiciona o componente à câmera principal automaticamente.
- Proxy Node.js em `AI-Backend/VillageVision` para chamar a OpenAI Responses API sem colocar a chave secreta dentro do APK.

## Ativar o servidor (necessário para a análise funcionar)
1. Publique a pasta `AI-Backend/VillageVision` em um serviço HTTPS (Railway, Render ou outro host Node 20).
2. Configure no servidor a variável secreta `OPENAI_API_KEY`.
3. Opcional: configure `OPENAI_MODEL` (padrão: `gpt-4.1-mini`).
4. O servidor expõe `POST /v1/village/inspect`.
5. No Unity, selecione a câmera `Main Camera`, componente `VillageAIEye`, e preencha `Inspection Endpoint` com a URL HTTPS completa, por exemplo `https://SEU-SERVICO/v1/village/inspect`.
6. Gere novamente o APK.

## Formato
O cliente envia JSON com `imageBase64`, `mimeType`, `deviceModel`, `sceneName` e `instruction`. O servidor responde `{"analysis":"..." }`.

## Segurança e limites
- A chave `OPENAI_API_KEY` fica somente no servidor; nunca a coloque no projeto Unity nem no APK.
- O proxy limita o corpo da imagem e aplica limite simples de 8 análises por IP por minuto. Para uso público, acrescente autenticação de usuário, limites de gasto e rate limiting persistente no provedor.
- Cada análise usa a API da OpenAI e pode gerar cobrança na conta API. O recurso só envia imagem quando o jogador toca em **ANALISAR CÂMERA**; não transmite vídeo contínuo.
- A IA faz inspeção visual, não comprova colisões, geometria escondida ou alinhamento milimétrico a partir de uma única imagem.

## API
Usa a Responses API com entrada `input_image` em data URL JPEG e modelo configurável. Referência: https://developers.openai.com/api/docs/guides/images-vision?api-mode=responses
