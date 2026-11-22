# FPS Hand

Игровой ассет лежит в `Assets/JustOneMorePeel/Resources/FPS_Hand.fbx`. Unity сохраняет исходный GUID, импортирует три деформируемых меша, а `PeelToolView` выставляет отдельные хваты для карты, шпателя и отпаривателя. Кисть держит инструмент, а предплечье подстраивается под положение локтя с ограничением поворота запястья.

## Исходники

- `Blender/FPS_Hand_Source.blend` — исторический проектный риг и рукав.
- `Blender/FPS_Hand_Refined.blend` — текущий редактируемый ассет с текстурой.
- `Blender/import_makehuman_hand.py` — выделение и подгонка правой кисти MakeHuman. Подгоняет длину пальцев и веса запястья, создаёт полую манжету и обрезает текстурный атлас.
- `Export/FPS_Hand_Refined.fbx` — экспорт того же ассета. Материалы URP и обрезанная текстура находятся в `Assets/JustOneMorePeel/Art/FPS_Hand`.

Для повторного экспорта запусти Blender в фоновом режиме с `--python FPS_Hand/Blender/import_makehuman_hand.py -- base.obj default.mhskel default_weights.mhw young_lightskinned_male_diffuse.png`. Файлы сетки и костей находятся в `makehuman/data/3dobjs` и `makehuman/data/rigs` в зафиксированной ревизии. PNG находится в `skins/young_caucasian_male` официального пакета. Исходные ассеты распространяются под CC0 1.0. [MakeHuman a8bc2d5](https://github.com/makehumancommunity/makehuman/tree/a8bc2d54ff0ac92e78ff71431b1023eda42bf482/makehuman/data) · [Системные материалы](https://download.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html)

После экспорта скопируй FBX на игровой путь с сохранением его `.meta`. При обновлении модели проверь назначение URP-материалов и положение руки с игровой камеры. Поза пальцев рассчитывается в Unity по пяти цепочкам, у каждой есть маркер кончика, экспортированный из Blender.
