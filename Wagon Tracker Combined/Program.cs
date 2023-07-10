﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.Collections;

namespace Wagon_Tracker_Combined
{
    enum Action
    {
        inRailSiding,
        arrived,
        waybilled,
        pulled,
        onTrain,
        offRail,
        onRail,
        departed,
        unableToLocate,
        notSet
        // Add 500 internal server error and off rail
    }

    class Instruction
    {
        public string Command;
        public DateTime Time;

        public Instruction(string command)
        {
            Command = command;
            Time = DateTime.Now;
        }
    }

    static class Program
    {
        public static List<Instruction> DownloadInstructions;
        public static bool FileLocked;
        public static bool FileInUse;
        //public const string FilePath = "C:/Users/johnv/OneDrive/Documents/My Stuff/Wagon Tracker Combined/Wagon Tracker Combined/bin/Debug/net5.0/"; //     Surface
        //public const string FilePath = "C:/Users/John/source/repos/nzrdl9515/Wagon-Tracker-Combined/Wagon Tracker Combined/bin/Debug/net5.0/"; //            Desktop
        public const string FilePath = "";
        public const int NumClients = 10;

        static void Main(string[] args)
        {
            Loop();
        }

        private static void Loop()
        {
            FileLocked = false;
            FileInUse = false;
            Screen screen = new Screen(200, 50);
            Textbox box = new Textbox(50, 6, 5, 3);
            DownloadInstructions = new List<Instruction>();
            DownloadInstructions.Add(new Instruction("start"));
            Searches searches = new Searches();

            List<string> options = new List<string>(new string[] {
                "Search for new wagons",
                "Search manually for known wagons",
                "Perform saved search for known wagons",
                "Scroll through downloaded data",
                "Find trains in downloaded data",
                "View wagons in continuous download list"});

            screen.Update("Choose function".ToCharArray(), 3, 1);

            int option = 0;

            // Use this if the task needs to be awaited (but I don't think it does)
            // Task downloadTask = downloadConstantly();
            _ = downloadConstantly();

            while (option != -1)
            {
                screen.Clear();
                screen.Update("Choose function".ToCharArray(), 3, 1);
                option = selectFromList(ref screen, ref box, options, 0, new List<ConsoleKey>() { ConsoleKey.Escape });


                switch (option)
                {
                    case 0:

                        Textbox searchBox = new Textbox(20, 5, 5, 3);

                        string[] searchWagons = searchBox.GetKeyboardInput("Input wagons to download", ref screen, true).Replace(" ", "").Split(',');

                        FindWagons.Begin(searchWagons, ref screen);

                        break;

                    case 1:

                        DownloadWagons.RunManual(ref screen, ref searches);

                        break;

                    case 2:

                        DownloadWagons.RunSaved(ref screen, ref searches);

                        break;

                    case 3:

                        screen.Clear();
                        ScrollWagons.Run(new Data(FilePath + "data.txt"), ref screen);

                        break;

                    case 4:

                        screen.Clear();
                        FindTrains.Run(new Data(FilePath + "data.txt"), ref screen);

                        break;

                    case 5:
                        // View wagons in download list
                        screen.Clear();
                        screen.Update("Downloading latest information".ToCharArray(), 3, 1);

                        //List<string> contWagons = new List<string>(File.ReadAllLines(FilePath + "continuous_download_wagons.txt"));

                        SortedList<string, string> contWagonsData = new SortedList<string, string>(new StringLogicalComparer());

                        DownloadWagons.Download(new List<string>(File.ReadAllLines(FilePath + "continuous_download_wagons.txt")), ref contWagonsData, ref screen);
                        screen.Clear();

                        List<string> boxOutput = new List<string>();
                        ConsoleKeyInfo key;
                        Textbox displayBox = new Textbox(screen.Width - 8, screen.Height - 6, 5, 4);
                        int scrollPosition = 0;

                        for (int i = 0; i < displayBox.Height; i++)
                        {
                            if (i + scrollPosition == contWagonsData.Count)
                            {
                                break;
                            }

                            boxOutput.Add(contWagonsData.Keys[i + scrollPosition] + " - " + contWagonsData.Values[i + scrollPosition]);

                        }

                        screen.Update(("Continuous download wagons").ToCharArray(), 3, 1);
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

                                    if (scrollPosition + displayBox.Height < contWagonsData.Count)
                                    {
                                        scrollPosition += 5;
                                    }

                                    break;

                                case ConsoleKey.Enter:

                                    break;

                                case ConsoleKey.A:
                                    if (key.Modifiers == ConsoleModifiers.Control)
                                    {
                                        string[] wagonsToAdd = new Textbox(20, 5, 5, 3).GetKeyboardInput("Input wagons to add to continuous search", ref screen, true).Replace(" ", "").Split(',');

                                        foreach(string wagon in wagonsToAdd)
                                        {
                                            string wagonClass = wagon.Substring(0, wagon.IndexOfAny("0123456789".ToCharArray()));

                                            if (File.ReadAllLines(FilePath + "wagons/" + wagonClass + ".txt").Contains(wagon))
                                            {
                                                DownloadInstructions.Add(new Instruction("addwagon " + wagon));
                                            }
                                        }

                                        DownloadWagons.Download(new List<string>(wagonsToAdd), ref contWagonsData);

                                        screen.Clear();
                                    }
                                    break;

                                case ConsoleKey.R:
                                    if (key.Modifiers == ConsoleModifiers.Control)
                                    {
                                        // SelectMultipleFromList method to select which wagons out of contWagons to remove
                                        // Then re-write the text file
                                        // Then return to the view contWagons screen

                                        Textbox rightBox = new Textbox(10, 30, 20, 3);
                                        Textbox leftBox = new Textbox(10, 30, 5, 3);

                                        screen.Clear();
                                        screen.Update("Select wagons to remove from continuous download".ToCharArray(), 3, 1);
                                        List<string> wagonsToRemove = selectMultipleFromList(ref screen, ref leftBox, ref rightBox, new List<string>(contWagonsData.Keys));

                                        if (wagonsToRemove.Count != 0 && wagonsToRemove[0] != "escape")
                                        {
                                            screen.Clear();

                                            foreach(string wagon in wagonsToRemove)
                                            {
                                                contWagonsData.Remove(wagon);

                                                DownloadInstructions.Add(new Instruction("removewagon " + wagon));
                                            }
                                        }

                                        scrollPosition = 0;
                                    }

                                    break;

                                default:

                                    break;
                            }

                            boxOutput = new List<string>();

                            for (int i = 0; i < displayBox.Height; i++)
                            {
                                if (i + scrollPosition == contWagonsData.Count)
                                {
                                    break;
                                }

                                boxOutput.Add(contWagonsData.Keys[i + scrollPosition] + " - " + contWagonsData.Values[i + scrollPosition]);

                            }

                            displayBox.UpdateData(boxOutput);
                            displayBox.PrintData(ref screen, true);
                        }

                        break;

                    // This function is now included in other places
                    /*case 5:

                        // Add new wagons to download list

                        break;*/

                    case -1:
                        // Escape

                        // **************************************************************
                        // ***** Worth double checking whether intentional to close *****
                        // **************************************************************

                        break;

                    case -2:
                        // Ctrl + N
                        break;

                    default:
                        Console.Clear();
                        Console.WriteLine("Option chosen: {0}", option);
                        DownloadInstructions.Add(new Instruction(option.ToString()));
                        System.Threading.Thread.Sleep(2000);
                        break;
                }
            }
        }

        static async Task downloadConstantly()
        {
            Instruction lastInstruction = DownloadInstructions[0];
            DateTime last = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute - (DateTime.Now.Minute % 5), 0, 0);
            SortedList<string, string> data = new SortedList<string, string>(new StringLogicalComparer());

            /*using(StreamReader sw = new StreamReader(FilePath + "continuous_download_wagons.txt"))
            {
                string line;

                while((line = sw.ReadLine()) != null)
                {
                    data.Add(line, "");
                }
            }*/

            string[] contWagons = File.ReadAllLines(FilePath + "continuous_download_wagons.txt");
            //int numWagons = contWagons.Length;

            for(int i = 0; i < contWagons.Length; i++)
            {
                /*if (i == 87)
                {
                    break;
                }*/

                data.Add(contWagons[i], "");
            }

            await Task.Run(() =>
            {
                while (true)
                {
                    if (DownloadInstructions.Last() != lastInstruction)
                    {
                        for (int i = DownloadInstructions.IndexOf(lastInstruction) + 1; i < DownloadInstructions.Count(); i++)
                        {
                            // ****************************************************
                            // *************** PERFORM INSTRUCTIONS ***************
                            // ****************************************************

                            int index = DownloadInstructions[i].Command.IndexOf(" ");

                            if(index == -1)
                            {
                                index = DownloadInstructions[i].Command.Length;
                            }

                            switch (DownloadInstructions[i].Command.Substring(0, index))
                            {
                                case "addwagon":

                                    string wagon = DownloadInstructions[i].Command.Substring(index + 1);

                                    if (!data.ContainsKey(wagon))
                                    {
                                        data.Add(wagon, "");
                                    }

                                    File.WriteAllLines(FilePath + "continuous_download_wagons.txt", data.Keys);

                                    break;

                                case "removewagon":

                                    wagon = DownloadInstructions[i].Command.Substring(index + 1);

                                    if (data.ContainsKey(wagon))
                                    {
                                        data.Remove(wagon);
                                    }

                                    File.WriteAllLines(FilePath + "continuous_download_wagons.txt", data.Keys);

                                    break;

                                /*case "viewwagons":

                                    string wagonList = data.Keys[0];

                                    for(int j = 1; j < data.Count; j++)
                                    {
                                        wagonList += (',' + data.Keys[j]);
                                    }

                                    DownloadInstructions.Add(new Instruction(wagonList));

                                    break;*/

                                default:

                                    break;
                            }
                        }

                        lastInstruction = DownloadInstructions.Last();
                    }

                    if (DateTime.Now > last.AddMinutes(5.0d))
                    {
                        last = last.AddMinutes(5);
                        //allData = new List<string>();

                        SortedList<string, string> newData = new SortedList<string, string>(new StringLogicalComparer());

                        DownloadWagons.Download(new List<string>(data.Keys), ref newData);

                        List<string> dataToAppend = new List<string>();

                        for(int i = 0; i < newData.Count; i++)
                        {
                            if(newData[newData.Keys[i]] != data[newData.Keys[i]])
                            {
                                data[newData.Keys[i]] = newData[newData.Keys[i]];

                                dataToAppend.Add(newData.Keys[i] + " - " + DateTime.Now.ToString("g") + " - " + newData[newData.Keys[i]]);
                            }
                        }

                        /*for (int i = 0; i < wagons.Count; i++)
                        {
                            string downloadString;

                            try
                            {
                                downloadString = client.DownloadString("https://www.kiwirailfreight.co.nz/tc/api/location/current/" + wagons[i]);
                            }
                            catch (Exception e)
                            {
                                downloadString = e.Message;
                            }

                            string fileLine = wagons[i] + " - " + DateTime.Now.ToString("g") + " - " + downloadString;

                            if (downloadString != lastData[wagons[i]])
                            {
                                lastData[wagons[i]] = downloadString;
                                
                                allData.Add(fileLine);
                            }
                        }*/

                        // To save the file
                        if (!FileLocked)
                        {
                            FileInUse = true;

                            File.AppendAllLines("data.txt", dataToAppend);

                            FileInUse = false;
                        }
                        else
                        {
                            while (FileLocked)
                            {
                                System.Threading.Thread.Sleep(500);
                            }

                            FileInUse = true;

                            File.AppendAllLines("data.txt", dataToAppend);

                            FileInUse = false;
                        }
                    }

                    // Remove instructions that are more than 10 minutes old, but making sure the instruction list is NOT empty
                    // ****************************************************************************** AddMinutes(10) ***********
                    if (DownloadInstructions.Count > 1 && DateTime.Now > DownloadInstructions[0].Time.AddMinutes(10)) // *******
                    {
                        for (int i = DownloadInstructions.Count - 2; i >=0 ; i--)
                        {
                            if (DateTime.Now > DownloadInstructions[i].Time.AddSeconds(5))
                            {
                                DownloadInstructions.RemoveAt(i);
                            }
                        }
                    }

                    System.Threading.Thread.Sleep(500);
                }
            });
        }

        public static List<string> selectMultipleFromList(ref Screen screen, ref Textbox leftBox, ref Textbox rightBox, List<string> options)
        {
            int leftScrollPosition = 0;
            int rightScrollPosition = 0;
            int leftPosition = 0;
            int rightPosition = 0;
            bool cursorInLeftBox = true;

            ConsoleKeyInfo key;

            List<string> leftBoxOutput = new List<string>();
            List<string> rightBoxOutput = new List<string>();
            List<int> availableOptions = new List<int>();
            List<int> selectedOptions = new List<int>();

            for(int i = 0; i < options.Count; i++)
            {
                availableOptions.Add(i);
            }

            // ********** Left Box **********
            // Only write the number of lines that fit inside the textbox
            if (options.Count < leftBox.Height)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    leftBoxOutput.Add("  " + options[i]);
                }
            }
            else
            {
                for (int i = 0; i < leftBox.Height; i++)
                {
                    leftBoxOutput.Add("  " + options[i + leftScrollPosition]);
                }
            }

            // ********** (Right box is empty) **********
            
            // Put the marker on the first entry
            leftBoxOutput[leftPosition] = "->" + leftBoxOutput[leftPosition].Substring(2);

            // Draw the initial state to the screen
            leftBox.UpdateData(leftBoxOutput);
            leftBox.PrintData(ref screen, true);

            rightBox.UpdateData(rightBoxOutput);
            rightBox.PrintData(ref screen, true);

            // Main loop
            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        // Get rid of previous ->
                        // determine whether to scroll
                        // if scrolling, update the whole list
                        // put -> back in the right position

                        if (cursorInLeftBox)
                        {
                            leftBoxOutput[leftPosition] = "  " + leftBoxOutput[leftPosition].Substring(2);

                            if (leftPosition == 0 && leftScrollPosition > 0)
                            {
                                // scroll up one space
                                leftScrollPosition--;
                            }
                            else if (leftPosition > 0)
                            {
                                // Move up one space
                                leftPosition--;
                            }
                        }
                        else
                        {
                            rightBoxOutput[rightPosition] = "  " + rightBoxOutput[rightPosition].Substring(2);

                            if (rightPosition == 0 && rightScrollPosition > 0)
                            {
                                // scroll up one space
                                rightScrollPosition--;
                            }
                            else if (rightPosition > 0)
                            {
                                // Move up one space
                                rightPosition--;
                            }
                        }
                        break;

                    case ConsoleKey.DownArrow:

                        if (cursorInLeftBox)
                        {
                            leftBoxOutput[leftPosition] = "  " + leftBoxOutput[leftPosition].Substring(2);

                            if (leftPosition >= leftBox.Height - 1 && leftScrollPosition < availableOptions.Count - leftBox.Height)
                            {
                                // Scroll down one space
                                leftScrollPosition++;
                            }
                            else if (leftPosition + leftScrollPosition < availableOptions.Count - 1)
                            {
                                // Move down one space
                                leftPosition++;
                            }
                        }
                        else
                        {
                            rightBoxOutput[rightPosition] = "  " + rightBoxOutput[rightPosition].Substring(2);

                            if (rightPosition >= rightBox.Height - 1 && rightScrollPosition < selectedOptions.Count - rightBox.Height)
                            {
                                // Scroll down one space
                                rightScrollPosition++;
                            }
                            else if (rightPosition + rightScrollPosition < selectedOptions.Count - 1)
                            {
                                // Move down one space
                                rightPosition++;
                            }
                        }
                        break;

                    case ConsoleKey.LeftArrow:

                        if(!cursorInLeftBox && selectedOptions.Count < options.Count)
                        {
                            cursorInLeftBox = true;
                            rightBoxOutput[rightPosition] = "  " + rightBoxOutput[rightPosition].Substring(2); 
                            leftPosition = 0;
                            rightScrollPosition = 0;
                        }

                        break;

                    case ConsoleKey.RightArrow:

                        if(cursorInLeftBox && selectedOptions.Count > 0)
                        {
                            cursorInLeftBox = false;
                            leftBoxOutput[leftPosition] = "  " + leftBoxOutput[leftPosition].Substring(2);
                            rightPosition = 0;
                            rightScrollPosition = 0;
                        }

                        break;

                    case ConsoleKey.Tab:

                        // Swap selected item to the opposite box
                        if (cursorInLeftBox)
                        {
                            int c = 0;
                            if (selectedOptions.Count == 0 || availableOptions[leftPosition + leftScrollPosition] > selectedOptions.Last())
                            {
                                selectedOptions.Add(availableOptions[leftPosition + leftScrollPosition]);
                            }
                            else
                            {
                                while (selectedOptions[c] < availableOptions[leftPosition + leftScrollPosition])
                                {
                                    c++;
                                }

                                selectedOptions.Insert(c, availableOptions[leftPosition + leftScrollPosition]);
                            }

                            availableOptions.RemoveAt(leftPosition + leftScrollPosition);

                            if(availableOptions.Count == 0)
                            {
                                cursorInLeftBox = false;
                            }

                            if(leftScrollPosition + leftBox.Height > availableOptions.Count && leftScrollPosition > 0)
                            {
                                leftScrollPosition--;
                            }
                            else if(leftPosition == availableOptions.Count && leftPosition > 0)
                            {
                                leftPosition--;
                            }
                        }
                        else
                        {
                            int c = 0;
                            if (availableOptions.Count == 0 || selectedOptions[rightPosition + rightScrollPosition] > availableOptions.Last())
                            {
                                availableOptions.Add(selectedOptions[rightPosition + rightScrollPosition]);
                            }
                            else
                            {
                                while (availableOptions[c] < selectedOptions[rightPosition + rightScrollPosition])
                                {
                                    c++;
                                }

                                availableOptions.Insert(c, selectedOptions[rightPosition + rightScrollPosition]);
                            }

                            selectedOptions.RemoveAt(rightPosition + rightScrollPosition);

                            if(selectedOptions.Count == 0)
                            {
                                cursorInLeftBox = true;
                            }

                            if (rightScrollPosition + rightBox.Height > selectedOptions.Count && rightScrollPosition > 0)
                            {
                                rightScrollPosition--;
                            }
                            else if (rightPosition == selectedOptions.Count && rightPosition > 0)
                            {
                                rightPosition--;
                            }
                        }

                        break;

                    case ConsoleKey.Escape:

                        return new List<string> { "escape" };

                    case ConsoleKey.N:

                        if (key.Modifiers == ConsoleModifiers.Control)
                        {
                            return new List<string> { "ctrl + N" };
                        }

                        break;

                    case ConsoleKey.D:

                        // Deselect everything
                        if (key.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Shift))
                        {
                            if(selectedOptions.Count > 0)
                            {
                                availableOptions = new List<int>();
                                selectedOptions = new List<int>();
                                cursorInLeftBox = true;

                                for (int i = 0; i < options.Count; i++)
                                {
                                    availableOptions.Add(i);
                                }
                            }
                        }

                        // Select everything
                        if (key.Modifiers == ConsoleModifiers.Control)
                        {
                            if (availableOptions.Count > 0)
                            {
                                availableOptions = new List<int>();
                                selectedOptions = new List<int>();
                                cursorInLeftBox = false;

                                for (int i = 0; i < options.Count; i++)
                                {
                                    selectedOptions.Add(i);
                                }
                            }
                        }

                        break;
                }

                leftBoxOutput = new List<string>();
                rightBoxOutput = new List<string>();

                // ********** Left Box **********
                // Only write the number of lines that fit inside the textbox
                if (availableOptions.Count < leftBox.Height)
                {
                    for (int i = 0; i < availableOptions.Count; i++)
                    {
                        leftBoxOutput.Add("  " + options[availableOptions[i]]);
                    }
                }
                else
                {
                    for (int i = 0; i < leftBox.Height; i++)
                    {
                        leftBoxOutput.Add("  " + options[availableOptions[i + leftScrollPosition]]);
                    }
                }

                // ********** Right Box **********
                // Only write the number of lines that fit inside the textbox
                if (selectedOptions.Count < rightBox.Height)
                {
                    for (int i = 0; i < selectedOptions.Count; i++)
                    {
                        rightBoxOutput.Add("  " + options[selectedOptions[i]]);
                    }
                }
                else
                {
                    for (int i = 0; i < rightBox.Height; i++)
                    {
                        rightBoxOutput.Add("  " + options[selectedOptions[i + rightScrollPosition]]);
                    }
                }

                // Write the marker
                if (cursorInLeftBox)
                {
                    leftBoxOutput[leftPosition] = "->" + leftBoxOutput[leftPosition].Substring(2);
                }
                else
                {
                    rightBoxOutput[rightPosition] = "->" + rightBoxOutput[rightPosition].Substring(2);
                }

                // Update the screen
                leftBox.UpdateData(leftBoxOutput);
                leftBox.PrintData(ref screen, true);

                rightBox.UpdateData(rightBoxOutput);
                rightBox.PrintData(ref screen, true);
            }
            // Main loop exit

            List<string> output = new List<string>();

            foreach(int i in selectedOptions)
            {
                output.Add(options[i]);
            }

            return output;
        }

        public static int selectFromList(ref Screen screen, ref Textbox box, List<string> options, int startIndex, List<ConsoleKey> allowedKeys)
        {
            int scrollPosition = 0;
            int position;

            if (startIndex >= box.Height)
            {
                scrollPosition = startIndex - box.Height + 1;
                position = box.Height - 1;
            }
            else
            {
                position = startIndex;
            }

            ConsoleKeyInfo key;

            List<string> output = new List<string>();

            if (options.Count < box.Height)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    output.Add("  " + options[i]);
                }
            }
            else
            {
                for (int i = 0; i < box.Height; i++)
                {
                    output.Add("  " + options[i + scrollPosition]);
                }
            }

            output[position] = "->" + output[position].Substring(2);

            box.UpdateData(output);
            box.PrintData(ref screen, true);

            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        // Get rid of previous ->
                        // determine whether to scroll
                        // if scrolling, update the whole list
                        // put -> back in the right position

                        output[position] = "  " + output[position].Substring(2);

                        if (position == 0 && scrollPosition > 0)
                        {
                            // scroll up one space
                            scrollPosition--;
                        }
                        else if (position > 0)
                        {
                            // Move up one space
                            position--;
                        }
                        break;

                    case ConsoleKey.DownArrow:

                        output[position] = "  " + output[position].Substring(2);

                        if (position >= box.Height - 1 && scrollPosition < options.Count - box.Height)
                        {
                            // Scroll down one space
                            scrollPosition++;
                        }
                        else if (position + scrollPosition < options.Count - 1)
                        {
                            // Move down one space
                            position++;
                        }
                        break;

                    case ConsoleKey.Escape:

                        if (allowedKeys.Contains(ConsoleKey.Escape))
                        {
                            return -1;
                        }

                        break;

                    case ConsoleKey.N:

                        if (key.Modifiers == ConsoleModifiers.Control && allowedKeys.Contains(ConsoleKey.N))
                        {
                            return -2;
                        }

                        break;

                    case ConsoleKey.D1:
                    case ConsoleKey.D2:
                    case ConsoleKey.D3:
                    case ConsoleKey.D4:
                    case ConsoleKey.D5:
                    case ConsoleKey.D6:
                    case ConsoleKey.D7:
                    case ConsoleKey.D8:
                    case ConsoleKey.D9:
                    case ConsoleKey.NumPad1:
                    case ConsoleKey.NumPad2:
                    case ConsoleKey.NumPad3:
                    case ConsoleKey.NumPad4:
                    case ConsoleKey.NumPad5:
                    case ConsoleKey.NumPad6:
                    case ConsoleKey.NumPad7:
                    case ConsoleKey.NumPad8:
                    case ConsoleKey.NumPad9:

                        int selection = key.KeyChar - 48;

                        if(selection <= options.Count)
                        {
                            return selection - 1;
                        }

                        break;
                }

                output = new List<string>();

                if (options.Count < box.Height)
                {
                    for (int i = 0; i < options.Count; i++)
                    {
                        output.Add("  " + options[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < box.Height; i++)
                    {
                        output.Add("  " + options[i + scrollPosition]);
                    }
                }

                output[position] = "->" + output[position].Substring(2);

                box.UpdateData(output);
                box.PrintData(ref screen, true);
            }

            return position + scrollPosition;
        }
    }

    public class StringLogicalComparer : IComparer<string>
    {
        public int Compare(object x, object y)
        {
            if ((x is string) && (y is string))
            {
                return Compare((string)x, (string)y);
            }
            return -1;
        }

        public int Compare(string s1, string s2)
        {
            //get rid of special cases
            if ((s1 == null) && (s2 == null)) return 0;
            else if (s1 == null) return -1;
            else if (s2 == null) return 1;

            if ((s1.Equals(string.Empty) && (s2.Equals(string.Empty)))) return 0;
            else if (s1.Equals(string.Empty)) return -1;
            else if (s2.Equals(string.Empty)) return -1;

            //WE style, special case
            bool sp1 = char.IsLetterOrDigit(s1, 0);
            bool sp2 = char.IsLetterOrDigit(s2, 0);
            if (sp1 && !sp2) return 1;
            if (!sp1 && sp2) return -1;

            int i1 = 0, i2 = 0; //current index
            int r = 0; // temp result
            while (true)
            {
                bool c1 = char.IsDigit(s1, i1);
                bool c2 = char.IsDigit(s2, i2);
                if (!c1 && !c2)
                {
                    bool letter1 = char.IsLetter(s1, i1);
                    bool letter2 = char.IsLetter(s2, i2);
                    if ((letter1 && letter2) || (!letter1 && !letter2))
                    {
                        if (letter1 && letter2)
                        {
                            r = char.ToLower(s1[i1]).CompareTo(char.ToLower(s2[i2]));
                        }
                        else
                        {
                            r = s1[i1].CompareTo(s2[i2]);
                        }
                        if (r != 0) return r;
                    }
                    else if (!letter1 && letter2) return -1;
                    else if (letter1 && !letter2) return 1;
                }
                else if (c1 && c2)
                {
                    r = CompareNum(s1, ref i1, s2, ref i2);
                    if (r != 0) return r;
                }
                else if (c1)
                {
                    return -1;
                }
                else if (c2)
                {
                    return 1;
                }
                i1++;
                i2++;
                if ((i1 >= s1.Length) && (i2 >= s2.Length))
                {
                    return 0;
                }
                else if (i1 >= s1.Length)
                {
                    return -1;
                }
                else if (i2 >= s2.Length)
                {
                    return -1;
                }
            }
        }

        private static int CompareNum(string s1, ref int i1, string s2, ref int i2)
        {
            int nzStart1 = i1, nzStart2 = i2; // nz = non zero
            int end1 = i1, end2 = i2;

            ScanNumEnd(s1, i1, ref end1, ref nzStart1);
            ScanNumEnd(s2, i2, ref end2, ref nzStart2);
            int start1 = i1; i1 = end1 - 1;
            int start2 = i2; i2 = end2 - 1;

            int nzLength1 = end1 - nzStart1;
            int nzLength2 = end2 - nzStart2;

            if (nzLength1 < nzLength2) return -1;
            else if (nzLength1 > nzLength2) return 1;

            for (int j1 = nzStart1, j2 = nzStart2; j1 <= i1; j1++, j2++)
            {
                int r = s1[j1].CompareTo(s2[j2]);
                if (r != 0) return r;
            }
            // the nz parts are equal
            int length1 = end1 - start1;
            int length2 = end2 - start2;
            if (length1 == length2) return 0;
            if (length1 > length2) return -1;
            return 1;
        }

        //lookahead
        private static void ScanNumEnd(string s, int start, ref int end, ref int nzStart)
        {
            nzStart = start;
            end = start;
            bool countZeros = true;
            while (char.IsDigit(s, end))
            {
                if (countZeros && s[end].Equals('0'))
                {
                    nzStart++;
                }
                else countZeros = false;
                end++;
                if (end >= s.Length) break;
            }
        }

    }
}
