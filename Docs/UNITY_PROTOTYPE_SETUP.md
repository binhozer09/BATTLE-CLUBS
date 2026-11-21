# Unity — montagem do primeiro protótipo

## Estado
Scripts iniciais de movimentação, câmera, ataque e regras de equipe foram adicionados. Eles ainda não foram compilados nem testados no Unity nesta etapa. A lógica de combate é local/provisória e não é segura para partidas online.

## Criar a cena
1. Crie um projeto 3D na versão Unity LTS.
2. Crie um objeto `Player` com `CharacterController`, um modelo provisório e o script `ThirdPersonMotor`.
3. Crie um objeto filho `HitOrigin` na altura/posição do taco e conecte ao campo do `BatAttack`.
4. Adicione `Combatant` e `BatAttack` ao jogador.
5. Crie uma câmera com `ThirdPersonCameraRig`, atribua o jogador como Target.
6. Para Android, crie um Canvas com joystick virtual e botões; conecte eventos aos métodos públicos `SetMoveInput`, `SetSprint`, `QueueJump` e `Attack`.
7. Crie um objeto `MatchRules` com `TeamMatchRules` e configure cinco equipes no Inspector, com quatro jogadores cada.

## Controles provisórios
- PC: WASD/setas, Shift para correr, Espaço para pular, botão de ataque ligado ao `BatAttack.Attack()`.
- Android: joystick e botões de UI ligados aos métodos públicos listados acima.

## Próxima integração obrigatória
- Sistema de input unificado e sensibilidade configurável.
- Animações de movimento/ataque/queda/reanimação.
- Reanimação com duração, distância e interrupção.
- Rede autoritativa: servidor valida posição, alcance, cooldown, dano, queda, reanimação, equipes e pontuação.
- Spawn e matchmaking de 20 jogadores.
- Modelos, cenário e kits licenciados ou assets originais.
- Testes de build Android e PC.
