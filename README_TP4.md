# TP4 – Class Fight: Daniela vs Lara

Pelea por turnos con barra ATB entre dos compañeras de clase, en el patio de la escuela y con estilo Pokémon (cajas de info, cuadro de diálogo y menú 2x2). Cubre los requisitos de **Nota 4** y **Nota 7**.
UI, jerarquía, scripts, clases y variables están en inglés, y los scripts no tienen comentarios ni headers.

---

## 1. Puesta en marcha

1. **Instalá glTFast** para que Unity importe los modelos `.glb` con esqueleto y animaciones: *Window ▸ Package Manager ▸ + ▸ Install package by name…* ▸ `com.unity.cloud.gltfast`.
   Si no lo instalás, el builder usa los `.obj` (sin esqueleto): el combate funciona igual, pero las chicas solo se desplazan, sin mover brazos ni cabeza.
2. **Borrá los scripts viejos en español** (no rompen nada, pero quedan en español):
   `Assets/Scripts/CreatureView.cs`, `Datos.cs`, `HoverInfo.cs`, `HPBar.cs`, `PokemonBattleUI.cs`, `Assets/Scripts/Editor/PokemonSceneBuilder.cs`, la escena `Assets/Scenes/CombatePokemon.unity` y la carpeta `Assets/Material` (los materiales nuevos se crean en `Assets/Materials`).
3. Menú **TP RPG ▸ Build Battle Scene (data + scene)**. Crea los ScriptableObjects en `Assets/Data`, los materiales y la escena `Assets/Scenes/BattleScene.unity`, que queda primera en Build Settings.
4. **Play.** Cuando se llena la barra ATB de Daniela aparece el menú **PUNCH / SKILLS / BAG / GUARD**. Se usa con mouse o teclado/joystick.

> Si volvés a ejecutar el menú, la escena se reconstruye pero **los assets de `Assets/Data` no se pisan**: lo que edites en el Inspector se conserva.

Modelos: **Daniela** = `character-female-f`, **Lara** = `character-female-d` (los mismos que habías puesto, a escala 2).

---

## 2. Ataques y animaciones

Las animaciones combinan los clips del modelo de Kenney con poses procedurales sobre los huesos (`torso`, `head`, `arm-left`, `arm-right`).

| Personaje | Acción | MP | Recuperación | Efecto | Animación | Reacción |
|---|---|---|---|---|---|---|
| **Daniela** | Punch | 0 | normal | daño ATK | corre hasta Lara, gira el torso y tira la piña (`attack-melee-right`) | Hurt: se dobla hacia atrás |
| | Insult | 5 | rápida (−15) | daño WIT + DEF −25% | se acerca, la señala y le habla; vuelan palabras ("Clown!", "Copycat!") | Outrage: niega con la cabeza (`emote-no`) |
| | Hair Pull | 9 | **muy lenta (50)** | daño fuerte + sacudida | la agarra del pelo (`interact-right`) y le da 3 tirones mientras le inclina la cabeza | Pain: se agarra la cabeza (`crouch`) |
| | Shove | 6 | rápida (−25) | daño + **ATB −45** | empuja con las dos manos y avanza | Surprise: sale despedida (`fall`) con saltito |
| | Guard | 0 | rápida (−30) | daño ×0.5 | cruza los brazos delante de la cara | — |
| **Lara** | Punch | 0 | normal | daño ATK | igual que Daniela | Hurt |
| | Callout (escracheo) | 7 | lenta (20) | daño WIT + ATK −20% | levanta el celular, flash de foto y la señala | Outrage |
| | Defamation | 11 | **muy lenta (50)** | daño WIT + SPD −30% + **ATB −30** | se gira y le susurra a los demás ("psst…"), asiente y los rumores vuelan hacia Daniela | Outrage |
| | Spit | 4 | rápida (−20) | daño + DEF −15% | echa la cabeza atrás y escupe | Disgust: se da vuelta y se limpia la cara |

**Bag:** Chocolate Bar (+50 HP), Energy Drink (+25 MP y vuelve a actuar rápido), Lucky Scrunchie (ATK +50%), Gossip Note (DEF −40% / ATK −20% a Lara), Sticky Gum (SPD −40% y retrasa su ATB), Water Balloon (25 de daño fijo).
Al ganar, la ganadora festeja (`emote-yes` y saltos); la que pierde cae con `die`.

Los nombres de los clips se pueden cambiar en `UnitVisual` (idle, walk, run, die, victory) y en `ActionChoreography` (punch, reach, throw, nod).

---

## 3. 🔊 Gruñidos y sonidos de las chicas

