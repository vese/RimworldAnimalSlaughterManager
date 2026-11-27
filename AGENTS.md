# AGENTS.md — справка для агента

Репозиторий `vese/RimworldAnimalSlaughterManager`: мод **Animal Slaughter Manager** для **RimWorld 1.6**.

```
About/                         — метаданные мода
1.6/Assemblies/                — собранные DLL (мод, Core, ATS-интеграция)
Languages/                     — переводы
Defs/                          — PawnColumnDef (колонка защиты)
Textures/                      — иконки (щит, шестерёнка)
Source/AnimalSlaughterManager/                  — точки входа: ASMMod, ASMSettings,
                                                 HarmonyInit (применяет патчи Core), GlobalSuppressions
Source/AnimalSlaughterManager.Core/            — ядро, фичи: Slaughter/ (движок забоя: MapComp,
                                                 ListBuilder, Patches/, Windows/, Columns/),
                                                 Settings/ (BasePreferenceSettings/ (база+панель
                                                 предпочтений, Common-энумы),
                                                 GlobalSettings/, KindSettings/ (KindPrioritySettings/
                                                 — Rules/, Validation/, Presets/, Alerts/;
                                                 KindPreferenceSettings/; SpecialSettings/ — вкладка
                                                 «Особые правила» + контракты секций; Interfaces/,
                                                 Presets/), Dialog/ (окно вида + Interfaces/ контракта
                                                 таба, каркас вкладок, GeneralTab/, PrioritiesTab/)),
                                                 Presets/ (IO + браузеры), Integrations/ (DI-реестр),
                                                 Legacy/, Ui/ (Dropdown, ReorderableList), Translations/ (ASMKeys)
Source/AnimalSlaughterManager.Integrations.AnimalTraitsSystem/ — ATS-интеграция (ссылается на Core)
```

Core: верхний уровень — фичи, внутри фичи — по функциональности (настройки, пресеты, окна,
UI-элементы фичи). Интерфейсы фичи — в `Interfaces/` соответствующего уровня; контракт лежит
в `Interfaces/` фичи-потребителя (`Slaughter/Interfaces/` — движок, `KindSettings/Interfaces/` —
данные интеграций в настройках вида, `KindSettings/SpecialSettings/Interfaces/` — секции вкладки
«Особые правила», `Settings/Dialog/Interfaces/` — табы окна вида,
`KindSettings/KindPrioritySettings/Interfaces/` — меню правил,
`Legacy/Interfaces/`). Пресеты — отдельные модели per скоуп (`AllPresetDto`/`KindPresetDto`/`ListPresetDto`): To/FromPreset у владельцев (ASM_MapComp — полный, KindSettings — вида, KindPrioritySettings и специальные настройки — списков); репозиторий пишет DTO сериализатором текущей версии (`IVersionedPresetDto.version`) и читает по корню файла: новые корни — текущий формат, старые корни — машина чтения v1 (`PresetSerializer.ReadPreset` + Legacy) с миграцией в модели. Смену модели сопровождать bump `PresetFormat.CurrentVersion` + шагом миграции.
Устаревшее — в `Legacy/` по фичам, всё внутри помечено `[Obsolete]` и изолировано за фасадами `LegacyPresets`/`LegacyListPresets`/`LegacyKindPriority`: снаружи — вызовы в Core (`PresetSerializer`, `PresetRepository.ListPresets`, `KindPrioritySettings.ExposeData`) и реализация `ILegacyConditionConverter` в ATS; удаление папки = убрать эти точки (+ ATS-`LegacyTraitConditions`). Патчи лежат
в Core, применяет их HarmonyInit главного проекта (`PatchAll()` без аргументов патчит только свою
сборку).

## Сборка

Мод — SDK-style .csproj, `net472` (RimWorld 1.6 = Unity 2022.3 / Mono), ссылается на API через NuGet **`Krafs.Rimworld.Ref` 1.6.\*** (reference-only, игра для сборки не нужна) и **`Lib.Harmony` 2.\*** (только компиляция; сама библиотека уже в игре).

