using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wagon_Tracker_Combined
{
    class Train
    {
        public string TrainNum;
        public DateTime DateTimeDeparted;
        public List<DateTime> PredictedArrivals;
        public DateTime DateTimeFirstPredictedArrival;
        public string Origination;
        public string Destination;
        public DateTime DateTimeLastPredictedArrival;
        public DateTime DateTimeArrivedMe;
        public DateTime DateTimeArrivedThem;

        public Train(string trainNum, string origination, string destination, DateTime dateTimeDeparted, DateTime firstPredictedArrival)
        {
            TrainNum = trainNum;
            Origination = origination;
            Destination = destination;
            DateTimeDeparted = dateTimeDeparted;
            DateTimeFirstPredictedArrival = firstPredictedArrival;

            PredictedArrivals = new List<DateTime>();
            DateTimeArrivedMe = DateTime.MinValue;
            DateTimeArrivedThem = DateTime.MinValue;
            DateTimeLastPredictedArrival = DateTime.MinValue;
        }

        public void Arrived(DateTime arrivedMe, DateTime arrivedThem, DateTime lastPredictedArrival)
        {
            DateTimeArrivedMe = arrivedMe;
            DateTimeArrivedThem = arrivedThem;
            DateTimeLastPredictedArrival = lastPredictedArrival;
        }

        public static bool operator ==(Train a, Train b)
        {
            if (a.TrainNum == b.TrainNum && a.Origination == b.Origination && a.Destination == b.Destination && Math.Abs(a.DateTimeDeparted.Subtract(b.DateTimeDeparted).TotalMinutes) < 10)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator !=(Train a, Train b)
        {
            if (a.TrainNum != b.TrainNum || a.Origination != b.Origination || a.Destination != b.Destination || Math.Abs(a.DateTimeDeparted.Subtract(b.DateTimeDeparted).TotalMinutes) >= 10)
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
