using System;
using Fleck;

namespace HiveServer
{
    public class PlayerSession
    {
        public int ClientId { get; set; }           // Короткий сетевой ID (1, 2, 3...)
        public Guid ConnectionId { get; set; }      // Временный GUID сокета
        public IWebSocketConnection Socket { get; set; } // Сам сетевой сокет для отправки данных
        public InsectClass Insect { get; set; }     // Класс жука, который выбрал игрок

        // Будущие координаты жука в 3D мире Unity (заложим сразу)
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float RotY { get; set; }

        public float Speed { get; set; }
    }
}
