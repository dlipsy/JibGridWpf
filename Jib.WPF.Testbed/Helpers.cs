using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jib.WPF.Testbed
{
    public class Helpers
    {
        static private Random rand = new Random(); 
        static public int GetNextRandomValueBetween(int minValue, int maxValue)
        {
            return rand.Next(minValue, maxValue);
        }
    }
}
