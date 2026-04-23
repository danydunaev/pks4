# ProductionControl

Интерактивная система управления производством на ASP.NET Core MVC + EF Core (SQLite).

## Возможности

- Управление материалами: просмотр, пополнение, добавление.
- Управление продуктами: категории, характеристики, привязка материалов.
- Управление заказами: создание, запуск, отмена, отслеживание статуса.
- Управление линиями: статус, коэффициент эффективности, прогресс.
- Live-обновление панели без ручного F5.
- REST API для материалов, продуктов, линий, заказов и расчётов.

## Технологии

- .NET 10
- ASP.NET Core MVC
- Entity Framework Core
- SQLite

## Запуск

1. Перейдите в папку проекта:

   ```bash
   cd ProductionControl
   ```

2. Соберите проект:

   ```bash
   dotnet build
   ```

3. Запустите приложение:

   ```bash
   dotnet run
   ```

4. Откройте URL из консоли (обычно http://localhost:5150).

## Полезные API-эндпоинты

- `GET /api/materials?low_stock=true`
- `POST /api/materials`
- `PUT /api/materials/{id}/stock`
- `GET /api/products?category={category}`
- `GET /api/products/{id}/materials`
- `POST /api/products`
- `GET /api/lines?available=true`
- `PUT /api/lines/{id}/status`
- `GET /api/lines/{id}/schedule`
- `GET /api/orders?status=active&date=today`
- `POST /api/orders`
- `PUT /api/orders/{id}/progress`
- `GET /api/orders/{id}/details`
- `POST /api/calculate/production`

## База данных

- Файл БД: `production-control.db`.
- База создаётся автоматически при первом запуске.
- Для сброса демонстрационных данных удалите `production-control.db` и запустите проект снова.

## Структура проекта

- `Controllers/` — MVC и API контроллеры.
- `Data/` — DbContext и начальные данные.
- `Models/` — сущности и DTO.
- `Services/` — бизнес-логика.
- `ViewModels/` — модели представления.
- `Views/` — Razor-представления.
- `wwwroot/` — стили, JS, статика.
