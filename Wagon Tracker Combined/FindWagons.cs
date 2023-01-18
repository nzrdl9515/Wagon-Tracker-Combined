using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading.Tasks;

namespace Wagon_Tracker_Combined
{
    static class FindWagons
    {
        public static void Begin(string[] wagonClasses)
        {
            List<string> allData = new List<string>();
            Dictionary<string, List<string>> wagons = new Dictionary<string, List<string>>();
            WebClient client = new WebClient();

            for (int j = 0; j < wagonClasses.Length; j++)
            {
                wagons.Add(wagonClasses[j], new List<string>());

                for (int i = 1; i < 500; i++)
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
                }
            }

            Console.WriteLine("Finished");
            Console.ReadKey();

            // Write all the data to a file
            using (StreamWriter sw = new StreamWriter("data.txt"))
            {
                for (int i = 0; i < allData.Count; i++)
                {
                    sw.WriteLine(allData[i]);
                }
            }
        }

        private static /*List<*/string createWagonNumber(string code, string number)
        {
            List<byte> digits = new List<byte>();

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
