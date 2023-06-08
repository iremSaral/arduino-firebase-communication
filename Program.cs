
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System.Timers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO.Ports;
using System.Text.Json;
using Timer = System.Timers.Timer;

namespace HelloWorld;

internal class Program
{
    static async Task Main(string[] args)
    {
        while (true)
        {

            ConnectFirebase();
            Console.ReadLine();
        }
    }

    public static async void ReadCommandinFireBase(IFirebaseClient client)
    {
        Console.WriteLine("Read Command");
      //  Timer timer = new Timer();
        //timer.Elapsed += async (sender, args) =>
       // {
            FirebaseResponse response = client.Get("command/");
            var current = response.Body;
            var com = "normal";

            Console.WriteLine("Güncelleme: " + current);

            if (current.Trim('\"') == "test")
            {     //Test mode: Tüm vanalar ardışık olarak açılıp kapatılır.
                Console.WriteLine("testmod");
                ConnectArdunio("5");

                var writeData = await client.SetAsync("command/", com);//Çalışma bitince durması için

            }
            else if (current.Trim('\"') == "mannual")
            {
                Console.WriteLine("mannualmode on");

                var writeData = await client.SetAsync("command/", com);//Çalışma bitince durması için
                MannualModeCommand(client);

            }
        //};

//        timer.Interval = 5000;//5sn de bir
  //      timer.Start();
    }
    public static async void MannualModeCommand(IFirebaseClient client)
    {
        //Manuel mod: seçilen vana/led istenilen süre kadar açılır.
        FirebaseResponse response = await client.GetAsync("mannual/");
        String json = response.Body.ToString();
        JObject obj = JObject.Parse(json);
        string ledNum = (string)obj["led"];
        string duration = (string)obj["duration"];
        string status = (string)obj["status"];

        SerialPort _serialPort = new SerialPort("COM3", 9600);
        _serialPort.Open();
        _serialPort.WriteLine("6");
        Console.WriteLine("Arrdunio bağlandı manuel");
        _serialPort.WriteLine(ledNum);
        _serialPort.WriteLine(duration);
        _serialPort.WriteLine(status);
        int veri = int.Parse(_serialPort.ReadLine());//ReadTo("\r\n");
        Console.WriteLine("led:" + veri);

        int dura = int.Parse(_serialPort.ReadLine());//ReadTo("\r\n");
        Console.WriteLine("duration:" + dura);

        int stat = int.Parse(_serialPort.ReadLine());//ReadTo("\r\n");
        Console.WriteLine("duration:" + stat);
        _serialPort.Close();


    }

