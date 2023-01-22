using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using jonas;
//using LibPiGpio;

namespace Display2
{
    class Program
    {
        /* see linux/prctl.h */
        const int PR_SET_NAME = 15;

        [DllImport("libc")]
        static extern int prctl(int option, string arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);

        /// <summary>
        /// set linux process name used by linux "ps" command
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>

        public static bool SetProcessName(string name)
        {
            return prctl(PR_SET_NAME, name, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) == 0;
        }

        static void Main(string[] args)
        {
            Logger.init("/home/pi/mpd/radio.log");
            SetProcessName("mpd_radio");
            radio_main radio = new radio_main();
            radio.init();
            radio.application_run();
            radio.application_close();
        }

    }

}
