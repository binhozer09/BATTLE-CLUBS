# BATTLE CLUBS — Painel de produção

Este painel acompanha a produção do jogo original. A compilação do antigo protótipo não é uma meta de entrega.

**Projeto Unity:** 2022.3.62f1  
**Repositório:** https://github.com/binhozer09/BATTLE-CLUBS  
**Branch de produção:** `production/3d-art-foundation`

## Etapas e situação

| Frente | Situação | Evidência / próximo passo |
|---|---|---|
| Base técnica Unity | Em ajuste | Habilitar módulos internos de Física e Animação no manifest |
| Materiais e texturas originais | Em desenvolvimento | Gerador procedural de tijolo, reboco, concreto, asfalto e piso da quadra; falta integrar e renderizar na cena |
| Cenário 3D da comunidade | A iniciar após materiais | Evoluir o blockout para arte com relevo, escadarias, becos, postes, fios, lajes e quadra |
| Personagem 3D | Pendente | Escolher/criar modelo humanoide com rig; integrar malha, roupas e animações |
| Uniformes | Pendente | Criar sistema de kits; usar marcas e escudos oficiais somente com autorização |
| HUD e controles Android | Base de teste existente | Validar novamente após integração do personagem final |
| Multiplayer | Pendente | Não iniciado |

## Registro de trabalho

### 21/11/2026 — Fundação da produção visual
- Criada a branch `production/3d-art-foundation`, separando a produção original da linha principal.
- Adicionado o primeiro gerador de texturas tileáveis originais: tijolo aparente, reboco pintado, concreto, asfalto e piso de quadra.
- Próxima ação técnica: integrar os materiais ao gerador do cenário e corrigir as dependências de Física/Animação.
- Ainda não há personagem final, cena renderizada, APK de produção ou validação visual no Unity Editor.

## Como acompanhar
- Commits da produção: https://github.com/binhozer09/BATTLE-CLUBS/commits/production/3d-art-foundation
- Arquivos da produção: https://github.com/binhozer09/BATTLE-CLUBS/tree/production/3d-art-foundation
- Compilações (somente quando uma build de validação for necessária): https://github.com/binhozer09/BATTLE-CLUBS/actions

**Regra de transparência:** uma tarefa só muda para concluída quando o arquivo foi gravado e a integração/teste correspondente foi confirmado. Plano não é asset pronto; código de geração não é captura de jogo.
