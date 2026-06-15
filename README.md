# Lily Market

Аукционная площадка для студентов ЮФУ — современный маркетплейс для торговли внутри кампуса с real-time ставками, тёмной темой и адаптивным интерфейсом.

---

## Технологии

**Frontend** — React 18 · TypeScript 5.7 · Vite 6 · Tailwind CSS 3.4 · TanStack Query · SignalR · React Router 6

**Backend** — .NET 8 · Entity Framework Core 8 · PostgreSQL 16 · JWT Bearer · SignalR · BCrypt.Net · Swagger

---

## Возможности

- Midnight Auction — тёмный дизайн с золотыми акцентами
- Real-time обновление ставок через SignalR / WebSocket
- Таймер обратного отсчёта с визуальными индикаторами (красный при < 1 часа)
- Мгновенная покупка (Buy Now)
- JWT-авторизация для студентов @sfedu.ru
- Бесконечный скролл с skeleton-загрузкой
- Toast-уведомления: «Вас перебили!», «Аукцион завершается»
- Карусель изображений и история ставок
- Полная адаптация под мобильные устройства (375px / 768px / 1024px+)

---

## Структура проекта

```
LilyMarket/
├── LilyMarket-frontend/
│   └── src/
│       ├── api/            # HTTP-клиент с JWT, auth, auctions
│       ├── components/     # AuctionCard, BidHistory, ImageCarousel,
│       │                   # Layout, ProtectedRoute, SkeletonCard, Toast
│       ├── hooks/          # useAuth, useSignalR, useToast
│       └── pages/          # AuctionList, AuctionDetail, CreateAuction,
│                           # Login, Register
└── LilyMarket-backend/
    ├── Controllers/        # API контроллеры
    ├── Application/        # Бизнес-логика
    ├── Domain/             # Доменные модели
    ├── Infrastructure/     # EF Core, репозитории
    ├── Middleware/
    ├── Migrations/
    └── Tests/              # Unit и интеграционные тесты
```

---

## Запуск

### Требования

- Node.js v24+ и npm v11+
- .NET SDK 8.0
- PostgreSQL 16
- Docker (опционально)

### С Docker

```bash
git clone https://github.com/your-login/LilyMarket.git
cd LilyMarket/LilyMarket-backend

docker-compose up -d   # запуск базы данных
dotnet run             # backend → http://localhost:5079

# в другом терминале
cd ../LilyMarket-frontend
npm install
npm run dev            # frontend → http://localhost:3000
```

### Без Docker

```bash
# Backend
cd LilyMarket-backend

# appsettings.json → ConnectionStrings:DefaultConnection
# "Host=localhost;Database=LilyMarket;Username=postgres;Password=yourpassword"

dotnet ef database update
dotnet run

# Frontend
cd ../LilyMarket-frontend
npm install
npm run dev
```

---

## API

### Аутентификация

| Метод | Endpoint | Авторизация |
|-------|----------|:-----------:|
| POST | `/api/auth/register` | — |
| POST | `/api/auth/login` | — |

### Аукционы

| Метод | Endpoint | Авторизация |
|-------|----------|:-----------:|
| GET | `/api/auctions` | — |
| GET | `/api/auctions/{id}` | — |
| POST | `/api/auctions` | JWT |
| PUT | `/api/auctions/{id}` | JWT |
| DELETE | `/api/auctions/{id}` | JWT |
| POST | `/api/auctions/{id}/bids` | JWT |

### SignalR Hub

```
/hubs/auction?access_token={token}
```

| Событие | Направление | Описание |
|---------|-------------|----------|
| `JoinAuction(auctionId)` | клиент → сервер | Войти в комнату аукциона |
| `LeaveAuction(auctionId)` | клиент → сервер | Покинуть комнату |
| `BidPlaced` | сервер → клиент | Новая ставка |
| `Outbid` | сервер → клиент | Вас перебили |
| `AuctionEnded` | сервер → клиент | Аукцион завершён |
| `AuctionEndingSoon` | сервер → клиент | Аукцион скоро завершится |

---

## Дизайн

Цветовая схема **Midnight Auction**:

| Роль | HEX |
|------|-----|
| Фон | `#0F0F0F` |
| Карточки | `#1A1A1A` |
| Акцент (золото) | `#D4AF37` |
| Основной текст | `#F5F5F5` |
| Вторичный текст | `#A0A0A0` |
| Ошибка | `#DC2626` |
| Успех | `#22C55E` |

Скругления: 16px (карточки), 9999px (кнопки). Переходы: 0.2s ease.

---

## Тестирование

```bash
# Backend
cd LilyMarket-backend
dotnet test

# Frontend (в разработке)
cd LilyMarket-frontend
npm run test
```

---

## Адреса

| Сервис | URL |
|--------|-----|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:5079 |
| Swagger | http://localhost:5079/swagger |
| SignalR Hub | ws://localhost:5079/hubs/auction |

Для тестирования на телефоне: устройство и ПК должны быть в одной Wi-Fi сети, в `vite.config.ts` установите `host: '0.0.0.0'`, затем откройте `http://[IP-адрес-ПК]:3000`.

---

## Безопасность

- JWT токены с ограниченным сроком жизни
- Хеширование паролей BCrypt
- Валидация email (@sfedu.ru)
- Защита от ставок на собственные аукционы
- CORS политики

---

## Roadmap

- Чат между продавцом и покупателем
- Рейтинговая система пользователей
- Интеграция с ЮФУ API
- PWA поддержка
- Администраторская панель
- Email-уведомления
- Поиск и фильтрация
- Избранные аукционы
- CI/CD пайплайн

---

## Лицензия

MIT — свободное использование и модификация.

---

*Lily Market создан для студентов Южного Федерального Университета как площадка для торговли внутри кампуса.*
