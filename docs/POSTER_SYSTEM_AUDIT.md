# Аудит системы плакатов — 2026-10-28

## A. Исходное состояние и границы проверки

Исследована актуальная рабочая копия **Just One More Peel**, а не проект из внешнего примера. Это UVCS/Plastic workspace (`.plastic`), без `.git`; Git status/diff недоступны. Не заявляется чистота всей рабочей копии. Перед правками сохранены только три изменяемых исходника и SHA-256 защищаемых файлов: [initial-sha256](../Logs/poster-audit/initial-sha256.txt). FirstShift уже содержала несохранённые изменения: 133 корня, 19 стопок, 152 полосы, камера (0, 1.53, −1.60). Сцену не сохраняли и не перезагружали с диска; тестовые состояния отбрасывались выходом из Play.

**Среда:** Unity **6000.6.0f1**, URP **17.6.0**, активное качество **PC**, `PC_RPAsset`, **Forward+**, Render Graph, SRP Batcher, GPU Resident Drawer `InstancedDrawing` и GPU occlusion уже включены. Windows x64, текущий API **D3D11**, RTX 4060 Ti, Ryzen 7 3700X. Windows Standard содержит FirstShift, Development/Deep Profiling/Autoconnect выключены. В измерениях Editor Play — 1167×556, VSync 0, targetFrameRate −1. Наличие Windows profile не является результатом Player build.

`Packages/manifest.json`: Input System 1.20.1, Cinemachine 6.6.0, Test Framework 1.8.0, Burst 2.0.0, Collections 6.6.0, AI Navigation 2.0.16, uGUI 2.6.0, Unity MCP с закреплённым Git revision. Сторонние пакеты не изменялись. Игровые скрипты входят в Assembly-CSharp; отдельного poster asmdef, SO-хранилища, prefab variants плакатов и специализированной NUnit poster-сборки нет. Импортированные Starter Assets имеют собственные asmdef; их код сохранён. Тесты проекта — Editor `PeelProgressionChecks`, MCP-пробы и опциональный `WallFrameProbe`. Старый неудачный input-harness прогон из PLAYTEST не превращён в успешный.

### Карта компонентов

| Звено | Фактическая реализация |
|---|---|
| Данные/размещение | [PeelPosterStack](../Assets/JustOneMorePeel/Scripts/PeelPosterStack.cs): 19 компонентов в FirstShift, по 128 логических листов; дизайны и позиции вычисляются детерминированно. Стопки не являются prefab instances. |
| Состояние | `ClearedPosters`, четырёхбитная маска снятых частей, progress/IsRemoved текущих полос. Отдельного объекта на каждый из 2432 листов нет. |
| Интерактивное представление | Два массива по четыре PeelStrip на стопку: **152** повторно используемых MeshRenderer/MeshFilter/BoxCollider/MonoBehaviour, по 50 вершин и 48 треугольников. |
| Глубокая бумага | Один MeshRenderer/MeshFilter и отдельный полный non-convex MeshCollider на стопку: **19** представлений, до десяти material submeshes. Нижние листы существуют до раскрытия; это не бесконечный счётчик поверх двух картинок. |
| Видимость | [PeelWallVisibility](../Assets/JustOneMorePeel/Scripts/PeelWallVisibility.cs): консервативные bounds, шесть переиспользуемых frustum planes, 60 м, render-only `forceRenderingOff`, сетка 2 м для соседей пара. |
| Взаимодействие | [PeelGame](../Assets/JustOneMorePeel/Scripts/PeelGame.cs): один основной Physics.Raycast из камеры; физическое первое попадание сохраняет перекрытия. Steam query только в начале захвата, до двух дополнительных видимых соседей. |
| Срывание | [PeelStrip](../Assets/JustOneMorePeel/Scripts/PeelStrip.cs): CPU-деформация 24 сегментов; карта — 1 полоса, шпатель — до 4, пар — до 12 полос/3 листов. Нет Cloth, Animator или Rigidbody на бумаге. |
| Эффекты | [PeelScrapPool](../Assets/JustOneMorePeel/Scripts/PeelScrapPool.cs): 32 предсозданных визуальных объекта/меша, без коллайдеров и Rigidbody, срок 2.2 с. При переполнении переиспользуется старейшая позиция кольца. Целый лист даёт один визуал. |
| Аудио/частицы | Один общий looping AudioSource срывания с короткими fade; отдельный источник музыки. Одна система пара, лимит 40 частиц. Источники/эмиттеры на каждом логическом листе отсутствуют. |
| Persistence | [PeelSettings](../Assets/JustOneMorePeel/Scripts/PeelSettings.cs) сохраняет EN/RU, громкость и чувствительность. Состояние смены не сохраняется; Restart загружает свежую сцену. World save и стабильные save IDs ещё не реализованы — B01/B02, не новая регрессия. |

