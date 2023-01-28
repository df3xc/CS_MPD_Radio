using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LcdDisplay;
using jonas;

namespace Display2
{
    public partial class radio_main
    {
        /// <summary>
        /// write to log file and console and add timestamp
        /// </summary>
        /// <param name="text"></param>
        public void log(string text)
        {
            now = DateTime.Now;
            Console.WriteLine(now.ToLocalTime() + " " + text);
            Logger.writeline(now.ToLocalTime() + " " + text);
        }

        /// <summary>
        /// get the second word from text as string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string getWord2asString(string text)
        {
            string[] words;
            words = text.Split(':');
            return (words[1]);
        }

        /// <summary>
        /// get the second word from text as integer
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public int getWord2asInt(string text)
        {
            string[] words;
            words = text.Split(':');
            return (Int32.Parse(words[1]));
        }

        /// <summary>
        /// load settings from ini file
        /// </summary>
        public void LoadSettings()

        {
            // Read a text file line by line.  
            string[] lines = File.ReadAllLines("/home/pi/mpd/radio.ini");

            log("****** LOAD SETTINGS ******");

            foreach (string line in lines)
            {
                //log("ini file:" + line);

                if (line.Contains("Volume"))
                {
                    volume = getWord2asInt(line);
                    log(" init volume=" + volume);
                }

                if (line.Contains("StationNumber"))
                {
                    current_station = getWord2asInt(line);
                    log(" init current_station=" + current_station);
                }

                if (line.Contains("PlaylistName"))
                {
                    current_m3u_name = getWord2asString(line);
                    log(" init current_m3u_name=" + current_m3u_name);
                }

                if (line.Contains("PlaylistIndex"))
                {
                    current_m3uIdx = getWord2asInt(line);
                    log(" init current_m3uIdx=" + current_m3uIdx);
                }

                if (line.Contains("DeviceName"))
                {
                    current_device_name = getWord2asString(line);
                    log(" init current_device_name=" + current_device_name);
                }

                if (line.Contains("Mode"))
                {
                    current_mode = getWord2asInt(line);
                    log(" init current_mode=" + current_mode);
                }

            }
            log("*** LOAD SETTINGS DONE ***");
        }

        /// <summary>
        /// save settings to ini file
        /// </summary>
        public void SaveSettings()

        {
            string log_file_name = "/home/pi/mpd/radio.ini";
            StreamWriter re = new StreamWriter(log_file_name);
            re.WriteLine("Volume:" + volume);
            re.WriteLine("StationNumber:" + current_station);
            re.WriteLine("PlaylistName:" + m3uFiles[current_m3uIdx]);
            re.WriteLine("PlaylistIndex:" + current_m3uIdx);
            re.WriteLine("DeviceName:" + current_device.name);
            re.WriteLine("Mode:" + current_mode);
            re.Flush();
            re.Close();
            log(" Settings saved to " + log_file_name);

        }

        /// <summary>
        /// display content of the ini file
        /// </summary>
        public void display_inifile()
        {
            log(" ini file : ");
            output = excecute("cat", "/home/pi/mpd/radio.ini");
            logList(output);
        }

        /// <summary>
        /// display text
        /// </summary>
        /// <param name="line"></param>
        /// <param name="text"></param>
        public void display(int line, string text)
        {
            log(" Display : " + text);
            if (line == 2)
            {
                Display.Line2();
            }
            Display.Write(text);
        }

        /// <summary>
        /// write all strings in output to console
        /// </summary>
        /// <param name="output"></param>
        public void logList(List<string> output)
        {
            if (output == null)
            {
                log("WARNING: output is null");
                return;
            }

            foreach (string s in output)
            {
                log("<" + s + ">");
            }
            log("#");
        }
    }
}
