# Архитектура мода

Мод построен по **Vertical Slice Architecture**: код сгруппирован по фичам-вертикалям,
и каждая фича владеет всем своим — моделью, логикой и своим UI (`<фича>/UI/`).
Общий `src/UI/` — только переиспользуемые примитивы интерфейса. Интеграции со сторонними
модами изолированы в `Integrations/` и во внутренние срезы не заглядывают.

## Структура

```
Source/AnimalSlaughterManager/
  AnimalSlaughterManager.csproj     # SDK-style, net472, неявные включения *.cs
  src/
    ASMMod.cs, ASMSettings.cs       # точка входа мода и его настройки (логирование)
    HarmonyInit.cs                  # bootstrap Harmony-патчей мода
    Core/                           # сквозное ядро — ни на кого не ссылается
    UI/                             # ОБЩИЙ UI: переиспользуемые примитивы
      UIConstants.cs                #   размеры/отступы/цвета интерфейса
      Controls/                     #   Dropdown + DropdownController, GrayFloatMenuOption
      TraitPicker/                  #   таблица черт: базовая Dialog_TraitTable + Picker (одиночный
                                    #   выбор) + FlagPicker (пары ✓/✗) — юзают и Settings, и ATS
    Engine/                         # ФИЧА: движок забоя
      ASM_MapComp                   #   состояние карты: настройки видов, защита, беременные режимы
      SlaughterListBuilder          #   построение списка забоя поверх ванильного авторезня
      PregnancyUtility              #   «беременность» incl. яйцекладущие (hediff + eggProgress)
      Patches/                      #   точки входа ванили в движок (см. перечень ниже)
      UI/
        Dialog_SlaughterManager     #   главное окно управления забоем (лимиты по видам)
        Columns/                    #   колонки вкладки Питомцы (индивидуальная защита)
    Settings/                       # ФИЧА: модель настроек (данные + поведение, без UI и файлов)
      Global*, Kind*, Preference*   #   иерархия: глобальные ↔ настройки вида, 4 корзины возраст×пол
      IPresettable                  #   мост к сериализации пресетов
      KindPrioritySettingsLegacy    #   чтение сейвов до рефакторинга правил
      Rules/                        #   правила приоритетов: BasePriorityRule + наследники
                                    #   (беременность, привязанность, болезни, тренировка) +
                                    #   SlaughterCondition (устаревший формат сейвов)
      Validation/                   #   осевая валидация списков правил: сет состояний по осям,
                                    #   Duplicate/Coverage/SetClosure валидаторы
      UI/
        Dialog_KindSlaughterSettings        # окно настроек вида (3 вкладки)
        KindSlaughterSettingsDialog*Tab     # вкладки: Общие / Приоритеты / Особые правила
        BaseKindSlaughterSettingsTab, IKindSlaughterSettingsDialogTab
        KindSlaughterSettingsTabListHelper  # общие элементы строк вкладок
        PreferenceSettingsPanel              # панель направлений возраста (вкладка Приоритеты)
        ListSectionState                    # скролл/выделение секции списка
        Alert_ConditionProblems             # алерт о проблемах валидации настроек
    Presets/                        # ФИЧА: работа с файлами пресетов (XML, persistentDataPath)
      PresetIO                      #   листинг/экспорт/применение, три уровня All/Kind/List
      Dtos/                         #   XML-формы (по классу на файл), incl. старые форматы
      UI/
        Dialog_PresetBrowser               # браузер пресетов All/Kind/List
        Dialog_ConditionPresetBrowser      # браузер пресетов корзин списков правил
    Integrations/
      AnimalTraitsSystem/           # ИНТЕГРАЦИЯ ATS: весь код про чужой мод — здесь
        AnimalTraitsAccess          #   доступ к ATS API (черты-hediff, дефы, цвета, тултипы)
        Common/                     #   enums ATS (наследуемость, тип черты)
        Rules/                      #   правила ATS: черта конкретная/общая, keep/force-cull списки
        Settings/                   #   KindTraitsSettings: списки keep/force-cull вида
        UI/                         #   секции списков черт (вкладка «Особые правила»), кнопка дефа
```

## Что мод патчит в ванили (`Engine/Patches/`)

Патчи — точки входа ванили в движок, лежат рядом с кодом, который пускают в ход:

- `Patch_AutoSlaughter` — результат `AutoSlaughterManager.AnimalsToSlaughter` подменяется
  списком `SlaughterListBuilder`, когда есть кастомизация;
- `Patch_Notify` — ванильные `Notify_*` (изменение лимитов и т.п.) сбрасывают кэш списка;
- `Patch_AnimalsTab` — колонки мода в таблице вкладки «Питомцы»;
- `Patch_PawnGizmos` — гизмо переключения индивидуальной защиты на животном.

(`HarmonyInit` — bootstrap всех патчей — лежит в корне `src/`, у точки входа мода.)

## Правила зависимостей

Стрелка «A → B» означает «A может ссылаться на B».

```
Core      → (никто)
Settings  → Core
Presets   → Core, Settings        # DTO правил конвертирует в классы Settings
Engine    → Core, Settings        # читает настройки, строит список забоя
UI (общий)→ Core
Integrations/ATS → Core, Settings, UI(общий)   # свои правила/настройки/UI внутри
любой UI фичи → Core, свой срез, общие UI/Settings по необходимости
```

Запрещено (и на что смотреть в ревью):

- `Core`, `Settings`, `Presets`, `Engine` (модельная часть) не ссылаются на UI и не знают про окна;
- `Settings` не знает про файлы — сериализацию пресетов делает `Presets`;
- интеграции не тянут свои типы в ядро — только реализуют его контракты
  (`BasePriorityRule`, `IPresettableRuleSet`), доступ к чужому API — через свой Access-класс;
- новый экран идёт в `UI/` своей фичи, а не в общий `UI/` — общий только если нужен двум+ фичам.

## Куда что класть

- **новый вид правила приоритета** — `Settings/Rules/` (+ свой сет в `Settings/Validation/`, если
  у правила новая ось); его доп. контрол — контракт на правиле, отрисовка — в `Settings/UI`;
- **новый контрол/окно общего назначения** — `UI/Controls/` (или `UI/<Группа>/`);
- **новый формат пресета/файловая операция** — `Presets/` (+ свой браузер в `Presets/UI/`);
- **изменение алгоритма забоя** — `Engine/` (+ патч-точку входа в `Engine/Patches/` с записью
  в перечень выше);
- **новая колонка/алерт своей фичи** — `<фича>/UI/`;
- **новая интеграция** — `Integrations/<ModName>/` со своими Access/Rules/Settings/UI
  по образцу `AnimalTraitsSystem`.

## Спецификация поведения

Поведенческие фичи описываются спеками в `Specs/` (Gherkin, см. `Specs/README.md`):
сначала меняется спека, потом код. Структура папок поведением не управляется —
перенос файлов не требует правок спек.

## Прочее

- Пространство имён одно — `ASM`: расположение файла говорит о роли, суффиксов имён нет.
- Сборка: `dotnet build Source/AnimalSlaughterManager/AnimalSlaughterManager.csproj -c Release`,
  DLL коммитится в `1.6/Assemblies/`.
