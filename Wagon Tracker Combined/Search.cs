using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Wagon_Tracker_Combined
{
    public struct Search
    {
        public List<string> Wagons;
        public string Keyword;

        public Search(string wagons, string keyword)
        {
            Wagons = new List<string>(wagons.Split(','));
            Keyword = keyword;
        }
        
        public static bool operator ==(Search a, Search b)
        {
            if (a.Wagons.SequenceEqual(b.Wagons) && a.Keyword == b.Keyword)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator !=(Search a, Search b)
        {
            if (a.Wagons.SequenceEqual(b.Wagons) && a.Keyword == b.Keyword)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    internal class Searches
    {
        private Dictionary<string, Search> searches;

        public Searches()
        {
            searches = new Dictionary<string, Search>();

            if (File.Exists("searches.txt"))
            {
                string[] searchesFile = File.ReadAllLines("searches.txt");

                for (int i = 0; i < searchesFile.Length; i += 3)
                {
                    searches.Add(searchesFile[i], new Search(searchesFile[i + 1], searchesFile[i + 2]));
                }
            }
        }

        public bool CheckNameUnique(string name)
        {
            return !searches.ContainsKey(name);
        }

        public string CheckExistingSearch(string wagons, string keyword)
        {
            return searches.FirstOrDefault(x => x.Value == new Search(wagons, keyword)).Key;
        }

        public void UpdateSearchName(string oldName, string newName)
        {
            searches.Add(newName, searches[oldName]);
            searches.Remove(oldName);

            // Re-write the entire file to update the corrected name
            using (StreamWriter sw = new StreamWriter("searches.txt", false))
            {
                foreach (string name in searches.Keys)
                {
                    string wagonsToSave = searches[name].Wagons[0];

                    for (int i = 1; i < searches[name].Wagons.Count; i++)
                    {
                        wagonsToSave = wagonsToSave + "," + searches[name].Wagons[i];
                    }

                    sw.WriteLine(name);
                    sw.WriteLine(wagonsToSave);
                    sw.WriteLine(searches[name].Keyword);
                }
            }
        }

        public void AddSearch(string name, string wagons, string keyword)
        {
            searches.Add(name, new Search(wagons, keyword));

            // Append the new search to the existing file
            using (StreamWriter sw = new StreamWriter("searches.txt", true))
            {
                sw.WriteLine(name);
                sw.WriteLine(wagons);
                sw.WriteLine(keyword);
            }
        }

        /*public Search GetSearch(string name)
        {
            return searches[name];
        }*/

        public Search Choose(ref Screen screen)
        {
            Textbox optionsBox = new Textbox(40, 10, 5, 3);

            List<string> searchNames = new List<string>(searches.Keys);
            searchNames.Sort();

            return searches[searchNames[Program.selectFromList(ref screen, ref optionsBox, searchNames, 0, new List<ConsoleKey>())]];
        }
    }
}
