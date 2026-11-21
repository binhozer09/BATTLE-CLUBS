# Integração de personagem humanoide — BATTLE CLUBS

Este documento define o contrato entre o modelo 3D humanoide, o Animator e os scripts de gameplay. Ele prepara a integração, mas não substitui o modelo, o rig nem os clipes de animação finais.

## Requisitos do modelo

1. Importar um modelo humanoide licenciado para uso no jogo (FBX ou formato equivalente).
2. Na aba Rig do importador Unity, selecionar **Humanoid** e criar/validar o Avatar.
3. Conferir escala aproximada de 1,8–2,0 m, orientação frontal e posição dos pés na origem.
4. O modelo visual deve ser filho do objeto raiz que contém CharacterController, ThirdPersonMotor e Combatant.
5. O Animator deve ficar no modelo visual. Desativar Apply Root Motion; o movimento no jogo é controlado pelo ThirdPersonMotor.

## Parâmetros do Animator

O componente `HumanoidAnimationDriver` procura estes parâmetros (nomes configuráveis no Inspector):

| Parâmetro | Tipo | Origem |
|---|---|---|
| MoveSpeed | Float | Velocidade horizontal calculada pelo ThirdPersonMotor |
| Grounded | Bool | Estado no chão do CharacterController |
| VerticalSpeed | Float | Velocidade vertical do motor |
| Downed | Bool | Estado Downed do Combatant |
| Eliminated | Bool | Estado Eliminated do Combatant |
| Attack | Trigger | Acionado por BatAttack quando o ataque começa |

O driver ignora parâmetros ausentes para permitir montar o Animator gradualmente. O parâmetro Attack é disparado diretamente pelo BatAttack, não pelo driver.

## Estados e clipes necessários

- Locomotion: Idle, Walk e Run; usar MoveSpeed para a mistura.
- Jump/Fall: usar Grounded e VerticalSpeed.
- Attack: clipe de golpe com evento de animação chamando `ApplyAttackHit` no BatAttack no quadro de contato.
- Downed: entrar quando Downed ficar verdadeiro.
- Revive/Recover: transição de retorno à locomoção quando Downed voltar a falso.
- Eliminated: estado final quando Eliminated ficar verdadeiro.

## Hierarquia recomendada

```text
Player_Root
├── CharacterController
├── ThirdPersonMotor
├── Combatant
├── BatAttack
├── HumanoidAnimationDriver
└── Visual_Humanoid
    └── Animator (Avatar Humanoid)
        └── Armature / SkinnedMeshRenderer
```

## Ferramentas de integração no Editor\n\nO projeto inclui `BATTLE CLUBS > Characters > Create Humanoid Animator Template`, que cria/atualiza `Assets/Animation/BC_Humanoid.controller` com os parâmetros e estados de transição usados pelo driver. Os estados ficam intencionalmente sem clipes: o projeto não deve fingir que animações finais existem quando os arquivos licenciados ainda não foram importados.\n\nDepois de importar o FBX e configurar o Avatar como Humanoid, selecione o `Player_Root` na Hierarchy ou o prefab e execute `BATTLE CLUBS > Characters > Validate Selected Humanoid`. A ferramenta verifica o Avatar e os componentes `ThirdPersonMotor` e `Combatant`, adiciona/configura o `HumanoidAnimationDriver` e desativa root motion. Ela não cria nem substitui o modelo 3D.\n\n## Critérios de validação no Unity

- Avatar válido e `Animator.isHuman == true`.
- Pés não atravessam o chão e o modelo acompanha a rotação do Player_Root.
- Idle/Walk/Run respondem ao joystick e ao sprint.
- Jump/Fall acompanham o movimento vertical.
- O evento de contato do ataque aplica apenas um golpe por ataque.
- Downed, revive e Eliminated mudam os estados de animação corretamente.
- Sem root motion duplicando o deslocamento do CharacterController.

**Situação atual:** o driver, telemetria do motor e ferramenta de criação/validação do Animator Controller estão no repositório. Ainda falta importar/selecionar o modelo humanoide, atribuir clipes finais e executar os testes no Unity Editor. Nenhuma dessas etapas é considerada validada até abrir e testar no Unity Editor.
