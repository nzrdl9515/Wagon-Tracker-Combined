using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace Wagon_Tracker_Combined
{
    static class FindWagons
    {
        public static void Begin(string[] wagonClasses, ref Screen screen)
        {
            List<string> allData = new List<string>();
            //Dictionary<string, List<string>> wagons = new Dictionary<string, List<string>>();
            //WebClient client = new WebClient();

            for (int j = 0; j < wagonClasses.Length; j++)
            {
                //wagons.Add(wagonClasses[j], new List<string>());

                Dictionary<string, string> searchWagons = new Dictionary<string, string>();

                for(int i = 1; i < 200; i++)
                {
                    int wagonNumber = i * 10;

                    if (createWagonNumber(wagonClasses[j], wagonNumber) == "invalid")
                    {
                        wagonNumber = i * 10 + 1;
                    }

                    searchWagons.Add(createWagonNumber(wagonClasses[j], wagonNumber), "");
                }

                List<string> wagons = new List<string>(searchWagons.Keys);

                screen.Clear();
                screen.Update(string.Format("Searching for {0} wagons", wagonClasses[j]).ToCharArray(), 3, 1);
                Download(ref searchWagons, ref screen);

                int lowIndex = 0, highIndex = 0, c = 0;
                string lowWagon = "", highWagon = "";

                while (c < 199 && searchWagons[wagons[c]].Contains("\"\""))
                {
                    c++;
                }

                if(c == 199)
                {
                    // No wagons found
                    screen.Clear();

                    screen.Update(string.Format("No {0} wagons found. Press any key to continue.", wagonClasses[j]).ToCharArray(), 3, 1);

                    Console.ReadKey();
                }
                else
                {
                    // Wagons found so look for the end index
                    lowIndex = c;

                    lowWagon = createWagonNumber(wagonClasses[j], lowIndex * 10);
                    if(lowWagon == "invalid")
                    {
                        lowWagon = createWagonNumber(wagonClasses[j], lowIndex * 10 + 1);
                    }

                    while (c < 198)
                    {
                        c++;

                        if (!searchWagons[wagons[c]].Contains("\"\""))
                        {
                            highIndex = c + 2;

                            highWagon = createWagonNumber(wagonClasses[j], highIndex * 10);
                            if (highWagon == "invalid")
                            {
                                highWagon = createWagonNumber(wagonClasses[j], highIndex * 10 - 1);
                            }
                        }
                    }

                    screen.Clear();
                    screen.Update(string.Format("{0} wagons found between {1} and {2}. Perform automatic search in this range?", wagonClasses[j], lowWagon, highWagon).ToCharArray(), 3, 1);

                    Textbox yesNoBox = new Textbox(5, 2, 4, 4);

                    switch(Program.selectFromList(ref screen, ref yesNoBox, new List<string>() { "Yes", "No" }, 0, new List<ConsoleKey>()))
                    {
                        case 0:
                            // Perform automatic search in the range identified

                            searchWagons = new Dictionary<string, string>();

                            if(lowIndex == 1)
                            {
                                lowIndex = 0;
                            }
                            else if(lowIndex > 2)
                            {
                                lowIndex -= 2;
                            }

                            highIndex += 2;

                            for (int i = lowIndex * 10; i < highIndex * 10 + 1; i++)
                            {
                                string wagon = createWagonNumber(wagonClasses[j], i);

                                if (wagon != "invalid")
                                {
                                    searchWagons.Add(wagon, "");
                                }
                            }

                            wagons = new List<string>(searchWagons.Keys);

                            screen.Clear();
                            screen.Update(string.Format("Performing automatic search for {0} wagons", wagonClasses[j]).ToCharArray(), 3, 1);
                            Download(ref searchWagons, ref screen);

                            /*using (StreamWriter sw = new StreamWriter(wagonClasses[j] + ".txt"))
                            {
                                for (int i = 0; i < wagons.Count; i++)
                                {
                                    if (!searchWagons[wagons[i]].Contains("\"\""))
                                    {
                                        sw.WriteLine(wagons[i]);
                                    }
                                }
                            }*/

                            break;

                        case 1:
                            // Ask for manual range to search
                            screen.Clear();
                            
                            Textbox inputBox = new Textbox(20, 5, 5, 3);

                            string[] range = inputBox.GetKeyboardInput("Input range to search (including check digit)", ref screen, true).Replace(" ", "").Split(",");

                            lowIndex = Convert.ToInt32(range[0].Substring(0, range[0].Length - 1));
                            highIndex = Convert.ToInt32(range[1].Substring(0, range[1].Length - 1));

                            searchWagons = new Dictionary<string, string>();

                            for (int i = lowIndex; i < highIndex + 1; i++)
                            {
                                string wagon = createWagonNumber(wagonClasses[j], i);

                                if (wagon != "invalid")
                                {
                                    searchWagons.Add(wagon, "");
                                }
                            }

                            wagons = new List<string>(searchWagons.Keys);

                            screen.Clear();
                            screen.Update(string.Format("Performing manual search for {0} wagons", wagonClasses[j]).ToCharArray(), 3, 1);
                            Download(ref searchWagons, ref screen);

                            /*using (StreamWriter sw = new StreamWriter(wagonClasses[j] + ".txt"))
                            {
                                for (int i = 0; i < wagons.Count; i++)
                                {
                                    if (!searchWagons[wagons[i]].Contains("\"\""))
                                    {
                                        sw.WriteLine(wagons[i]);
                                    }
                                }
                            }*/



                            break;
                    }

                    List<string> newFile = new List<string>();

                    foreach (string wagon in wagons)
                    {
                        if (!searchWagons[wagon].Contains("\"\""))
                        {
                            newFile.Add(wagon);
                        }
                    }

                    // Check if there is an existing file for the same wagon class.
                    if (File.Exists(string.Format("wagons/{0}.txt", wagonClasses[j])))
                    {
                        string[] oldFile = File.ReadAllLines(string.Format("wagons/{0}.txt", wagonClasses[j]));
                        WebClient client = new WebClient();

                        foreach(string wagon in oldFile)
                        {
                            if (!newFile.Contains(wagon))
                            {
                                // ******************* Consider optimising this by using the fast downloader *********************
                                if(!client.DownloadString("https://www.kiwirailfreight.co.nz/tc/api/location/current/" + wagon).Contains("\"\""))
                                {
                                    newFile.Add(wagon);
                                }
                            }
                        }
                    }
                    else
                    {
                        // No file, so we need to update wagon_classes.txt
                        List<string> allWagonClasses = new List<string>(File.ReadAllLines("wagon_classes.txt"));
                        allWagonClasses.Add(wagonClasses[j]);
                        allWagonClasses.Sort();
                        File.WriteAllLines("wagon_classes.txt", allWagonClasses);
                    }

                    newFile.Sort();
                    File.WriteAllLines(string.Format("wagons/{0}.txt", wagonClasses[j]), newFile);
                }




                /*for (int i = 1; i < 500; i++)
                {
                    string wagon = createWagonNumber(wagonClasses[j], i.ToString());

                    // As long as the check digit is valid (i.e. not 10), download the string from KiwiRail
                    if (wagon != "invalid")
                    {
                        string result = client.DownloadString("https://www.kiwirailfreight.co.nz/tc/api/location/current/" + wagon);
                        //allData.Add(wagon + " - " + client.DownloadString("https://www.kiwirailfreight.co.nz/tc/api/location/current/" + wagon));
                        //allData.Add("CS" + (char)i + "118" + client.DownloadString("https://www.kiwirailfreight.co.nz/tc/api/location/current/" + "CS" + (char)i + "118"));
                        //string result = wagonClasses[j] + i + client.DownloadString("https://www.kiwirailfreight.co.nz/tc/api/location/current/" + wagonClasses[j] + i);

                        if (result.IndexOf("\"\"") == -1)
                        {
                            Console.WriteLine(wagon + " - " + result);
                            wagons[wagonClasses[j]].Add(wagon);
                        }
                    }

                    if (i % 50 == 0)
                    {
                        Console.WriteLine(i);
                    }
                }

                if (wagons[wagonClasses[j]].Count > 0)
                {
                    File.WriteAllLines(wagonClasses[j] + ".txt", wagons[wagonClasses[j]]);
                }
                else
                {
                    Console.WriteLine("No {0} wagons found.", wagonClasses[j]);
                }*/
            }

            // Write all the data to a file
            
        }

        public static void Download(/*List<string> wagons, */ref Dictionary<string, string> allWagonsData, ref Screen screen)
        {
            List<string> wagons = new List<string>(allWagonsData.Keys);
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

                //screen.Update(string.Format("Wagons downloaded: {0}/{1}         ", j, wagons.Count).ToCharArray(), 3, 1);

                if (j > 30)
                {
                    screen.Update(("Time remaining: " + Math.Round(estTimeRemaining.TotalSeconds).ToString() + "s        ").ToCharArray(), 3, 3);
                }
            }

            sw.Stop();
        }

        private static /*List<*/string createWagonNumber(string code, int numberInput)
        {
            List<byte> digits = new List<byte>();

            string number = numberInput.ToString();

            int[] numClassNums = convStringtoCharInts(code);
            int[] numNumNums = convInttoCharInts(number);

            addArraytoList(numClassNums, digits);

            if (code.Length + 2 + number.Length < 8)
            {
                for (int i = 1; i < 8 - (code.Length + 1 + number.Length); i++)
                {
                    int[] toAdd = new int[1];
                    toAdd[0] = 0;
                    addArraytoList(toAdd, digits);
                }
            }

            addArraytoList(numNumNums, digits);

            int total = addListNums(digits);

            int checkDigit = total % 11;

            if (checkDigit == 10)
            {
                return "invalid";
            }
            else
            {
                return code + number + checkDigit.ToString();
            }
        }

        private static int[] convInttoCharInts(string input)
        {
            int[] charInts = new int[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                charInts[i] = ((int)Convert.ToChar(input.Substring(i, 1))) - 48;
            }

            return charInts;
        }

        private static int[] convStringtoCharInts(string input)
        {
            List<int> charInts = new List<int>();
            int a = 0;

            for (int i = 0; (i - a) < input.Length;)
            {
                charInts.Add((int)Convert.ToChar(input.Substring(i - a, 1)));

                if (charInts[i] > 96)
                {
                    charInts[i] -= 96;
                }
                else
                {
                    charInts[i] -= 64;
                }

                if (charInts[i] < 10)
                {
                    charInts.Insert(i, 0);
                    i += 2;
                    a++;
                }
                else
                {
                    i++;
                }
            }

            return charInts.ToArray();
        }

        private static int addListNums(List<byte> digits)
        {
            int total = 0;
            for (int i = 0; i < digits.Count; i++)
            {
                total += digits[i] * (int)Math.Pow(2, i);
            }
            return total;
        }

        private static void addArraytoList(int[] input, List<byte> digits)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (Convert.ToString(input[i]).Length != 1)
                {
                    int[] tmp = convInttoCharInts(Convert.ToString(input[i]));
                    addArraytoList(tmp, digits);
                }
                else
                {
                    digits.Add((byte)input[i]);
                }
            }
        }
    }
}
