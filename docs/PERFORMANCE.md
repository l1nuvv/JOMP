# Performance checks

## Этап 5 — Windows x64 Player, 2026-11-06

PF-001. Диагностическая Development-сборка с единственной сценой `ThreeAreaPrototype`, Unity 6000.6.0f1. Windows 11, D3D11, драйвер 32.0.16.1742, Ryzen 7 3700X, RTX 4060 Ti, 32 ГБ RAM. Фактические 1920×1080, FullScreenWindow, PC, масштаб 1, VSync 0, без ограничения FPS. 11 790 кадров повторной серии с фокусом. В замере используется программная камера, а диагностическая запись CSV добавляет собственные аллокации. Целевой минимальный ПК из PR-05 остаётся отдельно.

| Сценарий | Кадр median / p95 / p99, мс | CPU p95, мс | GPU p95, мс |
|---|---:|---:|---:|
| От двери | 1,61 / 2,27 / 2,59 | 2,29 | 1,69 |
| У двери | 2,49 / 3,65 / 4,09 | 3,66 | 3,52 |
| Движение у двери | 2,59 / 3,56 / 3,90 | 3,56 | 3,13 |
| Подвал, свет включён | 2,33 / 3,37 / 3,69 | 3,37 | 3,28 |
| Магазин | 2,08 / 3,14 / 3,86 | 3,14 | 3,13 |
| Пар, 12 полос | 2,64 / 3,45 / 3,99 | 3,45 | 3,03 |
| Широкий пар, 20 полос | 2,74 / 3,48 / 3,79 | 3,48 | 2,98 |

- **PF-002/004/005.** В первом прогоне боковой ход дал p95 2,50 мс против 2,76 мс в неподвижном виде. В повторе — 3,56 против 3,65 мс. Медиана `Poster.Visibility` в повторе выросла с 0,019 до 0,348 мс при движении. Это измеримый вклад пересчёта, при этом в выбранной траектории кадр уложился в бюджет. Обычный и широкий пар сняли 9 и 10 листов с выплатами $72 и $80. В первом коротком окне подтвердилось число полос, до отрыва оно не дошло. Физический маршрут исходной жалобы требует отдельного воспроизведения.
- **PF-006.** GPU p95 у двери в парной серии 3,39 и 3,62 мс в базовых повторах, 2,27 без SSAO и 3,07 без теней. В освещённом подвале 3,00 и 3,11 мс в базовых повторах, 2,18 без SSAO и 2,66 без теней. Состояния менялись только в диагностическом процессе. Отдельный Frame Debugger в Editor зафиксировал Shadowmap и SSAO у двери, в освещённом подвале и при широком паре. Полные списки событий в `Logs/Peel-stage5-frame-debugger.json`. В Player предупреждения о недоступных вариантах Depth of Field и Panini Projection относятся к выключенным режимам в профиле, где mode и distance равны нулю. Дополнительно записаны ошибки DNS телеметрии Unity.
- **PF-007.** В 6500 кадрах отмечены три автосейва. Запись заняла 3,80–4,33 мс, полные кадры 5,64–6,30 мс, общие GC-аллокации этих кадров около 45,8 КБ. Путь изолирован на диске E:. Модель и отдельный медленный накопитель не измерены.
- **PF-008.** После трёх перезагрузок счётчики и размеры Mesh, Material, Texture и AudioClip стабилизировались. Между второй и третьей загрузками внутренняя память Unity в Intro изменилась на 596 байт, в подвале на 444 байта. Последние семь замеров private bytes процесса 880–919 МиБ при переходах. Эти точки включают выгрузку и повторную загрузку, их нельзя считать точными замерами одного и того же состояния процесса.
- **PF-010.** В отдельной трассе пара `Physics.Raycast` занял 39,1 мс суммарно на 2000 кадрах основного потока. Счётчик GC в Player показал медианы 3,8 КБ у двери и 6,3 КБ в магазине с учётом диагностического CSV. Объём, относящийся только к UI, ещё не выделен.
- **PF-003, AU-09.** До первого callback Intro прошло 3,84–4,11 с. При немедленном старте два кадра заняли 134–151 и 115–128 мс. Короткая трасса выделила первичную регистрацию `GPUResidentDrawer` около 91 мс и ожидание Present около 96 мс. После секунды в Intro худший кадр перехода в смену составил 43,0 мс. В нём около 15 мс заняли IMGUI и около 15 мс регистрация отрисовки. Следующий шаг с активным хватом занял 1,03 мс внутри кадра 6,78 мс. `PaperRustleCandidate` был загружен до хвата. `SoundManager.LoadFMODSound` виден при старте сцены и отдельно в серии первого отрыва паром (1,30 мс). Непосредственно на первом хвате маркер загрузки отсутствует. Холодные трассы и замеры находятся в `Logs/Stage5Player-cold-20261106` и `Logs/Stage5Player-intro-wait-20261106`.

Полные данные: `Logs/Stage5Player-baseline-20261106-171944`, `Logs/Stage5Player-recheck-20261106` и `Logs/Stage5Player-trace-20261106`. Порог p95 16,67 мс выполнен на измеренном ПК. Два кадра повторной серии превысили 16,67 мс при первом входе в смену.

## Этап 5 — базовая серия Editor, 2026-11-06

`ThreeAreaPrototype`, Unity 6000.6.0f1, PC, D3D11, 1167×555, VSync 0, без ограничения частоты. Ryzen 7 3700X и RTX 4060 Ti. Сценарии по 30 кадров прогрева и 120 измеренных кадров, 600/600 с фокусом. Камера ставилась программно. У двери `(0.79, 1.46, 1.58)` с поворотом `(337.41, 18.41, 359.96)`, боковой ход ±0,35 м по X. В подвале камера `(0, -0.4, 10)`, направлена вперёд. Журналы `Logs/Peel-stage5-editor-profile.txt` и `Logs/Peel-stage5-editor-frames.csv`.

