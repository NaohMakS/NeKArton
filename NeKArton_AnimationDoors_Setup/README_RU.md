# NeKArton — анимация с отдельным Idle и переходы через двери

## Установка
1. Сначала слей PR #6: https://github.com/NaohMakS/NeKArton/pull/6
2. Распакуй архив прямо в корень Unity-проекта (рядом с `Assets` и `ProjectSettings`).
3. Убедись, что исходные файлы находятся здесь:
   - `Assets/Sprite/движение_гг.png`
   - `Assets/Sprite/движение_насти.png`
4. Скопируй `Assets/Scripts/NastyaCameraCue.cs` в `Assets/Srpt/`, заменив старый файл.
5. Открой сцену `Location-1`, дождись компиляции и выбери `NeKArton > Generate padded character frames`.

## Настройка спрайтов
Генератор создаст:
- `Assets/Generated/Player/Idle.png` и `Walk_00.png`, `Walk_01.png`...
- `Assets/Generated/Nastya/Idle.png` и `Walk_00.png`, `Walk_01.png`...

Первый кадр сохранён отдельно для неподвижного состояния. Вокруг каждого кадра добавлено прозрачное поле 32 px, чтобы между изображениями было больше свободного пространства и не обрезались края.

В Inspector:
- у объекта игрока (`PlayerMovement`) назначь `Player/Idle.png` в поле `Idle Sprite`;
- в `Walk Frames` добавь все `Player/Walk_*.png` строго по возрастанию имени;
- у `Nastya_NPC` в `NastyaCameraCue` назначь `Nastya/Idle.png` и все `Nastya/Walk_*.png`.

## Подсказка у двери и ENTER
1. В Hierarchy создай пустой объект, например `Door_To_Location2`.
2. Поставь его в центре двери. Увеличь/уменьши позицию и BoxCollider2D, чтобы зона перекрывала подход к двери.
3. Выбери объект и нажми `NeKArton > Create door trigger at selected object`.
4. В компоненте `DoorTransitionTrigger` заполни `Destination Scene` точным именем следующей сцены.
5. При входе игрока в зону появится подсказка «Нажмите ENTER, чтобы войти». Нажатие ENTER загрузит заданную сцену. UI подсказка создаётся автоматически.

## Важное ограничение
В текущем проекте в Build Settings есть только `SampleScene` и `Location-1`. Отдельной следующей локации пока нет. Создай новую сцену (например, `Location-2`), добавь её в `File > Build Profiles > Scene List`, затем укажи `Location-2` в `Destination Scene`. Сам переход и подсказка уже реализованы, но загрузить отсутствующую сцену Unity не сможет.

Если генератор не нашёл разделители между кадрами, он использует равномерное деление исходной полосы на ожидаемые 24/22 кадра. После генерации проверь изображения в `Assets/Generated` перед назначением.
