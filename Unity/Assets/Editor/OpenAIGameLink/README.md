# OLHO DA IA — OpenAI GameLink (Unity Editor)

Plugin de Editor para ligar o projeto Unity a um serviço intermediário de IA. Ele **não** contém chave de API e não se conecta automaticamente à conversa do ChatGPT.

## Instalação
1. Copie a pasta `OpenAIGameLink` para `Unity/Assets/Editor/`.
2. Abra o projeto no Unity.
3. No menu, escolha **Vilarejo → OLHO DA IA - GameLink**.

## O que já faz localmente
- Gera um relatório JSON da cena ativa: hierarquia, nomes, transforms, componentes, materiais e bounds.
- Salva relatórios e capturas PNG em `GameLinkReports/` na raiz do projeto.
- Tem interface para URL HTTPS e token de acesso do relay.
- Pode enviar relatório e imagem para um relay configurado.
- Consulta comandos a cada 5 segundos quando ativado.
- Permite somente comandos `set_transform` em objetos existentes; execução remota vem desativada por padrão e usa Undo.

## Endpoints esperados no relay
- `POST /v1/unity/ping` — JSON com versão Unity.
- `POST /v1/unity/scene-report` — relatório JSON.
- `POST /v1/unity/scene-image` — multipart/form-data com campo `image`.
- `GET /v1/unity/commands` — JSON: `{"commands":[{"id":"...","action":"set_transform","targetPath":"Vilarejo_Gerado/Casa","position":[0,0,0],"rotationEuler":[0,0,0],"scale":[1,1,1],"note":"..."}]}`.

## Segurança e limites
- A URL deve usar HTTPS. O token é guardado nas EditorPrefs locais do Unity.
- Nunca coloque `OPENAI_API_KEY` no APK, em Assets, no GitHub ou no cliente Unity.
- O relay precisa autenticar o token, validar payloads e guardar a chave da API apenas como variável secreta no servidor.
- O plugin não executa código arbitrário, não apaga objetos e não importa assets remotamente.
- O plugin só funciona no **Unity Editor**; não é um botão runtime dentro do APK.
- Sem relay implantado e credenciais configuradas, a captura e o relatório continuam disponíveis localmente, mas não há análise de IA nem acesso remoto pela conversa ChatGPT.

## Próxima etapa
Hospedar o relay HTTPS e implementar autenticação/filas de comandos. A conexão direta com esta conversa ChatGPT não é automática; depende de uma integração de serviço suportada e autorizada.
