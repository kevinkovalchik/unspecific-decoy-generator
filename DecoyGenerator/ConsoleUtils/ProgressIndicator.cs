using System;

namespace DecoyGenerator.ConsoleUtils
{
    internal class ProgressIndicator
    {
        private string Message { get; }
        private int _x = 0;
        private int _previousX = 0;
        private int TotalX { get; }
        private int WriteNow { get; }
        private DateTime _previousTime;
        private DateTime _startTime;

        public ProgressIndicator(int total, string message)
        {
            TotalX = total;
            if (TotalX > 100)
            {
                WriteNow = TotalX / 100;
            }
            else
            {
                WriteNow = TotalX;
            }
            this.Message = message;
        }

        public void Start()
        {
            _previousTime = DateTime.Now;
            _startTime = DateTime.Now;
            Console.Write($"{Message}: 0/{TotalX} - 0%");
        }

        public void Update()
        {
            if (_x % WriteNow == 0)
            {
                var newTime = DateTime.Now;
                var elapsedTime = (newTime - _previousTime).TotalSeconds;
                var perSecond = (_x - _previousX) / elapsedTime;
                string msg = $"\r{Message}: {_x}/{TotalX} - {((_x * 100) / TotalX)}% - {Math.Round(perSecond, 2)} it/s";
                if (msg.Length > Console.BufferWidth)
                {
                    Console.CursorTop -= (int)Math.Floor((double) msg.Length / Console.BufferWidth);
                }
                Console.Write(msg);
                _previousX = _x;
                _previousTime = newTime;
            }
            _x += 1;
        }

        public void Done()
        {
            var time = (DateTime.Now - _startTime).TotalSeconds;
            Console.WriteLine($"\r{Message}: 100% - Total time: {time} seconds");
        }
        
        static void ClearLastLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop-1);
        }
    }
}