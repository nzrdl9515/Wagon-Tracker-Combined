using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wagon_Tracker_Combined
{
    class Screen
    {
        private string[] stringOut;

        public int Width
        {
            get;
            private set;
        }

        public int Height
        {
            get;
            private set;
        }

        public Screen(int _width, int _height)
        {
            Width = _width;
            Height = _height;
            stringOut = new string[Height];

            for (int j = 0; j < Height; j++)
            {
                stringOut[j] = "";

                for (int i = 0; i < Width; i++)
                {
                    stringOut[j] = stringOut[j] + ' ';
                }
            }

            Console.WindowWidth = Width;
            Console.BufferWidth = Width;
            Console.WindowHeight = Height;
            Console.BufferHeight = Height;
            Console.CursorVisible = true;
        }

        private string replaceAt(string input, int index, char value)
        {
            return input.Substring(0, index) + value + input.Substring(index + 1, Width - index - 1);
        }

        private void draw()
        {
            Console.CursorVisible = false;

            for (int i = 0; i < Height - 1; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write(stringOut[i]);
            }

            Console.Write(stringOut[^1].Remove(stringOut.Length - 1, 1));

            Console.CursorVisible = true;
        }

        public void Clear()
        {
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    if (stringOut[j][i] != ' ')
                    {
                        stringOut[j] = replaceAt(stringOut[j], i, ' ');
                    }
                }
            }

            draw();

            Console.SetCursorPosition(0, 0);
        }

        public void PrintFromFile(string path, int left, int top)
        {
            List<string> input = new List<string>();
            using (StreamReader sr = new StreamReader(path))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    input.Add(line);
                }
            }

            int length = 0;
            for (int i = 0; i < input.Count; i++)
            {
                if (input[i].Length > length)
                {
                    length = input[i].Length;
                }
            }

            char[,] inputChars = new char[length, input.Count];
            for (int j = 0; j < input.Count; j++)
            {
                for (int i = 0; i < length; i++)
                {
                    if (i < input[j].Length)
                    {
                        inputChars[i, j] = input[j][i];
                    }
                    else
                    {
                        inputChars[i, j] = ' ';
                    }
                }
            }

            Update(inputChars, left, top);
        }

        public void Update(char[,] arrayIn)
        {
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    if (arrayIn[i, j] != stringOut[j][i])
                    {
                        stringOut[j] = replaceAt(stringOut[j], i, arrayIn[i, j])
;
                    }
                }
            }

            draw();
        }

        public void Update(char[,] arrayIn, int left, int top)
        {
            for (int j = 0; j < arrayIn.GetLength(1); j++)
            {
                for (int i = 0; i < arrayIn.GetLength(0); i++)
                {
                    if (arrayIn[i, j] != stringOut[j + top][i + left])
                    {
                        stringOut[j + top] = replaceAt(stringOut[j + top], i + left, arrayIn[i, j]);
                    }
                }
            }

            draw();
        }

        public void Update(char[] arrayIn, int left, int top)
        {
            for (int i = 0; i < arrayIn.GetLength(0); i++)
            {
                if (arrayIn[i] != stringOut[top][i + left])
                {
                    stringOut[top] = replaceAt(stringOut[top], i + left, arrayIn[i]);
                }
            }

            draw();
        }

        public void Update(int x, int y, char value)
        {
            Console.SetCursorPosition(x, y);
            Console.Write(value);

            stringOut[y] = replaceAt(stringOut[y], x, value);
        }
    }
}