```bash
dotnet build Source/AnimalSlaughterManager/AnimalSlaughterManager.csproj -c Release   # три DLL (сборка главного тянет все проекты)
```

Собранные DLL коммитятся (в `.gitignore` исключены только `**/bin`, `**/obj`, `**/Assemblies/*.pdb|*.xml`). **Перед коммитом — собрать.**

## Проверенный RimWorld 1.6 API (декомпилировано из Assembly-CSharp)

Декомпилированный исходник: `C:\Users\Administrator\RimWorldDecompiled` (локальный клон `Chillu1/RimWorldDecompiled`, ветка `master`). Грепать локально для RimWorld API.

- **`Verse.AutoSlaughterManager`**: `configs`, `AnimalsToSlaughter` (патчится Harmony), `CanAutoSlaughterNow`/`CanEverAutoSlaughter` (ВНИМАНИЕ: `CanEverAutoSlaughter` проверяет только `HomeFaction==OfPlayer && !Dryad` — **не** проверяет Animal; гизмо надо гейтить `RaceProps.Animal`).
- **`Verse.Window.InnerWindowOnGUI`**: `draggable=true` → `GUI.DragWindow()` без аргументов = окно двигается с любого клика. `draggable=false` + переопределённый `LateWindowOnGUI(Rect)` с `GUI.DragWindow(specificRects)` — точечные зоны перетаскивания. Non-draggable окно: невостребованный `MouseDown` гасится `Event.current.Use()`.
- **`Verse.TabDrawer.DrawTabs`**: рисует табы на 32px ВЫШЕ baseRect (висят с верхней кромки). Под них резервируется полоса над контентом.
- **`Verse.ReorderableWidget`**: `NewGroup` только на Repaint (кэшировать groupID), `Reorderable` — на каждой строке. В non-draggable окне реордер работает без `ClaimDragHandle`.
- **`Verse.TexButton`**: `Search`, `Delete`, `CloseXSmall`, `Copy`, `Paste`, `Suspend`, `Infinity`, `Info` (подтверждено из локального клопа). Нет Save/галочки.
- **`Verse.Widgets`**: `ThingIcon(Rect, ThingDef)`, `ButtonImageWithBG`, `ButtonImage(Rect, Texture2D)`, `ButtonInvisible`, `ButtonText(Rect,string,bool active)` (active=false = серая), `DrawHighlightIfMouseover`, `DrawLineHorizontal/Vertical`, `CheckboxDraw(x,y,active,disabled,size)`, `DrawAltRect`, `ButtonImageFitted`, `DrawBox`, `DrawTextureFast`.
- **`RimWorld.Dialog_MessageBox(text, buttonA, actionA, buttonB)`** — confirm с Yes/No.
- **`RimWorld.Pawn_TrainingTracker.HasLearned(TrainableDef)`** — натренирован ли навык. `TrainableUtility.TrainableDefsInListOrder` — все доступные TrainableDef.
- **`RimWorld.HediffDef.makesSickThought`** — true для болезней (грипп, чума и т.д.).
- **`Verse.Hediff_MissingPart`** — отсутствующая конечность.
- **`Verse.GUI.enabled`** — блокирует все контролы (drawn grey + не кликабельны).

## Animal Traits System (ATS) — интеграция

ATS (`packageId = luved.animaltraits`), Steam-Workshop ID 3652630316. Исходников публично нет; API изучен из декомпиляции DLL.

- **Черта ATS — это `HediffDef` с `defName` вида `AnimalTrait_*`** (`AnimalTrait_Common` — маркер «без черт»). Хранится на животном как Hediff (`pawn.health.hediffSet`). Проверка наличия: `hediffSet.HasHediff(def)`.
- **`AnimalTraitExtension`** (`DefModExtension`, namespace `AnimalTraitExtension`): поля `isAnimalTrait`, `traitName`, `inheritChance` (int, %), `IsInheritable => inheritChance>0`. Достаём по имени типа (без жёсткой ссылки на сборку ATS), чтобы мод компилировался и без ATS.
- **Цвет черты**: vanilla `HediffDef.isBad` (true→плохая/красная). Модификаторы — во всех стадиях `def.stages` (`statOffsets`/`statFactors`/`capMods`).

