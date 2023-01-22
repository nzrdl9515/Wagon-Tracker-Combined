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
            List<string> boxOutput;
            List<string> data;
            SortedList<string, string> allWagonsData = new SortedList<string, string>(new StringLogicalComparer());

            foreach(string wagonClass in wagonClasses)
            {
                foreach(string wagon in File.ReadAllLines("wagons/" + wagonClass + ".txt"))
                {
                    allWagonsData.Add(wagon, "");
                }
            }

            Download(new List<string>(allWagonsData.Keys), ref allWagonsData, ref screen);

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
            Textbox displayBox = new Textbox(screen.Width - 8, screen.Height - 6, 5, 4);

            data = getDisplayData(keyword, ref allWagonsData, wagonClasses.Count);

            boxOutput = new List<string>();

            for (int i = 0; i < displayBox.Height; i++)
            {
                if (i + scrollPosition == data.Count)
                {
                    break;
                }

                boxOutput.Add(data[i + scrollPosition]);

            }

            screen.Update(("Keyword: " + keyword).ToCharArray(), 3, 1);
            displayBox.UpdateData(boxOutput);
            displayBox.PrintData(ref screen, true);

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

                            data = getDisplayData(keyword, ref allWagonsData, wagonClasses.Count);

                            screen.Clear();
                            screen.Update(("Keyword: " + keyword).ToCharArray(), 3, 1);
                        }
                        break;

                    case ConsoleKey.R:
                        if (key.Modifiers == ConsoleModifiers.Control)
                        {
                            // Download all wagons again
                            // i.e everything in allSelectedWagons

                            Download(new List<string>(allWagonsData.Keys), ref allWagonsData, ref screen);

                            data = getDisplayData(keyword, ref allWagonsData, wagonClasses.Count);

                            screen.Clear();
                            screen.Update(("Keyword: " + keyword).ToCharArray(), 3, 1);
                        }
                        else if((key.Modifiers & ConsoleModifiers.Control) != 0 && (key.Modifiers & ConsoleModifiers.Shift) != 0)
                        {
                            // Download only those wagons currently shown
                            // i.e. everything in data

                            List<string> searchWagons = new List<string>();

                            foreach(string entry in data)
                            {
                                if(entry.Substring(0, 5) != "Class")
                                {
                                    searchWagons.Add(entry.Substring(0, entry.IndexOf(" ")));
                                }
                            }

                            Download(searchWagons, ref allWagonsData, ref screen);

                            data = getDisplayData(keyword, ref allWagonsData, wagonClasses.Count);

                            screen.Clear();
                            screen.Update(("Keyword: " + keyword).ToCharArray(), 3, 1);
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

        private static List<string> getDisplayData(string keyword, ref SortedList<string, string> allWagonsData, int numClasses)
        {
            List<string> displayData = new List<string>();

            string lastClass = "";
            int classCounter = 0;

            for (int i = 0; i < allWagonsData.Count; i++)
            {
                // Check if the next wagon is from a new class, so as to add the heading line to the output data
                string nextClass = allWagonsData.Keys[i].Substring(0, allWagonsData.Keys[i].IndexOfAny("123456789".ToCharArray()));

                if (nextClass != lastClass)
                {
                    lastClass = nextClass;

                    displayData.Add(string.Format("Class {0} ({1}/{2})", nextClass, classCounter + 1, numClasses));

                    classCounter++;
                }

                // If the wagon matches the keyword, add it to the output data
                if (keyword == "" || (allWagonsData.Values[i].Contains(keyword) || allWagonsData.Values[i].Contains(keyword.ToLower()) || allWagonsData.Values[i].Contains(keyword.ToUpper())))
                {
                    displayData.Add(allWagonsData.Keys[i] + " - " + allWagonsData.Values[i]);
                }
            }

            return displayData;
        }

        public static void Download (List<string> wagons, ref SortedList<string, string> allWagonsData, ref Screen screen)
        {
            foreach(string wagon in wagons)
            {
                if (!allWagonsData.ContainsKey(wagon))
                {
                    allWagonsData.Add(wagon, "");
                }
            }

            // Set up multiple download clients
            WebClient[] clients = new WebClient[Program.NumClients];
            for (int j = 0; j < Program.NumClients; j++)
            {
                clients[j] = new WebClient();
            }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // Download multiple wagons at the same time
            for (int j = 0; j < wagons.Count; j += Program.NumClients)
            {
                // Set up the async tasks
                Task<string>[] tasks = new Task<string>[Program.NumClients];

                for (int k = 0; k < Program.NumClients; k++)
                {
                    if (j + k < wagons.Count)
                    {
                        tasks[k] = clients[k].DownloadStringTaskAsync(new Uri("https://www.kiwirailfreight.co.nz/tc/api/location/current/" + wagons[j + k]));
                    }
                }

                // Get the task results
                for (int k = 0; k < Program.NumClients; k++)
                {
                    if (j + k < wagons.Count)
                    {
                        string result;

                        try
                        {
                            result = tasks[k].Result;
                        }
                        catch (Exception e)
                        {
                            result = e.Message;
                        }

                        allWagonsData[wagons[j + k]] = result;
                    }
                }

                // Update the screen to show download progress
                TimeSpan timePerWagon = sw.Elapsed / (j + Program.NumClients);
                TimeSpan estTimeRemaining = timePerWagon * (wagons.Count - j - Program.NumClients);

                screen.Update(string.Format("Wagons downloaded: {0}/{1}         ", j, wagons.Count).ToCharArray(), 3, 1);

                if (j > 30)
                {
                    screen.Update(("Time remaining: " + Math.Round(estTimeRemaining.TotalSeconds).ToString() + "s        ").ToCharArray(), 3, 2);
                }
            }

            sw.Stop();
        }

        public static void Download(List<string> wagons, ref SortedList<string, string> allWagonsData)
        {
            foreach (string wagon in wagons)
            {
                if (!allWagonsData.ContainsKey(wagon))
                {
                    allWagonsData.Add(wagon, "");
                }
            }

            // Set up multiple download clients
            WebClient[] clients = new WebClient[Program.NumClients];
            for (int j = 0; j < Program.NumClients; j++)
            {
                clients[j] = new WebClient();
            }

            // Download multiple wagons at the same time
            for (int j = 0; j < wagons.Count; j += Program.NumClients)
            {
                // Set up the async tasks
                Task<string>[] tasks = new Task<string>[Program.NumClients];

                for (int k = 0; k < Program.NumClients; k++)
                {
                    if (j + k < wagons.Count)
                    {
                        tasks[k] = clients[k].DownloadStringTaskAsync(new Uri("https://www.kiwirailfreight.co.nz/tc/api/location/current/" + wagons[j + k]));
                    }
                }

                // Get the task results
                for (int k = 0; k < Program.NumClients; k++)
                {
                    if (j + k < wagons.Count)
                    {
                        string result;

                        try
                        {
                            result = tasks[k].Result;
                        }
                        catch (Exception e)
                        {
                            result = e.Message;
                        }

                        allWagonsData[wagons[j + k]] = result;
                    }
                }
            }
        }
    }
}
