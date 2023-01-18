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
            Textbox keywordBox = new Textbox(20, 5, 5, 3);
            ConsoleKeyInfo key;
            int scrollPosition = 0;
            List<string> boxOutput = new List<string>();
            List<string> data = new List<string>();

            // Using the dictionary to store all of the data so that a new keyword can be used
            // Changing to list as there is no access to the specific wagon numbers later on, earier to loop through this way.
            //Dictionary<string, string> allSelectedWagons = new Dictionary<string, string>();
            List<string> allSelectedWagons = new List<string>();

            screen.Clear();
            screen.Update("Input search parameter".ToCharArray(), 3, 1);
            keywordBox.PrintData(ref screen, true);
            keywordBox.SetCursor();

            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                keywordBox.KeyPressLoop(key, ref screen, true);

                keywordBox.SetCursor();
            }

            string keyword = "";
            foreach (char i in keywordBox.GetData())
            {
                keyword += i;
            }

            screen.Clear();
            Textbox displayBox = new Textbox(screen.Width - 8, screen.Height - 7, 5, 5);

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
            displayBox.PrintData(ref screen, true);

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

                if (data.Count > displayBox.Height)
                {
                    scrollPosition = data.Count - displayBox.Height;
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

                            allSelectedWagons.Add(/*wagons[j + k], */wagons[j + k] + " - " + result);

                            if (keyword == "" || (result.Contains(keyword) || result.Contains(keyword.ToLower()) || result.Contains(keyword.ToUpper())))
                            {
                                data.Add(wagons[j + k] + " - " + result);

                                if (data.Count > displayBox.Height)
                                {
                                    scrollPosition++;
                                }
                            }

                            boxOutput = new List<string>();

                            for (int l = 0; l < displayBox.Height; l++)
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

                    displayBox.UpdateData(boxOutput);
                    displayBox.PrintData(ref screen, true);

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

                        if (scrollPosition + displayBox.Height < data.Count)
                        {
                            scrollPosition += 5;
                        }

                        break;

                    case ConsoleKey.Enter:

                        break;

                    case ConsoleKey.D:
                        if (key.Modifiers == ConsoleModifiers.Control)
                        {
                            screen.Clear();
                            screen.Update("Input new search parameter".ToCharArray(), 3, 1);
                            keywordBox.PrintData(ref screen, true);
                            keywordBox.SetCursor();

                            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
                            {
                                keywordBox.KeyPressLoop(key, ref screen, true);

                                keywordBox.SetCursor();
                            }

                            keyword = "";
                            foreach (char c in keywordBox.GetData())
                            {
                                keyword += c;
                            }

                            scrollPosition = 0;

                            data = new List<string>();
                            string lastClass = "";
                            int classCounter = 0;

                            for(int i = 0; i < allSelectedWagons.Count; i++)
                            {
                                string nextClass = allSelectedWagons[i].Substring(0, allSelectedWagons[i].IndexOfAny("123456789".ToCharArray()));

                                if(nextClass != lastClass)
                                {
                                    lastClass = nextClass;

                                    data.Add(string.Format("Class {0} ({1}/{2})", wagonClasses[classCounter], classCounter + 1, wagonClasses.Count));

                                    classCounter++;
                                }

                                if (keyword == "" || (allSelectedWagons[i].Contains(keyword) || allSelectedWagons[i].Contains(keyword.ToLower()) || allSelectedWagons[i].Contains(keyword.ToUpper())))
                                {
                                    data.Add(allSelectedWagons[i]);
                                }
                            }

                            screen.Clear();
                        }
                        break;

                    default:

                        break;
                }

                boxOutput = new List<string>();

                for (int i = 0; i < displayBox.Height; i++)
                {
                    if (i + scrollPosition == data.Count)
                    {
                        break;
                    }

                    boxOutput.Add(data[i + scrollPosition]);

                }

                displayBox.UpdateData(boxOutput);
                displayBox.PrintData(ref screen, true);
            }
        }
    }
}