## Animal Slaughter Manager (`namespace ASM`)

Главный проект `Source/AnimalSlaughterManager/` — только точки входа: `ASMMod` (Mod: регистрация настроек, окно мода), `ASMSettings` (ModSettings: флаг логирования), `Patches/HarmonyInit` (применяет Harmony-патчи главного проекта и Core), `GlobalSuppressions`. Весь движок, окна и колонки — в Core.

Ключевое:
- **`ASM_MapComp.Recompute`** порядок: (1) vanilla per-config cull по лимитам с приоритетами по корзинам; (2) ATS force-cull; (3) forceCull-черты; (4) keep-trait защита (резервируется до забоя, входит в порог). Защита перевешивает forceCull.
- **4 настройки возраста×пола** (глобальные, вкладка «Общие»): `globalMalePref` и т.д. — применяются ко всем видам как дефолтные. Per-kind override — на вкладке «Приоритеты».
- **Списки правил приоритетов** (4 корзины: взрослые/молодые × самцы/самки): упорядоченные списки `BasePriorityRule` (верх = сохранение, низ = забой). Ванильные правила: беременность, привязанность, болезнь, тренировка; правила черт — из ATS-интеграции. Легаси-формат `SlaughterCondition` (`CondType` + `has` + defs + `inheritMode`) конвертируется при загрузке сейва/пресета; неизвестные ядру условия отдаются контрактам `ILegacyConditionConverter`.
- Ранжирование: позиция в списке = приоритет.
- **Валидация**: дубликаты и противоречия подсвечиваются (красный фон + Alert). Для тренировок: 2 из 3 состояний — нормально, все 3 или дубликаты — конфликт.
- **Трёхзначная колонка беременных**: Never (галка) / Defer (пауза) / Always (галка). Defer: учитываются в пороге, но забиваются после родов.
- **Яйцекладущие** считаются «беременными» (`CompEggLayer.eggProgress > 0` через рефлексию).
- **Защита** (keep) и **принудительный забой** (forceCull) — на вкладке «Особые правила».
- **Пресеты**: скоупы All/Kind + два вкуса списков (`PriorityList` — списки правил приоритетов, `SpecialList` — списки спец-настроек; вкусы друг к другу не применяются). Папки — по имени значения скоупа (`PriorityList/<корзина>`, `SpecialList/<вид списка>`); v1-раскладка `List/…` (общий корень из main) читается, но не пишется. По окну на скоуп на общем каркасе `BasePresetBrowserDialog` (зоны — `SearchRow`/`PresetList`/`EntryRow`/`SaveRow`). Сериализация в `<persistentDataPath>/AnimalSlaughterManagerPresets/`.

## Типичные правки / грабли

- Гизмо на пешках: обязательно `RaceProps.Animal`.
- Кэш списка забоя: сбрасывать через ванильные `Notify_*` (Postfix).
- ReorderableWidget: `NewGroup` только на Repaint; колбэк получает `(from,to)` ДО `RemoveAt`.
- Табы рисуются вверх от baseRect на 32px — резервировать полосу.
- Non-draggable окно + `GUI.DragWindow(specificRects)` в `LateWindowOnGUI` — чтобы списки/таблицы не конфликтовали с перетаскиванием.
- `Messages.Message(text, MessageTypeDefOf.NegativeHealthEvent, false)` — красное сообщение. Alert (постоянное) — класс extends `Alert`, автообнаруживается.

## Тестирование

RimWorld **не установлен** локально — проверяется только **компиляция** против `Krafs.Rimworld.Ref`. Поведение проверяется в игре (лог: `Ctrl+L` → `Player.log`).
