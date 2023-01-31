using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using jonas;
using LcdDisplay;

namespace Display2
{
    public partial class radio_main
    {
        #region variables

        public static keyboard kBoard;

        List<string> m3uFiles = new List<string>();
        int current_m3uIdx = 0;
        string current_m3u_name = "";

        List<station> stations = new List<station>();
        int current_station_index = 0;
        int next_station_index = 0;

        public AudioDeviceClass current_device = new AudioDeviceClass("hifiberry",  "udef ",  "HifiBerry", "hifiberry");

        List<AudioDeviceClass>  btDevices = new List<AudioDeviceClass>();
        public AudioDeviceClass device_holz = new AudioDeviceClass("HolzRadio", "/home/pi/mpd/holz", "FY-R919 - A2DP", "0A:A5:88:33:28:94");
        public AudioDeviceClass device_bose = new AudioDeviceClass("bose", "/home/pi/mpd/bose", "Bose Mini II SoundLin - A2DP", "28:11:A5:19:8A:E3");
        public AudioDeviceClass device_airpods = new AudioDeviceClass("airpods", "/home/pi/mpd/airpods",  "Airpods", " E8:85:4B:6E:48:56");
        public AudioDeviceClass device_wch500 = new AudioDeviceClass("Sony wch500 in Blau", "wch500", "WH-CH500 - A2DP", "00:18:09:8D:97:6D");
        public AudioDeviceClass device_wch710 = new AudioDeviceClass("Sony wch710 in Schwarz", "wch710", "WH-CH710N", "74:45:CE:CC:EE:D9");

        public AudioDeviceClass disconnect  = new AudioDeviceClass("disconnect", "" ,  "", "");

        string current_device_name = "";
        Boolean device_connected = false;

        int btDevice = 0;
        int next_btDevice = 0;

        string lastText = "";

        /// <summary>
        /// output provided by process exec
        /// </summary>
        List<string> output = new List<string>();

        int current_mode = 0;

        int volume = 85;
        Thread timer;
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
                    display_inifile();

                    if (current_mode == application_mode.stopped)
                    {
                        Display.mpc_init();
                        Display.lcd_init();
                        Display.Clear();
                        display(0, "Select <play>");
                        Display.Line2();
                        display(2, "to continue");
                        continue;
                    }

                    if(current_mode != application_mode.normal)
                    {
                       continue;
                    }

                    output = excecute("mpc", "current");
                    logList(output);

