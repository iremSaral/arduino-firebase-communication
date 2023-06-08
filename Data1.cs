using Newtonsoft.Json.Linq;

namespace HelloWorld
{
    public class Data
    {
        public string controller { get; set; }
        public string day { get; set; }
        public string start { get; set; }
        public string stop { get; set; }
        public string valve { get; set; }
        public string Id { get; set; }

        public Data(
            string Id
            ,string dayName,
                         string startDate,
                         string stopData,
                         string valveId,
                         string controllerName)
        { 
            this.Id = Id;
           this.day = dayName;
           this.controller = controllerName;
            this.start = startDate;
            this.stop = stopData;   
            this.valve=valveId;

        }

    }
}
