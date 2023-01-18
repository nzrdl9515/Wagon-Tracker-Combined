using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wagon_Tracker_Combined
{
    class Textbox
    {
        public int Left
        {
            get;
            private set;
        }

        public int Top
        {
            get;
            private set;
        }

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

        private List<char> data; // --------------------- List of input characters
        private List<int> linePos; // ------------------- List of the positions of each new line on the screen when printing 'data'
        private int pos; // ----------------------------- Position in the 'data' string for each iteration
        private int keepLinePos; // --------------------- Maintains the line index if moving up or down to a shorter line
        private int lineIndex; // ----------------------- Line index for the current position in the 'data' string
        private int scrollPosition; // ------------------ Index of the first line to be shown on the screen

        public Textbox(int _width, int _height, int _left, int _top)
        {
            Width = _width;
            Height = _height;
            Left = _left;
            Top = _top;
            pos = 0;
            keepLinePos = -1;
            lineIndex = 0;
            scrollPosition = 0;

            data = new List<char>();
            linePos = new List<int> { 0 };

            Console.SetCursorPosition(Left, Top);
        }

        public Textbox(int _width, int _height, int _left, int _top, List<char> _data)
        {
            Width = _width;
            Height = _height;
            Left = _left;
            Top = _top;
            pos = _data.Count;
            keepLinePos = -1;
            lineIndex = 0;

            scrollPosition = 0;

            data = _data;
            linePos = new List<int> { 0 };
        }

        public Textbox(int _width, int _height, int _left, int _top, string path)
        {
            data = new List<char>();
            using (StreamReader sr = new StreamReader(path))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    foreach (char i in line)
                    {
                        data.Add(i);
                    }
                    data.Add('\n');
                }

                data.RemoveAt(data.Count - 1);
            }

            Width = _width;
            Height = _height;
            Left = _left;
            Top = _top;
            pos = data.Count;
            keepLinePos = -1;
            lineIndex = 0;

            scrollPosition = 0;
        }

        public Textbox(int _width, int _height, int _left, int _top, List<string> input)
        {
            data = new List<char>();
            foreach (string j in input)
            {
                foreach (char i in j)
                {
                    data.Add(i);
                }
                data.Add('\n');
            }

            if (data.Count > 0)
            {
                data.RemoveAt(data.Count - 1);
            }

            Width = _width;
            Height = _height;
            Left = _left;
            Top = _top;
            pos = data.Count;
            keepLinePos = -1;
            lineIndex = 0;

            scrollPosition = 0;
        }

        public void UpdateData(List<string> input)
        {
            data = new List<char>();
            foreach (string j in input)
            {
                foreach (char i in j)
                {
                    data.Add(i);
                }
                data.Add('\n');
            }
        }

        // ┌─┐
        // │ │
        // └─┘

        public void SetCursor()
        {
            // Set the cursor position based on pos and scrollPosition
            Console.SetCursorPosition(pos - linePos[lineIndex] + Left, lineIndex + Top - scrollPosition);
        }

        public List<char> GetData()
        {
            return data;
        }

        public void KeyPressLoop(ConsoleKeyInfo key, ref Screen screen, bool printBorder)
        {
            // Check various possible key presses
            if (key.KeyChar >= 97 && key.KeyChar <= 122) // ----- Uppercase characters
            {
                data.Insert(pos, key.KeyChar);
                pos++; // Move cursor
                keepLinePos = -1;
            }
            else if (key.KeyChar >= 65 && key.KeyChar <= 90) // - Lowercase characters
            {
                data.Insert(pos, key.KeyChar);
                pos++; // Move cursor
                keepLinePos = -1;
            }
            else if (key.Key == ConsoleKey.Backspace) // --------- Backspace
            {
                if (pos > 0) // Make sure the cursor is not at the beginning
                {
                    pos--; // Move cursor
                    data.RemoveAt(pos); // Delete
                }
                keepLinePos = -1;
            }
            else if (key.Key == ConsoleKey.Delete) // ------------ Delete
            {
                if (pos < data.Count)
                {
                    data.RemoveAt(pos);
                }
                keepLinePos = -1;
            }
            else if (key.Key == ConsoleKey.LeftArrow) // --------- Left arrow
            {
                if (pos > 0) // Make sure the cursor is not at the beginning
                {
                    pos--; // Move cursor
                }
                keepLinePos = -1;
            }
            else if (key.Key == ConsoleKey.RightArrow) // ------- Right arrow
            {
                if (pos < data.Count) // Make sure the cursor is not at the end
                {
                    pos++; // Move cursor
                }
                keepLinePos = -1;
            }
            else if (key.Key == ConsoleKey.UpArrow) // ---------- Up arrow
            {
                if (lineIndex > 0) // Make sure the cursor is not on the first line
                {
                    // Check if the cursor is not beyond the end of the new line
                    if (pos - (linePos[lineIndex] - linePos[lineIndex - 1]) >= linePos[lineIndex])
                    {
                        if (keepLinePos == -1) // Set up keepLinePos if not already set
                        {
                            keepLinePos = pos - linePos[lineIndex];
                        }

                        // Move the cursor to the end of the line
                        pos = linePos[lineIndex] - 1;
                    }
                    else
                    {
                        // Check keepLinePos
                        if (keepLinePos == -1) // keepLinePos not set -> Move directly upwards
                        {
                            pos -= (linePos[lineIndex] - linePos[lineIndex - 1]);
                        }
                        else if (keepLinePos + linePos[lineIndex - 1] >= linePos[lineIndex]) // keepLinePos beyond the new line
                        {                                                                    //      -> Move to the end of the new line
                            pos = linePos[lineIndex] - 1;
                        }
                        else // keepLinePos within the new line -> Move to keepLinePos
                        {
                            pos = keepLinePos + linePos[lineIndex - 1];
                        }
                    }
                }
            }
            else if (key.Key == ConsoleKey.DownArrow) // -------- Down arrow
            {
                // Check if the cursor is on the penultimate line
                if (lineIndex == linePos.Count - 2)
                {
                    // If it is, check if the cursor is before the end of the line
                    if (pos + (linePos[lineIndex + 1] - linePos[lineIndex]) < data.Count + 1)
                    {
                        // Check keepLinePos
                        if (keepLinePos == -1) // keepLinePos not set -> Move directly downwards
                        {
                            pos += (linePos[lineIndex + 1] - linePos[lineIndex]);
                        }
                        else if (keepLinePos + linePos[lineIndex + 1] >= data.Count) // keepLinePos beyond the new line
                        {                                                            //       -> Move to the end of the new line
                            pos = data.Count;
                        }
                        else // keepLinePos within the new line -> Move to keepLinePos
                        {
                            pos = linePos[lineIndex + 1] + keepLinePos;
                        }
                    }
                    else
                    {
                        if (keepLinePos == -1) // Set up keepLinePos if not already set
                        {
                            keepLinePos = pos - linePos[lineIndex];
                        }

                        // Otherwise move the cursor to the end of the line
                        pos = data.Count;
                    }
                }
                // If not, make sure the cursor is not on the last line
                else if (lineIndex < linePos.Count - 2)
                {
                    // If so, check if the cursor is beyond the end of the line
                    if (pos + (linePos[lineIndex + 1] - linePos[lineIndex]) >= linePos[lineIndex + 2])
                    {
                        if (keepLinePos == -1) // Set up keepLinePos if not already set
                        {
                            keepLinePos = pos - linePos[lineIndex];
                        }

                        // If it is, move to the end of the line
                        pos = linePos[lineIndex + 2] - 1;
                    }
                    else
                    {
                        // Check keepLinePos
                        if (keepLinePos == -1) // keepLinePos not set -> Move directly downwards
                        {
                            pos += (linePos[lineIndex + 1] - linePos[lineIndex]);
                        }
                        else if (linePos[lineIndex + 1] + keepLinePos >= linePos[lineIndex + 2]) // keepLinePos beyond the new line
                        {                                                                        //      -> Move to the end of the new line
                            pos = linePos[lineIndex + 2] - 1;
                        }
                        else // keepLinePos within the new line -> Move to keepLinePos
                        {
                            pos = linePos[lineIndex + 1] + keepLinePos;
                        }
                    }
                }
            }
            else if (key.Key == ConsoleKey.Enter && key.Modifiers != ConsoleModifiers.Control) // ------------- Enter
            {
                data.Insert(pos, '\n');
                pos++; // Move cursor
                keepLinePos = -1;
            }
            else
            {
                data.Insert(pos, key.KeyChar); // --- Any other key
                pos++; // Move cursor
                keepLinePos = -1;
            }

            int[] cursorPosition = new int[] { 0, 0 }; // Variable for the cursor position (column, row)

            // **********************************************************************************************************************************************************************************************************************************************************************

            char[,] output = new char[Width + 4, Height + 2];

            for (int i = 0; i < Width + 4; i++)
            {
                for (int j = 0; j < Height + 2; j++)
                {
                    output[i, j] = ' ';
                }
            }

            // Reset 'linePos'
            linePos = new List<int> { 0 };
            bool exceedWidth = false; // true if a single word exceeds boxWidth

            // Read the 'data' string to determine the position of each line.
            // In this iteration, cursorPosition is a dummy for moving through 'data'.
            for (int i = 0; i < data.Count; i++)
            {
                // Index of the next space or \n
                int index = data.FindIndex(i, x => x == ' ' || x == '\n');

                // Check for for single words that exceed the box width
                if (index == -1)
                {
                    if (data.Count - i >= Width)
                    {
                        exceedWidth = true;
                    }
                }
                else if (index - i >= Width)
                {
                    exceedWidth = true;
                }

                // Check when to move the cursor to a new line. There are four cases:
                // - The next space or \n is beyond the edge of the box.
                // - A long word exceeding the width of the box has reached the edge.
                // - The end of the text is beyond the edge of the box.
                // - A \n is encountered directly.
                if ((index - i + cursorPosition[0] > Width /*+ boxPos[0]*/ && !exceedWidth) ||
                    (cursorPosition[0] >= Width /*+ boxPos[0]*/ && data[i] != ' ') ||
                    (data.Count - i + cursorPosition[0] > Width /*+ boxPos[0]*/ && index == -1 && data.Count - i < Width && !exceedWidth) ||
                    data[i] == '\n')
                {
                    linePos.Add(linePos[^1] + cursorPosition[0] /*- boxPos[0]*/);
                    cursorPosition[0] = 0;// boxPos[0];
                    cursorPosition[1]++;

                    exceedWidth = false;
                }

                if (data[i] == '\n')
                {
                    linePos[^1]++; // Include the new line character in the previous line
                    continue;
                }
                cursorPosition[0]++;
            }

            // Determine which line the current position is on by going backwards through 'linePos' until it is less than 'pos'
            if (linePos.Count != 0)
            {
                lineIndex = linePos.Count - 1;
                while (linePos[lineIndex] > pos)
                {
                    lineIndex--;
                }
            }

            // Now the position of the lines is determined
            // Write the appropriate lines to the screen
            int startIndex, endIndex, printLineIndex;
            cursorPosition = new int[] { 0, 0 /*boxPos[0], boxPos[1]*/ }; // reset cursor position to use for real this time

            if (linePos.Count <= Height)
            {
                scrollPosition = 0;
                startIndex = 0;
                endIndex = data.Count;
            }
            else
            {
                if (lineIndex < scrollPosition)
                {
                    scrollPosition--;
                }
                else if (lineIndex == scrollPosition + Height)
                {
                    scrollPosition++;
                }

                startIndex = linePos[scrollPosition];

                if (scrollPosition <= linePos.Count - 1 - Height)
                {
                    endIndex = linePos[scrollPosition + Height];
                }
                else
                {
                    endIndex = data.Count;
                }
            }
            printLineIndex = scrollPosition;

            for (int i = startIndex; i < endIndex; i++)
            {
                if (printLineIndex < linePos.Count - 1 && i == linePos[printLineIndex + 1])
                {
                    cursorPosition[0] = 0;// boxPos[0];
                    cursorPosition[1]++;
                    printLineIndex++;
                }

                if (data[i] == '\n')
                {
                    continue;
                }

                if (cursorPosition[0] != Width)
                {
                    output[cursorPosition[0] + 2, cursorPosition[1] + 1] = data[i];
                }
                cursorPosition[0]++;
            }

            if (printBorder)
            {
                output[0, 0] = '┌';
                output[Width + 3, 0] = '┐';
                output[0, Height + 1] = '└';
                output[Width + 3, Height + 1] = '┘';

                for (int i = 1; i < Width + 3; i++)
                {
                    output[i, 0] = '─';
                    output[i, Height + 1] = '─';
                }

                for (int i = 1; i < Height + 1; i++)
                {
                    output[0, i] = '│';
                    output[Width + 3, i] = '│';
                }
            }
            screen.Update(output, Left - 2, Top - 1);
        }

        public void PrintData(ref Screen screen, bool printBorder)
        {
            int[] cursorPosition = new int[] { 0, 0 }; // Variable for the cursor position (column, row)

            char[,] output = new char[Width + 4, Height + 2];

            for (int i = 0; i < Width + 4; i++)
            {
                for (int j = 0; j < Height + 2; j++)
                {
                    output[i, j] = ' ';
                }
            }

            // Reset 'linePos'
            linePos = new List<int> { 0 };
            bool exceedWidth = false; // true if a single word exceeds boxWidth

            // Read the 'data' string to determine the position of each line.
            // In this iteration, cursorPosition is a dummy for moving through 'data'.
            for (int i = 0; i < data.Count; i++)
            {
                // Index of the next space or \n
                int index = data.FindIndex(i, x => x == ' ' || x == '\n');

                // Check for for single words that exceed the box width
                if (index == -1)
                {
                    if (data.Count - i >= Width)
                    {
                        exceedWidth = true;
                    }
                }
                else if (index - i >= Width)
                {
                    exceedWidth = true;
                }

                // Check when to move the cursor to a new line. There are four cases:
                // - The next space or \n is beyond the edge of the box.
                // - A long word exceeding the width of the box has reached the edge.
                // - The end of the text is beyond the edge of the box.
                // - A \n is encountered directly.
                if ((index - i + cursorPosition[0] > Width && !exceedWidth) ||
                    (cursorPosition[0] >= Width && data[i] != ' ') ||
                    (data.Count - i + cursorPosition[0] > Width && index == -1 && data.Count - i < Width && !exceedWidth) ||
                    data[i] == '\n')
                {
                    linePos.Add(linePos[^1] + cursorPosition[0]);
                    cursorPosition[0] = 0;
                    cursorPosition[1]++;

                    exceedWidth = false;
                }

                if (data[i] == '\n')
                {
                    linePos[^1]++; // Include the new line character in the previous line
                    continue;
                }
                cursorPosition[0]++;
            }

            // Determine which line the current position is on by going backwards through 'linePos' until it is less than 'pos'
            if (linePos.Count != 0)
            {
                lineIndex = linePos.Count - 1;
                while (linePos[lineIndex] > pos)
                {
                    lineIndex--;
                }
            }

            // Now the position of the lines is determined
            // Write the appropriate lines to the screen
            int startIndex, endIndex, printLineIndex;
            cursorPosition = new int[] { 0, 0 }; // reset cursor position to use for real this time

            if (linePos.Count <= Height)
            {
                scrollPosition = 0;
                startIndex = 0;
                endIndex = data.Count;
            }
            else
            {
                if (lineIndex < scrollPosition)
                {
                    scrollPosition--;
                }
                else if (lineIndex >= scrollPosition + Height)
                {
                    scrollPosition += (lineIndex - scrollPosition - Height + 1);
                }

                startIndex = linePos[scrollPosition];

                if (scrollPosition <= linePos.Count - 1 - Height)
                {
                    endIndex = linePos[scrollPosition + Height];
                }
                else
                {
                    endIndex = data.Count;
                }
            }
            printLineIndex = scrollPosition;

            for (int i = startIndex; i < endIndex; i++)
            {
                if (printLineIndex < linePos.Count - 1 && i == linePos[printLineIndex + 1])
                {
                    cursorPosition[0] = 0;
                    cursorPosition[1]++;
                    printLineIndex++;
                }

                if (data[i] == '\n')
                {
                    continue;
                }

                if (cursorPosition[0] != Width)
                {
                    output[cursorPosition[0] + 2, cursorPosition[1] + 1] = data[i];
                }
                cursorPosition[0]++;
            }

            if (printBorder)
            {
                output[0, 0] = '┌';
                output[Width + 3, 0] = '┐';
                output[0, Height + 1] = '└';
                output[Width + 3, Height + 1] = '┘';

                for (int i = 1; i < Width + 3; i++)
                {
                    output[i, 0] = '─';
                    output[i, Height + 1] = '─';
                }

                for (int i = 1; i < Height + 1; i++)
                {
                    output[0, i] = '│';
                    output[Width + 3, i] = '│';
                }
            }
            screen.Update(output, Left - 2, Top - 1);
        }
    }
}
