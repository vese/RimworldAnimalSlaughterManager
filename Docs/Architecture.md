# Архитектура мода

Мод ориентирован на **Vertical Slice Architecture**: код сгруппирован по фичам-вертикалям
(`Slaughter`, `Settings`, `Presets`), каждая из которых замкнута вокруг своей задачи; UI —
отдельный внешний слой-потребитель. Интеграции со сторонними модами изолированы в
`Integrations/` и во внутренние слои не заглядывают.

## Структура

```
Source/AnimalSlaughterManager/
  AnimalSlaughterManager.csproj   # SDK-style, net472, неявные включения *.cs
  src/
    ASMMod.cs, ASMSettings.cs     # точка входа мода и его настройки (логирование)
    Core/                         # сквозное ядро: ключи перевода, константы, SettingsChanges,
                                  # общие enums, GlobalSuppressions
    Slaughter/                    # фича: бизнес-логика забоя
      ASM_MapComp, SlaughterListBuilder, PregnancyUtility
      Patches/                    # Harmony-патчи ванильного авторезня/колонок/гизмо
    Settings/                     # фича: модель настроек (данные + поведение, без UI и файлов)
      Kind*/Global*/Preference*   # иерархия настроек вида и глобальных
      Rules/                      # правила приоритетов забоя (BasePriorityRule и наследники)
      Validation/                 # осевая валидация списков правил (сеты, валидаторы)
    Presets/                      # фича: работа с файлами пресетов
      PresetIO                    # экспорт/импорт/листинг (XML в persistentDataPath)
      Dtos/                       # XML-формы (по классу на файл)
    UI/                           # внешний слой: весь интерфейс
      Windows/                    # диалоги и вкладки настроек
      Controls/                   # переиспользуемые контролы (Dropdown и др.)
      Columns/, Alerts/           # колонки вкладки «Питомцы», алерты
      UIConstants
    Integrations/
      AnimalTraitsSystem/         # вся интеграция с ATS: доступ (AnimalTraitsAccess),
                                  # свои правила, настройки и UI-секции списков
```

## Правила зависимостей

Стрелка «A → B» означает «A может ссылаться на B».

```
Core      → (никто)
Settings  → Core
Presets   → Core, Settings        # DTO правил конвертирует в классы Settings
Slaughter → Core, Settings        # читает настройки, строит список забоя
UI        → все                   # внешний слой: собирает фичи в окна
Integrations/ATS → Core, Settings # правила реализуют контракты Settings;
                                  # свой UI внутри; в Slaughter/Presets не лезет
```

Запрещено (и на что смотреть в ревью):

- `Core`, `Settings`, `Presets`, `Slaughter` не ссылаются на `UI` и не знают про окна;
- `Settings` не знает про файлы (сериализацию пресетов делает `Presets`);
- интеграции не тянут свои типы в ядро — только реализуют его контракты
  (`IPresettableRuleSet`, `BasePriorityRule`), доступ к чужому API — через свой Access-класс.

## Куда что класть

- **Новый вид правила приоритета** — `Settings/Rules/` (+ набор валидации в `Settings/Validation/`,
  если у правила новая ось);
- **новый контрол** — `UI/Controls/`; новое окно/вкладка — `UI/Windows/`;
- **новый формат пресета/файловая операция** — `Presets/`;
- **изменение алгоритма забоя** — `Slaughter/` (+ Harmony-патч в `Slaughter/Patches/`);
- **новая интеграция** — `Integrations/<ModName>/` со своей папкой Access/Rules/UI,
  по образцу `AnimalTraitsSystem`.

## Спецификация поведения

Поведенческие фичи описываются спеками в `Specs/` (Gherkin, см. `Specs/README.md`):
сначала меняется спека, потом код. Структура папок поведением не управляется —
перенос файлов не требует правок спек.

## Прочее

- Пространство имён одно — `ASM`: расположение файла говорит о роли, суффиксов имён нет.
- Сборка: `dotnet build Source/AnimalSlaughterManager/AnimalSlaughterManager.csproj -c Release`,
  DLL коммитится в `1.6/Assemblies/`.
