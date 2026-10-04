using System;
using System.Collections.Generic;

namespace HiveServer
{
    public static class ResourceManager
    {
        public static Dictionary<int, ResourceNode> Nodes = new Dictionary<int, ResourceNode>();
        private static int _idCounter = 1;

        // --- ГЕЙМДИЗАЙН НАСТРОЙКИ ---
        private static int _maxResourcesOnMap = 40; // Чуть увеличим лимит для красивых кучек
        private static float _safeZoneRadius = 15f;   // Безопасная зона у склада
        private static float _mapRadius = 80f;        // Радиус острова

        public static Dictionary<ResourceType, int> Warehouse = new Dictionary<ResourceType, int>()
        {
            { ResourceType.Tree, 0 }, { ResourceType.Stone, 0 },
            { ResourceType.Seaweed, 0 }, { ResourceType.FishBone, 0 }, { ResourceType.Moss, 0 }
        };

        // Генерация стартового мира
        public static void GenerateMap()
        {
            Nodes.Clear();
            Random rand = new Random();

            // Будем спавнить ресурсы группами, пока не забьем карту до лимита
            while (Nodes.Count < _maxResourcesOnMap)
            {
                // 1. Случайно выбираем, какой ресурс генерировать следующим
                ResourceType type = (ResourceType)rand.Next(0, 5);

                // 2. РЕДКИЙ РЕСУРС (Рыбья кость): спавним поодиночке по всему острову
                if (type == ResourceType.FishBone)
                {
                    SpawnSingleNode(type, _mapRadius);
                    continue;
                }

                // 3. ОБЫЧНЫЕ РЕСУРСЫ (Дерево, Камень, Мох, Водоросли): спавним ГРУППОЙ (кластером)
                // Находим валидную "точку-центр" для будущей кучки ресурсов
                var center = GetValidRandomPoint(_mapRadius, _safeZoneRadius);
                int clusterSize = rand.Next(3, 5);

                for (int i = 0; i < clusterSize; i++)
                {
                    if (Nodes.Count >= _maxResourcesOnMap) break;

                    float finalX = 0f;
                    float finalZ = 0f;
                    bool validPointFound = false;
                    int attempts = 0; // Защита от бесконечного цикла, если на карте нет места

                    // Будем подбирать точку внутри кластера, пока она не пройдет проверку на дистанцию к соседям
                    while (!validPointFound && attempts < 20)
                    {
                        attempts++;

                        float offsetX = (float)(rand.NextDouble() * 12 - 6); // Немного расширим радиус кучки до 6м
                        float offsetZ = (float)(rand.NextDouble() * 12 - 6);

                        finalX = center.X + offsetX;
                        finalZ = center.Z + offsetZ;

                        // Проверяем расстояние до ВСЕХ уже существующих ресурсов на карте
                        bool tooCloseToSomeone = false;
                        foreach (var pair in Nodes)
                        {
                            float dx = pair.Value.X - finalX;
                            float dz = pair.Value.Z - finalZ;
                            float distanceBetweenNodes = (float)Math.Sqrt(dx * dx + dz * dz);

                            // МИНИМАЛЬНОЕ РАССТОЯНИЕ: Если ближе 2.5 метров — точка плохая, префабы пересекутся!
                            if (distanceBetweenNodes < 1.5f)
                            {
                                tooCloseToSomeone = true;
                                break; // Дальше этот узел проверять нет смысла, ищем новую точку
                            }
                        }

                        // Если точка не слишком близко к соседям и она за пределами безопасной зоны базы
                        float distToCenter = (float)Math.Sqrt(finalX * finalX + finalZ * finalZ);
                        if (!tooCloseToSomeone && distToCenter >= _safeZoneRadius)
                        {
                            validPointFound = true;
                        }
                    }

                    // Если за 20 попыток нашли хорошую точку — спавним узел
                    if (validPointFound)
                    {
                        ResourceNode node = new ResourceNode
                        {
                            Id = _idCounter++,
                            Type = type,
                            X = finalX,
                            Y = 0f,
                            Z = finalZ,
                            Health = 3
                        };
                        Nodes[node.Id] = node;
                    }
                }
            }

            Console.WriteLine($"🌲 [МИР] Успешно сгенерировано {Nodes.Count} ресурсов кластерами. Кости разбросаны одиночно!");
        }

