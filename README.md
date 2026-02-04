## Какие пакеты стоят (NuGet)

По твоему выводу `dotnet list package`:

**UI (Avalonia 11.3.11)**

* `Avalonia` — ядро UI-фреймворка
* `Avalonia.Desktop` — запуск приложения под desktop (Windows/macOS/Linux)
* `Avalonia.Diagnostics` — инструменты диагностики
* `Avalonia.Fonts.Inter` — шрифт Inter
* `Avalonia.Themes.Fluent` — Fluent-тема

**База данных (EF Core + PostgreSQL)**

* `Microsoft.EntityFrameworkCore` — ORM EF Core
* `Microsoft.EntityFrameworkCore.Design` — инструменты для `dotnet ef` (scaffold/migrations)
* `Npgsql.EntityFrameworkCore.PostgreSQL` — провайдер EF Core для PostgreSQL

**CAPTCHA**

* `SkiaSharp` — рисование картинки капчи (шум/линии/символы) и вывод в Avalonia `Image`

Вот как можно записать **кратко и правильно** (как в README/отчёт), без лишних деталей по UI-пакетам:

---

## База данных (EF Core + PostgreSQL)

* **Microsoft.EntityFrameworkCore** — ORM EF Core (работа с БД через модели/контекст).
* **Microsoft.EntityFrameworkCore.Design** — инструменты для `dotnet ef` (scaffold/migrations).
* **Npgsql.EntityFrameworkCore.PostgreSQL** — провайдер EF Core для PostgreSQL.

### Как установить (если нужно)

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

---

## CAPTCHA

* **SkiaSharp** — рисование изображения CAPTCHA (шум/линии/символы) и вывод в `Avalonia Image`.

### Как установить

```bash
dotnet add package SkiaSharp
```

---

## Scaffold моделей и DbContext в правильные папки

Твой запрос правильный по смыслу. Ниже — **готовая команда**, которая:

* создаст модели в `Models/`
* создаст `DemoDbContext.cs` в `Data/`
* сохранит имена таблиц/полей “как в БД” (`--use-database-names`)
* **не будет** генерировать строку подключения в `OnConfiguring` (`--no-onconfiguring`)
* перезапишет файлы при повторном запуске (`--force`)

### ✅ Команда scaffold (одной строкой)

```bash
dotnet ef dbcontext scaffold "Host=localhost;Port=5432;Database=trade;Username=postgres;Password=123" Npgsql.EntityFrameworkCore.PostgreSQL --context DemoDbContext --output-dir Models --context-dir Data --use-database-names --no-onconfiguring --force
```

### ✅ Вариант “красиво многострочно” (в терминале тоже работает)

```bash
dotnet ef dbcontext scaffold \
  "Host=localhost;Port=5432;Database=trade;Username=postgres;Password=123" \
  Npgsql.EntityFrameworkCore.PostgreSQL \
  --context DemoDbContext \
  --output-dir Models \
  --context-dir Data \
  --use-database-names \
  --no-onconfiguring \
  --force
```

---

## Рекомендуемая структура проекта

```
MyApp/
 ├─ Data/
 │   └─ DemoDbContext.cs           // DbContext
 ├─ Models/
 │   ├─ User.cs                    // сущности EF
 │   ├─ Role.cs
 │   ├─ Product.cs
 │   └─ Order.cs
 ├─ MainWindow.axaml               // окно входа
 ├─ MainWindow.axaml.cs            // логика входа + капча
 ├─ DashboardWindow.axaml          // “главный экран”
 ├─ DashboardWindow.axaml.cs       // отображение ФИО + выход
 ├─ App.axaml / App.axaml.cs
 └─ Program.cs
```

---

## Модуль 4: Авторизация + CAPTCHA (кратко, как в отчёт)

**Реализовано:**

1. При запуске приложения открывается **окно входа** (`MainWindow`).
2. Пользователь может:

   * войти по логину/паролю (из PostgreSQL через EF Core),
   * или перейти в режим **“Гость”** (без проверки БД).
3. После успешной авторизации открывается `DashboardWindow`, где в правом верхнем углу выводится **ФИО пользователя** + его роль, и есть кнопка **“Выход”** (возврат на окно входа).
4. После **первой неуспешной попытки** авторизации появляется **CAPTCHA**:

   * 4 символа (латиница + цифры),
   * символы расположены **не на одной линии**,
   * присутствует **графический шум**, линии **перечёркивают**/частично перекрывают символы.
5. После **неудачной попытки с введённой CAPTCHA** вход **блокируется на 10 секунд** (с отображением обратного отсчёта).

---

## Про модели `User` и `Role` (что важно понимать)

У тебя в моделях:

### `User`

* `Userid` — ключ пользователя
* `Usersurname`, `Username`, `Userpatronymic` — ФИО (части)
* `Userlogin`, `Userpassword` — данные для входа
* `Userrole` — внешний ключ на роль (int)
* `UserroleNavigation` — навигационное свойство на `Role`

### `Role`

* `Roleid` — ключ роли
* `Rolename` — название роли (“Клиент”, “Менеджер”, “Админ”)
* `Users` — список пользователей, у которых эта роль