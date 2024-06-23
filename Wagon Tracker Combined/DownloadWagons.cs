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
        public static void RunManual(ref Screen screen, ref Searches searches)
        {
            Textbox keywordBox = new Textbox(20, 5, 5, 3);
            Textbox rightBox = new Textbox(15, 30, 25, 5);
            Textbox leftBox = new Textbox(15, 30, 5, 5);
            string[] wagonClasses = File.ReadAllLines("wagon_classes.txt");

            List<int> wagonClassesCount = new List<int>();

            foreach(string wagon in wagonClasses)
            {
                wagonClassesCount.Add(File.ReadAllLines("wagons/" + wagon + ".txt").Length);
            }

            screen.Clear();
            screen.Update("Select wagons".ToCharArray(), 3, 1);
            List<string> selectedOptions = new List<string>();// = Program.selectMultipleFromList(ref screen, ref leftBox, ref rightBox, new List<string>(wagonClasses));

            while(selectedOptions.Count == 0 || selectedOptions[0] == "escape")
            {
                selectedOptions = Program.selectMultipleFromList(ref screen, ref leftBox, ref rightBox, new List<string>(wagonClasses), wagonClassesCount);
            }

            screen.Clear();

            string keyword = "";// keywordBox.GetKeyboardInput("Input search parameter", ref screen, true);

            //screen.Clear();

            run(selectedOptions, keyword, ref screen, ref searches);
        }

        public static void RunSaved(ref Screen screen, ref Searches searches)
        {
            screen.Clear();

            screen.Update(("Choose saved search").ToCharArray(), 3, 1);
            Search search = searches.Choose(ref screen);
            
            screen.Clear();

            run(search.Wagons, search.Keyword, ref screen, ref searches);
        }

        private static void run(List<string> wagonClasses, string keyword, ref Screen screen, ref Searches searches)
        {
            Textbox keywordBox = new Textbox(20, 5, 5, 3);
            keywordBox.UpdateData(new List<string>() { keyword }, false);
            ConsoleKeyInfo key;
            int scrollPosition = 0;
            bool inUniqueTrains = false;
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

            Textbox displayBox = new Textbox(screen.Width - 10/*50*/, screen.Height - 5, 5, 3); // - 50 used when creating output for the instruction manual.

            data = getDisplayData(keyword, ref allWagonsData, wagonClasses.Count);

            // ***** BEGIN - Remove wagons with no data from the saved files *****
            List<string> wagonsToRemove = new List<string>();
            string wagonClassForRemoval = data[0].Split(' ')[1];
            int c = 1;

            while(c < data.Count)
            {
                if (data[c].Contains("\"\"}"))
                {
                    // List wagons to be removed and note them down for future reference just in case
                    wagonsToRemove.Add(data[c].Split(" ")[0]);
                    File.AppendAllText("Deleted Wagons.txt", wagonsToRemove.Last() + '\n');

                    // Remove wagons with no data from the display data
                    data.RemoveAt(c);
                    c--;
                }

                // If encountering a new class, update the list of wagons for the previous class
                if (data[c].Contains("Class") || c == data.Count - 1)
                {
                    if (wagonsToRemove.Count > 0)
                    {
                        List<string> wagonsInClass = new List<string>(File.ReadAllLines("wagons/" + wagonClassForRemoval + ".txt"));
                        foreach (string wagonToRemove in wagonsToRemove)
                        {
                            wagonsInClass.Remove(wagonToRemove);
                        }

                        File.Delete("wagons/" + wagonClassForRemoval + ".txt");
                        File.AppendAllLines("wagons/" + wagonClassForRemoval + ".txt", wagonsInClass);
                    }

                    wagonsToRemove = new List<string>();
                    wagonClassForRemoval = data[c].Split(' ')[1];
                }

                c++;
            }
            // ***** END *********************************************************

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

                    // ************************ Filter search results ************************
                    case ConsoleKey.F:
                    case ConsoleKey.G: // ************************ Filter search results ************************
                        if (key.Modifiers == ConsoleModifiers.Control && !inUniqueTrains)
                        {
                            keyword = keywordBox.GetKeyboardInput("Input new search parameter", ref screen, true);

                            scrollPosition = 0;

                            data = getDisplayData(keyword, ref allWagonsData, wagonClasses.Count);

                            screen.Clear();
                            screen.Update(("Keyword: " + keyword).ToCharArray(), 3, 1);
                        }
                        break;

                    // ************************ Add wagons to continuous download ************************
                    case ConsoleKey.D: // ************************ Add wagons to continuous download ************************
                        if (key.Modifiers == ConsoleModifiers.Control && !inUniqueTrains)
                        {
                            screen.Clear();

                            List<string> contWagons = new List<string>();

                            foreach (string entry in data)
                            {
                                if (entry.Substring(0, 5) != "Class")
                                {
                                    contWagons.Add(entry.Substring(0, entry.IndexOf(" ")));
                                }
                            }

                            screen.Update(string.Format("Confirm {0} wagons to continuously download", contWagons.Count).ToCharArray(), 3, 1);

                            Textbox selectBox = new Textbox(50, 2, 5, 3);
                            if(Program.selectFromList(ref screen, ref selectBox, new List<string> { "No", "Yes" }, 0, new List<ConsoleKey>()) == 1)
                            {
                                foreach(string wagon in contWagons)
                                {
                                    Program.DownloadInstructions.Add(new Instruction("addwagon " + wagon));
                                }
                            }

                            screen.Clear();
                        }
                        break;

                    // ************************ Refresh data ************************
                    case ConsoleKey.R: // ************************ Refresh data ************************
                        if (key.Modifiers == ConsoleModifiers.Control && !inUniqueTrains)
                        {
                            // Download all wagons again
                            // i.e everything in allSelectedWagons

                            List<string> searchWagons = new List<string>();

                            foreach (string entry in data)
                            {
                                if (entry.Substring(0, 5) != "Class")
                                {
                                    searchWagons.Add(entry.Substring(0, entry.IndexOf(" ")));
                                }
                            }

                            Download(new List<string>(allWagonsData.Keys), ref allWagonsData, ref screen);

                            data = getDisplayData(searchWagons, ref allWagonsData, wagonClasses.Count);

                            screen.Clear();
                            screen.Update(("Data refreshed. Original keyword: " + keyword).ToCharArray(), 3, 1);
                        }
                        else if((key.Modifiers & ConsoleModifiers.Control) != 0 && (key.Modifiers & ConsoleModifiers.Shift) != 0 && !inUniqueTrains)
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

                            data = getDisplayData(searchWagons, ref allWagonsData, wagonClasses.Count);

                            screen.Clear();
                            screen.Update(("Data refreshed. Original keyword: " + keyword).ToCharArray(), 3, 1);
                        }
                        break;

                    // ************************ Save search ************************
                    case ConsoleKey.S: // ************************ Save search ************************
                        if (key.Modifiers == ConsoleModifiers.Control)
                        {
                            screen.Clear();

                            Textbox saveSearchBox = new Textbox(20, 5, 5, 3);

                            string searchName = saveSearchBox.GetKeyboardInput("Input a name for save", ref screen, true);
                            string wagonsToSave = wagonClasses[0];

                            for(int i = 1; i < wagonClasses.Count; i++)
                            {
                                wagonsToSave = wagonsToSave + "," + wagonClasses[i];
                            }

                            string previousSearchName = searches.CheckExistingSearch(wagonsToSave, keyword);

                            if (previousSearchName == null)
                            {
                                // Search hasn't already been saved under a different name, so now just check that the name is unique
                                while (!searches.CheckNameUnique(searchName))
                                {
                                    searchName = saveSearchBox.GetKeyboardInput("A different save has the same name. Input a new name for this save.", ref screen, true);
                                }

                                searches.AddSearch(searchName, wagonsToSave, keyword);
                            }
                            else // Search has already been saved
                            {
                                if (previousSearchName == searchName)
                                {
                                    // Name was the same, so no action needs to be taken.
                                    screen.Clear();

                                    screen.Update("This search has already been saved under the same name. Press any key to continue.".ToCharArray(), 3, 1);

                                    Console.ReadKey();
                                }
                                else
                                {
                                    // Name was different, so ask if it should be updated.
                                    screen.Clear();
                                    screen.Update(string.Format("This search has already been saved as '{0}'. Re-name this saved search as '{1}'?", previousSearchName, searchName).ToCharArray(), 3, 1);

                                    Textbox selectBox = new Textbox(50, 2, 5, 3);
                                    if(Program.selectFromList(ref screen, ref selectBox, new List<string> { "No", "Yes" }, 0, new List<ConsoleKey>()) == 1)
                                    {
                                        searches.UpdateSearchName(previousSearchName, searchName);
                                    }
                                }
                            }

                            screen.Clear();
                        }
                        break;

                    // ************************ Search for all the unique trains in the data ************************
                    case ConsoleKey.T: // Search for all the unique trains in the data
                        if (key.Modifiers == ConsoleModifiers.Control)
                        {
                            if (!inUniqueTrains)
                            {
                                inUniqueTrains = true;

                                List<string> uniqueTrains = new List<string>();

                                foreach (string wagon in allWagonsData.Values)
                                {
                                    if (!uniqueTrains.Contains(wagon) && wagon.Contains("train"))
                                    {
                                        uniqueTrains.Add(wagon);
                                    }
                                }

                                uniqueTrains.Sort();

                                data = uniqueTrains;

                                screen.Clear();
                                screen.Update(("Unique trains").ToCharArray(), 3, 1);

                                scrollPosition = 0;
                            }
                            else
                            {
                                inUniqueTrains = false;

                                scrollPosition = 0;

                                data = getDisplayData(keyword, ref allWagonsData, wagonClasses.Count);

                                screen.Clear();
                                screen.Update(("Keyword: " + keyword).ToCharArray(), 3, 1);
                            }
                        }

                        break;

                    // ************************ Write search results to file ************************
                    case ConsoleKey.P: // ************************ Write search results to file ************************
                        if (key.Modifiers == ConsoleModifiers.Control)
                        {
                            string fileName = string.Format("Download result {0}.txt", DateTime.Now.ToString("MM-dd-yyyy HH-mm-ss"));

                            foreach(string line in data)
                            {
                                if (!line.Contains("Class"))
                                {
                                    File.AppendAllText(fileName, line + '\n');
                                }
                            }

                            screen.Clear();

                            screen.Update("Search results saved to file".ToCharArray(), 3, 1);
                            screen.Update("Press [ENTER] to continue...".ToCharArray(), 3, 3);

                            while(Console.ReadKey(true).Key != ConsoleKey.Enter)
                            {

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

                if (keyword.Length > 0 && keyword[0] == '!')
                {
                    string keyword2 = keyword.Substring(1);
                    // Show if the wagon string does NOT contain the keyword
                    if(keyword == "!" || !allWagonsData.Values[i].ToLower().Contains(keyword2.ToLower()))
                    {
                        displayData.Add(allWagonsData.Keys[i] + " - " + allWagonsData.Values[i]);
                    }
                }
                else
                {
                    // Show if the wagon string DOES contain the keyword
                    if (keyword == "" || allWagonsData.Values[i].ToLower().Contains(keyword.ToLower()))
                    {
                        displayData.Add(allWagonsData.Keys[i] + " - " + allWagonsData.Values[i]);
                    }
                }
            }

            return displayData;
        }

        private static List<string> getDisplayData(List<string> wagons, ref SortedList<string, string> allWagonsData, int numClasses)
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
                if (wagons.Contains(allWagonsData.Keys[i]))
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

        public static string Download (string wagon)
        {
            WebClient cl = new WebClient();

            try
            {
                return cl.DownloadString(new Uri("https://www.kiwirailfreight.co.nz/tc/api/location/current/" + wagon));
            }
            catch
            {
                return wagon + " download error";
            }
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
