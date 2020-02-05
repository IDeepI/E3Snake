using ConnectToE3;
using e3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;



namespace ConsoleSnake
{
  
    class Program
    {
        private static AppConnect e3App = new AppConnect();
        public static e3Application App;// объект приложения
        public static e3Job Prj = null;	// объект проекта
        public static e3Sheet Sheet = null;	// объект 
        public static e3Graph Graph = null;	// объект 
        public static e3Symbol Sym = null;	// объект 
        public static float xmin, ymin, xmax, ymax;
        public static int sheetid = 0;
        public static double frameTime = 200;
        public static bool gameover = false;

        static void Main()
        {
            // Подключаем E3
            App = e3App.ToE3();
            App?.PutInfo(0, "Starting The Snake!");

            Prj     = App?.CreateJobObject();
            Sheet   = Prj.CreateSheetObject();
            Graph   = Prj.CreateGraphObject();
            Sym     = Prj.CreateSymbolObject();

            // Выберем  лист

            while (sheetid == 0)
            {
                sheetid = Prj.GetActiveSheetId();
                Sheet.SetId(sheetid);

                if (sheetid == 0)
                {
                    MessageBox.Show("Выберите лист для игры!");
                }

            }

            //находим середину листа
            object xmin = 0;
            object ymin = 0;
            object xmax = 0;
            object ymax = 0;

            Sheet.GetDrawingArea(ref xmin, ref ymin, ref xmax, ref ymax);

            double centerX = Math.Round(((double)xmin + (double)xmax) / 2);
            double centerY = Math.Round(((double)ymin + (double)ymax) / 2);

            // Переменные мира
            int score = 5;

            Pixel head = new Pixel(centerX, centerY, 13);

            List<Pixel> body = new List<Pixel>();

            for (int i = 0; i < score; i++)
            {
                if (i == 0)
                {
                    body.Add(new Pixel(head.XPos - 2, head.YPos, 35));
                }
                else
                {
                    body.Add(new Pixel(body[i - 1].XPos - 2, body[i - 1].YPos, 35));
                }
            }
            body.Reverse();

            Direction currentMovement = Direction.Right;

            //Собираем коллекцию символов 
            object symIds = 0;
            object xSmin = 0;
            object ySmin = 0;
            object xSmax = 0;
            object ySmax = 0;
            object xS = 0;
            object yS = 0;
            object gridS = 0;           
            List<Symbol> symbols = new List<Symbol>();
            int symCnt = Sheet.GetSymbolIds(ref symIds);
            var symIdsArray = (Array)symIds;
            foreach( var symId in symIdsArray)
            {
                if (symId != null)
                {
                    Sym.SetId((int)symId);
                    Sym.GetArea(ref xSmin, ref ySmin, ref xSmax, ref ySmax);
                    Sym.GetSchemaLocation(ref xS, ref yS, gridS);
                    string rotS = Sym.GetRotation();
                    if (rotS.Contains("x"))
                    {
                        symbols.Add(new Symbol((double)xS + (double)xSmin , (double)yS - (double)ySmax, (double)xS + (double)xSmax, (double)yS - (double)ySmin, (int)symId));
                    } else if (rotS.Contains("y"))
                    {
                        symbols.Add(new Symbol((double)xS - (double)xSmax, (double)yS + (double)ySmin, (double)xS - (double)xSmin, (double)yS + (double)ySmax, (int)symId));
                    }
                    else
                    {
                        symbols.Add(new Symbol((double)xS + (double)xSmin, (double)yS + (double)ySmin, (double)xS + (double)xSmax, (double)yS + (double)ySmax, (int)symId));
                    }                                            
                }                
            }    
////////////////////////////////////////////////////////////////////////////////////////
            while (true)
            {
                // Выход за границы листа
                gameover |= (head.XPos >= (double)xmax - 1 || head.XPos <= 1 || head.YPos >= (double)ymax - 1 || head.YPos <= 1);

                // Съесть символ
                foreach (var symbol in symbols)
                {
                    if (symbol.Collide(head.XPos, head.YPos))
                    {
                        score++;
                        frameTime *= 0.9;
                        Sym.SetId(symbol.Id);
                        Sym.Delete();
                        symbols.Remove(symbol);                        
                        break;
                    }
                }
                Console.Clear();
                Console.WriteLine($"Счет: {score - 5}");
                //Рисуем тело
                for (int i = 0; i < body.Count; i++)
                {
                    Pixel tmpbody = body[i];
                    DrawPixel(ref tmpbody);
                    body[i] = tmpbody;
                    gameover |= (body[i].XPos == head.XPos && body[i].YPos == head.YPos);
                }

                if (gameover)
                {
                    break;
                }
                //Рисуем голову
                DrawPixel(ref head);
                //Ждем кадр
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds <= frameTime)
                {
                    currentMovement = ReadMovement(currentMovement);
                }
                //Добавляем пиксель тела чтобы подвинуться
                body.Add(new Pixel(head.XPos, head.YPos, 35));
                // Изменяем положение головы
                switch (currentMovement)
                {
                    case Direction.Up:
                        head.YPos += 2;
                        break;
                    case Direction.Down:
                        head.YPos -= 2;
                        break;
                    case Direction.Left:
                        head.XPos -= 2;
                        break;
                    case Direction.Right:
                        head.XPos += 2;
                        break;
                }
                //Удаляем змейку, чтобы перерисовать в новом кадре
                Clear(head, body);

                // Удаляем последнее звено из коллекции тела чтобы змея двигалась
                if (body.Count > score)
                {
                    body.RemoveAt(0);
                }
            }