| Сценарий | Кадр median / p95 / p99, мс | CPU кадр p95, мс | GPU p95, мс |
|---|---:|---:|---:|
| От двери | 9,47 / 11,08 / 11,49 | 11,14 | 3,77 |
| У двери | 10,13 / 11,97 / 15,79 | 12,01 | 4,06 |
| Движение у двери | 11,50 / 13,02 / 16,27 | 13,32 | 5,18 |
| Подвал | 10,16 / 11,82 / 13,74 | 11,80 | 4,15 |
| Магазин | 11,16 / 13,53 / 16,68 | 13,65 | 4,63 |

- **PF-002/005/010.** В этой серии боковой ход поднял p95 с 11,97 до 13,02 мс. Медиана `Poster.Visibility` выросла с 0,025 до 0,436 мс. Это вклад измеренного маркера, полная раскладка Mesh API и физический маршрут жалобы остаются открыты. Editor GC в кадре имел медиану 17 863 байта у двери и 20 259 в магазине. Для отделения UI и лучей нужен Timeline.
- **PF-003/004, AU-09.** В текущем импорте хват использует загруженный до действия `PaperRustleCandidate`. Первый программный шаг карты занял 1,317 мс внутри вызова, без замера холодного старта. Обычный и широкий пар захватили 12 и 20 полос, дали $24 и $40. Максимум последующих вызовов шага составил 2,275 и 2,237 мс. Это время вызовов на основном потоке. Нужны серии полных кадров и отдельный холодный запуск. `PaperDetach` при первом обращении был `Unloaded`, его первый запуск нужно выделить в трассе.
- **PF-007.** 25 прямых записей в изолированный временный каталог дали median/p95/p99 1,73/2,29/2,30 мс. Счётчик аллокаций вызывающего потока вернул ноль при создании данных сейва и для этой проверки непригоден. Пик кадра при автосейве и медленный накопитель требуют отдельной серии.
- **PF-008.** После трёх загрузок сцены по индексу 0 снимки Intro и подвала с циклом света показали одинаковые счётчики Editor: 1118 Mesh, 1144 Material, 1612 Texture и 5 AudioClip. Размеры по категориям также совпали. Счётчик памяти процесса вернул 0, поэтому память процесса остаётся открытой. `Logs/Peel-stage5-editor-reload-memory.csv`.

