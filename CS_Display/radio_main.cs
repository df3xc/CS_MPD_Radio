using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using jonas;
using LcdDisplay;

namespace Display2
{
    class radio_main
    {
        #region variables

        public static keyboard kBoard;

        List<string> m3uFiles = new List<string>();
        int current_m3uIdx = 0;
        string current_m3uFile = "";

        List<station> stations = new List<station>();
        int current_station = 0;
        int next_station = 0;

        public AudioDeviceClass current_device = new AudioDeviceClass("hifiberry",  "udef ", "1", "HifiBerry");

        List<AudioDeviceClass>  btDevices = new List<AudioDeviceClass>();
        public AudioDeviceClass device_holz = new AudioDeviceClass("HolzRadio", "/home/pi/mpd/holz", "5", "FY-R919 - A2DP");
        public AudioDeviceClass device_bose = new AudioDeviceClass("bose", "/home/pi/mpd/bose", "5", "Bose Mini II SoundLin - A2DP");
        public AudioDeviceClass disconnect  = new AudioDeviceClass("disconnect", "" , "5", "");

        int btDevice = 0;
        int next_btDevice = 0;

        string lastText = "";

        /// <summary>
        /// output provided by process exec
        /// </summary>
        List<string> output = new List<string>();

        int current_mode = 0;

        int volume = 85;

        DateTime now = new DateTime();

        #endregion

        /// <summary>
        /// Display station name and song title frequently
        /// </summary>
        public void timer_thread()