Жизненный цикл: Initialize → видимые front/back и глубокие слои → ray target/outline → захват только открытых полос → частичный progress либо Detach → однократная выплата/скрытие полосы/маска → после снятия всех четырёх частей и освобождения ссылок смена буферов → следующий слой. Нужен новый release LMB для каждого захвата. Меню/отмена завершают захват. Снятая часть не оплачивается повторно; состояние независимо от отсечения Renderer. Между запусками сбрасывается вся смена по существующему правилу.

### Материалы, текстуры, освещение и память

Уведомления используют **20 общих материалов — 10 на язык**, URP/Lit, `_ALPHATEST_ON`, очередь 2450, без `renderer.material` и MaterialPropertyBlock в игровом пути. Интерактивные полосы и обрывки ссылаются на sharedMaterial. Материал частиц и материал одной линии выделяются однократно и уничтожаются владельцами. `papers` на PeelGame — прежняя сериализованная ссылка; актуальные локализованные ссылки выбирает PeelSettings.

Тексты — compressed DXT1/DXT5, 1024×1024 либо 1024×512, 11 mip levels, bilinear, aniso 1, Read/Write выключен, streaming выключен. Для 20 notice textures Unity сообщает **41 962 784 байта** runtime asset memory; это не измерение чистой VRAM. Полный inventory также содержит два материала окружения: их нельзя приписывать бумаге. Два языка загружены по сериализованным ссылкам. Размеры/форматы/UV и импортеры не менялись.

171 display mesh занимал по Unity около **2 229 572 байт**; эта цифра не включает отдельные collision meshes и pool meshes. Whole Editor inventory: managed used около 100 МБ, total allocated около 4 ГБ; эти глобальные числа не равны памяти игры/плакатов. Retained capacity после максимальной глубины — сознательный обмен небольшого пикового запаса на отсутствие повторных выделений; он освобождается вместе с объектами сцены.

У глубокой бумаги и полос нет отбрасывания теней; у глубокой бумаги отключён receiveShadows. Lit/alpha clip и глубина сохраняют текущий вид. Frame Debugger подтвердил depth-normal passes, четыре SSAO passes, SRP Batcher и instance-occlusion passes: [capture](../Logs/poster-audit/frame-debugger.json). Это подтверждение путей сцены, а не доказательство, что каждый уникальный деформируемый меш отрисован одним instanced draw.

Свежая проверка всех пикселей обоих исходных Cinema PNG дала minAlpha=255: [opaque-cinema](../Logs/poster-audit/opaque-cinema.txt). Это подтверждает используемое сейчас предположение об occluder, но не даёт разрешения менять alpha позже без новой проверки.

Аудио-аудит: 1 активный AudioListener, 2 AudioSource с spatialBlend 0, 0 AudioMixer assets, выход 48 кГц, 32 доступных real voices. Загруженная музыка — 120 с, stereo, DecompressOnLoad, примерно **23 041 217 байт**; текущий rip — 4 с, stereo. RuntimeMemorySizeLong маленького ещё не проигранного клипа не отражает весь будущий PCM budget. Никаких per-poster voices/временных AudioSource нет. Изменение Load Type музыки отложено до проверки запуска/loop/seek и DSP на Player; выбор звука и баланс не затрагивались.

## B. Проблемы и приоритеты

