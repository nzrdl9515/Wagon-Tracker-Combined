using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wagon_Tracker_Combined
{
    static class ScrollWagons
    {
        public static void Run(Data data, ref Screen screen)
        {
            int wagonIndex = 0;
            int scrollPosition = 0;
            bool updateData = true;
            bool showUniqueByAction = true;
            ConsoleKeyInfo key;

            Textbox box = new Textbox(screen.Width - 8, screen.Height - 5, 5, 3);
            List<string> boxOutput = new List<string>();

            List<WagonEntry> entries = data.GetUniqueEntriesByAction(wagonIndex);

            screen.Update((entries[0].wagon + "    ").ToCharArray(), 3, 1);

            for(int i = 0; i < box.Height; i++)
            {
                if(i == entries.Count)
                {
                    break;
                }

                boxOutput.Add(string.Format("Action: {0} - Location: {1} - My date: {2} - Their date: {3} - Train: {4} - Siding: {5}",
                    entries[i].action.ToString().PadRight(14),
                    entries[i].location.PadRight(16),
                    entries[i].dateTimeMe.ToString("g").PadRight(19),
                    entries[i].dateTimeThem.ToString("g").PadRight(19),
                    entries[i].train.PadRight(8),
                    entries[i].siding));
            }

            box.UpdateData(boxOutput);
            box.PrintData(ref screen, true);

            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Escape)
            {
                switch (key.Key)
                {
                    case ConsoleKey.RightArrow:

                        updateData = true;

                        if (wagonIndex == data.GetWagons().Count - 1)
                        {
                            wagonIndex = 0;
                        }
                        else
                        {
                            wagonIndex++;
                        }
                        scrollPosition = 0;
                        break;

                    case ConsoleKey.LeftArrow:

                        updateData = true;

                        if (wagonIndex == 0)
                        {
                            wagonIndex = data.GetWagons().Count - 1;
                        }
                        else
                        {
                            wagonIndex--;
                        }
                        scrollPosition = 0;
                        break;

                    case ConsoleKey.UpArrow:

                        if(scrollPosition > 0)
                        {
                            scrollPosition -= 5;
                        }
                        break;

                    case ConsoleKey.DownArrow:

                        if(scrollPosition + box.Height < entries.Count)
                        {
                            scrollPosition += 5;
                        }

                        break;

                    case ConsoleKey.Enter:

                        updateData = true;

                        if (showUniqueByAction)
                        {
                            showUniqueByAction = false;
                        }
                        else
                        {
                            showUniqueByAction = true;
                        }
                        scrollPosition = 0;
                        break;

                    case ConsoleKey.D:

                        if (key.Modifiers == ConsoleModifiers.Control)
                        {
                            Textbox selectBox = new Textbox(10, 30, 5, 3);
                            screen.Clear();

                            screen.Update("Select wagon".ToCharArray(), 3, 1);
                            int newWagonIndex = Program.selectFromList(ref screen, ref selectBox, data.GetWagons(), 0, new List<ConsoleKey>() { ConsoleKey.Escape });
                            if(newWagonIndex != -1)
                            {
                                wagonIndex = newWagonIndex;
                            }
                            screen.Clear();
                        }

                        break;

                    default:

                        break;
                }

                if (updateData)
                {
                    if (showUniqueByAction)
                    {
                        entries = data.GetUniqueEntriesByAction(wagonIndex);
                    }
                    else
                    {
                        entries = data.GetUniqueEntries(wagonIndex);
                    }
                }

                screen.Update((entries[0].wagon + "    ").ToCharArray(), 3, 1);
                boxOutput = new List<string>();

                for (int i = 0; i < box.Height; i++)
                {
                    if (i + scrollPosition == entries.Count)
                    {
                        break;
                    }

                    boxOutput.Add(string.Format("Action: {0} - Location: {1} - My date: {2} - Their date: {3} - Train: {4} - Siding: {5}",
                        entries[i + scrollPosition].action.ToString().PadRight(14),
                        entries[i + scrollPosition].location.PadRight(16),
                        entries[i + scrollPosition].dateTimeMe.ToString("g").PadRight(19),
                        entries[i + scrollPosition].dateTimeThem.ToString("g").PadRight(19),
                        entries[i + scrollPosition].train.PadRight(8),
                        entries[i + scrollPosition].siding));
                }

                box.UpdateData(boxOutput);
                box.PrintData(ref screen, true);
            }
        }
    }
}
