namespace HiveServer
{
    public class ResourceNode
    {
        public int Id { get; set; }             // Уникальный ID объекта на карте
        public ResourceType Type { get; set; }   // Что это за ресурс
        public float X { get; set; }            // Координаты спавна
        public float Y { get; set; }
        public float Z { get; set; }
        public int Health { get; set; }         // Сколько раз нужно ударить, чтобы добыть
    }
}