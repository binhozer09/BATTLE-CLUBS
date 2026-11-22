# Integração de modelo humanoide — BATTLE CLUBS

## Objetivo

Substituir o visual provisório criado pela cena de teste por um modelo 3D Humanoid licenciado, mantendo os componentes de gameplay no objeto raiz do jogador.

## Requisitos do asset

- Modelo FBX/prefab com Animator e Avatar Humanoid válido.
- Importador do FBX configurado em **Rig > Animation Type > Humanoid**.
- Escala aproximada de 1,8–2,0 m, pés próximos à origem e personagem orientado para +Z.
- Clipes licenciados de Idle, Walk/Run, Jump/Fall, Attack, Downed e Eliminated.

## Integração no Editor

1. Abra a cena e localize o objeto Player_Local na Hierarchy.
2. Importe o modelo humanoide para Assets/Characters/ (ou pasta equivalente).
3. Selecione o Player_Local na Hierarchy e o modelo FBX/prefab na janela Project (Ctrl/Command para seleção múltipla).
4. Execute **BATTLE CLUBS > Characters > Integrate Selected Model Into Player Root**.
5. O assistente valida o Avatar, instancia o modelo como Visual_Humanoid, mantém os componentes de gameplay no Player_Root, conecta o Animator ao HumanoidAnimationDriver e ao BatAttack, adiciona ClubKitApplier e remove somente os objetos provisórios Player_Body_Visual e Player_Head_Visual.
6. Salve a cena. Abra **BATTLE CLUBS > Characters > Create Humanoid Animator Template** se o controller ainda não existir.
7. Atribua os clipes licenciados aos estados e configure o evento ApplyAttackHit no quadro de contato do ataque.

## Limites e segurança

- O assistente não baixa nem inventa modelos, uniformes oficiais ou animações.
- O asset selecionado deve ter licença compatível com distribuição comercial/uso no jogo.
- O assistente não apaga outros filhos do jogador, colliders ou componentes de gameplay.
- O modelo é integrado na cena atual; transforme-o em prefab somente depois de validar escala, rig e materiais.
- Uniformes reais dependem de malha/material e texturas adequadas; ClubKitApplier fornece a base de aplicação por equipe, não cria escudos oficiais.

## Validação obrigatória

- Avatar isHuman e isValid.
- Personagem acompanha movimento e rotação do Player_Root.
- Sem deslocamento duplicado por Root Motion.
- Idle/Walk/Run/Jump/Fall/Attack/Downed/Eliminated com clipes atribuídos.
- O ataque aplica somente um impacto por animação.
- Os cinco kits mudam os renderers corretos sem alterar materiais compartilhados.
- Testar no Unity Editor e em dispositivo Android; integração de código não equivale a teste executado.
