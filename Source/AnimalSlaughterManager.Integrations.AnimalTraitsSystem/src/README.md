# Интеграция: Animal Traits System (ATS)

Чужой мод: Animal Traits System (`luved.animaltraits`, Steam 3652630316) — черты животных как
`HediffDef` (`AnimalTrait_*`). Интеграция делает черты частью приоритетов забоя.

Проект `AnimalSlaughterManager.Integrations.AnimalTraitsSystem` ссылается на
`AnimalSlaughterManager.Core` и регистрирует свои сервисы в `IntegrationServices`
(механизм — `Docs/Integrations.md`).

## Состав

- **`AnimalTraitsAccess`** — обёртка чужого API: перечисление дефов черт, наличие черты у
  пешки, цвет/тултип, наследуемость, ATS-собственный force-cull. Единственное место, трогающее
  чужие внутренности (рефлексия там, где API нет).
- **Адаптеры контрактов** (по классу на контракт): `TraitRuleOptions` (опции меню правил),
  `TraitSettingsTab` (вкладка «Особые правила»), `TraitSlaughterSignals` (тай-брейкеры
  сортировки), `TraitKeepReserve` (keep-резервация племенного ядра с приоритетом пары,
  подсчёт), `TraitKindForceCull` (принудительный забой по правилам), `AtsExternalForceCull`
  (гейт решений самого ATS).
- **`AnimalTraitsSystemRegistration`** — саморегистрация: сервисы при активном ATS; провайдер
  настроек вида и `LegacyTraitConditions` (конвертер легаси-условий с чертами) — безусловно,
  чтобы настройки и сейвы пережили выключение ATS.
- **`Settings/KindTraitsSettings`** (`atsTraits`) — keep/force-cull списки вида: хранение
  (`keepTraits`/`forceCullTraits`), пресет-секция `atsTraits` (легаси-плоские узлы читаются);
  `IEditableTraitsRuleSet` — редактируемые списки для UI.
- **`Rules/`** — правила приоритетов: `TraitPriorityRule` (конкретная черта, с дропдауном
  наследуемости), `TraitGeneralPriorityRule` (положительная/отрицательная), keep/cull списки.
- **`Common/`** — понятия ATS `TraitInheritability` и `TraitType` (живут здесь, Core их не
  знает; в сейвах/пресетах — строковые имена).
- **`Validation/AnimalTraitSet`** — сет оси черт для общей валидации списков правил
  (`Specs/validation-rules.md`).
- **`Models/`** — DTO пресетов (`TraitDto`, `TraitsSectionDto`, списочные обёртки).
- **`UI/`** — вкладка «Особые правила», секции списков, пикеры черт/флагов, кнопка дефа.

## Поведение (специфика ATS)

- ATS не активен: черт нет нигде (меню, вкладка, движок); настройки вида сохраняются.
- Правила черт участвуют в общей валидации наравне с ванильными.
- Keep-резервация: племенная пара (наследуемая черта) + добор до keepCount; защита
  перевешивает force-cull.
