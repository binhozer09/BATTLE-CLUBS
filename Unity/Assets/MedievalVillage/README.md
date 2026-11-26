# MedievalVillage — BATTLE-CLUBS

Os arquivos-fonte do kit estão organizados no Dropbox em `/BATTLE-CLUBS-Uploads/MedievalVillage`.

## Inventário conferido
- FBX: 176 modelos
- glTF: 176 arquivos .gltf + 176 buffers .bin + 22 PNG auxiliares
- OBJ: 176 modelos + 176 materiais .mtl
- Textures: 32 PNG (26 na pasta principal + 6 mapas Normal em `Normals Godot-Unity`)
- Raiz: License_Standard.txt e Preview.jpg

## Pastas esperadas no projeto Unity
Copie os arquivos do Dropbox para:
- `Unity/Assets/MedievalVillage/FBX/`
- `Unity/Assets/MedievalVillage/Textures/`

Os diretórios glTF e OBJ ficam preservados como formatos-fonte alternativos; a primeira cena usa FBX para importação direta pelo Unity. Não é necessário duplicar glTF/OBJ dentro do projeto para gerar a cena.

## Gerador da cena
Com os 176 FBX e as texturas disponíveis no projeto Unity, abra o menu. Se `BattleClubs_HumanoidSource.unity` ainda não existir, o gerador cria uma cena temporária contendo somente `Player_Local`; depois transfere esse objeto para a vila. A câmera third-person é criada novamente e ligada ao controlador e ao ataque do jogador:

`BATTLE CLUBS → World → Build Medieval Village Scene`

O editor cria `Assets/Scenes/BattleClubs_MedievalVillage.unity` como a única cena habilitada para iniciar o jogo. Reaproveita somente `Player_Local` (humanoide, controlador e componentes anexados, incluindo HUD e ataque), cria uma câmera nova e recria as regras das 5 equipes e os 20 pontos de spawn. Nenhum objeto do mapa antigo ou câmera antiga é levado para a nova cena.

Os materiais gerados usam mapas Base Color e Normal: telhados, madeira, reboco, tijolo e pedra; o chão, as ruas e a praça também recebem textura de pedra. Os mapas Normal são importados com o tipo correto, incluindo os arquivos da subpasta `Normals Godot-Unity`. Os objetos de cada casa ficam agrupados e recebem MeshColliders para permitir colisão. O gerador também verifica o jogador, a câmera principal, a quantidade de modelos e os MeshColliders antes de salvar. Ainda é necessário executar no Unity Editor para confirmar visualmente escala, encaixe, colisões, materiais e iluminação antes de qualquer APK.