| Категория / приоритет | Место | Свидетельство / причина | Влияние и решение |
|---|---|---|---|
| Measured Bottleneck, P1 | `UpdateDeeperLayers` | Новые lists/arrays и `new[]{...}` для каждой нижней четырёхугольной поверхности при каждом переходе; полный render upload с normals до немедленного clipped upload. 608 переходов дали около 32 МБ managed-used growth. | Убраны повторные контейнеры и первый лишний render upload. Полный collider recook оставлен. Это подтверждённая стоимость перехода, не доказательство причины всех падений FPS. |
| Optimization Opportunity, P1 | PeelStrip Pull + Update | Оба пути вызывали Deform за кадр, до 12 полос одновременно; зависимость от порядка callbacks. | Held flutter перенесён в LateUpdate и пропускается, если Pull уже обновил меш в этом кадре. Профиль подтверждает 12, а не 24 вызова в steady active workload. |
| Confirmed Defect, P2, низкая текущая частота | `PeelWallVisibility.Initialize` | Повторный вызов давал 38 patch records вместо 19: [before](../Logs/poster-audit/baseline-reinitialize.txt). Производственный Start вызывает Initialize один раз. | Очистка/восстановление прежнего индекса, проверка повторной инициализации и уникальности query. Не выдаётся за текущий большой FPS bottleneck. |
| Optimization Opportunity, P1 | Visibility LateUpdate | До 171 одинаковой записи `forceRenderingOff` каждый кадр без изменения видимости. | Запись только при переходе видимости. Сам консервативный тест bounds остаётся каждый кадр. |
| Architectural Risk, P2 | Deep geometry + MeshCollider | CPU-перестроение clipped geometry при движении и collision recook при завершении зависят от оставшейся глубины. | Данные/буферы переиспользуются, но сложность не стала O(1). Для тысяч отдельных участков нужен отдельный замер. |
| Architectural Risk, P2 | Culling/occluder assumptions | Статичные размещения и одна игровая камера; opaque дизайн Cinema hardcoded как индекс 1. | Не добавлены движущиеся стены/вторые игровые камеры. Изменение alpha Cinema требует новой валидации. Bounds проверены при curl. |
| Optimization Opportunity, P2 | Music importer | Длинная музыка полностью распакована в память, около 22 MiB. | Возможен Streaming; это не per-poster утечка, текущая работа не меняет аудиоимпортер. |
| Optimization Opportunity, P2 | PeelGame.OnGUI | HUD собирает строки и повторяет workbench ray на GUI events; глобальный GC recorder содержит также эти затраты. | Кандидат при переводе prototype UI на событийные обновления. Не установлен как основной bottleneck и не смешан с выделениями глубокой бумаги. |
| Optional Enhancement, P3 | Streaming/render rewrite | Один маленький двор, 19 patches, уже есть batching/culling/pool. | Новые renderer framework, Addressables, ECS не обоснованы измерениями. |

Новый P0-дефект в итоговой версии не установлен. Отсутствие world save — известная ещё не реализованная функция. Нельзя записывать её как сломанное существовавшее сохранение.

## C. Выбранная стратегия и оценка альтернатив

Основной путь остаётся **обычные MeshRenderer + общие материалы + SRP Batcher/существующий GRD**, два интерактивных буфера на стопку, ограниченная активная CPU-деформация и фиксированный pool. Роль данных, состояния и представления уже разделена достаточно для текущего масштаба; новая иерархия Definition/View/Factory/Registry не нужна.