        // Вспомогательный метод для одиночного спавна (используется для Костей и Респавна!)
        private static ResourceNode SpawnSingleNode(ResourceType type, float mapRadius)
        {
            var point = GetValidRandomPoint(mapRadius, _safeZoneRadius);

            ResourceNode node = new ResourceNode
            {
                Id = _idCounter++,
                Type = type,
                X = point.X,
                Y = 0f,
                Z = point.Z,
                Health = 3
            };

            Nodes[node.Id] = node;
            return node;
        }

        // Вспомогательный математический метод: ищет случайную точку за пределами безопасной зоны
        private static (float X, float Z) GetValidRandomPoint(float mapRadius, float safeZone)
        {
            Random rand = new Random();
            float x = 0f, z = 0f;
            bool valid = false;

            while (!valid)
            {
                x = (float)(rand.NextDouble() * (mapRadius * 2) - mapRadius);
                z = (float)(rand.NextDouble() * (mapRadius * 2) - mapRadius);

                float dist = (float)Math.Sqrt(x * x + z * z);
                if (dist >= safeZone) valid = true;
            }

            return (x, z);
        }

        // Метод обработки удара и динамического респавна
        public static bool HitResource(int resourceId, out ResourceType type, out bool isDestroyed, out byte[] respawnPacketData)
        {
            type = ResourceType.Tree;
            isDestroyed = false;
            respawnPacketData = null;

            if (Nodes.TryGetValue(resourceId, out ResourceNode node))
            {
                node.Health--;
                type = node.Type;

                if (node.Health <= 0)
                {
                    Nodes.Remove(resourceId); // Стираем старый истощенный ресурс
                    isDestroyed = true;

                    // === МАГИЯ УМНОГО РЕСПАВНА В БИОМЕ ===
                    // Ищем все ОСТАВШИЕСЯ на карте ресурсы точно ТАКОГО ЖЕ типа
                    List<ResourceNode> backupNodesOfTheSameType = new List<ResourceNode>();
                    foreach (var pair in Nodes)
                    {
                        if (pair.Value.Type == node.Type)
                        {
                            backupNodesOfTheSameType.Add(pair.Value);
                        }
                    }

                    float finalX = 0f;
                    float finalZ = 0f;
                    Random rand = new Random();

                    // Если на карте есть еще хоть один живой ресурс этого типа (например, другое дерево)
                    if (backupNodesOfTheSameType.Count > 0)
                    {
                        // Случайно выбираем ОДНО из выживших деревьев на карте в качестве "родителя"
                        int randomParentIndex = rand.Next(0, backupNodesOfTheSameType.Count);
                        ResourceNode parentNode = backupNodesOfTheSameType[randomParentIndex];

                        // Генерируем координаты для нового объекта строго вокруг выбранного "родителя" 
                        // в небольшом радиусе от 3 до 5 метров
                        float offsetX = (float)(rand.NextDouble() * 10 - 5);
                        float offsetZ = (float)(rand.NextDouble() * 10 - 5);

                        finalX = parentNode.X + offsetX;
                        finalZ = parentNode.Z + offsetZ;

                        // Проверяем, чтобы новая точка случайно не залезла в безопасную зону у склада базы
                        float distToCenter = (float)Math.Sqrt(finalX * finalX + finalZ * finalZ);
                        if (distToCenter < _safeZoneRadius)
                        {
                            // Если залезла — спавним по старой безопасной схеме в случайном месте острова
                            var safePoint = GetValidRandomPoint(_mapRadius, _safeZoneRadius);
                            finalX = safePoint.X;
                            finalZ = safePoint.Z;
                        }
                    }
                    else
                    {
                        // Если игроки умудрились вырубить вообще ВСЕ деревья этого типа под корень,
                        // спавним новое дерево в случайном месте острова, основывая новый биом/рощу!
                        var randomPoint = GetValidRandomPoint(_mapRadius, _safeZoneRadius);
                        finalX = randomPoint.X;
                        finalZ = randomPoint.Z;
                    }

                    // Создаем новый узел на вычисленных координатах
                    ResourceNode newNode = new ResourceNode
                    {
                        Id = _idCounter++,
                        Type = node.Type, // Строго тот же тип, что и уничтоженный
                        X = finalX,
                        Y = 0f,
                        Z = finalZ,
                        Health = 3
                    };

                    Nodes[newNode.Id] = newNode;

                    // Упаковываем данные для мгновенной отправки в Unity
                    respawnPacketData = PacketSerializer.CreateSpawnNewResourceBroadcast(newNode);
                }
                return true;
            }
            return false;
        }
    }
}
