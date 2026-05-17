# Frontend

Клиентская часть RentGen на Next.js.

## Запуск

Из папки `frontend`:

```bash
npm install
npm run dev
```

По умолчанию dev-сервер поднимается на локальном порту, который задается общим скриптом запуска из корня проекта.

## Полезные команды

```bash
npm run lint
npm run build
npm run smoke:e2e
```

## Основные разделы

- `src/app` — маршруты приложения
- `src/features` — крупные пользовательские сценарии
- `src/components` — UI-оболочка и переиспользуемые элементы
- `src/lib` — API-клиент и presentation helpers
- `src/store` — Redux store и slices
- `src/types` — типы API и клиентских моделей