PF-001 и PF-006 ждут Player. Для целевого ПК паспорт [PR-05](FIRST_SESSION_BASELINE.md#pr-05-паспорт-будущего-player-замера) требует фактические 1920×1080 и p95 полного кадра ≤16,67 мс. Результаты Editor хранятся отдельной серией.

## Стена с миллионом плакатов — 2026-11-06

Открыть `Assets/JustOneMorePeel/Scenes/MillionPosterStress.unity` и нажать Play. Стартовая площадка сразу смотрит на всю стену. Удержание **Z** приближает выбранный участок. Сцена отдельная от основного маршрута и не входит в его профиль сборки.

На одной стене 1 000 000 отдельных раскладок: 1600 позиций по ширине × 625 по высоте. Каждая имеет собственный ID, дизайн, размер, поворот и разброс позиции и глубины. Размеры 0,69–1,125 м взяты из обычных дворовых плакатов основной сцены. Размер стены 1440×625 м, стартовая точка находится в 750 м от неё, чтобы полная раскладка входила в кадр.

Из разбора SHRED A BILLION применены построение геометрии участками, группировка по материалам и обновление затронутого участка при изменении. Здесь 80 мешей, представляющих 1 000 000 лицевых квадов. Для общего плана URP один раз снимает всю стену в текстуру. При приближении отображаются меши и Unity отсекает участки вне кадра. Раскладки создаются при запуске сцены, ходьба для загрузки плакатов не требуется. На полном плане перекрытия и ограничение пикселей экрана скрывают отдельные детали.

Финальный прогон в Editor, 1167×555, Ryzen 7 3700X / RTX 4060 Ti, три серии по 240 кадров с фокусом. Подсчёт индексов подтвердил 1 000 000 квадов в 80 мешах, обе границы стены поместились в кадр. Виртуальная клавиша Z переключила FOV 60° → 2° → 60° и вернула общий план.

| Ракурс | Медиана, мс | p95, мс | Кадров дольше 10 мс |
| --- | ---: | ---: | ---: |
| Общий план | 8,27 | 9,83 | 6 из 240 |
| Приближение | 8,23 | 9,45 | 4 из 240 |
| Общий план, повтор | 7,98 | 9,50 | 5 из 240 |

Строгий минимум 100 FPS не достигнут в этой серии. Максимум времени кадра 18,75 мс на общем плане и 10,94 мс при приближении. Стартовая генерация, переключение режима, Player и 1440p в эти серии не входят. Данные и кадры в `Logs/million-wall-delivery-20261106.csv`, `Logs/million-wall-delivery-20261106.png`, `Logs/million-wall-zoom-delivery-20261106.png` и `Logs/million-wall-zoom-check-20261106.txt`.

## Первая дверь — 2026-11-06

**Пакеты по материалам на всей двери.** Основной маршрут теперь объединяет неподвижную геометрию 50 дверных стопок по материалам и настройкам рендера. Изгибающиеся полосы рисуются отдельно. Изменение стопки перестраивает только затронутые пакеты. Коллизии и смена слоёв продолжают использовать исходные стопки.

В парной проверке на `ThreeAreaPrototype` при 1167×555 и 240/240 кадрах с фокусом на серию счётчик UnityStats снизился с 720 до 496 у двери. Медианы двух неподвижных серий до 10,80–12,10 мс, после 10,32–10,67 мс. При боковом движении 12,50 → 11,73 мс, p95 14,07 → 13,17 мс. Пять пар кадров 640×360 совпали пиксельно, включая изгиб полосы и открытие следующего листа. Настройки SSAO возвращены после пробы: альтернативные режимы Depth и After Opaque не дали выигрыша. Player и 1440p не измерялись.

Данные в `Logs/door-material-batch-audit-20261106.csv`, `Logs/door-material-batch-final-20261106.csv`, `Logs/door-material-batch-visual-20261106.txt`, `Logs/door-material-batch-clear-20261106.txt` и `Logs/door-ao-audit-20261106.csv`.

### Первое объединение полос

На текущей двери 50 стопок по 5 листов. В исходном сеансе с тремя снятыми плакатами неподвижный кадр у двери занимал 13,0–13,9 мс по медианам четырёх серий. Временно скрыли только рендереры бумаги, сохранив коллизии и остальную сцену. Кадр занял 9,51 мс, GPU 3,26 мс вместо 4,96–5,31 мс, счётчик UnityStats показал 155 вместо 1251 draw calls. Скрытие руки дало 13,51 мс. Это отдельные диагностические серии в Editor без фокуса.

Ровные полосы переднего и резервного слоя теперь рисуются одним мешем на лист. У каждой полосы остаётся коллайдер. При первом изгибе полоса выходит из общего меша и рисуется своим деформируемым мешем. Языковые материалы обновляются для обоих представлений.

В повторном A/B исходные полосы и оптимизированный меш переключались в одном запуске. ThreeAreaPrototype, PC, VSync 0, 1167×555, 240/240 кадров с фокусом в каждой серии. Камера (0, 1,46, 2,15) направлена в центр первой двери. В движении использован тот же ракурс с боковым смещением до 0,32 м.

| Ракурс | Медиана до → после, мс | p95 до → после, мс | UnityStats draw calls |
| --- | ---: | ---: | ---: |
| Неподвижно, две серии | 12,32–12,38 → 10,43–10,51 | 13,53–13,67 → 11,44–11,59 | 1228 → 720 |
| В движении, две серии | 13,92–13,95 → 12,11–12,13 | 15,29–15,52 → 13,76–14,02 | 1272 → 738 |

В первой парной пробе фокус менялся, а p95 движения ухудшился с 13,31 до 15,54 мс. В повторе с постоянным фокусом обе серии движения дали более низкий p95. В четырёх сравнительных кадрах 640×360 отличия больше 8 уровней на канал обнаружены в 0–4 пикселях на кадр. Прямой прогон снял все 250 плакатов на двери, начислил $2000 и подтвердил переключение рендера и коллайдеров без расхождений. Player и 1440p не измерялись.

Данные в `Logs/door-fps-audit-20261106.csv`, `Logs/door-fps-paired-20261106.csv`, `Logs/door-fps-paired-repeat-20261106.csv`, `Logs/door-fps-visual-check-20261106.txt` и `Logs/door-fps-clear-20261106.txt`.

## Camera motion and wide steam — 2026-11-05

### Batched index upload experiment

Tested a single 16-bit index-buffer upload followed by batched submesh descriptors instead of one `SetTriangles` call per material. All 1,295 vertex, UV, normal, index and mesh-bounds snapshots matched the original across the same 37 camera positions, including 219 shrink and 212 growth transitions. Submesh bounds and edge cases outside those positions were not independently exhaustively verified.

Under the existing focused movement probe, the original series measured 9.08 / 10.12 and 9.37 / 11.61 ms median / p95. The candidate measured 9.40 / 10.99 and 8.96 / 10.97 ms. The frozen-geometry control measured 7.38 / 8.35 ms in the candidate run. These mixed full-frame results do not establish an improvement. Reverted the candidate rather than retaining the additional index-buffer complexity. Evidence: `Logs/moving-mesh-20261105-index-batch.csv` and `Logs/moving-visibility-ab-20261105-index-batch.csv`; the existing pre-change snapshots and probe are referenced below.

### Breakdown of the moving-camera visibility work

Captured two short Editor CPU traces with temporary nested `ProfilerMarker`s, using the same courtyard trajectory, 1556×749, PC/D3D11 and VSync 0. The enclosing 90-frame series had 90/90 focused frames. Each trace retained 20 instrumented frames. In the first trace `Poster.Visibility` averaged 0.940 ms/frame: clipping calculations 0.041 ms, constructing visible geometry 0.173 ms, and mesh API calls 0.688 ms. In the second trace the corresponding totals were 0.916, 0.040, 0.168 and 0.668 ms. Nested markers add measurement overhead, so these are diagnostic Editor CPU times, not a standalone benchmark or GPU transfer timings.

In the second trace, mesh clear/submesh setup accounted for 0.270 ms/frame, vertex/UV/normal calls 0.125 ms, and triangle calls 0.251 ms. These child measurements are contained within the upload measurement and must not be added to it. This points to repeated mesh operations as the main cost within this sample. The previously tested conditional-clear and dynamic-mesh candidate reduced the marker but did not establish a full-frame gain. Future work should investigate reducing mesh submissions while preserving exact partial borders, UVs and complete collision. No further production optimization is claimed from these traces.

Evidence: `Logs/moving-visibility-decomposition-{cpu,marked,upload}.raw`, the matching `.txt` summaries, and `Logs/moving-visibility-decomposition-{marked,upload}.csv`. The first attempted trace had the CPU profiler area disabled and is excluded. Temporary markers were removed after capture.

`ThreeAreaPrototype`, authored depth and placements, PC quality, D3D11, RTX 4060 Ti / Ryzen 7 3700X, 1556×749, VSync 0, cap −1. At (0, 1.46, 1.10), wide steam selected five reachable stacks. Unity's virtual mouse held for 720 focused frames, with up to 20 active strips. It cleared 25 posters in five waves of five. Median / p95 was 8.43 / 10.58 ms, peak completion frame 8.92 ms, and main / GPU medians 3.62 / 1.53 ms. Scripted lateral camera motion of ±0.45 m while aiming at the same point gave 9.19 / 10.43 ms in a 400-frame series. Stationary baseline was 7.02 / 7.74 ms. This movement simulates the camera path without physical controller input.

In a fresh scene, a bracketed movement control temporarily froze only `PeelPosterStack.LateUpdate`, keeping cached geometry rendered. The full path and warmup were identical for all three 400-frame focused series. Visibility recalc showed 0.88 and 0.89 ms median in the normal series and zero when frozen. This control deliberately displays stale occlusion at moving viewpoints. Its frame difference includes the effects of submitting different geometry and does not justify freezing the production renderer.

| Movement series | Frame median / p95, ms | `Poster.Visibility` median, ms |
| --- | ---: | ---: |
| Normal A | 9.08 / 10.12 | 0.88 |
| Cached geometry only | 7.20 / 8.32 | 0 |
| Normal B | 9.37 / 11.61 | 0.89 |
| Dynamic mesh and conditional clear A | 9.62 / 11.58 | 0.75 |
| Candidate cached geometry only | 7.04 / 8.20 | 0 |
| Dynamic mesh and conditional clear B | 9.23 / 11.35 | 0.75 |

The candidate used `Mesh.MarkDynamic`, reused the existing index buffer when vertex count did not shrink and skipped redundant submesh-count assignment. Hashes matched for all 1,295 mesh snapshots across 37 camera positions, including 219 shrink and 212 growth transitions. Hashes covered bounds, vertices, UVs, normals and triangle indices. Lower visibility marker time did not translate to consistent improvement in frame time or p95. Reverted the candidate. The result isolates camera-dependent geometry as a measurable contributor in the courtyard, while the user-reported severe lag and Player performance remain open. Any future batching or clipping change needs the same geometry checks and frame-level validation.

Evidence: `Logs/wide-steam-20261105.csv`, `Logs/moving-visibility-ab-20261105.csv`, `Logs/moving-visibility-ab-20261105-buffer-reuse.csv` and `Logs/moving-mesh-20261105-{before,after}.csv`. Probe bodies are stored alongside the CSVs.


## Held tools and first grip — 2026-11-05

Unity MCP ran the authored `ThreeAreaPrototype` from fresh Play Mode, with world saving bypassed for the probe. The camera stayed in the courtyard at (0, 1.46, 1.35). A virtual Input System mouse held the normal peel input. The test used the first scraper and standard steam, with three reachable stacks in the latter's aim. PC quality, D3D11, RTX 4060 Ti / Ryzen 7 3700X, 1556×749, VSync 0, frame cap −1. All reported samples were focused. The repeat sampled the mouse in the runtime input callback. The initial trace's editor-side mouse column read zero despite confirmed grabs and payouts, so it is retained only as secondary evidence.

| Repeat series | Samples | Cleared posters | Peak active strips | Frame median / p95, ms | Main / GPU median, ms |
| --- | ---: | ---: | ---: | --- | --- |
| Idle before | 240 | 0 | 0 | 6.95 / 7.86 | 2.55 / 1.47 |
| Held scraper | 600 | 2 | 4 | 7.84 / 9.53 | 3.21 / 1.48 |
| Held standard steam | 600 | 15 | 12 | 7.91 / 9.29 | 3.37 / 1.44 |
| Idle after | 240 | 0 | 0 | 7.34 / 8.50 | 2.84 / 1.49 |

Two initial runs had a 38.97 ms and 28.94 ms frame at the first scraper grip. Completed poster transitions were below 9 ms in both runs. The Editor profiler's in-memory history returned no frames, so a separate tagged binary capture was used. Its first tagged frame was 29.951 ms, with 20.712 ms in `SoundManager.LoadFMODSound` on the main thread. The other tagged frames were 7.913–8.427 ms.

`PeelGame` plays `PaperRustleCandidate.wav` at grip start. The clip had preload disabled. Enabled preload in `AudioImporter.defaultSampleSettings` through Unity MCP and reimported the single clip. Decompress On Load and Vorbis were retained. The first importer command used a retired property and failed at compilation without changing the asset. Repeated from fresh Play Mode with a short binary capture around the same grip. Four tagged frames measured 8.371–9.097 ms. `SoundManager.LoadFMODSound` was absent from all 36 recorded main-thread frames, and the CSV confirms the held input and four active strips. This verifies removal of the reproduced first-grip stall in the Editor. Scene-start cost, standalone Player, physical input and other sources of lag remain unmeasured.

Evidence: `Logs/held-tools-20261105*.csv`, `Logs/held-tools-20261105-probe.cs`, `Logs/held-tools-20261105-trace-markers.txt` and `Logs/held-tools-20261105-preload-markers.txt`. Raw profiler captures are `Logs/held-tools-20261105-first-grip.raw` and `Logs/held-tools-20261105-preload.raw`.

### Startup cost of audio preload

Unity MCP alternated the clip's importer setting over eight focused Play Mode entries into `ThreeAreaPrototype`. Each trial reimported the clip before starting the timer. The timer ran from the Play request until the Editor callback observed an initialized Intro with 450 posters. Domain and scene reload were disabled in the existing Editor settings. The game never entered the shift during this test.

| Clip preload | Entries | Median to Intro, ms | Range, ms | Clip at Intro |
| --- | ---: | ---: | --- | --- |
| Off | 4 | 453.71 | 451.30–475.77 | Unloaded in 4/4 |
| On | 4 | 469.06 | 467.35–472.04 | Loaded in 4/4 |

The median difference was +15.35 ms, with overlapping ranges and four samples per setting. This measures total Editor entry time under those settings. Exact audio-only load time and standalone cold launch remain unmeasured. Kept preload enabled because it made the clip ready by Intro and the earlier first-grip trace verified removal of the runtime load spike. Evidence: `Logs/peel-preload-startup-20261105.csv` and `Logs/peel-preload-startup-20261105-probe.cs`.


## Active paper follow-up — 2026-11-05

In the Editor on RTX 4060 Ti / Ryzen 7 3700X at 1556×749, PC quality, D3D11 and VSync off, `PosterPerformanceProbe` redistributed 450 logical posters over 35 stacks in `ThreeAreaPrototype`. The generic log header says 19 patches because it predates this scene. Each series had 30 warmup frames and 120 focused samples. The probe first waited in paused Play Mode. Sampling began after resuming.

| Series | Frame median / p95, ms | Main / GPU median, ms |
| --- | --- | --- |
| Idle before | 7.51 / 9.07 | 2.73 / 1.51 |
| 12 deformed strips and scrap pool activity | 8.55 / 9.45 | 3.66 / 1.80 |
| Idle after | 7.84 / 8.79 | 3.12 / 1.65 |

Raw data: `Logs/reference-followup-20261105.csv` and `.txt`. The synthetic load exercises deformation and pooled scraps. It does not exercise tool input, selection of targets, completed peel transitions or the authored distribution of posters. The final idle series may still include falling scraps. The results apply to this Editor workload and do not identify the cause of user-reported lag. Next check: profile sustained peels and steam in the authored scene and capture the slow frames.

## Thirty-design content budget — 2026-10-28
Retained 19 patches, two reusable four-strip buffers each, complete deep collision, clipped/culled render geometry and the 32-entry scrap pool. The catalogue has 30 designs per language. Each patch rotates ten distinct deep designs selected from that complete catalogue; all thirty appear on the wall. This bounds deep submeshes/material groups at the former ten rather than tripling every patch's material submissions. New prints export at 512 px width; existing source PNGs/GUIDs are retained. Physical aspect is stored separately from power-of-two GPU import sizing, with each paper fitted inside its original maximum sheet footprint. Only PNGs fully opaque in **both** languages qualify as deep occluders; holes/remnants retain their lower geometry. Occluder projection now accounts for each sheet's width as well as height; geometry verification is in PLAYTEST.

Actual runtime texture formats: **60/60 BC7**, clamp/bilinear, mipmaps, no Read/Write, maximum imported dimension 1024. `Logs/PosterVariety/textures.txt` reports **35.03 MiB per language / 70.06 MiB for both** using `Profiler.GetRuntimeMemorySizeLong` in Editor; do not interpret this Editor allocation metric as a standalone GPU or Player-memory measurement. Both languages remain referenced for instant switching.

Same hardware, PC/D3D11, 1167×556, vSync off; existing WallFrameProbe, 40 warmup + 150 measured frames per mode. Raw CSVs and report text are in `Logs/PosterVariety/`:

| Scenario | Before catalogue expansion | Final thirty-design content |
|---|---:|---:|
| Close median / p95, ms | 9.31 / 11.46 | 7.18 / 8.09 |
| Repeated close median / p95, ms | 9.46 / 12.43 | 7.16 / 7.82 |
| Moving close median / p95, ms | 11.81 / 14.42 | 9.40 / 10.35 |
| Close / moving draw calls | 740 / 670 | 740 / 670 |

These runs compare actual changed art/proportions as well as code. The baseline was 0/150 focused frames and the final sample 150/150; background running was enabled, but focus differs and invalidates a strict isolated speedup claim. Final close worst frame 18.47 ms. No Player run, controlled physical-input test or universal FPS improvement is claimed; these local observations support the bounded content budget only.

Preserved rejected attempts: `after-first-attempt.*` used all 30 deep materials per stack (1685 close draw calls) and NPOT import with `npotScale=None`, which silently produced RGBA32 for many prints despite BC7 settings (256.10 MiB total Editor texture allocation). `import-diagnostic.txt` records the actual platform, support and formats. A one-texture test changed only NPOT scaling and produced BC7; applying it to the set, then separating physical aspect, corrected the issue. `after-before-format-fit.*` retains the intermediate compressed/local-rotation run (755 close draws) before tall-sheet size correction. None is substituted for the final result. Full runtime/geometry checks pass; human assessment of the new art remains open.

## Comprehensive poster audit — 2026-10-28
Latest scope, evidence and limits: [POSTER_SYSTEM_AUDIT](POSTER_SYSTEM_AUDIT.md). Retained geometry/index/material buffers removed recurring deep-layer construction churn and the redundant initial render upload. Local 608-transition median 0.4051→0.3163 ms; p95 0.5338→0.3475. Summed Mono-used growth 32075776→0 bytes in this experiment; this is approximate heap growth with collection-spanning samples excluded, not exact thread GC. The thread allocation API failed a 1 MiB positive control. A CPU trace independently records 38 warmed deep builds with no nested GC.Alloc events. Detailed methods and failures are preserved under `Logs/poster-audit/`.

Six logical-depth totals × six camera/effect scenarios × 120 measured frames, 30 warmup each, 1167×556, PC, D3D11, same hardware. **Frame performance is mixed; no broad FPS improvement is claimed.** At 2432 posters close median 10.60→10.85 ms and p95 12.97→14.00; moving 11.96→12.35. At 5000 moving 12.96→12.88. The slower 2000 moving sample and an unfocused longer repeat remain in the report. Active-12 final run is separate after moving held flutter to LateUpdate; CPU trace confirms 189 steady frames at 12 deformations per frame. These depth tests retain 19 patches. Independent-patch scalability, Player builds and physical held-input/steam profiling remain unverified. Draw/triangle counts alone do not close the owner FPS issue.

## Partial edge clipping follow-up — 2026-10-28
Owner reported 35–40 FPS close, ~54 FPS at full-wall distance and ~40 while peeling after the first visibility fix. The remaining conservative case was a lower quad with any exposed border: its entire surface still rendered. Now the nearer opaque Cinema rectangle is projected onto each deeper sheet's plane; only the intersection is omitted and up to four textured border rectangles render. Full original collision geometry remains. UVs are derived from original sheet coordinates; the mesh index buffer is cleared before shrinking vertices. Managed caches rebuild if lost during Play Mode script reload.

Controlled reproduction used the recorded owner camera (0.79,1.46,1.58), Euler (337.41,18.41,359.96), 1167×556, same PC settings and focused 150-frame samples. `Logs/partial-before.txt/.csv`: close median 14.86 / p95 16.95 ms, repeat 13.29 / 15.76, moving 12.70 / 15.04, no deep render 8.33 / 9.68. Final `Logs/partial-final.txt/.csv`: close 10.41 / 12.79, repeat 8.69 / 9.83, moving 11.14 / 14.73, no deep render 8.61 / 10.89. Worst close/moving frames were 27.05/24.11 ms, so occasional spikes remain. Resolution differs from earlier 1556×749 runs: do not compare their absolute FPS. Peeling was functionally tested, but no physical held-input performance recording is claimed.

`Logs/partial-correctness.txt`: 31887 rendered-area, exposed-edge UV and complete-collision checks, zero failures at centre/side/high camera positions. `Logs/partial-runtime-checks.txt`: 21870 PASS / 0 FAIL, all 2432 removed, exact payout and bounded meshes/objects. `Logs/partial-targeting-checks.txt`: 13 PASS / 0 FAIL. Final runtime Console: no error/warning entries. `Assets/Screenshots/screenshot-20261028-165406.png` inspected at owner camera. Intermediate failures are retained in `Logs/partial-cache-errors.json` and `Logs/partial-mesh-errors.json`: cache recovery and shrinking-index ordering were fixed before final verification; diagnostic invocations while out of Play Mode were rejected and are not success evidence. `Logs/partial-geometry-probe.cs` is the repeatable geometry check. The automatic WallFrameProbe now also supports OwnerView and named output files. Owner retest remains open.

## Applied optimization methods — 2026-10-28
Added PeelWallVisibility: cached conservative patch bounds, reused six frustum planes, render-only distance culling at 60 m, and a 2 m spatial grid for steam neighbours. Culled patches skip their camera-dependent deep-occlusion updates; renderer.forceRenderingOff preserves removal state and colliders. Bounds include paper curl. Authored patches are stationary; moving walls would require index/bounds rebuilding.

PC URP already had SRP Batcher and GPU Resident Drawer InstancedDrawing enabled; static scenery already used four combined meshes. Enabled the PC asset's GPU occlusion culling in cameras. No suitable repeated static mesh groups were found for separate manual instancing; deforming strip meshes are unique. The 32-entry scrap pool remains active. The failed atlas remains reverted. Flat paper already has minimal base geometry, so no redundant LOD meshes were added; distance culling provides the terminal visibility cutoff. Addressables were not installed: the single-scene prototype has no chapter streaming/content-delivery requirement justifying a new integration.

`Logs/optimization-methods-performance.txt/.csv`, focused 1556×749 automated test: close median 9.23 / p95 11.64 ms, away 8.30 / 9.59, far 11.34 / 12.95, close repeat 9.78 / 11.80, moving close 10.73 / 13.09 (worst 15.28). GPU occlusion adds passes and raises draw counts; counts are not the success criterion. These are Editor results on the recorded hardware, not a universal Player FPS guarantee. `Logs/optimization-methods-checks.txt`: 61 PASS, zero FAIL for spatial equivalence, frustum/distance hiding, collider retention and return to view; `Logs/methods-targeting-checks.txt`: 13 PASS; `Logs/methods-runtime-checks.txt`: 21870 PASS, all 2432 posters removed with exact payout and bounded objects/meshes. Console: zero runtime errors/warnings. Owner visual/FPS acceptance remains open.

## Current measured fix — 2026-10-28
Root-cause comparison: `Logs/wall-frame-before.txt/.csv`, fixed camera scenarios, 40 warmup + 150 consecutive measured frames each at 1556×749, PC quality, VSync=0, RTX 4060 Ti / Ryzen 3700X. All 150 frames in each case were focused. Near-wall median 31.57 ms, away 8.09, far 13.20; disabling only deep renderers gave 8.04, disabling only deep colliders 30.56, disabling SSAO 27.46; repeated close 30.44. Deep paper overdraw, not collision or draw-call count alone, dominates this reproduction.

Implemented conservative per-stack view occlusion in PeelPosterStack. Every deep quad remains in the vertex data and complete separate collision mesh. The renderer skips a quad only if all its corners projected onto a nearer fully opaque Cinema sheet fall strictly inside that sheet. Side views with exposed edges retain the entire quad; no atlas or arbitrary layer cap is used. Visibility is recomputed when the camera position changes or buffers/materials change; triangle buffers update only when visibility changes. The opaque design is the existing 02_Cinema texture; both EN and RU alpha were checked as fully opaque. Changing that design to include transparency requires updating this assumption.

Same automated test after the change (`Logs/wall-frame-after.txt/.csv`): close median 10.60 ms / p95 13.68; away 9.36; far 10.95; close without deep renderer 9.29; close without deep collision 11.79; close without SSAO 9.00; close repeat 10.77 / p95 11.91. Separate moving-close test (`Logs/wall-frame-moving.txt/.csv`): median 12.89, p95 16.25, worst 18.52 ms, 150 focused frames. These are measured Editor conditions, not a universal FPS or Player guarantee. Draw calls remained about 221 close and 818 far: avoiding hidden fragments fixed this reproduction despite similar draw-call counts.

Verification: `Logs/occlusion-correctness.txt` records 22511 projection/ray and collision checks, 5604 hidden-quad cases, zero failures across centre/side/above/behind camera positions. `Logs/occlusion-runtime-checks.txt`: 21870 PASS / 0 FAIL including all 2432 poster removals, exact payouts, unchanged layer geometry/material promotions and bounded objects/meshes. `Logs/occlusion-targeting-checks.txt`: 13 PASS / 0 FAIL for range, occlusion, release, steam and cart. `Logs/occlusion-final-check.txt`: opaque Cinema in both languages, original language restored, fresh Intro with 2432 posters, no diagnostic component attached. Runtime Console had no errors/warnings. Game-camera frame `Assets/Screenshots/screenshot-20261028-162551.png` was inspected. WallFrameProbe is an Editor-only reusable automatic test and is never attached to the saved scene. Owner retest remains open.

## Historical investigation
Do not equate simplicity with optimization, or Editor counters with Player FPS.
Known work: 100 static scenery renderers combined into five material meshes; idle PeelStrip Update disabled; paper shadow casting disabled. Deformation only concerns active paper; no per-frame global paper search.
Earlier Editor UnityStats snapshots changed from 635 to 343 draw calls under different observed captures; these are diagnostic observations, not a controlled benchmark and not proof of a specific speedup.
For a performance task capture the same viewport, resolution, hardware, Play/Player mode and scenario. Measure CPU/GPU frame time, GC allocations, draw calls, triangles and active paper during idle, peel, steam and falling scraps.
Only build a Player when requested or when a specific authorized performance investigation needs it. Never package routinely.

2026-10-28 logical-depth change: 2432 posters use 152 reusable display strips (front/back buffers on 19 stacks) and 32 pooled falling visuals. The complete aimed-peeling simulation in Logs/Peel-progression-checks.txt verified unchanged Transform and Mesh counts across clearing all 2432 posters. This proves those count invariants only; it is not an FPS, CPU/GPU frame-time, GC allocation or Player benchmark. No claim is made for a 500,000-poster scenario.

2026-10-28: `Assets/Settings/Build Profiles/Windows Standard.asset` is a Windows x64 Player profile with FirstShift, development and deep profiling disabled. It inherits the project PC quality/URP settings and is not evidence of measured optimization. It was not used to build a Player. Measure target hardware and representative idle/peel/steam scenarios before selecting further quality changes or claiming performance. The deeper poster meshes added after the older count test require a new performance assessment.

2026-10-28 owner feedback: close to the wall, perceived FPS falls to roughly 30–40; this is a human observation, not a benchmark. A preliminary Editor MCP probe at camera (0, 1.45, 2.25), facing the wall, reported GPU 6.33 ms in one sample; temporarily disabling the 19 combined deep-layer renderers reported 2.66 ms in one sample, then the renderers were restored. Both used the same view, but isolated samples, Editor scheduling and vsync prevent a performance conclusion. A separate away-facing sample reported GPU 6.68 ms and is not sufficient for a comparison. The deep meshes contain 9576 vertices and 190 material slots; counts do not prove the bottleneck. Capture controlled repeated frame timings and GPU/CPU data, with equivalent resolution, peel and steam cases, before and after a change; preserve the progressive reveal and occlusion.

2026-10-28 ordering experiment: deep-sheet submeshes now order material groups by their nearest sheet, and omit empty material groups; geometry, materials and collider sheets remain. At fixed close camera (0,1.45,2.25), Editor Game view, vsync 60, six sparse GPU frame timings before were 5.783, 5.702, 6.238, 7.928, 6.822, 6.846 ms. Six after compilation were 5.569, 5.439, 5.571, 6.166, 7.077, 6.370 ms. Medians 6.530 vs 5.868 ms; overlapping ranges and sparse Editor sampling prevent a reliable speedup or FPS claim. A frame timing callback probe failed due to connection closure; data were not collected from it. One temporary reorder command timed out, but a subsequent check confirmed 19/19 stacks had reordered before the temporary experiment was discarded. Editor frame times remained around 29–34 ms in ordinary samples, with one 1629 ms outlier before. No Player benchmark, CPU/GPU trace, peel/steam performance comparison, or owner retest was completed; the reported severe close-wall slowdown remains unresolved.

`Logs/fps-runtime-checks.txt`: 21870 PASS, zero FAIL, all 2432 posters removed, exact payout, display-buffer and deeper-quad promotions, fixed object and mesh counts. `Logs/fps-targeting-checks.txt`: 13 PASS, zero FAIL for targeting, occlusion, steam, release and cart. Console had no errors/warnings; this is functional verification, not a performance result.

Owner Game view Stats comparison, 2026-10-28: close to centre of wall about 29 FPS / 33 ms frame, GPU 10–13 ms, 20k tris, 41k vertices, 512 draw calls; facing away 100–103 FPS / 8–10 ms, GPU 5–6 ms, 4k tris, ~10k vertices, 25 draws; facing wall from further away ~80 FPS / 12 ms, GPU ~6 ms, 160k tris and vertices, 815 draws. Allocated memory ~3300 MB and texture memory 66 MB at close view. These are observed Editor Stats, not controlled Player profiling. Near-wall has worse FPS even with fewer draws and triangles than the distant view; counts alone do not explain the slowdown. MCP confirmed the distant observation at camera (0.32, 1.45, -3.61), 815 draws and ~160k triangles, PC quality, VSync 0 and targetFrameRate -1; one sampled frame reported 10.41 ms CPU frame, 2.75 ms main, 1.90 ms render and 5.85 ms GPU. Capture close-view CPU Main/Render and profiler timeline before attributing the bottleneck. Preserve the player's current unsaved session during investigation.

Subsequent actual Profiler recording at the owner's close-wall position: `Logs/wall-profile.raw`, with `Logs/wall-profile-summary.txt` summarising every fourth frame from 40–316 (70 frames). Main-thread sample averages include `WaitForLastPresentation` 20.49 ms, `SceneView.Paint` 10.08 ms and `GameView.Paint` 10.98 ms. These markers are nested, sometimes recorded more than once per frame, and cannot be added; the 39.03 ms aggregate UpdateScene demonstrates this. A short separate capture temporarily maximising Game view (`Logs/wall-maximized-profile.raw`, 27 frames) removed SceneView.Paint but reported 64.48 ms average WaitForLastPresentation; focus, viewport size and timing differed, so this is not a valid performance comparison or proof of a game bottleneck. Game view was restored. The presentation wait warrants investigation of Editor focus, frame pacing and graphics synchronization alongside a controlled player-camera comparison. No new optimization or FPS fix was made by this diagnostic.

Same position and viewport, 2026-10-28: Frame Debugger reported DrawDepthNormalPrepass and four SSAO render-graph passes before opaque rendering. The PC renderer's SSAO was temporarily disabled and restored; five sparse Editor CPU frame samples in ms were 31.706, 34.939, 33.303, 30.496, 31.006 with SSAO; 14.343, 28.721, 29.108, 33.051, 29.411 without. GPU timing included zero/unavailable and a 24.754 ms outlier, so it is not a reliable comparison. The samples suggest modest benefit but overlap and are too sparse to establish a reliable speedup or justify claiming the severe slowdown is fixed. Captures `Assets/Screenshots/screenshot-20261028-160728.png` (off) and `screenshot-20261028-160737.png` (on) show this one close-wall view. SSAO was restored, no renderer asset change was saved. Frame Debugger was disabled, Play resumed, and the owner's one removed strip was preserved. Console contained no errors/warnings. Next investigate the duplicated depth/normal work and deep paper's many material draws with repeatable GPU capture and a representation that retains alpha edges and depth; no packaging/build was authorized.

Atlas optimization, 2026-10-28: the ten notice textures per language are packed into two 4096² atlases (`Resources/DeepPaperEnglish` / `DeepPaperRussian`, with adjacent rectangle mappings). Each deep-layer mesh now uses one material and one submesh in layer order rather than up to ten. All 128 logical layers, geometry, alpha cutouts and MeshColliders remain; the two interactive buffers still use their original separate materials. At approximately the previously observed close camera UnityStats reported 92 draw calls versus 219, and at the previous far camera 475 versus 815, with comparable triangle counts; these are Editor diagnostics from separate sessions, not an FPS guarantee. GPU timing often returned zero or focus-throttled samples, and a controlled Player test was not run. The uncompressed 4096² atlases have a substantial texture-memory cost (at least 64 MiB per loaded atlas; the measured Russian texture reported 128 MiB) and must be considered when adding notice designs. Both localized atlases and their mappings must be regenerated together if the ten source textures change.

Owner retest: close to wall 20–33 FPS, global frame time 28–32 ms, GPU 9–15 ms, 101–107 draw calls and 10k triangles; no improvement and potentially worse. The atlas implementation and generated files were reverted. The prior near-to-far material grouping and collision geometry were restored; fresh Play had 2432 posters and 19 stacks, and runtime Console had no errors/warnings. Three pre-existing ArtRevision obsolete-API warnings occurred at compilation. FPS cause remains unresolved. This feedback supersedes the atlas implementation status above; the historical measurements and checks below document the rejected experiment.

`Logs/atlas-runtime-checks.txt`: 21870 PASS / 0 FAIL, all 2432 posters, exact payout, preserved layer geometry and atlas UV for each promoted deeper layer; `Logs/atlas-targeting-checks.txt`: 13 PASS / 0 FAIL for targeting, occlusion, steam, release and station; `Logs/atlas-languages-check.txt`: both languages updated all 19 meshes and the original language was restored. Nine sampled Cinema texture points agreed with the atlas to 0.000013 maximum channel error. `Assets/Screenshots/screenshot-20261028-161008.png` was inspected from the Game camera. Compilation succeeded with three historical obsolete API warnings from ArtRevision; runtime Console had no errors/warnings. Fresh Play started at Intro with 2432 posters and music playing. Human FPS and visual playtest remain outstanding.
## Этап 6 — предварительная проверка PF-009 в Editor

Один неподвижный ракурс подвала в `ThreeAreaPrototype`, свет включён, Game View 1167×555, фокус во всех 540 измеренных кадрах. По 60 кадров прогрева и 180 замеров на вариант. Варианты по очереди запускались в одной временной сессии Play Mode до начала смены.

| Вариант | Участки / листы подвала | Медиана / p95 кадра, мс | Попадания прямым лучом в центр передней полосы |
|---|---:|---:|---:|
| Контроль | 7 / 104 | 9,58 / 10,61 | 18/28 |
| Удвоенная глубина | 7 / 208 | 9,66 / 10,92 | 18/28 |
| Удвоенное число участков | 14 / 208 | 10,48 / 12,13 | 20/56 |

В третьем варианте пробный ряд смещён вниз на 1,1 м. Сетка дополнительных лучей дала 16/28 попаданий по исходному ряду и 18/28 по новому. Прямые лучи пропускают часть полос уже в контроле, поэтому эти результаты показывают чувствительность к расстановке, но не устанавливают игровую достижимость. Геометрия пробы не сохранена. Журналы: `Logs/Peel-stage6-pf009-editor.csv`, `Logs/Peel-stage6-pf009-editor.txt`.

Во второй серии камера смещалась на ±0,45 м. При том же разрешении и фокусе p95 составил 15,09 / 14,82 / 14,66 мс соответственно. Через каждые 30 кадров принудительно перестраивался один слой через восстановление прогресса. Максимум из шести перестроений на вариант составил 0,77 / 0,38 / 0,40 мс. Различие видимых участков между статической и подвижной сериями не позволяет напрямую сравнивать их абсолютные времена. Снятие листа обычным хватом в этой серии не измерялось. Журналы: `Logs/Peel-stage6-pf009-motion.csv`, `Logs/Peel-stage6-pf009-motion.txt`.

В итоговой сцене LV-09 сохранены 7 участков подвала и 9 архива. После расширения измерены 1200 кадров в Editor при 1167×555 с фокусом. Смены слоёв в сериях хвата выполнял игровой `StepPeeling`.

| Итоговая сцена | Кадров | Медиана / p95, мс | Снято листов |
|---|---:|---:|---:|
| Подвал, движение | 240 | 14,22 / 18,83 | 0 |
| Подвал, удержание пара | 360 | 13,57 / 18,49 | 15 |
| Архив, движение | 240 | 16,26 / 35,14 | 0 |
| Архив, удержание пара | 360 | 13,14 / 17,79 | 5 |

Замер Editor выявил выбросы, особенно при движении камеры в архиве. Бюджет Player при 1920×1080 остаётся открытым. Автопроверка отклонила создание диагностического Windows Player без прямого запроса на сборку. Журналы: `Logs/Peel-stage6-final-editor.csv`, `Logs/Peel-stage6-final-editor.txt`.
