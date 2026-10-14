# Unity MCP

Подготовлено 14.10.2026:
- В Packages/manifest.json добавлен com.coplaydev.unity-mcp, закреплённый на v10.3.0.
- В глобальной конфигурации Codex зарегистрирован сервер unity: http://127.0.0.1:8080/mcp.
- uv/uvx уже установлены. FishNet пока не установлен.

Проверено на ПК: сервер запущен в фоне, Unity показывает Session Active.
MCP-клиент получил 48 инструментов и успешно прочитал сцену Assets/Scenes/MainMenu.unity.
Инструменты ещё не загружены в каталог текущего чата; перезапуск Codex обновит его.

Повторное подключение после закрытия программ:
1. Открыть этот проект в Unity 6000.3.26f1, дождаться импорта пакетов и компиляции.
2. Открыть Window → MCP for Unity. Выбрать HTTP Local и запустить сервер
   с адресом http://localhost:8080; подключить Unity к серверу в этом окне.
3. Перезапустить Codex/открыть новый чат проекта, чтобы загрузились инструменты нового MCP.
4. Проверить связь чтением Unity Console и активной сцены. Затем создавать TestArena и префабы.

Если автоматический запуск не сработал, в PowerShell:
```powershell
uvx --from 'mcpforunityserver==10.3.0' mcp-for-unity --transport http --http-url http://127.0.0.1:8080
```

MCP позволяет редактировать сцены, объекты, компоненты, ассеты, запускать тесты и сборки.
Для анимаций можно собирать Animator, Timeline и rigging из импортированных клипов.
Качественные модели и motion clips требуют исходных ассетов или отдельного пайплайна создания.
Повторная проверка связи: uv run --with mcp python Tools/Checks/check_unity_mcp.py.

Источники:
- https://github.com/CoplayDev/unity-mcp/tree/v10.3.0
- https://developers.openai.com/codex/mcp
