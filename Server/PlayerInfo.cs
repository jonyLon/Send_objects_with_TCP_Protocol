using CommandClasses;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Server
{
    // клас, який містить інформацію та методи для взаємодії з гравцем
    public class PlayerInfo
    {
        public TcpClient TcpClient { get; set; }
        public string Nickname { get; set; }
        public bool IsX { get; set; }

        public List<CellCoord> occupiedCells = new List<CellCoord>();

        // метод для відправки об'єкту команди
        private void SendCommand(ServerCommand command)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(TcpClient.GetStream(), command);
        }
        // методи відправки конкретних команд
        public void SendCloseCommand()
        {
            SendCommand(new ServerCommand(CommandType.CLOSE));
        }
        public void SendWaitCommand()
        {
            SendCommand(new ServerCommand(CommandType.WAIT));
        }
        public void SendStartCommand(string opponentName)
        {
            ServerCommand command = new ServerCommand(CommandType.START)
            {
                IsX = this.IsX,
                OpponentName = opponentName
            };

            SendCommand(command);
        }
        public void SendMoveCommand(CellCoord moveCoord)
        {
            ServerCommand command = new ServerCommand(CommandType.MOVE)
            {
                MoveCoord = moveCoord
            };

            SendCommand(command);
        }
        // метод отримання команди від сервера
        public ClientCommand ReceiveCommand()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            return (ClientCommand)formatter.Deserialize(TcpClient.GetStream());
        }

        // метод обробки команд від клієнта та взаємодії з опонентом
        public void StartSession(PlayerInfo opponent)
        {
            bool isExit = false;
            while (!isExit)
            {
                // отримання команди від клієнта
                ClientCommand command = ReceiveCommand();
                Console.WriteLine(occupiedCells.Count);

                // обробка команди
                switch (command.Type)
                {
                    // команда ходу
                    case CommandType.MOVE:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"Move on {command.MoveCoord} from {command.Nickname}");
                        // повідомлення опонента про виконаний хід
                        opponent.SendMoveCommand(command.MoveCoord);
                        occupiedCells.Add(command.MoveCoord);
                        // Перевіряємо, чи є нічия
                        if (CheckWin(command.MoveCoord))
                        {
                            SendWinCommand();
                            opponent.SendLoseCommand();
                            isExit = true;
                        }
                        else if (opponent.occupiedCells.Count + occupiedCells.Count == 9)
                        {
                            // Перевірка на нічию
                            SendTieCommand();
                            opponent.SendTieCommand();
                            isExit = true;
                        }
                        break;
                    // команда закриття сесії на сервері
                    case CommandType.CLOSE:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"Close command from {command.Nickname}");
                        // встановлення змінної для закриття цикла обробки подій поточного гравця
                        isExit = true;
                        break;
                    // команда завершення гри
                    case CommandType.EXIT:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Exit command from {command.Nickname}");
                        // повідомлення опонента про закриття сесії
                        opponent.SendCloseCommand();
                        // встановлення змінної для закриття цикла обробки подій поточного гравця
                        isExit = true;
                        break;
                }
            }
        }
        // метод отримання нічиї
        public void SendTieCommand()
        {
            SendCommand(new ServerCommand(CommandType.TIE));
        }
        public bool CheckWin(CellCoord lastMove)
        {
            // Створюємо масив для збереження станів клітинок гравця
            char[,] board = new char[3, 3];

            // Ініціалізуємо масив за допомогою збережених координат
            foreach (var coord in occupiedCells)
            {
                board[coord.RowIndex, coord.ColumnIndex] = IsX ? 'X' : 'O';
            }

            // Перевіряємо рядки
            for (int row = 0; row < 3; row++)
            {
                if (board[row, 0] != '\0' && board[row, 0] == board[row, 1] && board[row, 1] == board[row, 2])
                    return true;
            }

            // Перевіряємо стовпці
            for (int col = 0; col < 3; col++)
            {
                if (board[0, col] != '\0' && board[0, col] == board[1, col] && board[1, col] == board[2, col])
                    return true;
            }

            // Перевіряємо діагоналі
            if (board[0, 0] != '\0' && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2])
                return true;

            if (board[0, 2] != '\0' && board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0])
                return true;

            return false;
        }
        public void SendLoseCommand()
        {
            SendCommand(new ServerCommand(CommandType.LOSE));
        }
        public void SendWinCommand()
        {
            SendCommand(new ServerCommand(CommandType.WIN));
        }
    }
}
