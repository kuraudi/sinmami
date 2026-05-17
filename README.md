# RentGen

MVP-приложение для подготовки договора аренды через пошаговый диалоговый сценарий с ИИ.

## Что умеет проект

- создание черновика договора аренды
- пошаговый сбор данных по сторонам, объекту и условиям аренды
- AI-help по текущему шагу
- проверка полноты данных перед генерацией
- генерация черновика договора
- стандартный или персонализированный мини-гайд после генерации
- premium-приложения к договору в виде отдельных мини-опросов
- PDF-предпросмотр и скачивание договора, гайда и приложений

## Стек

### Backend

- .NET 8
- ASP.NET Core Web API
- PostgreSQL
- EF Core
- FluentValidation
- Serilog
- Swagger
- DeepSeek API

### Frontend

- Next.js
- React
- TypeScript
- Tailwind CSS
- Axios
- Redux Toolkit

## Структура проекта

```text
src/
  RentGen.Api
  RentGen.Application
  RentGen.Domain
  RentGen.Infrastructure

frontend/
  src/
  public/

tests/
  RentGen.UnitTests
  RentGen.IntegrationTests

scripts/
  start-dev.ps1
  stop-dev.ps1
  smoke-live-deepseek.ps1
```

## Быстрый старт

## Что нужно для запуска

Перед запуском убедитесь, что на машине установлены:

- .NET 8 SDK
- Node.js 20+ и npm

Проверить можно так:

```powershell
dotnet --version
node --version
npm --version
```

### 1. Запуск backend и frontend

Из корня проекта:

```powershell
dotnet restore RentGen.sln
cd frontend
npm install
cd ..
```

Затем:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\start-dev.ps1
```

После запуска:

- frontend: [http://127.0.0.1:3007](http://127.0.0.1:3007)
- backend / swagger: [http://127.0.0.1:5099/swagger/index.html](http://127.0.0.1:5099/swagger/index.html)

### 2. Остановка сервисов

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\stop-dev.ps1
```

## Если проект запускается из архива

После распаковки:

1. откройте папку `RentGen`
2. запустите PowerShell в корне проекта
3. выполните:

```powershell
dotnet restore RentGen.sln
cd frontend
npm install
cd ..
powershell -ExecutionPolicy Bypass -File .\scripts\start-dev.ps1
```

Если нужно остановить локальные сервисы:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\stop-dev.ps1
```

## Настройка DeepSeek

Для реальных ответов и генерации через DeepSeek нужен API key.

Можно передать его через переменную окружения:

```powershell
$env:DeepSeek__ApiKey = "YOUR_DEEPSEEK_KEY"
```

Если ключ не задан или провайдер недоступен, приложение использует резервные локальные сценарии там, где это предусмотрено.

## Полезные команды

### Backend tests

```powershell
dotnet test tests/RentGen.UnitTests/RentGen.UnitTests.csproj
dotnet test tests/RentGen.IntegrationTests/RentGen.IntegrationTests.csproj
```

### Frontend checks

```powershell
cd frontend
npm run lint
npm run build
```

### Smoke-проверка

```powershell
cd frontend
npm run smoke:e2e
```

## Основные пользовательские сценарии

### Основной договор

- выбрать тип документа
- пройти пошаговый диалог
- получить help по шагу
- проверить полноту
- открыть предпросмотр договора
- скачать договор в PDF

### Premium-приложения

- открыть нужное приложение из карточки документа
- заполнить отдельный мини-опрос
- получить юридический предпросмотр
- сохранить итоговую версию
- скачать PDF

## Приложения

В premium-режиме доступны:

- акт приема-передачи
- опись имущества
- приложение о проживании с животными
- график арендных платежей
- соглашение об обеспечительном платеже
- правила проживания

## Примечание

Проект находится в формате MVP и рассчитан на дальнейшее расширение по новым типам документов, шаблонам и провайдерам LLM.