| Метод | Решение и основание |
|---|---|
| Unity frustum | Сохранён. Дополнительный patch test уже нужен, чтобы не запускать camera-dependent CPU clipping вне вида; он не выключает логические объекты. Вращение камеры не требует перепроецировать silhouettes при неизменной позиции. |
| Distance | Существующие 60 м render distance и 3.4 м reach сохранены отдельно. В малом дворе 60 м не является видимым ограничением. Fade/dither не добавлен без доказанного pop-in. |
| Occlusion | Сохранены GRD GPU occlusion и conservative clipping только под полностью непрозрачной Cinema. Визуал может иметь четыре border rectangles; collider всегда полный. Бумага не превращается в occluder стен/комнат. Baked occlusion не обоснован одним открытым двором без сети помещений. |
| Spatial partition | Существующая uniform/hash grid 2 м достаточна для steam. Основной target уже использует PhysX broadphase и первый hit. Octree/BVH/quadtree не требуются. Стоимость видимости ещё O(patches), сетка её не скрывает. |
| CullingGroup | Отложен до реально большого числа участков: сейчас 19 bounds, дополнительные bounding spheres/events дублировали бы имеющийся путь. |
| SRP / MPB / GRD | SRP/GRD уже активны и совместимы с текущими URP shaders и compute-capable D3D11. MPB не добавлен: он мешает этим путям. GRD использует BRG, но не превращает уникальные меши в одинаковые экземпляры. [Unity: GRD](https://docs.unity.com/en-us/engine/6000.5/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/reduce-rendering-work-on-cpu/gpu-resident-drawer). |
| Manual GPU instancing | Не выбран: интерактивная геометрия уникальна/деформируется, глубокая объединена по patch. Один prefab сам по себе не даёт instancing. Для будущих многочисленных одинаковых плоских views потребуется отдельное A/B. |
| Static/dynamic batching | Runtime-деформируемые полосы и меняющая submesh geometry глубокая бумага не кандидаты на blanket static batching. Static batching добавляет копии геометрии и конфликтует с GRD path. Dynamic batching не включён как дополнительный CPU-проход. Уже объединённое окружение сохранено. [Unity: выбор метода](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/graphics-performance-profiling/reduce-draw-calls/optimizing-draw-calls-choose-method). |
| Atlas / Texture2DArray | Атлас ранее был отвергнут owner FPS retest и стоил памяти; не возвращён ради уменьшения counts. Array требует одинаковых размеров/форматов, здесь они различаются, а также новый shader/index plumbing. Его выгода не измерена. Mip padding/bleeding и читаемость текста потребовали бы отдельного сравнения. [Unity: Texture2DArray](https://docs.unity3d.com/cn/6000.0/ScriptReference/Texture2DArray.html). |
| Lit → Unlit, alpha, shadows | Не менялись: мягкое освещение и вырезанные края — часть текущего вида. Нельзя считать удаление освещения безопасной CPU-оптимизацией. Бумажные shadow casters уже отключены. |
| Shader deformation / Cloth | Текущие максимум 12 × 50 вершин достаточно малы; CPU mesh сохраняет прямое копирование в scrap и текущий вид. Cloth/Rigidbody на каждый лист неоправданны. Shader deformation — отдельный GPU/визуальный A/B, не необходимый рефакторинг. |
| Low-level RenderMesh* / indirect / BRG | Собственный submission/culling/selection pipeline не оправдан текущими метриками; остаются штатные Renderer. |
| LOD/HLOD | Deep quad уже 2 треугольника до border clipping. LODGroup на каждый лист не помогает главному overdraw. HLOD для одного двора не требуется. |
| Texture streaming / Addressables / additive | 20 изображений и одна сцена не требуют нового integration. Mip streaming сначала проверять на близком тексте; Addressables/additive — при утверждённых главах, budget и save policy. |
| Jobs/Burst/ECS | Burst/Collections установлены, но Unity Mesh/Collider API здесь main-thread. Планировать jobs ради 19 bounds нельзя. Будущий большой CPU clipping batch требует профиля и отделения вычислений от upload. |

Raycast не заменён на RaycastAll/NonAlloc: одиночный Raycast не создаёт result array, а первое препятствие принципиально важно для перекрытий и workbench. Blanket layer mask, исключающая окружение, сломала бы occlusion. Стартовые Find/Instantiate не приписываются покадровой нагрузке. Pool уже ограничен и не заменён конкурирующим pool framework.

## D. Изменения, риск, проверка и откат

| Файл | Изменение | Риск / проверка / откат |
|---|---|---|
| PeelPosterStack.cs | Retained geometry/index/material caches; `deepCount` задаёт активную часть capacity; прямой design→submesh lookup; render upload один раз, collision всё ещё полная. Добавлены `Poster.BuildDeepLayers` и `Poster.Visibility` markers. | Ошибки live-range/UV/material/bounds проверены полной глубиной, площадями и UV, EN/RU, cache recovery. Десяток material arrays может выделяться при изменении числа групп у конца стопки; не обещан абсолютный zero-GC для любого состояния. |
| PeelStrip.cs | Marker `Poster.Deform`; frame stamp; held flutter в LateUpdate после gameplay Update, без повторной загрузки того же состояния. | Порядок callbacks фиксируется фазой, а не случайным порядком компонентов. Синхронный Pull всё ещё немедленно меняет mesh. Проверены active frames, completion и payout. |
| PeelWallVisibility.cs | Идемпотентная регистрация; forceRenderingOff меняется только на границе видимости. | Bounds/grid остаются для неподвижной стены. Проверены equivalence, repeat Initialize, far/return, collider retention, disable/enable и частичный progress. |
| PosterPerformanceProbe.cs | Новый опциональный **UNITY_EDITOR-only** нагрузочный сценарий, не прикреплён к сохранённой сцене и исключён из Player. | Проба намеренно меняет runtime depth/progress; её запускают в отдельном Play-сеансе и выходят после измерения. В finally возвращает camera/depth, но не выдаёт это за полное восстановление игрового сеанса. |

Inspector-поля, scene/prefab references, существующие GUID, управление, цены, выплаты, скорости и release rules не изменены. Откат — три исходника в `Logs/poster-audit/before`, затем импорт/компиляция; новый probe можно убрать с его созданной `.meta`. До отката проверять, не появились ли последующие правки владельца. Сцена и настройки рендера не требуют отката. Ни commit, push, merge, пакет, ни Player build не создавались.

## E. Измерения before/after

### Стоимость операций

Одинаковая рабочая стена, 20 прогревочных перемещений камеры; 180 синхронных обновлений 19 стопок и 608 переходов (32 на стопку). Stopwatch включает реальные удаления четырёх частей, pool emit, mesh update и collision recook; не включает время следующего кадра. GC heap sampling выполняется за границами измеряемого интервала.

| Метрика | До | Итог | Вывод |
|---|---:|---:|---|
| Один переход: median | 0.4051 мс | 0.3163 мс | −21.9% локальной стоимости |
| Один переход: p95 | 0.5338 мс | 0.3475 мс | −34.9% в этом эксперименте |
| 608 переходов: summed managed-used growth | 32 075 776 Б | 0 Б наблюдаемого прироста | Удалён основной повторный managed churn; не абсолютная гарантия zero-GC |
| Moving visibility 19: median | 1.0640 мс | 1.0277 мс | Малое различие; не основание обещать FPS |

Первый оптимизированный повтор дал 0.3256 мс median перехода, то есть выигрыш воспроизвёлся. [baseline](../Logs/poster-audit/baseline-micro.txt), [final](../Logs/poster-audit/final-micro.txt), raw CSV рядом. `GC.GetAllocatedBytesForCurrentThread()` в этой среде вернул 0 даже после контрольного выделения 1 MiB; ранние нули признаны недействительными. Отключить GC в Editor нельзя. Итоговая оценка использует Mono used delta; samples с коллекцией/отрицательной разницей исключены из суммы. Независимая CPU-трасса содержит **38 прогретых `Poster.BuildDeepLayers` samples без вложенных GC.Alloc events**: [allocations](../Logs/poster-audit/profile-build-allocations.csv). Это более узкое подтверждение, чем обещание отсутствия аллокаций во всей игре.

### Кадровая матрица

**100 / 500 / 1000 / 2000 / 2432 / 5000 логических листов**, распределённых между существующими 19 patches. Шесть сценариев на глубину: близко, отвернуться, непрозрачная преграда, боковое движение, быстрое вращение, 12 активных strip views + заполненный pool. На каждый — 30 warmup + 120 последовательных кадров. Это depth stress; не тест 5000 независимо размещённых GameObjects. Для 12 views используются реальные Pull/Begin и pool, но это scripted workload, не физический held input и не полноценный steam-tool benchmark с аудио/частицами.

| Logical | Close median, до→после, мс | Moving median, до→после, мс | Active 12 + scraps, до→после, мс |
|---:|---:|---:|---:|
| 100 | 9.55 → 9.99 | 9.88 → 10.19 | 10.85 → 10.77 |
| 500 | 10.28 → 10.45 | 11.12 → 11.24 | 10.99 → 11.07 |
| 1000 | 10.54 → 10.50 | 11.37 → 11.46 | 10.88 → 11.02 |
| 2000 | 10.29 → 10.34 | 11.55 → 12.99 | 10.91 → 10.58 |
| 2432 | 10.60 → 10.85 | 11.96 → 12.35 | 10.96 → 10.63 |
| 5000 | 10.18 → 10.54 | 12.96 → 12.88 | 10.83 → 10.40 |

Для 2432 close: CPU main median **3.00→3.01 мс**, render **2.0066→2.0061**, GPU **2.0511→1.9507**, p95 frame **12.97→14.00**. Draw/triangle/vertex medians в close/moving совпали; это дополнительный контроль представления. UnityStats — глобальные Editor counters, не гарантированное число GPU draws ровно одной камеры. GC Allocated In Frame около 16–20 КБ/кадр включает IMGUI, Editor и запись строк самой пробы; его нельзя объявлять GC только плакатов. Все значения и p99: [comparison.csv](../Logs/poster-audit/comparison.csv).

**Устойчивого общего FPS-улучшения эта матрица не доказала.** Несколько результатов стали хуже, особенно moving 2000/p95, и они сохранены. Более длинный after-only повтор дал 11.93 мс для 2000 и 11.53 для 2432, но был unfocused (0/240), поэтому не заменяет сравнение focused матриц. Требуется контролируемый Player A/B, прежде чем закрывать вопрос FPS. Падения представления/Editor scheduling нельзя приписывать только числу draw calls или бумаге по этим данным. Доказанный результат этой задачи — стоимость переходов/managed churn, а не универсальная новая частота кадров.

Обе основные stress-матрицы записали по 4320 measured frames; focus каждого кадра сохранён (основные матрицы focused). Финальный active-only проход проверяет LateUpdate-версию; `analyze.ps1` подставляет его вместо промежуточного active workload. Ранний `baseline-frames` на исходной глубине был unfocused и остаётся только диагностикой, а не сравнением с focused результатами.

CPU Profiler: [optimized-profile.raw](../Logs/poster-audit/optimized-profile.raw), 1137 записанных кадров, [summary](../Logs/poster-audit/profile-summary.csv). В **189 steady frames** было ровно **12 `Poster.Deform` calls**, среднее суммарное время около **0.14 мс**; Physics.Simulate на тех кадрах в среднем около **0.09 мс**. Два кадра инициализации отдельно содержат 152/176 деформаций; их нельзя выдавать за steady cost. Профилирование добавляет overhead, поэтому `profile-active` не подмешан в before/after.

## F. Что отложено

Сохранены текущие GRD, clipping, spatial grid, pool и rendering path. Не внедрялись новый atlas/array shader, manual/indirect rendering, static/dynamic batching поверх GRD, baked room occlusion, CullingGroup, shader peel, Cloth, LOD/HLOD, Addressables, additive scenes, Jobs/Burst/ECS migration. Причины — в таблице C. Будущий мир с большим числом отдельных стен требует теста пространственной ширины, а не дополнительного умножения глубины одной стены. Сохранение мира должно появляться по отдельной B01/B02 спецификации с устойчивыми идентификаторами и partial-state DTO; culling для этого переписывать не требуется.

## G. Реально выполненные проверки

| Проверка | Итог / свидетельство |
|---|---|
| Компиляция | Unity закончила compilation; ошибок нет. При полном compile остаются пять прежних obsolete API warnings в WallFrameProbe/ArtRevision; новые скрипты их не добавляют. Финальный runtime Console без error/warning. |
| Текущие правила и вся глубина | **21 870 PASS / 0 FAIL**, все 2432 листа, exact payout, promotions без изменения позиции/высоты/material, bounded object/mesh counts: [final-runtime](../Logs/poster-audit/final-runtime.txt). |
| Rendering/collision | **31 887 checks / 0 FAIL**: площадь оставшейся бумаги, UV открытых краёв, полная collision geometry, несколько точек камеры: [geometry](../Logs/poster-audit/geometry.txt). |
| Targeting/steam/workbench | **13 PASS / 0 FAIL**: range, facing, преграда, восстановление цели, 1–3 sheets, однократный visual/payout, held-release: [targeting](../Logs/poster-audit/targeting.txt). |
| Масштабное снятие | **17 305 PASS / 0 FAIL**: полностью сняты 100/500/1000/2000/5000 листов; payout 800/4000/8000/16000/40000, constant object/mesh counts, полный live-range коллайдера: [completion](../Logs/poster-audit/scaled-completion.txt). |
| State/caches/pool | **112 PASS / 0 FAIL**: repeat registration, grid equivalence, far/return, collider retention, partial progress, EN/RU, native mesh reuse после потери managed caches, 1024 pool emissions, bounds curl, снятая полоса не возвращается после culling/GameObject reactivation: [state-checks.txt](../Logs/poster-audit/state-checks.txt). |
| Визуальная инспекция | Фактическая игровая камера: [кадр](../Assets/Screenshots/screenshot-20261028-172525.png). Не является owner art acceptance или проверкой мерцания во всех траекториях. |

Промежуточные ошибки не скрыты: [measurement-limitations](../Logs/poster-audit/measurement-limitations.txt), [intermediate-errors](../Logs/poster-audit/intermediate-errors.json). Первая версия не учитывала восстановление пустого private array после reload; исправлено и проверено через cache recovery. Первые функциональные fixtures зависели от порядка FindObjects и положения/перекрытия полос: одна попытка не нашла цель, другая целилась в закрытую соседним плакатом оставшуюся полосу. Теперь fixture детерминированно выбирает стопку с четырьмя действительно видимыми полосами. Провал сохранён в `final-runtime-fixture-failure.txt`, итоговый проход записан отдельно. Игровой raycast не ослаблялся ради теста.

World-save restore, переходы между ещё не созданными зонами, физическое управление/alt-tab/долгая человеческая сессия, устройства слабее текущего и Player GPU/memory trace **не проверены**. Старая A01 owner closure сохранена; новый human acceptance не заявляется.

Финал: Edit Mode, без диагностического компонента в сцене, Profiler/Frame Debugger выключены, исходные 133 корня/19 стопок/152 полосы и камера восстановлены; исходный dirty flag сохранён. Шесть защищаемых файлов сцены/рендера/пакетов/настроек совпали по SHA-256: [protected-files](../Logs/poster-audit/protected-files.txt), [Editor state](../Logs/poster-audit/final-editor-state.json). Локальные ссылки отчёта/обновлённых документов проверены, отсутствующих целей нет. Сравнение трёх исходников с копиями before выполнено через `git diff --no-index`; это не Git status всей UVCS workspace.

## H. Remaining risks

CPU visibility и collider cooking всё ещё зависят от глубины. Основная камера управляет render-only culling всех patches; дополнительные игровые камеры и движущиеся стены требуют иной политики. `forceRenderingOff` не заменяет состояние снятия; возврат видимости должен сохранять `Renderer.enabled=false` у удалённой полосы. Изменение прозрачности Cinema сломает предположение clipping. При изменении дизайнов проверять максимальную высоту, EN/RU и все deep→back promotions. Нет доказательства отсутствия утечек в 60–120-минутном Player-сеансе только из object counts и коротких Editor-прогонов. Более плохие кадровые samples остаются открытым performance follow-up.

## I. Следующие действия

1. Контролируемый Player A/B по тому же camera route на целевом железе, с CPU/GPU/GC trace, реальными card→scraper→steam действиями и одинаковым focus/viewport; отдельно разобраться с moving/p95.
2. Перед расширением уровней проверить сотни отдельных пространственных patches, загрузку и память; только затем выбирать sector activation/CullingGroup или дальнейшее сокращение camera-dependent upload/collider rebuild.
3. Когда будет утверждена B01/B02, добавить world save со стабильными IDs и сохранением частично снятых полос; проверить load/revisit без повторной награды.
4. Отдельно сравнить Streaming музыки с текущим Load Type на Player, сохранив codec/quality/volume и проверив start/loop. Продолжить человеческую A02 оценку текущего звука и feel.

### Повторение проверки

Только через Unity MCP, без OS automation. Сохранённую сцену не менять. Начать отдельный Play-сеанс; для stress добавить `PosterPerformanceProbe` на временный GameObject и задать уникальный ReportName под Logs. Totals/Scenarios/SampleFrames доступны только диагностике. После завершения выйти из Play. Для локальных сравнений выполнить [microbench.cs](../Logs/poster-audit/microbench.cs) через execute_code, заменив `RESULT` на имя результата. Functional/targeting/state/scaled-completion/geometry `.cs`-пробы лежат рядом; разрушительные completion-пробы запускать в свежем Play-сеансе. Затем запустить `Logs/poster-audit/analyze.ps1`. Для исходного A/B использовать точные три файла из before, восстановить актуальные файлы после опыта и перекомпилировать. Не сравнивать unfocused прогоны с focused и не считать runtime array capacity числом живых листов.
