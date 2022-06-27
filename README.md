# multiplayerPrototype
Для сетевого взаимодействия используется PhotonNetworkEngine - PUN Classic. Есть
возможность на одном компьютере запустить несколько клиентов.
При подключении игрок появляется на сцене, имеет 100 ХП, может смотреть по
сторонам (движение мыши), ходить (WASD), прыгать (Space), запускать огненные
шары (левая кнопка мыши).
Огненный шар просто летит, как обычный мячик и уничтожается при соударении с
чем угодно. При уничтожении образуется взрыв, который наносит урон игроку в зависимости
от расстояния до центра взрыва. При смерти (ХП опускаются до 0) игрок снова появляется
на сцене и снова имеет 100 ХП. ХП игрока отображаются над ним в виде числа.
Также на сцене есть бонусы при подборе которых восстанавливается 50 ХП,
красная полусфера, нахождение на которой наносит урон игроку каждую секунду.
Игра стартует после подключении второго игрока. Чтобы подключиться 
к игре нужно нажать кнопку старт и дождаться подключения.
Также на сцене есть движущиеся боты - они не наносят урона,
но получают урон от взрывов. Уничтожение ботов засчитывается в общий счет.
Раунд длится минуту, у кого будет больше счет в конце раунда - тот и побеждает.