    public static async void ConnectFirebase()
    {
        IFirebaseClient client;

        IFirebaseConfig fc = new FirebaseConfig()
        {
            AuthSecret = "AuthSecret",
            BasePath = "Basepath"

        };

        try
        {
            client = new FireSharp.FirebaseClient(fc);
            Console.WriteLine("Firebase'e baglanıldı");
            Timer timer = new Timer();
            timer.Elapsed += async (sender, args) =>
            {
                // WriteDatatoFirebaseAsync(client);//-->Firebase  veri ekler
                // DeleteDatatoFirebase(client);
                await ReadDatatoFirebase(client);
                ReadCommandinFireBase(client);
                //    AcknowladgeMessage(client);
            };

            timer.Interval = 5000;//5sn de bir
            timer.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public static async Task ConnectArdunio(string command)
    {
        try
        {
            SerialPort _serialPort;
            _serialPort = new SerialPort();
            _serialPort.PortName = "COM4";//Set your board COM
            _serialPort.BaudRate = 9600;
            _serialPort.Open();
            _serialPort.WriteLine(command);
            Console.WriteLine("Arrdunio bağlandı");
            Console.WriteLine(command);
            _serialPort.WriteLine(command);

            _serialPort.Close();


        }
        catch
        {
            Console.WriteLine("Bağlanyı kurulamadı");
        }
    }

    public static async void WriteDatatoFirebaseAsync(IFirebaseClient client)
    {
        try
        {
            Console.WriteLine("İsim :");
            String b = Console.ReadLine();
            Console.WriteLine("Değerler :");
            String a = Console.ReadLine();

            var writeData = await client.SetAsync("Statu/" + b, a);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

    }

    public static async void DeleteDatatoFirebase(IFirebaseClient client)
    {
        try
        {
            Console.WriteLine("İsim :");
            String b = Console.ReadLine();
            var writeData = await client.DeleteAsync("Statu/" + b);
            Console.WriteLine("Deleted!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

    }

    public static async Task ReadDatatoFirebase(IFirebaseClient client)
    {
        FirebaseResponse response = await client.GetAsync("mannualmode/");

        String json = response.Body.ToString();
        JObject obj = JObject.Parse(json);
        // Console.WriteLine(obj);

        List<Valve> valveList = new List<Valve>();


        foreach (JProperty item in obj.Properties())
        {
            string Id = item.Name;
            //  Console.WriteLine(Id); Valve
            JObject prog = (JObject)item.Value;
            List<Data> programList = new List<Data>();
            // Console.WriteLine(prog);
            foreach (JProperty data in prog.Properties())
            {
                string ids = data.Name;
                // Console.WriteLine(ids);
                JObject props = (JObject)data.Value;
                string day = (string)props["day"];
                string start = (string)props["start"];
                string end = (string)props["stop"];
                string controller = (string)props["controller"];
                string vana = (string)props["vana"];
                Data dat = new Data(ids, day, start, end, vana, controller);
                programList.Add(dat);
            }
            //en önceki günden en sonraki güne göre sıralarız
            programList = programList.OrderBy(p => p.day).ToList();

            Valve valveAdd = new Valve(Id, programList);
            valveList.Add(valveAdd);

        }
        //  Console.WriteLine(valveList[0].Programs[0].day);
        // MannualModeControl(valveList[0].Programs[0]);
        foreach (Valve valve in valveList)
        {
            foreach (Data dat in valve.Programs)
            {
                await MannualModeControl(dat);
            }
        }
    }

    //MannualModeControl(data);
    static Mutex mutex = new Mutex();
    public static async Task MannualModeControl(Data data)
    {
        Console.WriteLine("Mannual");
        // Console.WriteLine(data.day);
        DateTime currentTime = DateTime.Now;
        string day = currentTime.DayOfWeek.ToString();
        string start = data.start;
        string stop = data.stop;
        string valve = data.valve;
        Console.WriteLine(start);
        Console.WriteLine(stop);        
        if (data.day.ToLower() == day.ToLower() && currentTime.TimeOfDay > DateTime.Parse(start).TimeOfDay && currentTime.TimeOfDay < DateTime.Parse(stop).TimeOfDay)
        {
            while (currentTime.TimeOfDay < DateTime.Parse(stop).TimeOfDay)
            {
                if (valve == "1")
                {//vana 1 = led sarı
                    await Task.Run(() =>
                    Console.WriteLine("a"));//ConnectArdunio("1"));
                    //  

                    // Her turda mevcut zamanı günceller
                    currentTime = DateTime.Now;
                }
                else
                {
                    await Task.Run(() =>
                    Console.WriteLine("b"));
                    //ConnectArdunio("2"));
                    currentTime = DateTime.Now;
                }
                // Döngüden çıkış koşulunu kontrol edin
                if (currentTime.TimeOfDay >= DateTime.Parse(stop).TimeOfDay)
                {
                    Console.WriteLine("4");
                    //ConnectArdunio("4");
                    break;

                }
            }

        }
        else
        {
            Console.WriteLine("brealk");
        }
}

    public static async void AcknowladgeMessage(IFirebaseClient client)
    {
        FirebaseResponse response = await client.GetAsync("command/");
        Console.WriteLine(response.Body);
        String json = response.Body.ToString();
        Console.WriteLine("json value: " + json);
        string trimmedJson = json.Trim('"');
        Console.WriteLine(trimmedJson);
        if (trimmedJson.Equals("a"))
        {

            var writeData = await client.SetAsync("Statu/" + response.Body, "b");
            Console.WriteLine("ajhsd");
        }
        else
        {
            //var writeData = await client.SetAsync("Statu/" + response.Body, "b");
            Console.WriteLine("sss");
        }
    }
}