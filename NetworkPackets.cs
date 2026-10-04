namespace HiveServer
{
    // Полная копия типов пакетов из Unity
    public enum PacketType : byte
    {
        LoginRequest = 1,
        LoginResponse = 2,
        SpawnPlayer = 3,
        PlayerMove = 4,
        PlayerLeave = 5,
        SyncResources = 6,   // Сервер выдает новичку список всех ресурсов на карте
        HarvestResource = 7, // Игрок сообщает, что добывает ресурс / Сервер сообщает, что ресурс истощен
        DepositResource = 8,  // КЛИЕНТ -> СЕРВЕР (Я принес ресурсы на склад)
        SyncWarehouse = 9,     // СЕРВЕР -> КЛИЕНТ (На складе теперь X дерева, Y камня...)
        PlayerHitAnimation = 10
    }
    public enum ResourceType : byte
    {
        Tree = 0,        // Дерево
        Stone = 1,       // Камень
        Seaweed = 2,     // Водоросли
        FishBone = 3,    // Рыбья кость
        Moss = 4         // Мох
    }

    // Полная копия классов насекомых из Unity с твоими правками
    public enum InsectClass : byte
    {
        Ant = 0,      // Муравей (строительство)
        Beetle = 1,   // Жук (добыча еды/ресурсов, маленький)
        Crab = 2,     // Краб (добыча, медленный, высокая грузоподъёмность)
        Bee = 3,      // Пчела (разведка)
        Scorpion = 4  // Скорпион (боевой)
    }
}