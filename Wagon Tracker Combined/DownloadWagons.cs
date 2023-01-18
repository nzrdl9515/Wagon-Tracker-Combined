using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Wagon_Tracker_Combined
{
    static class DownloadWagons
    {
        public static void Run(List<string> wagonClasses, ref Screen screen)
        {
            Textbox box = new Textbox(20, 5, 5, 3);
            ConsoleKeyInfo key;
            int scrollPosition = 0;
            List<string> boxOutput = new List<string>();
            List<string> data = new List<string>();

            screen.Clear();
            screen.Update("Input search parameter".ToCharArray(), 3, 1);
            box.PrintData(ref screen, true);
            box.SetCursor();

            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                box.KeyPressLoop(key, ref screen, true);

                box.SetCursor();
            }

            string keyword = "";
            foreach (char i in box.GetData())
            {
                keyword += i;
            }

            screen.Clear();
            box = new Textbox(screen.Width - 8, screen.Height - 7, 5, 5);

            string searchTerm;

            if(wagonClasses.Count < 5)
            {
                searchTerm = "Searching classes: ";
                for (int i = 0; i < wagonClasses.Count - 1; i++)
                {
                    searchTerm += wagonClasses[i] + ", ";
                }
                searchTerm += wagonClasses.Last();
            }
            else
            {
                searchTerm = "Searching multiple classes";
            }

            screen.Update(searchTerm.ToCharArray(), 3, 1);
            screen.Update(("Keyword: " + keyword).ToCharArray(), 3, 2);
            box.PrintData(ref screen, true);

            for (int i = 0; i < wagonClasses.Count; i++)
            {
                string[] wagons = File.ReadAllLines("wagons/" + wagonClasses[i] + ".txt");
                //string[] results = new string[wagons.Length];
                //const int numClients = 10;

                WebClient[] clients = new WebClient[Program.NumClients];
                for (int j = 0; j < Program.NumClients; j++)
                {
                    clients[j] = new WebClient();
                }

                data.Add(string.Format("Searching class {0} ({1}/{2})", wagonClasses[i], i + 1, wagonClasses.Count));

                screen.Update(string.Format("Current search: {0} (0/{1})               ", wagonClasses[i], wagons.Length).ToCharArray(), 3, 3);

                if (data.Count > box.Height)
                {
                    scrollPosition = data.Count - box.Height;
                }

                for (int j = 0; j < wagons.Length; j += Program.NumClients)
                {
                    Task<string>[] tasks = new Task<string>[Program.NumClients];

                    for (int k = 0; k < Program.NumClients; k++)
                    {
                        if (j + k < wagons.Length)
                        {
                            tasks[k] = clients[k].DownloadStringTaskAsync(new Uri("https://www.kiwirailfreight.co.nz/tc/api/location/current/" + wagons[j + k]));
                        }
                    }

                    for (int k = 0; k < Program.NumClients; k++)
                    {
                        if (j + k < wagons.Length)
                        {
                            //results[j + k] = tasks[k].Result;

                            string result;

                            try
                            {
                                result = tasks[k].Result;
                            }
                            catch(Exception e)
                            {
                                result = e.Message;
                            }

                            if (keyword == "" || (result.Contains(keyword) || result.Contains(keyword.ToLower()) || result.Contains(keyword.ToUpper())))
                            {
                                data.Add(wagons[j + k] + " - " + result);

                                if (data.Count > box.Height)
                                {
                                    scrollPosition++;
                                }
                            }

                            boxOutput = new List<string>();

                            for (int l = 0; l < box.Height; l++)
                            {
                                if (l + scrollPosition == data.Count)
                                {
                                    break;
                                }

                                boxOutput.Add(data[l + scrollPosition]);
                            }
                        }
                    }

                    /*for (int j = 0; j < wagons.Length; j++)
                    {
                        if (keyword == "" || (results[j].Contains(keyword) || results[j].Contains(keyword.ToLower()) || results[j].Contains(keyword.ToUpper())))
                        {
                            data.Add(wagons[j] + " - " + results[j]);

                            if (data.Count > box.Height)
                            {
                                scrollPosition++;
                            }
                        }

                        boxOutput = new List<string>();

                        for (int k = 0; k < box.Height; k++)
                        {
                            if (k + scrollPosition == data.Count)
                            {
                                break;
                            }

                            boxOutput.Add(data[k + scrollPosition]);
                        }
                    }*/

                    box.UpdateData(boxOutput);
                    box.PrintData(ref screen, true);

                    screen.Update(string.Format("Current search: {0} ({1}/{2})                 ", wagonClasses[i], j, wagons.Length).ToCharArray(), 3, 3);
                }

                screen.Update("                                              ".ToCharArray(), 3, 3);
            }

            if (wagonClasses.Count < 5)
            {
                searchTerm = "Search complete: ";
                for (int i = 0; i < wagonClasses.Count - 1; i++)
                {
                    searchTerm += wagonClasses[i] + ", ";
                }
                searchTerm += wagonClasses.Last() + "                     ";
            }
            else
            {
                searchTerm = "Search complete                             ";
            }

            screen.Update(searchTerm.ToCharArray(), 3, 1);

            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Escape)
            {
                switch (key.Key)
                {
                    case ConsoleKey.RightArrow:

                        break;

                    case ConsoleKey.LeftArrow:

                        break;

                    case ConsoleKey.UpArrow:

                        if (scrollPosition > 0)
                        {
                            if (scrollPosition < 5)
                            {
                                scrollPosition = 0;
                            }
                            else
                            {
                                scrollPosition -= 5;
                            }
                        }
                        break;

                    case ConsoleKey.DownArrow:

                        if (scrollPosition + box.Height < data.Count)
                        {
                            scrollPosition += 5;
                        }

                        break;

                    case ConsoleKey.Enter:

                        break;

                    case ConsoleKey.D:

                        break;

                    default:

                        break;
                }

                boxOutput = new List<string>();

                for (int i = 0; i < box.Height; i++)
                {
                    if (i + scrollPosition == data.Count)
                    {
                        break;
                    }

                    boxOutput.Add(data[i + scrollPosition]);

                }

                box.UpdateData(boxOutput);
                box.PrintData(ref screen, true);
            }
        }
    }
}