                    if (output.Count() > 0)
                    {
                        string[] lines;
                        lines = output[0].Split(':');
                        log("Split count:" + lines.Length);


                        if (lines[0].Length > 16)
                        {
                            text = lines[0].Substring(0, 15);
                        }
                        else text = lines[0];

                        displayReset();
                        display(0, text);

                        if (lines[1].Length > 16)
                        {
                            text = lines[1].Substring(0, 15);
                        }
                        else text = lines[1];

                        display(2, text);
                    }
                    Thread.Sleep(7500);
                }
                catch (Exception ex)
                {
                    log("timer exception : "+ ex.Message);
                }
            }
        }


        public void hello()
        {
            LcdDisplay.Display.lcd_init();
            Display.White();
            display(0, "MPD Radio ");
            display(2, "created 01/2023");
            log("******** HELLO ********");
        }

        /// <summary>
        /// initialisation of the radio application
        /// </summary>
        public void init()
        {
            log(" radio start and init");
            log("Program init");

            displayReset();

            hello();

            timer = new Thread(timer_thread);
            timer.Start();

            btDevices.Add(device_holz);
            btDevices.Add(device_bose);
            btDevices.Add(device_wch500);
            btDevices.Add(device_wch710);
            btDevices.Add(device_airpods);
            btDevices.Add(disconnect);

            LoadSettings();

            log("Number of BT devices : " + btDevices.Count());
            log("current device name " + current_device.name);

            foreach (AudioDeviceClass d in btDevices)
            {
                log("available device " + d.name);

                if(d.name == current_device_name)
                {
                    log("select current device = " + d.name);
                    current_device = d;
                }
            }

            getRadioStations();
            log("Number of Stations  : " + stations.Count());

            getM3Ufiles();
            log("Number of M3U files : " + m3uFiles.Count());

            output = excecute("amixer", "-D bluealsa scontrols");
            logList(output);

            check_for_connected_devices();
 
            if (current_mode != application_mode.stopped)
            {
                log("*******************************************************");
                log("init : loading last playlist and play last station");
                if (device_connected == false) btConnect(current_device);
                output = excecute("mpc", " stop");
                logList(output);
                Thread.Sleep(350);
                output = excecute("mpc", "clear");
                logList(output);
                Thread.Sleep(350);
                output = excecute("mpc", "load " + m3uFiles[current_m3uIdx]);
                logList(output);
                Thread.Sleep(550);
                output = excecute("mpc", "play " + (current_station_index+1).ToString());
                logList(output);
                Thread.Sleep(350);
                setVolume(volume);
                log("*******************************************************");
            }
            Thread.Sleep(1500);
            //output = excecute("mpc", " play " + current_station_index + 1);
            //Thread.Sleep(350);
            //setVolume(volume);

            kBoard = new keyboard();
            kBoard.OnKeyEvent += new keyboard.OnKeyEventHandler(OnKeyPressed);
            kBoard.start();


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
            log("execute:" + cmd + " " + args);
            output = jonas.Process_util.process_exec_output(cmd, args);
            if (output!=null) logList(output);
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
            SaveSettings();
            log("Current Mode     :  " + current_mode);
        }


        /// <summary>
        /// check for already connected devices
        /// </summary>
        public void check_for_connected_devices()
        {
            log("Check for already connected devices");
            foreach (string s in output)
            {
                if (s.Contains("Bose"))
                {
                    current_device = device_bose;
                    device_connected = true;
                    log("connected is " + current_device.name);
                }

                if (s.Contains("WH-CH710N"))
                {
                    current_device = device_wch710;
                    device_connected = true;
                    log("connected is " + current_device.name);
                }

                if (s.Contains("WH-CH500"))
                {
                    current_device = device_wch500;
                    device_connected = true;
                    log("connected is " + current_device.name);
                }

                if (s.Contains("FY - R919"))
                {
                    current_device = device_holz;
                    device_connected = true;
                    log("connected is " + current_device.name);
                }
            }
            log("check done");

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
                //log(" No Stations. Now loading default playlist ...");
                //output = excecute("mpc","clear");
                //output = excecute("mpc", "load Deutschland");
                //getRadioStations();

                log(" No Stations. Please load playlist ...");
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
            display(0, "Bye Bye ...");
            log("Bye Bye ...");
        }


        /// <summary>
        /// restart bluetooth and mpd service and re-connects to bluetooth speaker
        /// </summary>
        public void mpd_restart()
        {
            log("****************** MPD RESTART ************************");
            btDisConnect(current_device);

            output = excecute("service", " bluetooth restart");
            Thread.Sleep(300);

            output = excecute("service", " mpd restart");
            Thread.Sleep(300);

            btConnect(current_device);

            //excecute(current_device.enable, " 0");
            //Thread.Sleep(1500);

            //output = excecute(current_device.enable, " 1");
            //Thread.Sleep(2500);

            //output = excecute(current_device.enable, " 1");
            //Thread.Sleep(2500);
            log("****************** MPD RESTART DONE *******************");
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
                btDisConnect(dev);
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// connect to a bluetooth device
        /// </summary>
        /// <param name="dev"></param>
        public void btConnectOld(AudioDeviceClass dev)
        {
            log(" btConnect to " + dev.name);
            timer.Suspend();
            output = excecute("mpc", " stop");
            output = excecute(dev.enable, " 1");
            Thread.Sleep(500);
            timer.Resume();
        }

        public void btConnect(AudioDeviceClass dev)
        {
            log(" btConnect to " + dev.name);
            timer.Suspend();
            output = excecute("mpc", " stop");
            excecute("systemctl", "stop mpd");
            excecute("bluetoothctl", "connect " + current_device.mac_address);
            logList(output);
            excecute("systemctl", "start mpd");
            Thread.Sleep(500);
            timer.Resume();
        }

        /// <summary>
        /// disconnect from a bluetooth device
        /// </summary>
        /// <param name="dev"></param>
        public void btDisConnectOld(AudioDeviceClass dev)
        {
            log(" btDisConnect from " + dev.name);
            output = excecute("mpc", " stop");
            output = excecute(dev.enable, " 0");
            Thread.Sleep(500);
        }

        /// <summary>
        /// disconnect from a bluetooth device
        /// </summary>
        /// <param name="dev"></param>
        public void btDisConnect(AudioDeviceClass dev)
        {
            log(" btDisConnect from " + dev.name);
            output = excecute("mpc", " stop");
            output = excecute("bluetoothctl", "disconnect " + current_device.mac_address); Thread.Sleep(500);
            logList(output);
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
            SaveSettings();
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
                displayReset();
                Display.Clear();
                Display.White();
                timer.Suspend();
                key = e.keycode;
                log("event key pressed: " + key);

                try
                {
                    switch (key)
                    {
                        case keys.LEFT:
                            {
                                if (current_mode == application_mode.normal || current_mode == application_mode.stopped)
                                {
                                    if (volume > 50) volume = volume - 5;
                                    output = excecute("mpc", "play " + (current_station_index + 1).ToString());
                                    display(0,"VOL-- " + volume);
                                    setVolume(volume);
                                }

                                if (current_mode == application_mode.menu1)
                                {
                                    display(0, "Stop");
                                    output = excecute("mpc", "stop");
                                    changeToMode(application_mode.stopped);
                                }

                                if (current_mode == application_mode.menu2)
                                {
                                    display(0, "RESTART MPD");
                                    mpd_restart();
                                    changeToMode(application_mode.normal);
                                    LcdDisplay.Display.Clear();
                                    display(0, "RESTART DONE");
                                }
                                timer.Resume();
                                break;
                            }

                        case keys.RIGHT:
                            {

                                if (current_mode == application_mode.normal || current_mode == application_mode.stopped)
                                {
                                    if (volume < 100) volume = volume + 5;
                                    output = excecute("mpc", "play " + (current_station_index + 1).ToString());
                                    display(0, "VOL++ " + volume);
                                    setVolume(volume);
                                }

                                if (current_mode == application_mode.select_station || current_mode == application_mode.stopped)
                                {
                                    current_station_index = next_station_index;
                                    display(0, "PLAY " + current_station_index);
                                    output = excecute("mpc", " stop ");
                                    Thread.Sleep(250);
                                    output = excecute("mpc", "play " + (current_station_index+1).ToString());
                                    Display.Clear();
                                    if (output != null) display(0, output[0]);
                                    SaveSettings();
                                    changeToMode(application_mode.normal);
                                }

                                if (current_mode == application_mode.menu1 || current_mode == application_mode.stopped)
                                {
                                    display(0, "Play");
                                    output = excecute("mpc", "play");
                                    changeToMode(application_mode.normal);
                                }

                                if (current_mode == application_mode.menu2)
                                {
                                    display(0, "Load M3U file ");
                                    display(2, m3uFiles[current_m3uIdx]);
                                    output = excecute("mpc", " stop");
                                    output = excecute("mpc", " clear");
                                    output = excecute("mpc", " load " + m3uFiles[current_m3uIdx]);
                                    getRadioStations();
                                    SaveSettings();
                                    changeToMode(application_mode.normal);
                                }

                                if (current_mode == application_mode.menu3)
                                {
                                    display(0, "Connect Device ");
                                    display(2, btDevices[next_btDevice].name);

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

                                }
                                timer.Resume();
                                break;
                            }

                        case keys.UP:
                            {
                                if (current_mode == application_mode.normal || current_mode == application_mode.select_station)
                                {
                                    next_station_index--;
                                    if (next_station_index < 0) next_station_index = stations.Count();
                                    changeToMode(application_mode.select_station);
                                    display(0, ">" +stations[next_station_index].name);
                                }

                                if (current_mode == application_mode.menu2)
                                {
                                    current_m3uIdx--;
                                    if (current_m3uIdx < 0) current_m3uIdx = m3uFiles.Count() - 1;
                                    log("current M3U:" + current_m3uIdx + " " + m3uFiles[current_m3uIdx]);
                                    display(0, "M3U:" + m3uFiles[current_m3uIdx]);
                                    display(2, "> select M3U");
                                }

                                if (current_mode == application_mode.menu3)
                                {
                                    next_btDevice--;
                                    if (next_btDevice < 0) next_btDevice = btDevices.Count() - 1;
                                    log("next BT device:" + btDevices[next_btDevice].name);
                                    display(0, btDevices[next_btDevice].name);
                                    display(2, "> select device");
                                }
                                timer.Resume();
                                break;
                            }

                        case keys.DOWN:
                            {
                                if (current_mode == application_mode.normal || current_mode == application_mode.select_station)
                                {
                                    next_station_index++;
                                    if (next_station_index > stations.Count()) next_station_index = 0;
                                    log("Station:" + next_station_index + " " + stations[next_station_index].name);
                                    current_mode = application_mode.select_station;
                                    display(0, ">" +stations[next_station_index].name);
                                }

                                if (current_mode == application_mode.menu2)
                                {
                                    current_m3uIdx++;
                                    if (current_m3uIdx > m3uFiles.Count() - 1) current_m3uIdx = 0;
                                    log("current M3U:" + current_m3uIdx + " " + m3uFiles[current_m3uIdx]);
                                    display(0, "M3U:" + m3uFiles[current_m3uIdx]);
                                    display(2, "> select M3U");
                                }

                                if (current_mode == application_mode.menu3)
                                {
                                    next_btDevice++;
                                    if (next_btDevice > btDevices.Count() - 1) next_btDevice = 0;
                                    log("next BT device:" + btDevices[next_btDevice].name);
                                    display(0, btDevices[next_btDevice].name);
                                    display(2, "> select device");

                                }
                                timer.Resume();
                                break;
                            }

                        case keys.MENU:
                            {

                                getRadioStations();
                                getM3Ufiles();

                                if (current_mode == application_mode.normal || current_mode == application_mode.stopped)
                                {
                                    changeToMode(application_mode.menu1);
                                    Display.Red();
                                    display(0, "MENU I");
                                    display(2, "STOP <> PLAY");
                                    break;
                                }

                                if (current_mode == application_mode.select_station || current_mode == application_mode.stopped)
                                {
                                    changeToMode(application_mode.normal);
                                    Display.Red();
                                    display(0, "MENU EXIT");
                                    break;
                                }

                                if (current_mode == application_mode.menu1 || current_mode == application_mode.stopped)
                                {
                                    changeToMode(application_mode.menu2);
                                    Display.Red();
                                    display(0, "MENU II: L=restart MPD");
                                    display(2, "Up/Dwn: select M3U file");
                                    break;
                                }

                                if (current_mode == application_mode.menu2)
                                {
                                    changeToMode(application_mode.menu3);
                                    Display.Red();
                                    display(0, "MENU III: Device");
                                    display(2, current_device.name);
                                    break;
                                }

                                if (current_mode == application_mode.menu3)
                                {
                                    changeToMode(application_mode.normal);
                                    hello();
                                    timer.Resume();
                                }
                            break;
                            }
                    }
                }

                catch (Exception ex)
                {
                    Display.Magenta();
                    log("Exception on Key press : \n\r" + ex.Message);
                    display(0, "Sorry, exception");
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
        public string enable = "udef";
        /// <summary>
        /// device name for bluealsa 
        /// </summary>
        public string bluealsa_name = "udef";
        /// <summary>
        /// BlueTooth MAC address
        /// </summary>
        public string mac_address = "udef";

        public AudioDeviceClass(string name, string enable, string bluealsa_name, string mac_address)
        {
            this.name = name;
            this.enable = enable;
            this.bluealsa_name = bluealsa_name;
            this.mac_address = mac_address;
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
        public const int stopped = 6;
    }

}
