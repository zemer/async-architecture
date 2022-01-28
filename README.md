# async-architecture

Awesome Task Exchange System (aTES) для UberPopug Inc

## Описание

Добавление пользователя или регистрация производится через сервис auth.
Добавленные пользователи используются для входа в остальные сервисы.
В сервисе tasks можно смотреть список, создавать и назначать задачи.
Сервис accounting считает стоимость задач, ведет счет пользователя и транзакции.
Сервис analytics выводит отчеты и показатели.

## Требования

.net core 5.0

Entity Framework tools (dotnet tool install --global dotnet-ef)

## Запуск

docker-compose -f rabbit-mq\docker-compose.yml build
docker-compose -f rabbit-mq\docker-compose.yml up

cd source

dotnet build

dotnet ef database update --project auth
dotnet ef database update --project tasks
dotnet ef database update --project accounting
dotnet ef database update --project analytics

dotnet run --project auth --urls=https://localhost:44311/
dotnet run --project tasks --urls=https://localhost:44300/
dotnet run --project accounting --urls=https://localhost:44333/
dotnet run --project analytics --urls=https://localhost:44324/

## Отличия начального подхода и текущего результата

| Начальный вариант                                                 | vs  | Вариант по AA                           |
| :---------------------------------------------------------------- | :-- | :-------------------------------------- |
| Аккаунтинг и аналитика в едином сервисе                           |     | Сервисы аккаутинга и аналитики отдельно |
| Сервис аккаутинга и аналитики запрашивает таски из сервиса тасков |     | У каждого сервиса своя копия данных     |
| Запросы напрямую сервиса                                          |     | Работа через message broker             |
