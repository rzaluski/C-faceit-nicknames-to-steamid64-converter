using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace steamidfinder
{
    class Program
    {
        public static ConcurrentDictionary<int, string> dictionary = new ConcurrentDictionary<int, string>();
        public static int count = 0;
        public static int max;
        public static string client_id = "";
        public static string GET(string url)
        {
            bool success;
            int tries = 0;
            do
            {
                success = true;
                tries++;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers.Add("Authorization: Bearer " + client_id);
                try
                {
                    WebResponse response = request.GetResponse();
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                        return reader.ReadToEnd();
                    }
                }
                catch (WebException ex)
                {
                    WebResponse errorResponse = ex.Response;
                    using (Stream responseStream = errorResponse.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
                        String errorText = reader.ReadToEnd();
                        Console.WriteLine(url);
                        // log errorText
                    }
                    success = false;
                }
            } while (!success && tries < 5);
            return null;
        }

        public static void FindId(string nick, int number)
        {
            string html = GET("https://open.faceit.com/data/v4/players?nickname=" + nick);
            if(html != null)
            {
                RootObject player = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObject>(html);
                dictionary.TryAdd(number, "https://steamcommunity.com/profiles/" + player.steam_id_64);
            }
            else
            {
                dictionary.TryAdd(number, "-");
            }
            Interlocked.Increment(ref count);
            Console.Clear();
            Console.WriteLine(count + "/" + Program.max);
            Console.WriteLine(nick);
        }
        static void Main(string[] args)
        {
            string inputFilename = @"input.txt";
            string outputFilename = @"output.txt";
            File.Delete(outputFilename);
            if(!File.Exists(outputFilename))
            {
                using (StreamWriter sw = File.CreateText(outputFilename))
                {
                    string[] lines = File.ReadAllLines(inputFilename);
                    Program.max = lines.Length;
                    List<Task> tasks = new List<Task>();
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var j = i;
                        tasks.Add(Task.Factory.StartNew(() =>
                        {
                            FindId(lines[j], j);
                        }));
                    }
                    Task.WaitAll(tasks.ToArray());
                    for(int i = 0; i < lines.Length; i++)
                    {
                        sw.WriteLine(dictionary[i]);
                    }
                    sw.Close();
                }
            }
        }
    }
}
