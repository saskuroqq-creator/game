# Blender → Unity: договор для моделей

Ты создаёшь игровые meshes/UV/rig/animations/LOD; код Unity управляет gameplay, движением, попаданиями, AI и VFX.

## Файлы
Храни FBX и текстуры в Assets/Yokai/Art/Characters/Player, Enemies, Bosses; окружение в Art/Environment и Art/Props.
Риг один на персонажа, до 4 весов, масштабы применены, 1 единица = метр. Forward Unity +Z, Up +Y.
Экспортировать FBX из Blender с -Z Forward/Y Up; итоговую ориентацию обязательно проверить в Unity.
Не помещать gameplay scripts, CharacterController или Rigidbody внутрь visual prefab.

## Подключение
Создать визуальный prefab с Animator + RuntimeAnimatorController и LODGroup.
Prefab появляется дочерним объектом существующего gameplay actor; collider prefab отключается.
Проверенный controller обязателен: без него текущий visual остаётся.
Сохранить готовые prefabs в Assets/Yokai/Resources/YokaiArt с именами:
Player, Grunt, Ronin, Stalker, Heavy, Elite, Kagane.
FBX сам по себе НЕ является готовым prefab/controller. Автогенерации AnimatorController здесь нет.
Root Motion OFF. In-place clips; положение и столкновения контролирует CharacterController.
При наличии готового prefab отключаются процедурный pose animator и foot grounding V14.

## Sockets
WeaponSocket, WeaponTip, ChestSocket, HeadSocket. WeaponTip — конец клинка для trail.
Оружие уже должно быть вложено в bone/socket внутри prefab.
Дополнительные VFX sockets разрешены; их привязка эффектов настраивается отдельно.

## Параметры Animator героя
| Имя | Тип | Значение |
| --- | --- | --- |
| Speed | Float | фактическая скорость |
| MoveX / MoveY | Float | направление движения относительно героя; для lock-on blend tree |
| DodgeX / DodgeY | Float | направление уклонения относительно героя |
| Grounded | Bool | CharacterController.isGrounded |
| LockedOn | Bool | захват цели |
| ActionState | Int | enum ниже |
| Stance | Int | 0 Gale, 1 Stone, 2 Spirit |
| AttackIndex | Int | 1–5 |
| ActionVersion | Int | меняется на каждом новом действии |
| ActionChanged | Trigger | новое действие, включая следующий Light внутри комбо |
| ActionNormalized | Float | прогресс gameplay-действия |

ActionState: 0 Free, 1 Light, 2 Heavy, 3 Dodge, 4 Parry, 5 Guard, 6 Art, 7 Finisher, 8 Hit, 9 Stagger, 10 Dead.
Отсутствующие параметры пропускаются без Animator warnings.
Настрой transitions между 5 Light states по AttackIndex + ActionChanged; одно изменение ActionState недостаточно для цепочки.
Клипы Idle/Walk/Run/Sprint/Strafe/Dodge/Light1–5/Heavy/Block/Parry/Hit/Stagger/Finisher/Death сопоставить в controller.
Knockdown/GetUp требуют отдельного будущего расширения gameplay state machine; они сейчас не вызываются автоматически.

## Параметры врагов
Speed Float, AttackNormalized Float, Attacking Bool, Dead Bool, Staggered Bool, Phase Int (Kagane 1–3).
Enemy AttackNormalized описывает замах; между ударами серии пока нужен authored controller. Отдельных swing triggers нет.

## Настройка попадания
Assets > Create > Yokai > Combat Tuning.
Сохранить asset как Assets/Yokai/Resources/YokaiArt/CombatTuning.asset для автоматической загрузки.
Пять lightCombo записей: duration, stamina, damageMultiplier, posture, contact (0–1).
Gale ускоряет клип/действие на 1.14, Stone замедляет до 0.9.
Contact должен предшествовать cancel/chain windows. Настроить speed controller под реальную gameplay duration.
Heavy: 0.62 s (Stone 0.72 s), контакт 46%; active hit window 0.16 s.
Light: active hit window 0.10 s. Healing 0.45 s; Finisher 0.42 s; Art 0.27 s.
Не добавлять damage Animation Events: это вызовет дублирование с gameplay.

Модульную карту пока не загружает visual-prefab hook: environment подключается отдельно после проверки размеров, маршрута и коллизий.
