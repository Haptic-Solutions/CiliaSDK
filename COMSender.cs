using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CiliaSDK
{
    class COMSender
    {
        /*Static Variable*/
        private static COMSender sInstance;
        /*Member Variables*/
        private SerialPort[] mCOMX = new SerialPort[GVS.MAX_NUMBER_OF_CILIAS];
        private Mutex[] mToken = new Mutex[GVS.MAX_NUMBER_OF_CILIAS];
        /**
         * Returns a singleton of COMSender.
         * Creates COMSender if it doesn't already exist.
         * @return COMSender instance.
         */
        public static COMSender GetComSender()
        {
            if (sInstance == null)
            {
                sInstance = new COMSender();
            }
            return sInstance;
        }
        /**
         * Constructor of COMSender.
         * Creates all the new serial ports and tokens.
         */
        private COMSender()
        {
            for (int cCI = 0; cCI < GVS.MAX_NUMBER_OF_CILIAS; cCI++)
            {
                /*Initialize to a list of new Serial Ports for communication with Cilias*/
                mCOMX[cCI] = new SerialPort();
                /*Initialize a mutex for each Cilia to make sure only one thread tries to talk with
                  a Cilia at a time*/
                mToken[cCI] = new Mutex();
            }
        }

        /**
         * Writes a string to a COM(Serial) Port.
         * Waits until mutex for a comport index of COMXi is free and
         * claims it using WaitOne. Then if the comport is open
         * writes the Input string message to the com port.
         * Finally uses ReleaseMutex function to release the comport.
         * @param COMXi index of COM(Serial) port to send a message to.
         * @param InputString to send to the COM(Serial) port.
         */
        public void WriteString(int COMXi, string InputString)
        {
            try
            {
                mToken[COMXi].WaitOne();
                if (mCOMX[COMXi].IsOpen)
                    mCOMX[COMXi].Write(InputString);
                mToken[COMXi].ReleaseMutex();
            }
            catch
            {

            }
        }
        /**
         * Returns a list of all serial port names.
         */
        public List<string> GetListOfPorts()
        {
            return new List<string>(SerialPort.GetPortNames());
        }
        /**
         * Sets up a Serial port.
         * Sets up a Serial port and inquires if it is a cilia by sending a C character. Returns the response of the inquiry.
         * @param aComInt to try to setup.
         * @return Response to inquiry.
         * @throws exception if cannot connect to port or fails to read/write to port.
         */
        public string SetupComport(int aComInt)
        {
            mCOMX[aComInt].PortName = "COM" + aComInt;
            mCOMX[aComInt].BaudRate = 19200;
            mCOMX[aComInt].ReadTimeout = 10;
            mCOMX[aComInt].WriteTimeout = 10;
            mCOMX[aComInt].Open();
            mCOMX[aComInt].DiscardInBuffer();
            mCOMX[aComInt].DiscardOutBuffer();
            mCOMX[aComInt].Write("C");
            Thread.Sleep(20);
            return mCOMX[aComInt].ReadLine();
        }
        /**
         * Closes a serial port.
         * @param aComInt port to close.
         */
        public void ClosePort(int aComInt)
        {
            try
            {
                if (mCOMX[aComInt].IsOpen)
                    mCOMX[aComInt].Close();
            }
            catch
            {
                //was open but failed to close
            }
        }

        /**
         * Get integer value of COM Port from string name.
         * removes COM from the name and casts to an int.
         * @param comName string
         * @return int value coresponding to the come name.
         */
        public int GetComInt(string comName)
        {
            try
            {
                return int.Parse(comName.Replace("COM", ""));
            }
            catch
            {
                return -1;
            }
        }
        /**
         * Returns if a serial port is open.
         * @param aComInt index to check.
         * @return bool value of whether the por is open.
         */
        public bool IsComOpen(int aComInt)
        {
            return mCOMX[aComInt].IsOpen;
        }
    }
}
