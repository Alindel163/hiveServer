using System;
using System.Collections.Generic;
using Fleck;
// Пересборка
namespace HiveServer
{
    class Program
    {
        // Словарь для хранения подключенных игроков
        // Ключ — это уникальный ID сокета, значение — сам сокет
        private static Dictionary<Guid, IWebSocketConnection> _connections = new Dictionary<Guid, IWebSocketConnection>();

        static void Main(string[] args)
        {
            ResourceManager.GenerateMap();
            // Создаем сервер, который слушает порт 8080 на всех IP-адресах компьютера
            // Именно к этой строке потом будет подключаться ngrok
            // Проверяем, выделило ли облако порт. Если нет (запуск дома) — используем наш 8080
            string port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            var server = new WebSocketServer($"ws://0.0.0.0:{port}");

            server.Start(socket =>
            {
                // 1. СОБЫТИЕ: Игрок только что подключился по сети
                socket.OnOpen = () =>
                {
                    Console.WriteLine($"🔌 Соединение открыто! Временный GUID: {socket.ConnectionInfo.Id}");
                    // Сохраняем сокет в наш список активных подключений
                    _connections[socket.ConnectionInfo.Id] = socket;
                };

                // 2. СОБЫТИЕ: Игрок отключился (закрыл вкладку браузера или пропал интернет)
                socket.OnClose = () =>
                {
                    Console.WriteLine($"❌ Соединение закрыто для: {socket.ConnectionInfo.Id}");

                    // 1. Пытаемся найти игрока в нашей базе сессий перед удалением
                    if (PlayerManager.Players.TryGetValue(socket.ConnectionInfo.Id, out var leavingPlayer))
                    {
                        int uhedshiyId = leavingPlayer.ClientId;

                        // 2. Создаем пакет уведомления о выходе
                        byte[] leavePacket = PacketSerializer.CreatePlayerLeavePacket(uhedshiyId);

                        // 3. Рассылаем этот пакет всем ОСТАЛЬНЫМ активным игрокам
                        foreach (var pair in PlayerManager.Players)
                        {
                            if (pair.Key != socket.ConnectionInfo.Id)
                            {
                                pair.Value.Socket.Send(leavePacket);
                            }
                        }

                        // 4. Теперь со спокойной душой удаляем его из памяти сервера
                        PlayerManager.RemovePlayer(socket.ConnectionInfo.Id);
                    }

                    _connections.Remove(socket.ConnectionInfo.Id);
                };

                // 3. СОБЫТИЕ: От Unity прилетел бинарный пакет (массив байт)
                socket.OnBinary = (bytes) =>
                {
                    // Теперь передаем еще и сам сокет (socket), чтобы обработчик мог занести его в сессию
                    PacketHandler.Handle(socket.ConnectionInfo.Id, socket, bytes);
                };
            });

            Console.WriteLine("🚀 Сервер Fleck успешно запущен и слушает порт 8080...");
            Console.WriteLine("Нажмите ENTER в консоли, чтобы остановить сервер.");
            Console.ReadLine(); // Блокируем поток, чтобы консоль не закрылась сама
        }
    }
}
