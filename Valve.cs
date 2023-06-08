using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorld
{
    public class Valve
    {
        public string ValveId { get; set; }
        public List<Data> Programs { get; set; }

        public Valve(string valveId,List<Data> valveList)
        {
            ValveId = valveId;
            Programs = valveList;
        }
    }
}
