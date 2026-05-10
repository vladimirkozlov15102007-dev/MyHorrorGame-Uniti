# Old Amber Factory / Заброшенный завод Янтарь

Реалистичный 3D survival-horror / tactical shooter на **Unity 6.3 + HDRP**.

Игрок просыпается ночью на заброшенном промышленном объекте 80-х годов. Цель: выжить, уничтожить 10 скелетов-лучников, добраться до жёлтого грузовика, запустить его и уехать.

---

## Честно о границах задачи

Этот репозиторий содержит **полный игровой код** (~30 C#-файлов: игрок, оружие, AI, урон, звук, финал, UI, HDRP-хуки) — всю «программную» часть сценария.

То, что **в принципе невозможно** сгенерировать текстом и требует отдельных художников:

| Слой | Что нужно | Где брать |
|------|-----------|-----------|
| 3D-модели скелетов (кости, ткань, броня) | High-poly + retopo + UV + rigging (Blender / ZBrush / Maya) | Quixel Megascans «Skeleton», Sketchfab, Unity Asset Store («Character Skeleton», «Horror Skeleton Enemies PBR»), или собственный моделлер |
| PBR-текстуры скелетов и завода (4K–8K) | Albedo, Normal, Roughness, AO, Displacement, SSS | Quixel Megascans (бесплатно внутри Unity), Poliigon, Textures.com |
| Motion-matching анимации скелетов | Mocap или ручная анимация | Kinetix, Rokoko, Mixamo (бесплатно для базы), Unity Asset Store — «HQ Skeleton Archer Animations» |
| Photogrammetry-модели завода (стены, пол, трубы, конвейеры, КАМАЗ) | Мегаскан | Quixel Megascans (Industrial, Abandoned), Sketchfab |
| Звук (шаги, выстрелы, стрелы, ambient, vo) | Библиотеки | Soundly, FreeSound, Unity Asset Store («Horror SFX Pack») |
| Слои адаптивной музыки (5 штук) | Композитор | FMOD / Wwise, либо 5 стемов WAV от композитора |

Когда ассеты появятся — **код уже готов их принять**: все ссылки (`[SerializeField]`) ждут перетаскивания в инспекторе.

---

## Структура проекта

```
Assets/OldAmberFactory/
├── Input/
│   └── PlayerControls.inputactions        Новый Input System: WASD, Shift, Space, Ctrl, E, ЛКМ, ПКМ, R
├── Scripts/
│   ├── Core/                              IInteractable, ServiceLocator, GameLayers / GameTags
│   ├── Damage/                            HealthSystem, HitBox, DamageInfo, DamageTable (30/20/10), RagdollController
│   ├── Player/                            FPSController, InputRouter, HeadBob, WeaponSway, PlayerInteractor, PlayerStamina
│   ├── Weapons/                           WeaponBase, Pistol (recoil, ADS, reload, spread, FX)
│   ├── AI/
│   │   ├── BehaviorTree/                  BTNode, Selector/Sequence/Inverter, UtilitySelector, Blackboard, BBKeys
│   │   ├── Perception.cs                  Зрение (FOV-конус + LOS) + слух через NoiseEventBus
│   │   ├── CoverPoint.cs                  Сеть укрытий с reservations
│   │   ├── PlayerBehaviorAnalyzer.cs      Адаптивный анализ стиля игры
│   │   ├── GroupAICoordinator.cs          Координатор: распределяет роли, обменивается интелом
│   │   ├── SkeletonArcher.cs              Состояния: patrol/investigate/ambush/assault/flank/suppress/retreat/melee
│   │   └── Arrow.cs                       Баллистическая стрела с пробитием
│   ├── Throwable/                         ThrowController + ThrowableItem (бутылки/трубы/арматура — шум для ИИ)
│   ├── Audio/                             NoiseEventBus, AdaptiveMusic (5 слоёв), SurfaceFootsteps, SpatialAudioManager
│   ├── World/                             GameManager, WaveSpawner, YellowTruck, KeyItem, PowerSwitch, SecurityRoom, SecurityCamera
│   ├── UI/                                HUDView, DirectionalDamageIndicator, InteractPromptUI
│   ├── Rendering/                         HDRPBootstrap, HDRPVolumeSetupHint, CameraShaker
│   └── Editor/                            SceneBootstrap (меню «Old Amber Factory > Bootstrap Empty Scene»)
```

---

## Как это работает (архитектурно)

### Стек систем

```
                       ┌─────────────────────┐
                       │   ServiceLocator    │   (GameManager, GroupAICoordinator)
                       └──────────┬──────────┘
                                  │
      ┌──────────────┬────────────┼────────────┬──────────────┐
      │              │            │            │              │
┌─────▼─────┐ ┌──────▼────┐ ┌─────▼────┐ ┌─────▼─────┐ ┌─────▼──────┐
│  Player   │ │ Weapons   │ │   AI     │ │  Audio    │ │   World    │
│  FSM+IK   │ │ Pistol    │ │ BT+UAI   │ │ Adaptive  │ │ Truck/UI   │
└─────┬─────┘ └──────┬────┘ └────┬─────┘ └─────┬─────┘ └────────────┘
      │              │           │             │
      └──────────────┴─────┬─────┴─────────────┘
                           │
                  ┌────────▼────────┐
                  │  NoiseEventBus  │  (шаги, выстрелы, броски)
                  └─────────────────┘
```

### AI — adaptive, group, behavior tree

Каждый скелет владеет собственным Behaviour Tree:

```
Selector
├── Sequence: перешёл в Combat
│   ├── Condition: вижу игрока или видел <4 сек назад
│   └── UtilitySelector
│        ├── Melee (если в радиусе удара)
│        ├── FlankAdvance (если роль FlankLeft/FlankRight)
│        ├── SuppressiveFire (если роль Suppressor)
│        ├── Assault (если роль Assault)
│        └── RetreatToCover (если HP < 35%)
├── Sequence: Ambush (если роль Ambusher)
├── Sequence: Investigate (если слышу шум <10 сек назад)
└── Patrol
```

Роли назначает `GroupAICoordinator` каждые 0.5 сек, используя классификацию из `PlayerBehaviorAnalyzer`:

| Стиль игрока | Что назначается |
|---|---|
| **CoverCamper** — много укрытий | 1 Suppressor + остальные Flankers |
| **Runner** — постоянно бежит | Assaulter + FlankRight (предсказывают маршрут) |
| **PositionLocked** — сидит в одном месте | Suppressor + Ambushers |
| **Aggressive** — рашит | Assault + Suppressors держат дистанцию |
| **Fixed_Shooter** — стреляет с одной точки | 2 Suppressor + Flankers |

Все скелеты **обмениваются позицией игрока** через `GroupAICoordinator.ReportPlayer(...)`. Когда один увидел — остальные получают `LastKnownPos` на свой blackboard.

### Damage model

Строго по дизайн-документу:

| Зона | Базовый урон |
|---|---|
| Голова | **30** |
| Торс | **20** |
| Ноги | **10** |

Реализация: каждая кость скелета имеет `HitBox` c `BodyPartType`. При попадании используется `DamageTable.ForBodyPart(part)` × `Weapon.baseDamage / 20`. После смерти `RagdollController.SetRagdoll(true)` переключает анимированный меш в физический ragdoll; импульс пули применяется к ближайшей кости.

### Noise / AI hearing

```csharp
NoiseEventBus.Broadcast(position, radius, NoiseSource.Gunshot, emitter);
```

- Шаги игрока: 4 м (idle) / 12 м (бег) / 1.5 м (присед)
- Выстрел: 35 м
- Бросок предмета об стену: 10 м
- Игра скелетов в бою: 8 м

Каждый `Perception` подписывается на шину и проверяет расстояние от себя до источника < min(hearing range, event radius).

### Адаптивная музыка (5 слоёв)

Все 5 стемов проигрываются одновременно на громкости 0. `AdaptiveMusic` каждые 0.25 сек выбирает один целевой слой и кросс-фейдит:

```
Calm     → ambient pad
Tension  → low drones
Detected → ostinato пульсация
Combat   → перкуссия + басс
Critical → полный mix
```

### Финал

```
[Power Switch (Admin)] ──► SetPower(true)
                                 │
[Key Item (Warehouse)] ──► InsertKey()
                                 │
                          Truck.Interact()  
                                 │
                  StartEngine() ──► GameManager.OnTruckEngineStarted()
                                 │
                       WaveSpawner.SpawnWave(0)  ← финальная волна
                                 │
              игрок Interact повторно  ── BeginEscape
                                 │
                         DriveAwayRoutine  ── фабрика в тумане
                                 │
                              Victory
```

---

## Что нужно сделать в Unity Editor (ручная работа)

### 1. Проект

1. Открыть папку в Unity **6.3** (Hub > Open).
2. Editor подтянет пакеты из `Packages/manifest.json` (HDRP 17, Cinemachine, Input System, AI Navigation, Burst, VFX Graph).
3. `Window > Rendering > Render Pipeline Wizard > HDRP > Fix All`.
4. `Project Settings > Player > Color Space = Linear`.
5. `Project Settings > Graphics`: назначить `HDRenderPipelineAsset` (создастся через Wizard).

### 2. Тэги и слои

TagManager.asset уже содержит кастомные слои (`Player=6, Enemy=7, Projectile=8, Cover=9, Interactable=10, Throwable=11, EnemyRagdoll=12`) и теги. При первом открытии Unity валидирует.

### 3. Сцена

`Menu: Old Amber Factory > Bootstrap Empty Scene` — создаст игрока, менеджеров и камеру.
Дальше артист:

- **Геометрия:** выложить 5 зон (Admin / Cex / Warehouse / Tunnels / Outdoor). Использовать Megascans + ProBuilder для черновой проброски, заменять на фотограмметрию.
- **NavMesh:** `AI Navigation > Bake` — скелетам нужен nav на всех полах и рампах.
- **Cover Points:** расставить пустые GameObject'ы с `CoverPoint` у каждого укрытия. Направление facing — туда, откуда прилетит огонь.
- **Skeleton Prefab:** собрать из модели + `SkeletonArcher` + `HealthSystem` (100 HP) + `RagdollController` + `Perception`. Каждая кость — отдельный Collider + `HitBox` с `BodyPartType` и ссылкой на тот же Rigidbody.
- **Пистолет:** ссылки на muzzle flash VFX, shell eject VFX, `AudioClip`.
- **Yellow Truck:** модель, ссылки на `_engineStartClip`, `_engineLoopClip`, `_failClickClip`; назначить `_escapeTargetPoint` (пустой GO за воротами).
- **Security Room:** несколько `Camera` с `SecurityCamera` + RenderTexture на каждую; ссылки в `SecurityRoom._cameras`; экран CRT — `MeshRenderer` с материалом, который принимает `_BaseMap`.
- **Global Volume:** создать по гиду из `HDRPVolumeSetupHint.cs` — там перечислены все Overrides и рекомендованные значения.

### 4. Освещение

Ночь + кинематографика:
- Directional Light (moon): 5800K, 0.35 lux, angular diameter 0.7
- Точечные источники: флуоресцентные лампы с моргающим `Light.intensity` анимацией
- Глобальный Volume: Volumetric Fog density 0.6, Physically Based Sky при выходе на улицу
- SSR + SSGI + SSAO включены; Ray Tracing — только если есть RT-железо

Outdoor зона (финал):
- Fog density снижен до 0.35 (ТЗ — «лунный свет, видно всё»)
- Отдельный Volume с профилем `OutdoorProfile` переключается триггером при входе на улицу
- Яркая луна + Physically Based Sky

### 5. Input

Input System actions уже лежат в `Assets/OldAmberFactory/Input/PlayerControls.inputactions`. Передать asset в `InputRouter._asset`.

---

## Рекомендуемые ассеты (быстрый старт)

Бесплатные / пробные:
- **Quixel Megascans** (бесплатно в Unity с 2024) — photogrammetry меши и 4K-материалы: «Industrial Decay», «Abandoned Factory»
- **Mixamo** (adobe.com/mixamo) — базовая анимационная библиотека для скелета (walk, idle, fire_bow, death, melee)
- **Kenney Game Assets** — пропсы (бочки, ящики, трубы) для прототипа
- **FreeSound.org** — ambient и footsteps

Платные (рекомендуемые качественные):
- Unity Asset Store: «Horror FPS Kit» (ядро атмосферы) — свой отредактировать поверх
- «Skeleton Warrior» / «Undead Archer» — готовый riggable скелет с анимациями
- «Soviet Cargo Truck» / «KAMAZ» — для жёлтой машины
- «FMOD Studio» + «Wwise» — для полноценной адаптивной музыки (код оставлен под AudioSource; при переходе на FMOD подменить `AudioSource` в `AdaptiveMusic.Layer`)

---

## Карта управления (как в ТЗ)

| Кнопка | Действие |
|---|---|
| WASD | Движение |
| Shift | Бег (тратит stamina) |
| Space | Прыжок (тратит 15 stamina) |
| Ctrl | Toggle crouch |
| E | Взаимодействие (дверь, пикап, машина, камеры) |
| ЛКМ | Стрельба / зарядка броска |
| ПКМ | Прицеливание (ADS) |
| R | Перезарядка |

---

## Производительность и NFR

- Все AI-перцепции работают **не чаще 60 Гц**; не делают лишних `FindObjects*` каждый кадр (исключение — `AdaptiveMusic`, с интервалом 0.25 сек).
- Группа координирует цели каждые **500 мс**.
- NavMeshAgent.velocity используется вместо отдельной скорости.
- Стрелы — ContinuousDynamic collision detection, но живут максимум 14 сек.
- Ragdoll'ы — `DestroyDelay` из сцены; рекомендуется Object Pool для стрел (ближайший шаг).

---

## Что осталось вне рамок этого PR (осознанные ограничения)

- **Нет 3D-моделей.** Код ждёт префабов; пользователь их собирает из купленных ассетов.
- **Нет HDRP `.asset`-файлов RenderPipeline/VolumeProfile.** Их Unity сериализует сама при первом открытии и Wizard > Fix All. Алгоритм настройки — в `HDRPVolumeSetupHint.cs`.
- **Нет FMOD/Wwise.** Используется обычный `AudioSource` — достаточно для качественного spatial audio, но под настоящую adaptive music рекомендую заменить на FMOD с 5 паттернами.
- **Нет тестов.** Явно не запрашивались.

---

## Быстрая проверка, что код компилируется

1. Открыть проект в Unity 6.3.6f1.
2. Дождаться импорта пакетов.
3. Консоль должна быть зелёной. Если красные ошибки — обычно из-за отсутствия HDRP Render Pipeline Asset → `Rendering > HDRP Wizard > Fix All`.
4. Меню `Old Amber Factory > Bootstrap Empty Scene` → получите игрока и менеджеров в сцене.