        {
            string text = "";

            log("Timer Thread started");

            while(true)
            {
                try
                {
                    Thread.Sleep(7500);
                    if (current_mode == application_mode.normal)
                    {
                        output = excecute("mpc", "current");
                        if (output != null)
                        {
                            string[] lines;
                            lines = output[0].Split(':');
                            log("Split count:" + lines.Length);


                            if (lines[0].Length > 16)
                            {
                                text = lines[0].Substring(0, 15);
                            }
                            else text = lines[0];

                            Display.Clear();
                            Display.Write(text);

                            if (lines[1].Length > 16)
                            {
                                text = lines[1].Substring(0, 15);
                            }
                            else text = lines[1];

                            Display.Line2();
                            Display.Write(text);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log(ex.Message);
                }
            }
        }

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

        public void SaveSettings()

        {
            Display2.Properties.Settings.Default.Volume = volume;
            Display2.Properties.Settings.Default.Station_Number = current_station;
            Display2.Properties.Settings.Default.btDevice = current_device.name;
            Display2.Properties.Settings.Default.Playlist = current_m3uFile;

        }

        public void LoadSettings()

        {
            volume = Display2.Properties.Settings.Default.Volume;
            current_station = Display2.Properties.Settings.Default.Station_Number;
            current_m3uFile = Display2.Properties.Settings.Default.Playlist;
            //current_device = Display2.Properties.Settings.Default.btDevice;

            log("Setting Volume = " + volume);
            log("Setting Station Number = " + current_station);
            log("Setting M3U file = " + current_m3uFile);

        }

        /// <summary>
        /// initialisation of the radio application
        /// </summary>
        public void init()
        {
            LoadSettings();

            log(" radio start and init");
            log("Program init");
            LcdDisplay.Display.lcd_init();
            Display.White();
            Display.Write("MPD Radio ");
            Display.Line2();
            Display.Write("created 01/2023");

            kBoard = new keyboard();
            kBoard.OnKeyEvent += new keyboard.OnKeyEventHandler(OnKeyPressed);
            kBoard.start();

            current_mode = application_mode.normal;

            getRadioStations();
            log("Number of Stations  : " + stations.Count());

            getM3Ufiles();
            log("Number of M3U files : " + m3uFiles.Count());

            Thread timer = new Thread(timer_thread);
            timer.Start();

            btDevices.Add(device_holz);
            btDevices.Add(device_bose);
            btDevices.Add(disconnect);
            log("Number of BT devices : " + btDevices.Count());

            //btDisconnectAll();

            current_device = btDevices[btDevice];
            btConnect(current_device);
            Thread.Sleep(1500);

            changeToMode(application_mode.normal);
            setVolume(volume);
        }

        /// <summary>
        /// execute a linux command and return the output/result
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public List<string> excecute (string cmd, string args)
        {
            List<string> output = new List<string>();
            output = jonas.Process_util.process_exec_output(cmd, args);
            //if (output!=null) logList(output);
            return (output);
        }

        /// <summary>
        /// change application mode depending on key pressed
        /// </summary>
        /// <param name="mode"></param>
        public void changeToMode(int mode)
        {

            log("Mode changed to  : " + mode.ToString());
            current_mode = mode;
            log("Current Mode     :  " + current_mode);
        }

        /// <summary>
        /// get a list of M3U files
        /// </summary>
        public void getM3Ufiles()
        {
            m3uFiles.Clear();
            output = excecute("mpc", "lsplaylists");

            foreach (string s in output)
            {
                m3uFiles.Add(s);
            }

            foreach (string s in m3uFiles)
            {
                log("M3U: " + s);
            }

            
        }

        /// <summary>
        /// get a list of stations in the M3U file
        /// </summary>
        public void getRadioStations()
        {
            stations.Clear();
            output = excecute("mpc", "playlist");

            if (output.Count > 0)
            {
                foreach (string s in output)
                {
                    station sta = new station();
                    sta.name = s;
                    stations.Add(sta);
                }

                foreach (station sta in stations)
                {
                    log(" Station: " + sta.name);
                }
            }
            else
            {
                log(" No Stations. Now loading default playlist ...");
                output = excecute("mpc", "load Deutschland");
                getRadioStations();
            }
        }

        /// <summary>
        /// main thread of this application
        /// </summary>
        public void application_run()
        {
            log("Application Run");
            while (true)
            {

            }

        }

        public void application_close()
        {
            Display.Clear();
            Display.Write("Bye Bye ...");
            log("Bye Bye ...");
        }

        /// <summary>
        /// write all strings in output to console
        /// </summary>
        /// <param name="output"></param>
        public void logList(List<string> output)
        {
            if(output == null)
            {
                log("WARNING: output is null");
                return;
            }

            foreach (string s in output)
            {
                log(s);
            }
            log("#");
        }

        /// <summary>
        /// restart bluetooth and mpd service and re-connects to bluetooth speaker
        /// </summary>
        public void mpd_restart()
        {
            output = excecute("service", " bluetooth restart");
            Thread.Sleep(300);

            output = excecute("service", " mpd restart");
            Thread.Sleep(300);

            excecute(current_device.enable, " 0");
            Thread.Sleep(1500);

            output = excecute(current_device.enable, " 1");
            Thread.Sleep(2500);

            output = excecute(current_device.enable, " 1");
            Thread.Sleep(2500);

        }

        /// <summary>
        /// disconnect from all bluetoothdevices
        /// </summary>
        public void btDisconnectAll()
        {
            output = excecute("mpc", "stop");

            foreach (AudioDeviceClass dev in btDevices)
            {
                log(" Disconnect from " + dev.name);
                output = excecute(dev.enable, " 0");
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// connect to a bluetooth device
        /// </summary>
        /// <param name="dev"></param>
        public void btConnect(AudioDeviceClass dev)
        {
            log(" btConnect to " + dev.name);
            output = excecute("mpc", " stop");
            output = excecute(dev.enable, " 1");
            Thread.Sleep(500);
            output = excecute("mpc", " play");
        }

        /// <summary>
        /// disconnect from a bluetooth device
        /// </summary>
        /// <param name="dev"></param>
        public void btDisConnect(AudioDeviceClass dev)
        {
            log(" btDisConnect from " + dev.name);
            output = excecute("mpc", " stop");
            output = excecute(dev.enable, " 0");
            Thread.Sleep(500);
        }

        /// <summary>
        /// set volume of bluetooth device and headphone
        /// </summary>
        /// <param name="volume"></param>
        public void setVolume(int volume)
        {
            log(" Set Volume " + volume);
            output = excecute("amixer", " -D bluealsa sset '" + current_device.bluealsa_name + "' " + volume.ToString() + "%");
            output = excecute("amixer", "sset Headphone " + volume.ToString() + "%");
        }

        /// <summary>
        /// handle key pressed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnKeyPressed(object sender, KeyEventArgs e)
        {
            int key;
            log("Key pressed : " + e.keycode.ToString() + " in mode : " + current_mode);
            LcdDisplay.Display.lcd_init();
            LcdDisplay.Display.Clear();

            if (e.keycode != 0x0)
            {
                Display.Clear();
                Display.White();
                key = e.keycode;
                //Display.Write("Key:" + key.ToString("X2"));
                //Display.Line2();

                try
                {
                    switch (key)
                    {
                        case keys.LEFT:
                            {
                                if (current_mode == application_mode.normal)
                                {
                                    if (volume > 50) volume = volume - 5;
                                    Display.Write("VOL-- " + volume);
                                    setVolume(volume);
                                }

                                if (current_mode == application_mode.menu1)
                                {
                                    Display.Write("Stop");
                                    output = excecute("mpc", "stop");
                                    changeToMode(application_mode.normal);
                                }

                                if (current_mode == application_mode.menu2)
                                {
                                    Display.Write("RESTART MPD");
                                    mpd_restart();
                                    current_mode = application_mode.normal;
                                    LcdDisplay.Display.Clear();
                                    Display.Write("RESTART DONE");
                                }
                                break;
                            }

                        case keys.RIGHT:
                            {

                                if (current_mode == application_mode.normal)
                                {
                                    if (volume < 100) volume = volume + 5;
                                    Display.Write("VOL++ " + volume);
                                    setVolume(volume);
                                    break;
                                }

                                if (current_mode == application_mode.select_station)
                                {
                                    current_station = next_station;
                                    Display.Write("PLAY " + current_station);
                                    output = excecute("mpc", " stop ");
                                    Thread.Sleep(250);
                                    output = excecute("mpc", "play " + (current_station+1).ToString());
                                    Display.Clear();
                                    if (output != null) Display.Write(output[0]);
                                    SaveSettings();
                                    changeToMode(application_mode.normal);
                                    break;
                                }

                                if (current_mode == application_mode.menu1)
                                {
                                    Display.Write("Play");
                                    output = excecute("mpc", "play");
                                    changeToMode(application_mode.normal);
                                }

                                if (current_mode == application_mode.menu2)
                                {
                                    Display.Write("Load M3U file ");
                                    Display.Line2();
                                    Display.Write(m3uFiles[current_m3uIdx]);
                                    output = excecute("mpc", " stop");
                                    output = excecute("mpc", " clear");
                                    output = excecute("mpc", " load " + m3uFiles[current_m3uIdx]);
                                    getRadioStations();
                                    SaveSettings();
                                    changeToMode(application_mode.normal);
                                    break;
                                }

                                if (current_mode == application_mode.menu3)
                                {
                                    Display.Write("Connect Device ");
                                    Display.Line2();
                                    Display.Write(btDevices[next_btDevice].name);

                                    if (btDevices[next_btDevice].name == "Disconnect")
                                    {
                                        btDisconnectAll();
                                    }
                                    else
                                    {
                                        btDisConnect(current_device);
                                        current_device = btDevices[next_btDevice];
                                        btConnect(current_device);
                                        changeToMode(application_mode.normal);
                                    }
                                    break;
                                }

                                break;
                            }

                        case keys.UP:
                            {
                                if (current_mode == application_mode.normal || current_mode == application_mode.select_station)
                                {
                                    next_station--;
                                    if (next_station < 0) next_station = stations.Count() - 1;
                                    changeToMode(application_mode.select_station);
                                    Display.Write(">"+stations[next_station].name);
                                    break;
                                }

                                if (current_mode == application_mode.menu2)
                                {
                                    current_m3uIdx--;
                                    if (current_m3uIdx < 0) current_m3uIdx = m3uFiles.Count() - 1;
                                    log("current M3U:" + current_m3uIdx + " " + m3uFiles[current_m3uIdx]);
                                    Display.Write("M3U:" + m3uFiles[current_m3uIdx]);
                                    Display.Line2();
                                    Display.Write("> select M3U");
                                    break;
                                }

                                if (current_mode == application_mode.menu3)
                                {
                                    next_btDevice--;
                                    if (next_btDevice < 0) next_btDevice = btDevices.Count() - 1;
                                    log("next BT device:" + btDevices[next_btDevice].name);
                                    Display.Write(btDevices[next_btDevice].name);
                                    Display.Line2();
                                    Display.Write("> select device");
                                    break;
                                }

                                break;
                            }

                        case keys.DOWN:
                            {
                                if (current_mode == application_mode.normal || current_mode == application_mode.select_station)
                                {
                                    next_station++;
                                    if (next_station > stations.Count() - 1) next_station = 0;
                                    log("Station:" + next_station + " " + stations[next_station].name);
                                    current_mode = application_mode.select_station;
                                    Display.Write(">"+stations[next_station].name);

                                    break;
                                }

                                if (current_mode == application_mode.menu2)
                                {
                                    current_m3uIdx++;
                                    if (current_m3uIdx > m3uFiles.Count() - 1) current_m3uIdx = 0;
                                    log("current M3U:" + current_m3uIdx + " " + m3uFiles[current_m3uIdx]);
                                    Display.Write("M3U:" + m3uFiles[current_m3uIdx]);
                                    Display.Line2();
                                    Display.Write("> select M3U");
                                    break;
                                }

                                if (current_mode == application_mode.menu3)
                                {
                                    next_btDevice++;
                                    if (next_btDevice > btDevices.Count() - 1) next_btDevice = 0;
                                    log("next BT device:" + btDevices[next_btDevice].name);
                                    Display.Write(btDevices[next_btDevice].name);
                                    Display.Line2();
                                    Display.Write("> select device");
                                    break;
                                }
                                break;
                            }

                        case keys.MENU:
                            {

                                getRadioStations();
                                getM3Ufiles();

                                if (current_mode == application_mode.normal)
                                {
                                    changeToMode(application_mode.menu1);
                                    Display.Red();
                                    Display.Write("MENU I");
                                    Display.Line2();
                                    Display.Write("STOP <> PLAY");
                                    break;
                                }

                                if (current_mode == application_mode.select_station)
                                {
                                    changeToMode(application_mode.normal);
                                    Display.Red();
                                    Display.Write("MENU EXIT");
                                    break;
                                }

                                if (current_mode == application_mode.menu1)
                                {
                                    changeToMode(application_mode.menu2);
                                    Display.Red();
                                    Display.Write("MENU II: L=restart MPD");
                                    Display.Line2();
                                    Display.Write("Up/Dwn: select M3U file");
                                    break;
                                }

                                if (current_mode == application_mode.menu2)
                                {
                                    changeToMode(application_mode.menu3);
                                    Display.Red();
                                    Display.Write("MENU III: Device");
                                    Display.Line2();
                                    Display.Write(current_device.name);
                                    break;
                                }

                                if (current_mode == application_mode.menu3)
                                {
                                    changeToMode(application_mode.normal);
                                    Display.Red();
                                    Display.Write("MENU EXIT");
                                    break;
                                }

                                break;
                            }
                    }
                }

                catch (Exception ex)
                {
                    Display.Magenta();
                    log("Exception on Key press : \n\r" + ex.Message);
 //                   Display.Write("Sorry, exception");
                }
            }

        }

        }

    /// <summary>
    /// an audio device
    /// </summary>
    public class AudioDeviceClass
    {
        public string name = "";
        /// <summary>
        /// the filename of a script on raspberry to enable the device
        /// </summary>
        public string enable = "";
        public string output = "";
        /// <summary>
        /// device name for bluealsa 
        /// </summary>
        public string bluealsa_name = "";

        public AudioDeviceClass(string name, string enable, string output, string bluealsa_name)
        {
            this.name = name;
            this.enable = enable;
            this.output = output;
            this.bluealsa_name = bluealsa_name;
        }

        public void setAlsaDeviceName(string bluealsa_name)
        {
            this.bluealsa_name = bluealsa_name;
        }
    }


    /// <summary>
    /// a radio station
    /// </summary>
    public class station
    {
        public string name;
        public string current_song;
    }

    /// <summary>
    /// the mode depends on key pressed
    /// </summary>
    public static class application_mode
    {
        public const int normal = 0;
        public const int menu1 = 1;
        public const int menu2 = 2;
        public const int menu3 = 3;
        public const int select_station = 4;
        public const int m3ufiles = 5;
    }

}
