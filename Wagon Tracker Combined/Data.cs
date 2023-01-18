using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Wagon_Tracker_Combined
{
    class Data
    {
        private Dictionary<string, List<WagonEntry>> allEntries;
        private List<string> wagons;

        public Data(string filePath)
        {
            allEntries = new Dictionary<string, List<WagonEntry>>();

            List<string> data = readFile(filePath);

            for (int i = 0; i < data.Count; i++)
            {

                WagonEntry wagon = parseEntry(data[i]);

                // If the wagon hasn't already been added to the dictionary, add it
                if (!allEntries.ContainsKey(wagon.wagon))
                {
                    allEntries.Add(wagon.wagon, new List<WagonEntry>());
                }

                // Add data to the wagon entry in the dictionary
                allEntries[wagon.wagon].Add(wagon);
            }

            wagons = new List<string>(allEntries.Keys);

            wagons.Sort(new StringLogicalComparer());
        }

        public List<string> GetWagons()
        {
            return wagons;
        }

        public WagonEntry GetSingleEntry(string wagon, int index)
        {
            return allEntries[wagon][index];
        }

        /*public List<WagonEntry> GetAllEntries(int wagonIndex)
        {
            return allEntries[wagons[wagonIndex]];
        }*/

        public List<WagonEntry> GetUniqueEntries(int wagonIndex)
        {
            List<WagonEntry> unique = new List<WagonEntry>();

            WagonEntry lastEntry = new WagonEntry();

            for (int i = 0; i < allEntries[wagons[wagonIndex]].Count; i++)
            {
                if (allEntries[wagons[wagonIndex]][i] != lastEntry)
                {
                    unique.Add(allEntries[wagons[wagonIndex]][i]);

                    lastEntry = allEntries[wagons[wagonIndex]][i];
                }
            }

            return unique;
        }

        public List<WagonEntry> GetUniqueEntriesByAction(int wagonIndex)
        {
            List<WagonEntry> unique = new List<WagonEntry>();

            Action lastAction = Action.notSet;

            for (int i = 0; i < allEntries[wagons[wagonIndex]].Count; i++)
            {
                if (allEntries[wagons[wagonIndex]][i].action != lastAction)
                {
                    unique.Add(allEntries[wagons[wagonIndex]][i]);

                    lastAction = allEntries[wagons[wagonIndex]][i].action;
                }
            }

            return unique;
        }

        private List<string> readFile(string filePath)
        {
            List<string> data = new List<string>();
            List<int> errorIndices = new List<int>();
            bool resave = false;

            // Read the data file of interest
            using (StreamReader sr = new StreamReader(filePath))
            //using (StreamReader sr = new StreamReader("data long 17-5.txt"))
            {
                string line;
                //string wagonError = "";
                //DateTime errorTime = DateTime.MinValue;
                int c = 0;

                while ((line = sr.ReadLine()) != null)
                {
                    /*if(line.Substring(0, line.IndexOf(" ")) == wagonError)
                    {
                        int index = line.IndexOf(" - ");
                        DateTime fixTime = DateTime.Parse(line.Substring(index + 3, line.IndexOf(" - ", index + 1) - index - 3));

                        Console.WriteLine(fixTime.Subtract(errorTime).TotalMinutes + " - " + wagonError);

                        wagonError = "";
                    }*/

                    if (line.IndexOf("500") != -1 || line.IndexOf("Unable") != -1 || line.IndexOf("timed out") != -1 || line.IndexOf("code\":-1") != -1 || line.IndexOf("error occurred") != -1)
                    {
                        // An error line has been encountered
                        errorIndices.Add(c);

                        if (!resave)
                        {
                            resave = true;
                        }

                        /*int index = line.IndexOf(" - ");
                        wagonError = line.Substring(0, index);
                        errorTime = DateTime.Parse(line.Substring(index + 3, line.IndexOf(" - ", index + 1) - index - 3));*/
                    }

                    data.Add(line);
                    c++;
                }
            }

            if (resave)
            {
                // Overwrite the file
                List<int> indicesToDelete = new List<int>();

                // ************************************************************************************************************************************************************************************
                // This doesn't account for what happens if the wagon starts on a train after the error, but that train has not yet arrived (i.e. more data entries for that train will be forthcoming)
                // ************************************************************************************************************************************************************************************

                //for(int i = 0; i < errorIndices.Count; i++)
                for (int i = 0; i < errorIndices.Count; i++)
                {
                    WagonEntry error = parseEntry(data[errorIndices[i]]);
                    WagonEntry before;
                    WagonEntry after;

                    int prevIndex = getPrevEntry(ref data, errorIndices[i], error.wagon);
                    int nextIndex = getNextEntry(ref data, errorIndices[i], error.wagon);

                    if (prevIndex == -1 && nextIndex == -1)
                    {
                        // There are NO entries before AND no entries after
                        // So just leave it, it's not going to hurt anything
                    }
                    else if (prevIndex == -1 && nextIndex != -1)
                    {
                        // There are no entries before
                        // So we should check if we need to remove entries after

                        after = parseEntry(data[nextIndex]);

                        if (after.action == Action.onTrain)
                        {
                            // Delete all train entries after the error

                            indicesToDelete.Add(errorIndices[i]);
                            int index = getNextEntry(ref data, errorIndices[i], error.wagon);
                            WagonEntry entry = parseEntry(data[index]);
                            while (entry.action == Action.onTrain || entry.action == Action.unableToLocate)
                            {
                                if (entry.action == Action.unableToLocate)
                                {
                                    errorIndices.Remove(index);
                                }
                                indicesToDelete.Add(index);
                                index = getNextEntry(ref data, index, error.wagon);
                                if(index == -1)
                                {
                                    Program.DownloadInstructions.Add(new Instruction("ignorewagon " + error.wagon));
                                    break;
                                }
                                entry = parseEntry(data[index]);
                            }
                        }
                        else
                        {
                            indicesToDelete.Add(errorIndices[i]);
                        }
                    }
                    else if (prevIndex != -1 && nextIndex == -1)
                    {
                        // There are no entries after
                        // So worry about this later so we can make a decision when the next data is available
                    }
                    else
                    {
                        // There ARE entries before AND after
                        // So we need to check both

                        before = parseEntry(data[prevIndex]);
                        after = parseEntry(data[nextIndex]);

                        if (after.dateTimeMe.Subtract(error.dateTimeMe).TotalMinutes < 9)
                        {
                            // Time gap is short enough to ignore, so just delete the missing entry
                            indicesToDelete.Add(errorIndices[i]);

                            if(before == after)
                            {
                                indicesToDelete.Add(nextIndex);
                            }
                        }
                        else if (before.action == Action.onTrain && after.action != Action.onTrain)
                        {
                            // Delete all train entries before the error
                            indicesToDelete.Add(errorIndices[i]);
                            int index = getPrevEntry(ref data, errorIndices[i], error.wagon);
                            while(parseEntry(data[index]).action == Action.onTrain)
                            {
                                indicesToDelete.Add(index);
                                index = getPrevEntry(ref data, index, error.wagon);
                            }
                        }
                        else if (before.action != Action.onTrain && after.action == Action.onTrain)
                        {
                            // Delete all train entries after the error

                            indicesToDelete.Add(errorIndices[i]);
                            int index = getNextEntry(ref data, errorIndices[i], error.wagon);
                            WagonEntry entry = parseEntry(data[index]);
                            while (entry.action == Action.onTrain || entry.action == Action.unableToLocate)
                            {
                                if(entry.action == Action.unableToLocate)
                                {
                                    errorIndices.Remove(index);
                                }
                                indicesToDelete.Add(index);
                                index = getNextEntry(ref data, index, error.wagon);
                                if (index == -1)
                                {
                                    Program.DownloadInstructions.Add(new Instruction("ignorewagon " + error.wagon));
                                    break;
                                }
                                entry = parseEntry(data[index]);
                            }
                        }
                        else if (before.train != after.train)
                        {
                            // Trains before and after are different, so delete both trains entirely
                            indicesToDelete.Add(errorIndices[i]);
                            int index = getPrevEntry(ref data, errorIndices[i], error.wagon);
                            while (parseEntry(data[index]).action == Action.onTrain)
                            {
                                indicesToDelete.Add(index);
                                index = getPrevEntry(ref data, index, error.wagon);
                            }

                            index = getNextEntry(ref data, errorIndices[i], error.wagon);
                            WagonEntry entry = parseEntry(data[index]);
                            while (entry.action == Action.onTrain || entry.action == Action.unableToLocate)
                            {
                                if (entry.action == Action.unableToLocate)
                                {
                                    errorIndices.Remove(index);
                                }
                                indicesToDelete.Add(index);
                                index = getNextEntry(ref data, index, error.wagon);
                                if (index == -1)
                                {
                                    Program.DownloadInstructions.Add(new Instruction("ignorewagon " + error.wagon));
                                    break;
                                }
                                entry = parseEntry(data[index]);
                            }
                        }
                        else if (before.train == after.train && before.train != "")
                        {

                            if (before == after)
                            {
                                indicesToDelete.Add(errorIndices[i]);
                                indicesToDelete.Add(nextIndex);
                            }
                            else
                            {

                                // Trains before and after appear to be the same, but just to be safe, better ask and check
                                Console.WriteLine("Line: " + (errorIndices[i] + 1));
                                Console.WriteLine(before.train.PadRight(10) + before.location.PadRight(20) + before.dateTimeMe.ToString("g").PadRight(20) + before.dateTimeThem.ToString("g"));
                                Console.WriteLine(after.train.PadRight(10) + after.location.PadRight(20) + after.dateTimeMe.ToString("g").PadRight(20) + after.dateTimeThem.ToString("g"));

                                Console.WriteLine("Confirm the wagon is on the same train (y/n)");

                                bool validKey = false;

                                while (!validKey)
                                {
                                    switch (Console.ReadKey(true).Key)
                                    {
                                        case ConsoleKey.Y:
                                            validKey = true;
                                            indicesToDelete.Add(errorIndices[i]);

                                            break;

                                        case ConsoleKey.N:
                                            validKey = true;
                                            Console.WriteLine("Confirm both before and after trains to be removed (y/n)");
                                            bool validKey2 = false;

                                            while (!validKey2)
                                            {
                                                switch (Console.ReadKey(true).Key)
                                                {
                                                    case ConsoleKey.Y:
                                                        validKey2 = true;
                                                        indicesToDelete.Add(errorIndices[i]);
                                                        int index = getPrevEntry(ref data, errorIndices[i], error.wagon);
                                                        while (parseEntry(data[index]).action == Action.onTrain)
                                                        {
                                                            indicesToDelete.Add(index);
                                                            index = getPrevEntry(ref data, index, error.wagon);
                                                        }

                                                        index = getNextEntry(ref data, errorIndices[i], error.wagon);
                                                        WagonEntry entry = parseEntry(data[index]);
                                                        while (entry.action == Action.onTrain || entry.action == Action.unableToLocate)
                                                        {
                                                            if (entry.action == Action.unableToLocate)
                                                            {
                                                                errorIndices.Remove(index);
                                                            }
                                                            indicesToDelete.Add(index);
                                                            index = getNextEntry(ref data, index, error.wagon);
                                                            entry = parseEntry(data[index]);
                                                        }

                                                        break;
                                                    case ConsoleKey.N:
                                                        validKey = false;
                                                        validKey2 = true;
                                                        Console.Clear();
                                                        Console.WriteLine("Confirm the wagon is on the same train (y/n)");

                                                        break;
                                                    default:

                                                        break;
                                                }
                                            }

                                            break;

                                        default:

                                            break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // The wagon wasn't on a train, so just delete the missing entry
                            indicesToDelete.Add(errorIndices[i]);

                            if (before == after)
                            {
                                indicesToDelete.Add(nextIndex);
                            }
                        }
                    }
                }

                /*Console.WriteLine("_________________________________________________________________________\n");

                for(int i = 0; i < indicesToDelete.Count; i++)
                {
                    Console.Write((indicesToDelete[i] + 1).ToString().PadRight(7));

                    if((i + 1) % 20 == 0)
                    {
                        Console.WriteLine();
                    }
                }

                using(StreamWriter sw = new StreamWriter("delete test.txt"))
                {
                    for(int i = 0; i < data.Count; i++)
                    {
                        if (!indicesToDelete.Contains(i))
                        {
                            sw.WriteLine(data[i]);
                        }
                    }
                }*/

                // ***********************************************************
                // ***** Re-write the file, provided it is safe to do so *****
                // ***********************************************************

                if (!Program.FileInUse)
                {
                    Program.FileLocked = true;
                    System.Threading.Thread.Sleep(500);

                    if (Program.FileInUse)
                    {
                        // File was opened at the same time, so throw an error
                        Program.FileLocked = false;
                        Console.WriteLine("Unable to write file at this time\nAnalysis will continue without saving the file");
                        System.Threading.Thread.Sleep(2000);
                    }
                    else
                    {
                        // Fine to write the file
                        File.Delete("data.txt");

                        using (StreamWriter sw = new StreamWriter("data.txt"))
                        {
                            for (int i = 0; i < data.Count; i++)
                            {
                                if (!indicesToDelete.Contains(i))
                                {
                                    sw.WriteLine(data[i]);
                                }
                            }
                        }

                        Program.FileLocked = false;
                    }
                }
            }

            return data;
        }

        private int getPrevEntry(ref List<string> data, int i, string errorWagon)
        {
            i--;
            while (i >= 0 && parseEntry(data[i]).wagon != errorWagon)
            {
                i--;
            }

            return i;
        }

        private int getNextEntry(ref List<string> data, int i, string errorWagon)
        {
            i++;
            while (i < data.Count && parseEntry(data[i]).wagon != errorWagon)
            {
                i++;
            }

            if (i == data.Count)
            {
                return -1;
            }

            return i;
        }

        private WagonEntry parseEntry(string data)
        {
            WagonEntry wagon = new WagonEntry();

            // Find the keyword to work out what type of result was received
            int startIndex = data.IndexOf("\":\"");
            int endIndex;
            string keyword;

            if (startIndex == -1)
            {
                keyword = "500";
            }
            else
            {
                endIndex = data.IndexOf(" ", startIndex);

                if (endIndex == -1)
                {
                    keyword = "";
                }
                else
                {
                    keyword = data.Substring(startIndex + 3, endIndex - startIndex - 3);
                }
            }

            // Parse the data differently depending on the keyword
            switch (keyword)
            {
                case "In":
                    wagon.action = Action.inRailSiding;

                    startIndex = data.IndexOf(" - ");
                    wagon.wagon = data.Substring(0, startIndex);

                    endIndex = data.IndexOf(" - ", startIndex + 1);
                    wagon.dateTimeMe = DateTime.Parse(data.Substring(startIndex + 3, endIndex - startIndex - 3));

                    startIndex = data.IndexOf("siding");
                    endIndex = data.IndexOf("at", startIndex);
                    wagon.siding = data.Substring(startIndex + 7, endIndex - startIndex - 9).Trim(' ');

                    startIndex = endIndex + 3;
                    endIndex = data.IndexOf("since");
                    wagon.location = data.Substring(startIndex, endIndex - startIndex - 1);

                    startIndex = endIndex + 6;
                    endIndex = data.IndexOf("and");
                    wagon.dateTimeThem = DateTime.Parse(data.Substring(startIndex, endIndex - startIndex - 1));

                    wagon.train = "";

                    break;

                case "Arrived":
                    wagon.action = Action.arrived;

                    startIndex = data.IndexOf(" - ");
                    wagon.wagon = data.Substring(0, startIndex);

                    endIndex = data.IndexOf(" - ", startIndex + 1);
                    wagon.dateTimeMe = DateTime.Parse(data.Substring(startIndex + 3, endIndex - startIndex - 3));

                    startIndex = data.IndexOf("in", endIndex);
                    endIndex = data.IndexOf("at", startIndex);
                    wagon.location = data.Substring(startIndex + 3, endIndex - startIndex - 4);

                    startIndex = endIndex + 3;
                    endIndex = data.IndexOf("but");
                    wagon.dateTimeThem = DateTime.Parse(data.Substring(startIndex, endIndex - startIndex - 1));

                    wagon.siding = "";
                    wagon.train = "";

                    break;

                case "Waybilled":
                    wagon.action = Action.waybilled;

                    startIndex = data.IndexOf(" - ");
                    wagon.wagon = data.Substring(0, startIndex);

                    endIndex = data.IndexOf(" - ", startIndex + 1);
                    wagon.dateTimeMe = DateTime.Parse(data.Substring(startIndex + 3, endIndex - startIndex - 3));

                    startIndex = data.IndexOf(" at ", endIndex);
                    endIndex = data.IndexOf("at", startIndex + 2);
                    wagon.location = data.Substring(startIndex + 4, endIndex - startIndex - 5);

                    startIndex = endIndex + 3;
                    endIndex = data.IndexOf("}");
                    wagon.dateTimeThem = DateTime.Parse(data.Substring(startIndex, endIndex - startIndex - 1));

                    wagon.siding = "";
                    wagon.train = "";

                    break;

                case "Pulled":
                    wagon.action = Action.pulled;

                    startIndex = data.IndexOf(" - ");
                    wagon.wagon = data.Substring(0, startIndex);

                    endIndex = data.IndexOf(" - ", startIndex + 1);
                    wagon.dateTimeMe = DateTime.Parse(data.Substring(startIndex + 3, endIndex - startIndex - 3));

                    startIndex = data.IndexOf("from", endIndex);
                    endIndex = data.IndexOf("at", startIndex);
                    wagon.location = data.Substring(startIndex + 5, endIndex - startIndex - 6);

                    startIndex = endIndex + 3;
                    endIndex = data.IndexOf("}");
                    wagon.dateTimeThem = DateTime.Parse(data.Substring(startIndex, endIndex - startIndex - 1));

                    wagon.siding = "";
                    wagon.train = "";

                    break;

                case "On":

                    if (data.IndexOf("On train") != -1)
                    {
                        wagon.action = Action.onTrain;

                        startIndex = data.IndexOf(" - ");
                        wagon.wagon = data.Substring(0, startIndex);

                        endIndex = data.IndexOf(" - ", startIndex + 1);
                        wagon.dateTimeMe = DateTime.Parse(data.Substring(startIndex + 3, endIndex - startIndex - 3));

                        startIndex = data.IndexOf("train", endIndex);
                        endIndex = data.IndexOf("due", startIndex);
                        wagon.train = data.Substring(startIndex + 6, endIndex - startIndex - 7);

                        startIndex = endIndex + 7;
                        endIndex = data.IndexOf("on ", startIndex);
                        wagon.location = data.Substring(startIndex, endIndex - startIndex - 1).Trim(' ');

                        startIndex = endIndex + 3;
                        endIndex = data.IndexOf("}");
                        wagon.dateTimeThem = DateTime.Parse(data.Substring(startIndex, endIndex - startIndex - 1));

                        wagon.siding = "";
                    }
                    else
                    {
                        // "On rail at [LOCATION] at [TIME]
                        wagon.action = Action.onRail;

                        startIndex = data.IndexOf(" - ");
                        wagon.wagon = data.Substring(0, startIndex);

                        endIndex = data.IndexOf(" - ", startIndex + 1);
                        wagon.dateTimeMe = DateTime.Parse(data.Substring(startIndex + 3, endIndex - startIndex - 3));

                        startIndex = data.IndexOf("at ", endIndex);
                        endIndex = data.IndexOf("at ", startIndex + 1);
                        wagon.location = data.Substring(startIndex + 3, endIndex - startIndex - 4);

                        startIndex = endIndex + 3;
                        endIndex = data.IndexOf("}");
                        wagon.dateTimeThem = DateTime.Parse(data.Substring(startIndex, endIndex - startIndex - 1));

                        wagon.siding = "";
                        wagon.train = "";
                    }

                    break;

                case "Departed":
                    wagon.action = Action.departed;

                    startIndex = data.IndexOf(" - ");
                    wagon.wagon = data.Substring(0, startIndex);

                    endIndex = data.IndexOf(" - ", startIndex + 1);
                    wagon.dateTimeMe = DateTime.Parse(data.Substring(startIndex + 3, endIndex - startIndex - 3));

                    startIndex = data.IndexOf("Departed", endIndex);
                    endIndex = data.IndexOf("at", startIndex);
                    wagon.location = data.Substring(startIndex + 9, endIndex - startIndex - 10);

                    startIndex = endIndex + 3;
                    endIndex = data.IndexOf("}");
                    wagon.dateTimeThem = DateTime.Parse(data.Substring(startIndex, endIndex - startIndex - 1));

                    wagon.siding = "";
                    wagon.train = "";

                    break;

                case "Off":
                    wagon.action = Action.offRail;

                    startIndex = data.IndexOf(" - ");
                    wagon.wagon = data.Substring(0, startIndex);

                    endIndex = data.IndexOf(" - ", startIndex + 1);
                    wagon.dateTimeMe = DateTime.Parse(data.Substring(startIndex + 3, endIndex - startIndex - 3));

                    startIndex = data.IndexOf("at ", endIndex);
                    endIndex = data.IndexOf("since", startIndex);
                    wagon.location = data.Substring(startIndex + 3, endIndex - startIndex - 4);

                    startIndex = endIndex + 6;
                    endIndex = data.IndexOf("}");
                    wagon.dateTimeThem = DateTime.Parse(data.Substring(startIndex, endIndex - startIndex - 1));

                    wagon.siding = "";
                    wagon.train = "";

                    break;

                /*case "Unable":
                    wagon.action = Action.unableToLocate;

                    startIndex = data.IndexOf(" - ");
                    wagon.wagon = data.Substring(0, startIndex);

                    endIndex = data.IndexOf(" - ", startIndex + 1);
                    wagon.dateTimeMe = DateTime.Parse(data.Substring(startIndex + 3, endIndex - startIndex - 3));

                    wagon.siding = "";
                    wagon.location = "";
                    wagon.train = "";
                    wagon.dateTimeThem = DateTime.MinValue;

                    break;*/

                default:
                    wagon.action = Action.unableToLocate;

                    startIndex = data.IndexOf(" - ");
                    wagon.wagon = data.Substring(0, startIndex);

                    endIndex = data.IndexOf(" - ", startIndex + 1);
                    wagon.dateTimeMe = DateTime.Parse(data.Substring(startIndex + 3, endIndex - startIndex - 3));

                    wagon.siding = "";
                    wagon.location = "";
                    wagon.train = "";
                    wagon.dateTimeThem = DateTime.MinValue;

                    break;
            }

            return wagon;
        }
    }
}
