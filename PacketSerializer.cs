using System.IO;

namespace HiveServer
{
    public static class PacketSerializer
    {
        // Метод собирает пакет одобряющий вход (LoginResponse)
        public static byte[] CreateLoginResponse(int assignedId)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    // 1. Пишем тип пакета (1 байт)
                    writer.Write((byte)PacketType.LoginResponse);

                    // 2. Пишем короткий ID игрока (4 байта, так как int занимает 4 байта)
                    writer.Write(assignedId);
                }

                return stream.ToArray(); // Возвращаем массив из 5 байт
            }
        }
        public static byte[] CreatePlayerMoveBroadcast(int clientId, float x, float y, float z, float rotY, float speed)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)PacketType.PlayerMove); // 1. Тип пакета (1 байт)
                    writer.Write(clientId);                     // 2. ЧЕЙ это жук (4 байта)
                    writer.Write(x);                            // 3. Координаты (12 байт)
                    writer.Write(y);
                    writer.Write(z);
                    writer.Write(rotY);                         // 4. Поворот (4 байта)
                    writer.Write(speed);                        // 5. Скорость для анимаций (4 байта)
                }
                return stream.ToArray(); // Всего 25 байт на пакет!
            }
        }
        public static byte[] CreateSpawnPlayerPacket(int clientId, InsectClass insectClass)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)PacketType.SpawnPlayer); // 1 байт
                    writer.Write(clientId);                     // 4 байта
                    writer.Write((byte)insectClass);            // 1 байт
                }
                return stream.ToArray(); // Всего 6 байт!
            }
        }
        public static byte[] CreatePlayerLeavePacket(int clientId)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)PacketType.PlayerLeave); // 1 байт
                    writer.Write(clientId);                     // 4 байта
                }
                return stream.ToArray(); // Итого 5 байт!
            }
        }
        public static byte[] CreateSyncResourcesPacket(Dictionary<int, ResourceNode> nodes)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)PacketType.SyncResources); // 1. Тип пакета
                    writer.Write(nodes.Count);                     // 2. Сколько всего ресурсов шлем

                    foreach (var pair in nodes)
                    {
                        ResourceNode node = pair.Value;
                        writer.Write(node.Id);          // 4 байта
                        writer.Write((byte)node.Type);  // 1 байт
                        writer.Write(node.X);           // 4 байта
                        writer.Write(node.Y);           // 4 байта
                        writer.Write(node.Z);           // 4 байта
                    }
                }
                return stream.ToArray();
            }
        }
        public static byte[] CreateResourceDestroyedPacket(int resourceId)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)PacketType.HarvestResource);
                    writer.Write(resourceId);
                }
                return stream.ToArray(); // 5 байт
            }
        }

        // Сервер рассылает всем новые данные склада
        public static byte[] CreateSyncWarehousePacket(Dictionary<ResourceType, int> warehouse)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)PacketType.SyncWarehouse);
                    writer.Write((byte)warehouse.Count); // Сколько типов ресурсов шлем

                    foreach (var pair in warehouse)
                    {
                        writer.Write((byte)pair.Key);   // Тип ресурса (1 байт)
                        writer.Write(pair.Value);        // Количество (4 байта)
                    }
                }
                return stream.ToArray();
            }
        }
        public static byte[] CreateHitAnimationPacket(int clientId)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)PacketType.PlayerHitAnimation); // 1 байт
                    writer.Write(clientId);                            // 4 байта
                }
                return stream.ToArray();
            }
        }
        public static byte[] CreateSpawnNewResourceBroadcast(ResourceNode node)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)PacketType.SyncResources); // Тип пакета (6)
                    writer.Write(1);                              // count = 1 новый объект!

                    writer.Write(node.Id);
                    writer.Write((byte)node.Type);
                    writer.Write(node.X);
                    writer.Write(node.Y);
                    writer.Write(node.Z);
                }

                // --- ВОТ ЭТОЙ СТРОЧКИ ТУТ НЕ ХВАТАЛО! ---
                return stream.ToArray(); // Возвращаем готовый массив из 19 байт
                                         // ----------------------------------------
            }
        }
    }
}
