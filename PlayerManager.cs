using System;
using System.Collections.Generic;
using Fleck;

namespace HiveServer
{
    public static class PlayerManager
    {
        // Наша база данных в оперативной памяти сервера
        // Ключ — это GUID сокета, значение — вся информация об игроке
        public static Dictionary<Guid, PlayerSession> Players = new Dictionary<Guid, PlayerSession>();

        // Счетчик для выдачи коротких ID
        private static int _idCounter = 1;

        // Метод регистрации нового игрока
        public static PlayerSession RegisterPlayer(Guid connectionId, IWebSocketConnection socket, InsectClass chosenClass)
        {
            // Создаем новую сессию
            PlayerSession newPlayer = new PlayerSession
            {
                ClientId = _idCounter++, // Выдаем текущий номер и увеличиваем счетчик на будущее
                ConnectionId = connectionId,
                Socket = socket,
                Insect = chosenClass,
                // Стартовые координаты в мире (пусть будет центр карты)
                X = 0,
                Y = 0,
                Z = 0,
                RotY = 0
            };

            // Сохраняем в наш Dictionary
            Players[connectionId] = newPlayer;

            Console.WriteLine($"📊 [БАЗА] Игрок {connectionId} сохранен под коротким ID: #{newPlayer.ClientId}");
            return newPlayer;
        }

        // Метод удаления игрока при выходе
        public static void RemovePlayer(Guid connectionId)
        {
            if (Players.TryGetValue(connectionId, out var player))
            {
                Console.WriteLine($"📊 [БАЗА] Игрок #{player.ClientId} удален из памяти сервера.");
                Players.Remove(connectionId);
            }
        }
    }
}
