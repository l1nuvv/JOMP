# FPS Hand

Авторский ассет правой руки. Исходник Blender содержит три редактируемых меша с UV0, общим Armature и весами. Масштаб в метрах. 19 520 треугольников.

- Hand_Glove / M_Glove_Black — матовая чёрная перчатка.
- Shirt_Cuff / M_Cuff_Cream — кремовая манжета.
- Suit_Sleeve / M_Sleeve_Gray — тёмно-серый рукав.

Материалы используют сплошные PBR цвета и шероховатость. В проекте Unity назначены отдельные материалы URP Lit. Текстурных карт нет.

## Риг и импорт

В Unity используйте модель из Assets/JustOneMorePeel/Resources/FPS_Hand.fbx. Для импорта в другой проект выберите Rig = Generic, Scale Factor = 1, Convert Units = On, Import Animation = On, Optimize Game Objects = Off. Поместите объект под FPS-камерой и подберите локальную позицию и поворот под FOV. Добавьте Animator Controller с нужными клипами.

Root → Forearm → Wrist. Под Wrist находятся пять цепочек из трёх костей и GripSocket. Прикрепите предмет как дочерний объект GripSocket и выставьте его локальные смещение и поворот под форму и размер предмета. Для ручной анимации вращайте локальные оси фаланг, учитывая их исходные углы.

Клипы Hand_Idle и Hand_Hold рассчитаны на цикл. Последовательность взаимодействия — Hand_Reach, Hand_Grab, Hand_Hold, Hand_Release, Hand_Return. В Unity имена клипов имеют префикс FPS_Hand|. В Blender нейтральная поза доступна в Rest Position скелета.

## Проверка и границы

В папке Previews пять рендеров из Blender. Holding показывает позу без предмета. Blender проверил UV и отсутствие вершин без весов и неманифолдных рёбер во всех трёх мешах. Unity 6000.6.0f1 импортировала три SkinnedMeshRenderer, шесть клипов и GripSocket. PeelToolView использует эту модель с отдельными хватами для карты, шпателя и отпаривателя. Кадры из камеры Play Mode находятся в Logs/authored-grip-0.png, Logs/authored-grip-1.png и Logs/authored-grip-2.png.
