using System;
using System.IO;
using Fleck; // Добавь этот using сверху!

namespace HiveServer
{
    public static class PacketHandler
    {
        // Обновляем сигнатуру метода — добавляем IWebSocketConnection socket
        public static void Handle(Guid connectionId, IWebSocketConnection socket, byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    PacketType packetType = (PacketType)reader.ReadByte();

                    switch (packetType)
                    {
                        case PacketType.LoginRequest:
                            // Передаем сокет дальше в функцию
                            HandleLoginRequest(connectionId, socket, reader);
                            break;
                        case PacketType.PlayerMove:
                            // Вызываем новый метод обработки движения
                            HandlePlayerMove(connectionId, reader);
                            break;
                        case PacketType.HarvestResource:
                            HandleHarvestResource(connectionId, reader);
                            break;
                        case PacketType.DepositResource:
                            HandleDepositResource(connectionId, reader);
                            break;
                    }
                }
            }
        }


        private static void HandlePlayerMove(Guid connectionId, BinaryReader reader)
        {
            // Ищем игрока в нашей базе (это уже написано)
            if (PlayerManager.Players.TryGetValue(connectionId, out PlayerSession player))
            {
                // Читаем байты из сети (это уже написано)
                player.X = reader.ReadSingle();
                player.Y = reader.ReadSingle();
                player.Z = reader.ReadSingle();
                player.RotY = reader.ReadSingle();
                player.Speed = reader.ReadSingle();

                // --- ДОБАВЛЯЕМ КОД РАССЫЛКИ ОТСЮДА ---

                // 1. Собираем пакет трансляции движения
                byte[] broadcastPacket = PacketSerializer.CreatePlayerMoveBroadcast(
                    player.ClientId, player.X, player.Y, player.Z, player.RotY, player.Speed
                );

                // 2. Бежим циклом по всей базе игроков на сервере
                foreach (var pair in PlayerManager.Players)
                {
                    // Важно: не отправляем игроку его же собственные координаты, 
                    // ведь его локальная Unity и так знает, где он находится!
                    if (pair.Key != connectionId)
                    {
                        // Отправляем пакет чужому сокету
                        pair.Value.Socket.Send(broadcastPacket);
                    }
                }
            }
        }
        private static void HandleLoginRequest(Guid connectionId, Fleck.IWebSocketConnection socket, BinaryReader reader)
        {
            InsectClass chosenClass = (InsectClass)reader.ReadByte();
            Console.WriteLine($"👑 [ВХОД] Игрок {connectionId} прислал запрос. Класс: {chosenClass}");

            // 1. Регистрируем новичка в базе данных
            PlayerSession newPlayer = PlayerManager.RegisterPlayer(connectionId, socket, chosenClass);

            // 2. Отправляем новичку LoginResponse с его ID (это уже было)
            byte[] responsePacket = PacketSerializer.CreateLoginResponse(newPlayer.ClientId);
            socket.Send(responsePacket);

            byte[] resourcesPacket = PacketSerializer.CreateSyncResourcesPacket(ResourceManager.Nodes);
            socket.Send(resourcesPacket);
            Console.WriteLine($"📤 [МИР] Карты ресурсов отправлена Игроку #{newPlayer.ClientId}");

            // --- НАЧАЛО НОВОЙ МАГИИ СПАВНА ---

            // 3. Собираем пакет спавна ДЛЯ НОВИЧКА
            byte[] spawnNewPlayerPacket = PacketSerializer.CreateSpawnPlayerPacket(newPlayer.ClientId, newPlayer.Insect);

            // 4. Бежим по всем игрокам на сервере
            foreach (var pair in PlayerManager.Players)
            {
                // Если это старый игрок:
                if (pair.Key != connectionId)
                {
                    // А) Отправляем СТАРОМУ игроку пакет о том, что надо заспавнить НОВИЧКА
                    pair.Value.Socket.Send(spawnNewPlayerPacket);

                    // Б) Собираем пакет спавна СТАРОГО игрока
                    byte[] spawnOldPlayerPacket = PacketSerializer.CreateSpawnPlayerPacket(pair.Value.ClientId, pair.Value.Insect);

                    // В) Отправляем НОВИЧКУ пакет о том, что на сервере уже есть СТАРАЯ кукла
                    socket.Send(spawnOldPlayerPacket);
                }
            }
            byte[] warehousePacket = PacketSerializer.CreateSyncWarehousePacket(ResourceManager.Warehouse);
            socket.Send(warehousePacket);
            Console.WriteLine($"📤 [СПАВН] Синхронизированы сущности для Игрока #{newPlayer.ClientId}");
        }
        private static void HandleHarvestResource(Guid connectionId, BinaryReader reader)
        {
            int resId = reader.ReadInt32();

            if (PlayerManager.Players.TryGetValue(connectionId, out PlayerSession player))
            {
                // Вызываем обновление анимации махания (это у нас уже написано)
                byte[] hitAnimPacket = PacketSerializer.CreateHitAnimationPacket(player.ClientId);
                foreach (var pair in PlayerManager.Players) pair.Value.Socket.Send(hitAnimPacket);

                if (resId != -1)
                {
                    // Передаем новые аргументы out для отслеживания респавна
                    if (ResourceManager.HitResource(resId, out ResourceType type, out bool isDestroyed, out byte[] respawnPacketData))
                    {
                        if (isDestroyed)
                        {
                            Console.WriteLine($"🪓 [ДОБЫЧА] Ресурс #{resId} ({type}) полностью истощен.");

                            // 1. Рассылаем всем приказ УДАЛИТЬ старый ресурс (это уже было)
                            byte[] destroyPacket = PacketSerializer.CreateResourceDestroyedPacket(resId);
                            foreach (var pair in PlayerManager.Players) pair.Value.Socket.Send(destroyPacket);

                            // 2. РАССЫЛАЕМ ВСЕМ ПРИКАЗ СОЗДАТЬ НОВЫЙ РЕСУРС!
                            if (respawnPacketData != null)
                            {
                                foreach (var pair in PlayerManager.Players)
                                {
                                    pair.Value.Socket.Send(respawnPacketData);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void HandleDepositResource(Guid connectionId, BinaryReader reader)
        {
            if (PlayerManager.Players.TryGetValue(connectionId, out PlayerSession player))
            {
                byte count = reader.ReadByte(); // Сколько типов ресурсов принес игрок

                for (int i = 0; i < count; i++)
                {
                    ResourceType type = (ResourceType)reader.ReadByte();
                    int amount = reader.ReadInt32();

                    // Добавляем в общий склад сервера
                    ResourceManager.Warehouse[type] += amount;
                    Console.WriteLine($"📦 [СКЛАД] Игрок #{player.ClientId} принес {amount} ед. ресурса {type}.");
                }

                // Рассылаем ВСЕМ игрокам обновленный склад
                byte[] syncWarehouse = PacketSerializer.CreateSyncWarehousePacket(ResourceManager.Warehouse);
                foreach (var pair in PlayerManager.Players)
                {
                    pair.Value.Socket.Send(syncWarehouse);
                }
                Console.WriteLine($"📢 [СЕРВЕР ОТЛАДКА] Пакет SyncWarehouse физически отправлен в сеть для {PlayerManager.Players.Count} игроков.");
            }
        }



    }
}