            // Сообщение с результатом
            MessageBox.Show($"Game over, Score: {score - 5}");

            Clear(head, body);

            App = null;


        }
        static void Clear(Pixel head, List<Pixel> body)
        {
            Graph.SetId(head.Id);
            Graph.Delete();
            for (int i = 0; i < body.Count; i++)
            {
                Graph.SetId(body[i].Id);
                Graph.Delete();
            }
        }
            static void DrawPixel(ref Pixel pixel)
        {

            pixel.Id = Graph.CreateRectangle(sheetid, pixel.XPos - 1, pixel.YPos - 1, pixel.XPos + 1, pixel.YPos + 1);
            Graph.SetHatchPattern(1, 0, 0);
            Graph.SetHatchColour(pixel.Color);
            Graph.SetColour(pixel.Color);

        }

        struct Pixel
        {
            public Pixel(double xPos, double yPos, int color)
            {
                XPos = xPos;
                YPos = yPos;
                Color = color;
                Id = 0;
            }
            public double XPos { get; set; }
            public double YPos { get; set; }
            public int Color { get; set; }
            public int Id { get; set; }
        }

        struct Symbol
        {
            public Symbol(double x1, double y1, double x2, double y2, int id)
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
                Id = id;
            }
            public double X1 { get; set; }
            public double Y1 { get; set; }
            public double X2 { get; set; }
            public double Y2 { get; set; }
            public int Id { get; set; }
            public bool Collide(double xPos, double yPos) 
            {
                if (( X1 <= xPos && xPos <= X2 ) && (Y1 <= yPos && yPos <= Y2))
                {
                    return true;
                }
                return false;
             }
        }

        enum Direction
        {
            Up,
            Down,
            Right,
            Left
        }
        static Direction ReadMovement(Direction movement)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.UpArrow && movement != Direction.Down)
                {
                    movement = Direction.Up;
                }
                else if (key == ConsoleKey.DownArrow && movement != Direction.Up)
                {
                    movement = Direction.Down;
                }
                else if (key == ConsoleKey.LeftArrow && movement != Direction.Right)
                {
                    movement = Direction.Left;
                }
                else if (key == ConsoleKey.RightArrow && movement != Direction.Left)
                {
                    movement = Direction.Right;
                }
                else if (key == ConsoleKey.Escape)
                {
                    gameover = true;
                }
            }
            return movement;
        }
    }

}


