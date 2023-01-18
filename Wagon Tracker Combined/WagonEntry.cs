using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wagon_Tracker_Combined
{
    class WagonEntry
    {
        public string wagon;
        public Action action;
        public string siding;
        public string location;
        public string train;
        public DateTime dateTimeMe;
        public DateTime dateTimeThem;

        public WagonEntry()
        {
            wagon = "";
            action = Action.notSet;
            siding = "";
            location = "";
            train = "";
            dateTimeMe = DateTime.MinValue;
            dateTimeMe = DateTime.MinValue;
        }

        public static bool operator ==(WagonEntry a, WagonEntry b)
        {
            if (a.wagon == b.wagon && a.action == b.action && a.siding == b.siding && a.location == b.location && a.train == b.train && a.dateTimeThem == b.dateTimeThem)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator !=(WagonEntry a, WagonEntry b)
        {
            if (a.wagon != b.wagon || a.action != b.action || a.siding != b.siding || a.location != b.location || a.train != b.train || a.dateTimeThem != b.dateTimeThem)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