```
Assets/Data/Voices/Daniela_Voice.asset
Assets/Data/Voices/Lara_Voice.asset
```

| Campo | Cuándo suena |
|---|---|
| **Attack Grunts** | Una entrada por ataque, ya creada con su nombre. Arrastrá uno o varios clips en `Clips`; si hay varios, suena uno al azar. |
| **Fallback Attack Grunts** | Para cualquier ataque sin clips propios. |
| **Reactions ▸ Hurt / Pain / Surprise / Outrage / Disgust** | Al recibir piña / tirón de pelo / empujón o globo / insulto, escracheo o difamación / escupitajo o chicle. Surprise también suena cuando la agarran del pelo. |
| **Other ▸ Guard / Item Use / Turn Ready / Knocked Out / Victory** | Al defenderse, usar un objeto, llenarse la barra (Daniela), caer K.O. y ganar. |
| **Volume / Pitch Variation** | Volumen y variación de tono. |

Lo que quede vacío no suena. Los efectos generales (golpe, flash, escupitajo…) los genera `ProceduralAudio`.

---

## 4. Jerarquía

```
BattleScene
├── Directional Light
├── Main Camera            Camera, AudioListener, PhysicsRaycaster, CameraShake
├── Systems
│   ├── BattleManager      BattleManager, EnemyAI, ActionChoreography
│   └── Feedback           FeedbackManager, ProceduralAudio, AudioSource
├── Schoolyard             Ground, PlayerPlatform, RivalPlatform, Background ▸ (Hill1, Hill2, School, Tree_1..5)
├── Fighters
│   ├── PlayerSide ▸ Daniela   CharacterAnimator, UnitVisual, UnitVoice, AudioSource, BattleUnit, BoxCollider ▸ Model (GLB)
│   └── EnemySide  ▸ Lara      (igual, con Phone en arm-right)
├── Canvas                 BattleUI
│   ├── RivalBox / PlayerBox   nombre, buffs, HP, MP y ATB
│   ├── ATBModeText, DialogBox, CommandPanel, SkillsPanel, BagPanel, TargetPanel
│   ├── ScreenFlash, PopupLayer
│   └── ResultPanel
└── EventSystem
```

---

## 5. Requisitos

### Nota 4
| Requisito | Dónde |
|---|---|
| Turnos secuenciales | `BattleManager` (cola de acciones) |
| Personaje con modelo 3D y animaciones | Modelos GLB de Kenney + `CharacterAnimator` (clips) + `UnitVisual` (poses) + `ActionChoreography` |
| Enemigo con decisiones | `EnemyAI` (Lara elige ataque según MP y probabilidad) |
| Ataque básico + habilidad con maná | Punch + 3 habilidades por chica con `manaCost` |
| Vida y maná | `BattleUnit` |
| Consumibles | Chocolate Bar (HP), Energy Drink (MP) |
| UI con vida, maná, participantes y acciones | `BattleUI` + `UnitStatusRow` (cajas), menú 2x2, Skills, Bag |
| Escena de combate | Patio de la escuela, Daniela vs Lara |
| Victoria/derrota | `BattleManager.CheckBattleEnd` + `ResultPanel` |

### Nota 7
| Requisito | Dónde |
|---|---|
| Barra ATB según velocidad | `BattleUnit.AdvanceATB` + barra ATB en cada caja |
| Costos de recuperación / acelerar y retrasar | `recoveryCost` (Hair Pull y Defamation 50, Shove −25, Energy Drink −40) + `AlterATB` (Shove, Defamation, Sticky Gum) |
| Habilidades diferenciadas | 6 habilidades con distinto MP, recuperación, efectos y animación |
| Buffs y debuffs con objetos | Lucky Scrunchie, Gossip Note, Sticky Gum |
| ScriptableObjects | `CharacterData`, `SkillData`, `ItemData`, `InventoryData`, `BattleConfig`, `CharacterVoice` |
| Feedback visual y sonoro | `FeedbackManager` (números, destellos, palabras, flash, proyectiles, sacudida) + `ProceduralAudio` + `UnitVoice` |

---

## 6. Scripts nuevos

```
Assets/Scripts
├── Data      Enums, EffectEntry, ActionData, SkillData, ItemData, CharacterData, CharacterVoice, InventoryData, BattleConfig
├── Combat    BattleManager, BattleUnit, CharacterAnimator, UnitVisual, ActionChoreography, ActionResolver, EnemyAI, Inventory, StatModifier, UnitVoice
├── Feedback  FeedbackManager, ProceduralAudio, CameraShake
├── UI        BattleUI, UnitStatusRow, UIBar, ListEntryUI
└── Editor    BattleSceneBuilder (menú "TP RPG")
```
