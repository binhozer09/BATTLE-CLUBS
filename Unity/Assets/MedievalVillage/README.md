# MedievalVillage — BATTLE-CLUBS

Os arquivos-fonte do kit estão organizados no Dropbox em `/BATTLE-CLUBS-Uploads/MedievalVillage`.

## Inventário conferido
- FBX: 176 modelos
- glTF: 176 arquivos .gltf + 176 buffers .bin + 22 PNG auxiliares
- OBJ: 176 modelos + 176 materiais .mtl
- Textures: 26 PNG
- Raiz: License_Standard.txt e Preview.jpg

## Pastas esperadas no projeto Unity
Copie os arquivos do Dropbox para:
- `Unity/Assets/MedievalVillage/FBX/`
- `Unity/Assets/MedievalVillage/Textures/`

Os diretórios glTF e OBJ ficam preservados como formatos-fonte alternativos; a primeira cena usa FBX para importação direta pelo Unity. Não é necessário duplicar glTF/OBJ dentro do projeto para gerar a cena.

## Gerador da cena
Com os 176 FBX e as texturas disponíveis no projeto Unity, abra o menu. Se `BattleClubs_Main.unity` ainda não existir, o gerador chama primeiro o construtor oficial da cena principal para preservar o Player, câmera e HUD; em seguida cria a cena medieval separada:

`BATTLE CLUBS → World → Build Medieval Village Scene`

O editor cria `Assets/Scenes/BattleClubs_MedievalVillage.unity` como a nova cena de jogo. Da cena antiga, reaproveita somente `Player_Local` (humanoide, controlador e componentes anexados ao jogador); cria uma câmera third-person nova para a vila. Nenhum objeto do mapa antigo, câmera antiga ou cenário anterior é levado para a nova cena. Os arquivos-fonte antigos permanecem no repositório/backup para evitar perda irreversível.

Os materiais gerados usam mapas Base Color e, quando disponíveis, mapas Normal (incluindo os arquivos da subpasta `Normals Godot-Unity`). Os objetos de cada casa ficam agrupados e recebem MeshColliders para permitir colisão. A cena ainda precisa ser executada e aberta no Unity Editor para validar escala, encaixe dos módulos, colisões, materiais e iluminação antes de qualquer APK.
