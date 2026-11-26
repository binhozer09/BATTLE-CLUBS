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
Depois de importar os 176 FBX e as 26 texturas no projeto Unity, abra o menu:

`BATTLE CLUBS → World → Build Medieval Village Scene`

O editor cria `Assets/Scenes/BattleClubs_MedievalVillage.unity` separada da cena principal, reutilizando os objetos de jogador/câmera/HUD da cena principal e substituindo apenas o cenário. A cena principal e os mapas antigos não são apagados nem sobrescritos.

A cena ainda precisa ser aberta no Unity Editor para validar escala, encaixe dos módulos, colisões e iluminação antes de qualquer APK.
