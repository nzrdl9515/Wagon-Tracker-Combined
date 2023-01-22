using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Wagon_Tracker_Combined
{
    static class FindTrains
    {
        public static void Run(Data data, ref Screen screen)
        {
            Dictionary<string, List<Train>> trains = new Dictionary<string, List<Train>>();
            //List<Train> trains = new List<Train>();

            // Find the trains in the data by going through each wagon
            for (int i = 0; i < data.GetWagons().Count; i++)
            {
                Action lastAction = Action.notSet;
                string trainNum = null;

                List<WagonEntry> entries = data.GetUniqueEntries(i);

                for (int j = 0; j < entries.Count; j++)
                {
                    if (entries[j].action != lastAction)
                    {
                        if (lastAction == Action.onTrain && trainNum != null)
                        {
                            // Train has just arrived
                            trains[trainNum][trains[trainNum].Count - 1].Arrived(
                                entries[j].dateTimeMe,
                                entries[j].dateTimeThem,
                                entries[j - 1].dateTimeThem);

                            trainNum = null;
                        }
                        else if (entries[j].action == Action.onTrain && lastAction != Action.notSet)
                        {
                            // Train has just departed
                            trainNum = entries[j].train.Substring(0, entries[j].train.Length - 2);

                            if (!trains.ContainsKey(trainNum))
                            {
                                trains.Add(trainNum, new List<Train>());
                            }

                            Train newTrain = new Train(
                                entries[j].train,
                                entries[j - 1].location,
                                entries[j].location,
                                entries[j].dateTimeMe,
                                entries[j].dateTimeThem);

                            bool trainAlreadyExists = false;

                            for (int k = 0; k < trains[trainNum].Count; k++)
                            {
                                if (trains[trainNum][k] == newTrain)
                                {
                                    trainAlreadyExists = true;
                                    trainNum = null;
                                    break;
                                }
                            }

                            if (!trainAlreadyExists)
                            {
                                trains[trainNum].Add(newTrain);
                            }
                        }

                        lastAction = entries[j].action;
                    }

                    // Get all the changes in the predicted time of arrival
                    /*if (trainDeparted && lastAction == Action.onTrain && entries[j].action == Action.onTrain && trains.Last().predictedArrivals.Last() != entries[j].dateTimeThem)
                    {
                        Train updateTrain = trains.Last();
                        updateTrain.predictedArrivals.Add(entries[j].dateTimeThem);

                        trains[trains.Count - 1] = updateTrain;
                    }*/
                }
            }

            foreach(string train in trains.Keys)
            {
                trains[train].Sort((a, b) => a.DateTimeDeparted.CompareTo(b.DateTimeDeparted));
            }

            int option = 0;
            Textbox optionBox = new Textbox(50, 5, 5, 3);
            Textbox mainBox = new Textbox(screen.Width - 8, screen.Height - 5, 5, 3);
            ConsoleKeyInfo key;
            int trainIndex = 0;
            int scrollPosition = 0;
            List<string> boxOutput = new List<string>();
            string[] trainIDs = trains.Keys.ToArray();
            Array.Sort(trainIDs);

            while (option != -1)
            {
                screen.Clear();
                screen.Update("Choose function".ToCharArray(), 3, 1);
                option = Program.selectFromList(ref screen, ref optionBox, new List<string> { "Browse trains", "Search by location" }, 0);

                /*if(option == -1)
                {
                    break;
                }*/

                switch (option)
                {
                    case 0:
                        // Browse trains
                        screen.Update((trainIDs[trainIndex] + "                              ").ToCharArray(), 3, 1);

                        for (int i = 0; i < mainBox.Height; i++)
                        {
                            if (i + scrollPosition == trains[trainIDs[trainIndex]].Count)
                            {
                                break;
                            }

                            if (trains[trainIDs[trainIndex]][i].DateTimeArrivedMe == DateTime.MinValue)
                            {
                                // Then the train has not arrived yet
                                boxOutput.Add(string.Format("From {0} to {1} scheduled to arrive at {2}",
                                    trains[trainIDs[trainIndex]][i].Origination.PadRight(20),
                                    trains[trainIDs[trainIndex]][i].Destination.PadRight(20),
                                    trains[trainIDs[trainIndex]][i].DateTimeFirstPredictedArrival));
                            }
                            else
                            {
                                boxOutput.Add(string.Format("From {0} to {1} arrived {2} trip length {3}",
                                    trains[trainIDs[trainIndex]][i].Origination.PadRight(20),
                                    trains[trainIDs[trainIndex]][i].Destination.PadRight(20),
                                    trains[trainIDs[trainIndex]][i].DateTimeArrivedMe.ToString("g").PadRight(20),
                                    trains[trainIDs[trainIndex]][i].DateTimeArrivedMe.Subtract(trains[trainIDs[trainIndex]][i].DateTimeDeparted).TotalMinutes));
                            }
                        }

                        mainBox.UpdateData(boxOutput);
                        mainBox.PrintData(ref screen, true);
                        while ((key = Console.ReadKey(true)).Key != ConsoleKey.Escape)
                        {
                            switch (key.Key)
                            {
                                case ConsoleKey.RightArrow:

                                    if (trainIndex == trainIDs.Length - 1)
                                    {
                                        trainIndex = 0;
                                    }
                                    else
                                    {
                                        trainIndex++;
                                    }
                                    scrollPosition = 0;
                                    break;

                                case ConsoleKey.LeftArrow:

                                    if (trainIndex == 0)
                                    {
                                        trainIndex = trainIDs.Length - 1;
                                    }
                                    else
                                    {
                                        trainIndex--;
                                    }
                                    scrollPosition = 0;
                                    break;

                                case ConsoleKey.UpArrow:

                                    if (scrollPosition > 0)
                                    {
                                        scrollPosition -= 5;
                                    }
                                    break;

                                case ConsoleKey.DownArrow:

                                    if (scrollPosition + mainBox.Height < trains[trainIDs[trainIndex]].Count)
                                    {
                                        scrollPosition += 5;
                                    }

                                    break;

                                case ConsoleKey.Enter:

                                    /*updateData = true;

                                    if (showUniqueByAction)
                                    {
                                        showUniqueByAction = false;
                                    }
                                    else
                                    {
                                        showUniqueByAction = true;
                                    }
                                    scrollPosition = 0;*/
                                    break;

                                case ConsoleKey.D:

                                    if (key.Modifiers == ConsoleModifiers.Control)
                                    {
                                        Textbox selectBox = new Textbox(10, 30, 5, 3);
                                        screen.Clear();

                                        screen.Update("Select train".ToCharArray(), 3, 1);
                                        int newTrainIndex = Program.selectFromList(ref screen, ref selectBox, new List<string>(trainIDs), 0);
                                        if (newTrainIndex != -1)
                                        {
                                            trainIndex = newTrainIndex;
                                        }
                                        screen.Clear();
                                    }

                                    break;

                                default:

                                    break;
                            }

                            screen.Update((trainIDs[trainIndex] + "                              ").ToCharArray(), 3, 1);
                            boxOutput = new List<string>();

                            for (int i = 0; i < mainBox.Height; i++)
                            {
                                if (i + scrollPosition == trains[trainIDs[trainIndex]].Count)
                                {
                                    break;
                                }

                                if (trains[trainIDs[trainIndex]][i].DateTimeArrivedMe == DateTime.MinValue)
                                {
                                    // Then the train has not arrived yet
                                    boxOutput.Add(string.Format("From {0} to {1} scheduled to arrive at {2}",
                                        trains[trainIDs[trainIndex]][i].Origination.PadRight(20),
                                        trains[trainIDs[trainIndex]][i].Destination.PadRight(20),
                                        trains[trainIDs[trainIndex]][i].DateTimeFirstPredictedArrival));
                                }
                                else
                                {
                                    boxOutput.Add(string.Format("From {0} to {1} arrived {2} trip length {3}",
                                        trains[trainIDs[trainIndex]][i].Origination.PadRight(20),
                                        trains[trainIDs[trainIndex]][i].Destination.PadRight(20),
                                        trains[trainIDs[trainIndex]][i].DateTimeArrivedMe.ToString("g").PadRight(20),
                                        trains[trainIDs[trainIndex]][i].DateTimeArrivedMe.Subtract(trains[trainIDs[trainIndex]][i].DateTimeDeparted).TotalMinutes));
                                }
                            }

                            mainBox.UpdateData(boxOutput);
                            mainBox.PrintData(ref screen, true);
                        }
                        break;

                    case 1:

                        Textbox box = new Textbox(20, 5, 5, 3);
                        scrollPosition = 0;
                        string location = "";
                        string[] locationFile = File.ReadAllLines(Program.FilePath + "locations.txt");

                        while (Array.FindIndex(locationFile, s => s.Contains(location) && s.IndexOf(location) + location.Length == s.Length) == -1)
                        {
                            location = box.GetKeyboardInput("Input location to search", ref screen, true);
                            /*screen.Clear();
                            screen.Update("Input location to search".ToCharArray(), 3, 1);
                            box.PrintData(ref screen, true);
                            box.SetCursor();

                            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
                            {
                                box.KeyPressLoop(key, ref screen, true);

                                box.SetCursor();
                            }

                            foreach (char i in box.GetData())
                            {
                                location += i;
                            }*/
                        }

                        screen.Clear();
                        mainBox = new Textbox(screen.Width - 8, screen.Height - 6, 5, 4);
                        List<Train> selectedTrains = new List<Train>();

                        for(int i = 0; i < trainIDs.Length; i++)
                        {
                            //foreach(Train train in trains[trainIDs[i]])
                            for (int j = 0; j < trains[trainIDs[i]].Count; j++)
                            {

                                if(isLocationOnPath(ref locationFile, trains[trainIDs[i]][j].Origination, trains[trainIDs[i]][j].Destination, location))
                                {
                                    /*if (train.DateTimeArrivedMe == DateTime.MinValue)
                                    {
                                        // Then the train has not arrived yet
                                        boxOutput.Add(string.Format("From {0} to {1} scheduled to arrive at {2}",
                                            train.Origination.PadRight(20),
                                            train.Destination.PadRight(20),
                                            train.DateTimeFirstPredictedArrival));
                                    }
                                    else
                                    {
                                        boxOutput.Add(string.Format("From {0} to {1} arrived {2} trip length {3}",
                                            train.Origination.PadRight(20),
                                            train.Destination.PadRight(20),
                                            train.DateTimeArrivedMe.ToString("g").PadRight(20),
                                            train.DateTimeArrivedMe.Subtract(train.DateTimeDeparted).TotalMinutes));
                                    }*/

                                    selectedTrains.Add(trains[trainIDs[i]][j]);
                                }
                            }
                        }

                        selectedTrains.Sort((a, b) => a.DateTimeDeparted.CompareTo(b.DateTimeDeparted));

                        boxOutput = new List<string>();

                        for (int i = 0; i < mainBox.Height; i++)
                        {
                            if (i + scrollPosition == selectedTrains.Count)
                            {
                                break;
                            }

                            if (selectedTrains[i].DateTimeArrivedMe == DateTime.MinValue)
                            {
                                // Then the train has not arrived yet
                                boxOutput.Add(string.Format("Train {0} departed {1} from {2} scheduled to arrive in {3} at {4}",
                                    selectedTrains[i].TrainNum.PadRight(8),
                                    selectedTrains[i].DateTimeDeparted.ToString("g").PadRight(20),
                                    selectedTrains[i].Origination.PadRight(20),
                                    selectedTrains[i].Destination.PadRight(20),
                                    selectedTrains[i].DateTimeFirstPredictedArrival));
                            }
                            else
                            {
                                boxOutput.Add(string.Format("Train {0} departed {1} from {2} arrived in {3} at {4}",
                                    selectedTrains[i].TrainNum.PadRight(8),
                                    selectedTrains[i].DateTimeDeparted.ToString("g").PadRight(20),
                                    selectedTrains[i].Origination.PadRight(20),
                                    selectedTrains[i].Destination.PadRight(32),
                                    selectedTrains[i].DateTimeArrivedMe.ToString("g").PadRight(20)));
                            }
                        }

                        mainBox.UpdateData(boxOutput);
                        mainBox.PrintData(ref screen, true);

                        while ((key = Console.ReadKey(true)).Key != ConsoleKey.Escape)
                        {
                            switch (key.Key)
                            {
                                case ConsoleKey.RightArrow:

                                    /*if (trainIndex == trainIDs.Length - 1)
                                    {
                                        trainIndex = 0;
                                    }
                                    else
                                    {
                                        trainIndex++;
                                    }
                                    scrollPosition = 0;*/
                                    break;

                                case ConsoleKey.LeftArrow:

                                    /*if (trainIndex == 0)
                                    {
                                        trainIndex = trainIDs.Length - 1;
                                    }
                                    else
                                    {
                                        trainIndex--;
                                    }
                                    scrollPosition = 0;*/
                                    break;

                                case ConsoleKey.UpArrow:

                                    if (scrollPosition > 0)
                                    {
                                        scrollPosition -= 5;
                                    }
                                    break;

                                case ConsoleKey.DownArrow:

                                    if (scrollPosition + mainBox.Height < selectedTrains.Count)
                                    {
                                        scrollPosition += 5;
                                    }

                                    break;

                                case ConsoleKey.Enter:

                                    /*updateData = true;

                                    if (showUniqueByAction)
                                    {
                                        showUniqueByAction = false;
                                    }
                                    else
                                    {
                                        showUniqueByAction = true;
                                    }
                                    scrollPosition = 0;*/
                                    break;

                                case ConsoleKey.D:

                                    /*if (key.Modifiers == ConsoleModifiers.Control)
                                    {
                                        Textbox selectBox = new Textbox(10, 30, 5, 3);
                                        screen.Clear();

                                        screen.Update("Select train".ToCharArray(), 3, 1);
                                        int newTrainIndex = Program.selectFromList(ref screen, ref selectBox, new List<string>(trainIDs), 0);
                                        if (newTrainIndex != -1)
                                        {
                                            trainIndex = newTrainIndex;
                                        }
                                        screen.Clear();
                                    }*/

                                    break;

                                default:

                                    break;
                            }

                            boxOutput = new List<string>();

                            for (int i = 0; i < mainBox.Height; i++)
                            {
                                if (i + scrollPosition == selectedTrains.Count)
                                {
                                    break;
                                }

                                if (selectedTrains[i + scrollPosition].DateTimeArrivedMe == DateTime.MinValue)
                                {
                                    // Then the train has not arrived yet
                                    boxOutput.Add(string.Format("Train {0} departed {1} from {2} scheduled to arrive in {3} at {4}",
                                        selectedTrains[i + scrollPosition].TrainNum.PadRight(8),
                                        selectedTrains[i + scrollPosition].DateTimeDeparted.ToString("g").PadRight(20),
                                        selectedTrains[i + scrollPosition].Origination.PadRight(20),
                                        selectedTrains[i + scrollPosition].Destination.PadRight(20),
                                        selectedTrains[i + scrollPosition].DateTimeFirstPredictedArrival));
                                }
                                else
                                {
                                    boxOutput.Add(string.Format("Train {0} departed {1} from {2} arrived in {3} at {4}",
                                        selectedTrains[i + scrollPosition].TrainNum.PadRight(8),
                                        selectedTrains[i + scrollPosition].DateTimeDeparted.ToString("g").PadRight(20),
                                        selectedTrains[i + scrollPosition].Origination.PadRight(20),
                                        selectedTrains[i + scrollPosition].Destination.PadRight(32),
                                        selectedTrains[i + scrollPosition].DateTimeArrivedMe.ToString("g").PadRight(20)));
                                }
                            }

                            mainBox.UpdateData(boxOutput);
                            mainBox.PrintData(ref screen, true);
                        }

                        break;

                    case 2:

                        break;
                }
            }

            // Calculate some stats and write the trains to the screen
            /*using (StreamWriter sw = new StreamWriter("stats 21-5.txt"))
            {
                sw.WriteLine("train,trainID,origination,destination,firstPredDuration,lastPredDuration,durationMe,durationThem");

                int c = 1;
                List<string> trainNums = new List<string>(trains.Keys);

                trainNums = trainNums.OrderBy(name => name).ToList();

                for (int i = 0; i < trainNums.Count; i++)
                {
                    Console.WriteLine("\nTrain " + trainNums[i]);
                    for (int j = 0; j < trains[trainNums[i]].Count; j++)
                    {

                        if (trains[trainNums[i]][j].DateTimeArrivedMe == DateTime.MinValue)
                        {
                            // Then the train has not arrived yet
                            Console.WriteLine("From {0} to {1} scheduled to arrive at {2}", trains[trainNums[i]][j].Origination.PadRight(20), trains[trainNums[i]][j].Destination.PadRight(20), trains[trainNums[i]][j].DateTimeFirstPredictedArrival);
                        }
                        else
                        {
                            Console.WriteLine("From {0} to {1} arrived {2} trip length {3}",
                                trains[trainNums[i]][j].Origination.PadRight(20),
                                trains[trainNums[i]][j].Destination.PadRight(20),
                                trains[trainNums[i]][j].DateTimeArrivedMe.ToString("g").PadRight(20),
                                trains[trainNums[i]][j].DateTimeArrivedMe.Subtract(trains[trainNums[i]][j].DateTimeDeparted).TotalMinutes);

                            /*for (int j = 0; j < trains[i].predictedArrivals.Count; j++)
                            {
                                //Console.WriteLine(trains[i].predictedArrivals[j]);
                            }

                            sw.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}",
                                trainNums[i].Substring(0, trainNums[i].Length - 2),
                                c,
                                trains[trainNums[i]][j].Origination,
                                trains[trainNums[i]][j].Destination,
                                trains[trainNums[i]][j].DateTimeFirstPredictedArrival.Subtract(trains[trainNums[i]][j].DateTimeDeparted).TotalMinutes,
                                trains[trainNums[i]][j].DateTimeLastPredictedArrival.Subtract(trains[trainNums[i]][j].DateTimeDeparted).TotalMinutes,
                                trains[trainNums[i]][j].DateTimeArrivedMe.Subtract(trains[trainNums[i]][j].DateTimeDeparted).TotalMinutes,
                                trains[trainNums[i]][j].DateTimeArrivedThem.Subtract(trains[trainNums[i]][j].DateTimeDeparted).TotalMinutes);
                            c++;
                        }
                    }

                }
            }*/
        }

        private static bool isLocationOnPath(ref string[] locationFile, string origination, string destination, string queryLocation)
        {
            int index = Array.FindIndex(locationFile, s => s.Contains(origination) && s.IndexOf(origination) + origination.Length == s.Length);
            int branchIndex = 0;

            while (locationFile[index][branchIndex] == '-')
            {
                branchIndex++;
            }

            Dictionary<string, string> directions = searchUpAndDown(index, branchIndex, ref locationFile, destination);

            List<string> locationsVisited = new List<string>();
            bool up = false;
            branchIndex++;

            /*foreach(string direction in directions.Keys)
            {
                Console.WriteLine("{0} - {1}", direction, directions[direction]);
            }*/

            while (directions[locationFile[index]] != "end")
            {
                locationsVisited.Add(RemoveInitialChars(locationFile[index]));

                switch (directions[locationFile[index]])
                {
                    case "up":
                        up = true;
                        index--;
                        branchIndex--;
                        break;

                    case "down":
                        up = false;
                        branchIndex--;
                        index++;
                        while (locationFile[index][branchIndex] == '-')
                        {
                            index++;
                        }
                        break;

                    case "branch":
                        up = false;
                        branchIndex++;
                        index++;
                        break;
                }

                while (!directions.ContainsKey(locationFile[index]))
                {
                    if (locationFile[index][branchIndex] != '-')
                    {
                        locationsVisited.Add(RemoveInitialChars(locationFile[index]));
                    }

                    if (up)
                    {
                        index--;

                        while (locationFile[index][branchIndex] == '-')
                        {
                            index--;
                        }
                    }
                    else
                    {
                        index++;

                        while (locationFile[index][branchIndex] == '-')
                        {
                            index++;
                        }
                    }
                }
            }

            locationsVisited.Add(RemoveInitialChars(locationFile[index]));

            if (locationsVisited.Contains(queryLocation))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static string RemoveInitialChars(string input)
        {
            int index = 0;

            while (input[index] == '-' || input[index] == '*')
            {
                index++;
            }

            return input.Substring(index);
        }

        private static Dictionary<string, string> searchUpAndDown(int locationIndex, int branchIndex, ref string[] locationFile, string destination)
        {
            Dictionary<string, string> directions = new Dictionary<string, string>();

            _searchUpAndDown(locationIndex, branchIndex, ref locationFile, destination, ref directions, true);

            return directions;
        }

        private static void _searchUpAndDown(int locationIndex, int branchIndex, ref string[] locationFile, string destination, ref Dictionary<string, string> directions, bool includeStartIndex)
        {
            bool up = false;
            int _locationIndex = locationIndex;
            directions.Add(locationFile[locationIndex], "down");

            if (!includeStartIndex)
            {
                _locationIndex++;
            }

            while (true)
            {
                switch (locationFile[_locationIndex][branchIndex])
                {
                    case '*':
                        // Check just in case we've actually found the destination
                        if (locationFile[_locationIndex].Contains(destination) && locationFile[_locationIndex].IndexOf(destination) + destination.Length == locationFile[_locationIndex].Length)
                        {
                            directions.Add(locationFile[_locationIndex], "end");
                            return;
                        }

                        // Line branches, so search along the branch
                        int nextSearch = searchAlongBranch(_locationIndex + 1, branchIndex + 1, ref locationFile, destination, ref directions);

                        switch (nextSearch)
                        {
                            case -1:
                                // The branch ended, so go back along the normal section
                                if (up)
                                {
                                    _locationIndex--;
                                }
                                else
                                {
                                    _locationIndex++;
                                    while (locationFile[_locationIndex][branchIndex] == '-')
                                    {
                                        _locationIndex++;
                                    }
                                }

                                break;

                            case -2:
                                // The file ended
                                // If we started by going down, swap and go up
                                if (!up)
                                {
                                    up = true;
                                    _locationIndex = locationIndex - 1;
                                }
                                break;

                            default:
                                // Destination has been found
                                if (directions.ContainsKey(locationFile[_locationIndex]))
                                {
                                    directions[locationFile[_locationIndex]] = "branch";
                                }
                                else
                                {
                                    directions.Add(locationFile[_locationIndex], "branch");
                                }
                                return;
                        }

                        break;

                    case '-':
                        // Location is on a branch, so skip until the branch ends
                        while (locationFile[_locationIndex][branchIndex] == '-')
                        {
                            if (up)
                            {
                                _locationIndex--;
                            }
                            else
                            {
                                _locationIndex++;
                            }
                        }
                        break;

                    default:
                        // Is a normal location
                        // Needs to check if it's the end of the file, or the end of a branch

                        if (branchIndex > 0)
                        {
                            if (up && locationFile[_locationIndex][branchIndex - 1] == '*')
                            {
                                // Exiting a branch
                                _searchUpAndDown(_locationIndex, branchIndex - 1, ref locationFile, destination, ref directions, false);
                                return;
                            }
                            else if (!up && locationFile[_locationIndex][branchIndex - 1] != '-')
                            {
                                // Branch has ended
                                directions[locationFile[locationIndex]] = "up";
                                up = true;
                                _locationIndex = locationIndex;
                            }
                        }
                        else if (!up && _locationIndex == locationFile.Length - 1)
                        {
                            if (locationFile[_locationIndex].Contains(destination) && locationFile[_locationIndex].IndexOf(destination) + destination.Length == locationFile[_locationIndex].Length)
                            {
                                if (directions.ContainsKey(locationFile[_locationIndex]))
                                {
                                    directions[locationFile[_locationIndex]] = "end";
                                }
                                else
                                {
                                    directions.Add(locationFile[_locationIndex], "end");
                                }
                                return;
                            }

                            // File has ended
                            directions[locationFile[locationIndex]] = "up";
                            up = true;
                            _locationIndex = locationIndex;
                        }

                        if (locationFile[_locationIndex].Contains(destination) && locationFile[_locationIndex].IndexOf(destination) + destination.Length == locationFile[_locationIndex].Length)
                        {
                            if (directions.ContainsKey(locationFile[_locationIndex]))
                            {
                                directions[locationFile[_locationIndex]] = "end";
                            }
                            else
                            {
                                directions.Add(locationFile[_locationIndex], "end");
                            }
                            return;
                        }

                        // Keep looking
                        if (up)
                        {
                            _locationIndex--;
                        }
                        else
                        {
                            _locationIndex++;
                        }
                        break;
                }

            }
        }

        private static int searchAlongBranch(int locationIndex, int branchIndex, ref string[] locationFile, string destination, ref Dictionary<string, string> directions)
        {
            while (true)
            {
                switch (locationFile[locationIndex][branchIndex])
                {
                    case '*':
                        if (locationFile[locationIndex].Contains(destination) && locationFile[locationIndex].IndexOf(destination) + destination.Length == locationFile[locationIndex].Length)
                        {
                            directions.Add(locationFile[locationIndex], "end");
                            return locationIndex;
                        }

                        // Line branches, so search along the branch
                        int nextSearch = searchAlongBranch(locationIndex + 1, branchIndex + 1, ref locationFile, destination, ref directions);

                        switch (nextSearch)
                        {
                            case -1:
                                // The branch ended, so go back along the normal section
                                locationIndex++;
                                while (locationFile[locationIndex][branchIndex] == '-')
                                {
                                    locationIndex++;
                                }

                                break;

                            default:
                                // Destination has been found
                                directions.Add(locationFile[locationIndex], "branch");
                                return nextSearch;
                        }

                        break;

                    case '-':
                        // Location is on a branch, so skip until the branch ends
                        while (locationFile[locationIndex][branchIndex] == '-')
                        {
                            locationIndex++;
                        }
                        break;

                    default:
                        // Is a normal location
                        // Needs to check if it's the end of the file, or the end of a branch

                        if (branchIndex > 0)
                        {
                            if (locationFile[locationIndex][branchIndex - 1] != '-' && locationFile[locationIndex][branchIndex - 1] != '*')
                            {
                                // Branch has ended
                                return -1;
                            }
                        }

                        if (locationFile[locationIndex].Contains(destination) && locationFile[locationIndex].IndexOf(destination) + destination.Length == locationFile[locationIndex].Length)
                        {
                            directions.Add(locationFile[locationIndex], "end");
                            return locationIndex;
                        }

                        // Keep looking
                        locationIndex++;
                        break;
                }

            }
        }
    }
}
