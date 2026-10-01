# Новые Animator параметры и клипы V16

Основное подключение visual prefabs и настройки экспорта остаются как в V15_BLENDER_CONTRACT_RU.md.
Root Motion OFF. Физикой прыжка, подбрасывания, зависания и SLAM управляет код; клипы in-place.

## ActionState — сохраняются индексы V15, новые добавлены в конце
| Значение | Состояние | Клип |
| --- | --- | --- |
| 11 | Jump | Jump/Takeoff |
| 12 | Launcher | Подбрасывающий удар |
| 13 | AirLight | Air Attack 1–3 по AirComboIndex |
| 14 | AirSlam | Замах/падение с ударом вниз |
| 15 | Skill | По SkillIndex |
| 16 | Landing | Приземление после SLAM |

0–10 прежние: Free/Light/Heavy/Dodge/Parry/Guard/Art/Finisher/Hit/Stagger/Dead.

## Дополнительные параметры героя
Airborne Bool; VerticalSpeed Float; AirComboIndex Int (1–3); SkillIndex Int (0 dash, 1 storm, 2 ward).
Grounded Bool и ActionChanged Trigger сохраняются. Пока Airborne=true и ActionState=Free — использовать Air Idle вместо наземного Idle.
Airborne/VerticalSpeed являются gameplay-данными; визуальное движение не должно перемещать actor.
SkillIndex фиксируется на время каста; смена умения во время Skill запрещена.

## Тайминги
Jump takeoff 0.16 s; движение вверх продолжается по физике.
Launcher 0.55 s, контакт 0.20 s; переход в AirLight возможен после 48% действия.
Air Attack 1/2/3: 0.405 / 0.450 / 0.495 s, contact 32%; следующая атака после 56%, dodge cancel после 48%.
SLAM: событие damage происходит только при фактическом касании земли. Falling clip loop; Landing 0.38 s.
Crescent Dash 0.55 s: start movement 0.12 s, dash 0.22 s, contact примерно 0.34 s.
Blade Storm 0.95 s: contacts 0.22 / 0.42 / 0.62 s.
Spirit Ward 0.55 s: shield activation 0.22 s, duration up to 5 s.

## Враги
Новый параметр Airborne Bool. Staggered уже существует.
Airborne pose/air-hit при подбрасывании, переход на locomotion после Airborne=false. Launch triggers/отдельный air-hit counter пока не передаются.
Прежние enemy параметры: Speed/AttackNormalized/Attacking/Dead/Staggered/Phase.

Не добавлять damage Animation Events поверх gameplay: это задвоит попадания.
