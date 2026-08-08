# AGENTS.md — справка для агента

Репозиторий `vese/RimworldAnimalSlaughterManager`: мод **Animal Slaughter Manager** для **RimWorld 1.6**.

```
About/                         — метаданные мода
Assemblies/                    — собранная DLL
Languages/                     — переводы
Defs/                          — PawnColumnDef (колонка защиты)
Textures/                      — иконки (щит, шестерёнка)
Source/AnimalSlaughterManager/ — исходники (.csproj + src/)
```

## Сборка

Мод — SDK-style .csproj, `net472` (RimWorld 1.6 = Unity 2022.3 / Mono), ссылается на API через NuGet **`Krafs.Rimworld.Ref` 1.6.\*** (reference-only, игра для сборки не нужна) и **`Lib.Harmony` 2.\*** (только компиляция; сама библиотека уже в игре).

```bash
dotnet build Source/AnimalSlaughterManager/AnimalSlaughterManager.csproj -c Release   # DLL → Assemblies/
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

Структура `Source/AnimalSlaughterManager/src/` (C#-исходники): `Data/` (модели/энумы), `Comps/` (`ASM_MapComp`, `AnimalTraitsAccess`, `PresetIO`), `Patches/` (Harmony), `Windows/` (диалоги), `Columns/` (колонки вкладки Питомцы), `Alerts/` (Alert для конфликтов).

Ключевое:
- **`ASM_MapComp.Recompute`** порядок: (1) vanilla per-config cull по лимитам с приоритетами по корзинам; (2) ATS force-cull; (3) forceCull-черты; (4) keep-trait защита (резервируется до забоя, входит в порог). Защита перевешивает forceCull.
- **4 настройки возраста×пола** (глобальные, вкладка «Общие»): `globalMalePref` и т.д. — применяются ко всем видам как дефолтные. Per-kind override — на вкладке «Приоритеты».
- **Списки условий приоритетов** (4 корзины: взрослые/молодые × самцы/самки): упорядоченные списки `SlaughterCondition` (верх = сохранение, низ = забой). Условия: беременность, привязанность, черта (наличие/отсутствие + наследуемость), болезнь, тренировка (не/обучено/полностью), положительные/отрицательные черты.
- **`SlaughterCondition`**: `CondType` + `has` + `trait/disease/trainable` + `inheritMode`. `Matches(Pawn)` проверяет животное. Ранжирование: позиция в списке = приоритет.
- **Валидация**: дубликаты и противоречия подсвечиваются (красный фон + Alert). Для тренировок: 2 из 3 состояний — нормально, все 3 или дубликаты — конфликт.
- **Трёхзначная колонка беременных**: Never (галка) / Defer (пауза) / Always (галка). Defer: учитываются в пороге, но забиваются после родов.
- **Яйцекладущие** считаются «беременными» (`CompEggLayer.eggProgress > 0` через рефлексию).
- **Защита** (keep) и **принудительный забой** (forceCull) — на вкладке «Особые правила».
- **Пресеты трёх уровней** (All/Kind/List): `Dialog_PresetBrowser` для черт, `Dialog_ConditionPresetBrowser` для списков условий. Сериализация в `<persistentDataPath>/AnimalSlaughterManagerPresets/`.

## Типичные правки / грабли

- Гизмо на пешках: обязательно `RaceProps.Animal`.
- Кэш списка забоя: сбрасывать через ванильные `Notify_*` (Postfix).
- ReorderableWidget: `NewGroup` только на Repaint; колбэк получает `(from,to)` ДО `RemoveAt`.
- Табы рисуются вверх от baseRect на 32px — резервировать полосу.
- Non-draggable окно + `GUI.DragWindow(specificRects)` в `LateWindowOnGUI` — чтобы списки/таблицы не конфликтовали с перетаскиванием.
- `Messages.Message(text, MessageTypeDefOf.NegativeHealthEvent, false)` — красное сообщение. Alert (постоянное) — класс extends `Alert`, автообнаруживается.

## Тестирование

RimWorld **не установлен** локально — проверяется только **компиляция** против `Krafs.Rimworld.Ref`. Поведение проверяется в игре (лог: `Ctrl+L` → `Player.log`).
