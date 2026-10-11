# Интеграции со сторонними модами

Механизм: ядро мода не ссылается на обёртки чужих API напрямую — только на контракты через
реестр. Интеграция живёт в своей папке `src/Integrations/<Mod>/` и регистрируется при старте.

## Участники

- **`ITraitsIntegration`** (`src/Integrations/`) — контракт услуг чужого мод-контента: наличие
  черт, проверки на пешке, принудительный забой, цвета/тултипы, опции меню правил, вкладка
  «Особые правила». Всё, что нужно ядру.
- **`IntegrationRegistry`** — точка доступа ядра: `IntegrationRegistry.Traits`. Пока интеграция
  не зарегистрирована, работает `NoneTraitsIntegration` (NullObject: «ничего нет»), поэтому
  вызывающим не нужны проверки активности.
- **`IntegrationsBootstrap`** (`[StaticConstructorOnStartup]`) — composition root: единственное
  место, перечисляющее интеграции; добавление интеграции = строка здесь + папка.
- **`AnimalTraitsIntegration`** (`src/Integrations/AnimalTraitsSystem/`) — адаптер ATS:
  реализует контракт над `AnimalTraitsAccess` (обёрткой чужого API). Вся папка ATS — свои
  модели (`Models/TraitDto`), свой сет валидации (`Validation/AnimalTraitSet`), свои правила,
  настройки и UI (`UI/KindSlaughterSettingsDialogSpecialRulesTab`, секции списков).

## Правила

- Вне `src/Integrations/<Mod>/` — ни одной ссылки на обёртки/типы чужого мода, только
  `IntegrationRegistry.Traits` (контракт).
- Новая услуга: метод в `ITraitsIntegration` + реализация в адаптере; `NoneTraitsIntegration`
  получает «пустое» поведение.
- Новая интеграция: папка по образцу ATS + контракт (свой или расширение существующего) +
  регистрация в `IntegrationsBootstrap`.
