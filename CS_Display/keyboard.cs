using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Display2
{
    public class keyboard
    {

        public delegate void OnKeyEventHandler(object sender, KeyEventArgs e);

        /// <summary>
        /// send key code for further processing
        /// </summary>

        public event OnKeyEventHandler OnKeyEvent;


        private BackgroundWorker RxWorker = new BackgroundWorker();

        public keyboard()
        {

        }

        /// <summary>
        /// call start() once to start keyboar thread
        /// </summary>

        public void start()
        {
            Console.WriteLine("Start Keyboard Thread ");
            RxWorker.WorkerSupportsCancellation = true;
            RxWorker.DoWork += new DoWorkEventHandler(this.KeyBoardThread);
            RxWorker.RunWorkerAsync();

        }

        public void KeyBoardThread(object sender, DoWorkEventArgs e)
        {
            byte key;

            while (true)
            {
                Thread.Sleep(150);
                key = LcdDisplay.Display.readkey(false);

                if (key != 0)
                {
                    if (OnKeyEvent != null)
                    {
                        KeyEventArgs f = new KeyEventArgs(key);
                        OnKeyEvent(this, f);
                    }
                }
            }
        }


    }


    public class KeyEventArgs : EventArgs
    {

        public int keycode;

        public KeyEventArgs(int code)
        {
            this.keycode = code;
        }
    }

    public class keys
    {
        public const int MENU = 0x01;
        public const int LEFT = 0x10;
        public const int RIGHT = 0x02;
        public const int UP = 0x08;
        public const int DOWN = 0x04;

    }
}
