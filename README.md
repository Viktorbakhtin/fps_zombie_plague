# Zombie Plague / Outbreak

Unity 6000.3.26f1 + URP + FishNet 4.7.3. Прототип: Assets/_Game/Scenes/TestArena.unity.
Исходный FPS-шаблон в Assets/Scripts использует Unity Netcode; он пока сохранён.

Новый код: Assets/_Game/{Domain,Application,Infrastructure,Tests}.
Описание: Docs/Architecture.md. Подключение ИИ: Docs/UnityMCP.md.

Открыть TestArena → Play → Host. Во втором клиенте нажать Join (127.0.0.1:7777).
Управление и сетевая модель: Docs/Networking.md. Эталон CS 1.6 и ограничения: Docs/CS16Parity.md.

Быстрая проверка: Assets/_Game/Scenes/DebugArena.unity → Play.
Автохост, два бота, случайное заражение через 5 секунд, F5 — новый раунд.
Использует окружение, персонажей, винтовку и Idle/Run анимации шаблона.
Подробнее: Docs/DebugArena.md.

## Открытие проекта на другом компьютере

Установить Unity **6000.3.26f1** через Unity Hub, Git и Git LFS.
Выполнить `git lfs install`, клонировать репозиторий, затем в его папке
выполнить `git lfs pull`. Открыть корневую папку проекта через Unity Hub
и дождаться импорта ассетов и загрузки пакетов. Для первого открытия нужен интернет.

Открыть `Assets/_Game/Scenes/DebugArena.unity` и нажать Play: сервер и два бота
запускаются автоматически. Unity MCP для обычного запуска игры не требуется.
Исходные ассеты шаблона нужны игре, поэтому передаётся весь проект с `Assets`,
`Packages` и `ProjectSettings`. Кэш `Library`, временные файлы и готовые сборки
исключены из Git. Бинарные ассеты передаются через Git LFS.
