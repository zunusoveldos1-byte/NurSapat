# Локальный PostgreSQL для NurSapat

Запуск приложения и базы: `Start-NurSapat-Local.cmd` в корне проекта.
Требуются установленный PostgreSQL 16 и .NET SDK 8.

- Адрес: `127.0.0.1`
- Порт: `5433` (порт 5432 занят системным PostgreSQL)
- База: `nursapat_db`
- Пользователь PostgreSQL: `admin`
- Пароль PostgreSQL: `1234`
- Вход в NurSapat: `admin` / `1234`

Режим `Authentication:Mode = LocalPostgreSQL` проверяет пароль по хешу
в таблице пользователей. Облачная синхронизация отключена. Локальная
сессия использует рабочие данные PostgreSQL.

Данные сервера хранятся в `.postgres-local/data` и исключены из Git.
Сервер слушает только локальный адрес. Скрипт запуска повторно использует
существующую базу, проверяет подключение и применяет миграции. Пароли
существующих пользователей приложения при обычном запуске не сбрасываются.
Если пароль роли PostgreSQL был изменён, перед запуском задайте его в
`NURSAPAT_LOCAL_POSTGRES_PASSWORD`. Скрипт передаёт локальное подключение
запущенному процессу NurSapat и не переписывает `appsettings.json`.

Чтобы запуск из IDE или напрямую собранного приложения использовал ту же базу,
выполните `./tools/start-local-postgres.ps1 -ConfigureDirectLaunch` и соберите проект.
Пароль сохраняется в локальном `appsettings.Local.json`, исключённом из Git и публикации.

Первичная подготовка таблиц и установка пароля администратора:

```powershell
$connection = ./tools/start-local-postgres.ps1 -PassThru
$previousConnection = $env:ConnectionStrings__PostgreSQL
$env:ConnectionStrings__PostgreSQL = $connection.get_ConnectionString()
try {
    dotnet run --project tools/LocalDatabase -- .
} finally {
    $env:ConnectionStrings__PostgreSQL = $previousConnection
}
```

Команда `dotnet run` в этом примере явно устанавливает пароль пользователя приложения `admin`
в `1234`; её не нужно выполнять при каждом запуске.

В меню «Утилиты → Резервные копии базы данных» можно создать архив `.zp`
со снимком PostgreSQL. Перед восстановлением окно покажет дату, заказ,
состав снимка и базу назначения. NurSapat создаст отдельную страховочную копию
с именем `before-restore-….zp`. Обычное открытие проекта `.zp` загружает заказ
и чертёж без восстановления базы. Восстанавливайте только собственные
или проверенные архивы.

Остановка локального сервера:

```powershell
& 'C:/Program Files/PostgreSQL/16/bin/pg_ctl.exe' -D './.postgres-local/data' -m fast -w stop
```
