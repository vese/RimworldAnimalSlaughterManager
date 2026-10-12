# Интеграция: Animal Traits System (ATS)

Чужой мод: Animal Traits System (`luved.animaltraits`, Steam 3652630316) — черты животных как
`HediffDef` (`AnimalTrait_*`). Интеграция делает черты частью приоритетов забоя.

## Состав

- **`AnimalTraitsAccess`** — обёртка чужого API: перечисление дефов черт, наличие черты у
  пешки, цвет/тултип, наследуемость, ATS-собственный force-cull. Единственное место, трогающее
  чужие внутренности (рефлексия там, где API нет).
- **`AnimalTraitsIntegration`** — адаптер `ITraitsIntegration`: опции меню правил (черта
  конкретная/общая), пикер черт (общий `UI/TraitPicker`), вкладка особых правил, keep-резервация
  племенного ядра (`SelectToKeep` с приоритетом пары), подсчёт, force-cull, индикаторы.
- **`AnimalTraitsSystemRegistration`** — саморегистрация: услуги при активном ATS, провайдер
  настроек — безусловно.
- **`Settings/KindTraitsSettings`** (`atsTraits`) — keep/force-cull списки вида: хранение
  (`keepTraits`/`forceCullTraits`), пресет-секция `atsTraits` (легаси-плоские узлы читаются);
  `IEditableTraitsRuleSet` — редактируемые списки для UI.
- **`Rules/`** — правила приоритетов: `TraitPriorityRule` (конкретная черта, с дропдауном
  наследуемости), `TraitGeneralPriorityRule` (положительная/отрицательная), keep/cull списки.
- **`Validation/AnimalTraitSet`** — сет оси черт для общей валидации списков правил
  (`Specs/validation-rules.md`).
- **`Models/`** — DTO пресетов (`TraitDto`, `TraitsSectionDto`, списочные обёртки).
- **`UI/`** — вкладка «Особые правила», секции списков, кнопка дефа.

## Поведение (специфика ATS)

- ATS не активен: черт нет нигде (меню, вкладка, движок); настройки вида сохраняются.
- Правила черт участвуют в общей валидации наравне с ванильными.
- Keep-резервация: племенная пара (наследуемая черта) + добор до keepCount; защита
  перевешивает force-cull.
