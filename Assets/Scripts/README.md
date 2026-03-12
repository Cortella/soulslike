# Soulslike - Estrutura do Projeto

## Como Configurar a Cena

1. Abra o Unity e carregue o projeto
2. No menu superior, clique em **Soulslike > Configurar Cena Completa**
3. Aguarde a geração automática
4. Pressione **Play** para testar!

## Controles

### Teclado + Mouse
| Ação            | Tecla              |
|-----------------|---------------------|
| Mover           | WASD                |
| Correr          | Left Shift          |
| Dodge/Roll      | Espaço              |
| Ataque Leve     | Click Esquerdo      |
| Ataque Pesado   | R                   |
| Bloquear/Parry  | Click Direito       |
| Lock-On         | Tab                 |
| Interagir       | E                   |
| Usar Item       | X                   |
| Pausar          | Escape              |
| Câmera          | Mouse               |
| Zoom            | Scroll do Mouse     |

### Gamepad
| Ação            | Botão               |
|-----------------|---------------------|
| Mover           | Stick Esquerdo      |
| Correr          | L3 (press stick)    |
| Dodge/Roll      | A / X               |
| Ataque Leve     | RB / R1             |
| Ataque Pesado   | RT / R2             |
| Bloquear        | LB / L1             |
| Lock-On         | R3 (press stick)    |
| Interagir       | X / □               |
| Usar Item       | Y / △               |

## Estrutura de Pastas

```
Assets/
├── Scripts/
│   ├── Camera/
│   │   └── ThirdPersonCamera.cs     - Câmera 3ª pessoa com lock-on
│   ├── Combat/
│   │   ├── DamageSystem.cs           - Sistema de cálculo de dano
│   │   ├── HitBox.cs                 - Hitbox para armas
│   │   └── IDamageable.cs            - Interface de dano
│   ├── Core/
│   │   └── GameManager.cs            - Gerenciador de estado do jogo
│   ├── Editor/
│   │   └── SoulslikeSceneSetup.cs    - Setup automático da cena
│   ├── Enemy/
│   │   ├── BossEnemy.cs              - Boss com 2 fases
│   │   ├── EnemyAI.cs                - IA: patrol, chase, attack
│   │   └── EnemyStats.cs             - Atributos do inimigo
│   ├── Player/
│   │   ├── InputHandler.cs           - Bridge Input System -> Scripts
│   │   ├── PlayerCombat.cs           - Ataque, bloqueio, parry
│   │   ├── PlayerController.cs       - Movimento, dodge, lock-on
│   │   └── PlayerStats.cs            - HP, Stamina, Souls
│   ├── UI/
│   │   ├── EnemyHealthBar.cs         - HP flutuante dos inimigos
│   │   └── PlayerHUD.cs              - Barras de vida/stamina, souls
│   └── World/
│       ├── Bonfire.cs                - Ponto de descanso
│       ├── DungeonGenerator.cs       - Gerador procedural de mapa
│       ├── FogGate.cs                - Portão de névoa (boss gate)
│       └── TorchFlicker.cs           - Efeito de chama nas tochas
├── InputActions/
│   └── SoulslikeInputActions.inputactions  - Mapeamento de controles
```

## Sistemas Implementados

### Player
- Movimento em terceira pessoa com rotação baseada na câmera
- Sprint com consumo de stamina
- Dodge roll com i-frames (invulnerabilidade)
- Ataque leve e pesado
- Bloqueio com redução de dano (70%)
- Parry (timing window de 0.2s)
- Lock-on em inimigos com Tab

### Inimigos
- IA com estados: Idle, Patrol, Chase, Attack, Stagger
- Detecção por cone de visão + distância
- NavMesh para navegação
- Boss com 2 fases (muda comportamento em 50% HP)

### Mapa
- Geração procedural de dungeon
- Salão central (Hub) com bonfire
- Corredores conectando salas
- Salas com pilares decorativos
- Arena de boss circular
- Fog Gate na entrada do boss
- Tochas com iluminação dinâmica
- Atmosfera escura estilo Dark Souls

### UI
- Barra de HP (vermelho)
- Barra de Stamina (verde)
- Contador de Souls
- Tela "YOU DIED"
- Barra de HP flutuante nos inimigos

## Próximos Passos Sugeridos

1. **Animações** - Adicionar Animator Controller com animações de ataque, roll, idle, run
2. **Estus Flask** - Sistema de cura com charges limitados
3. **Inventário** - Equipamento de armas e armaduras
4. **NPCs** - Diálogos e quests
5. **SFX / Música** - Áudio ambiente sombrio + música de boss
6. **VFX** - Partículas para fogo, hit effects, magia
7. **Save System** - Salvar/carregar progresso
8. **Mais inimigos** - Variações com diferentes comportamentos
9. **Boss patterns** - Combos, AOE, ataques especiais
10. **Level Design** - Atalhos, segredos, itens escondidos
