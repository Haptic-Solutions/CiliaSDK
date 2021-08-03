using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiliaSDK
{
    /**
     * Used to send messages to the control pannel through a named pipe.
     * @param aMessage to send over the named pipe.
     */
    class CiliaPipeSender
    {
        public static void SendMessageByPipeToCP(string aMessage)
        {
            using (NamedPipeClientStream ciliaClient =
            new NamedPipeClientStream(".", "ciliaControlPannelPipe", PipeDirection.Out))
            {
                try
                {
                    ciliaClient.Connect(1000);
                    try
                    {
                        using (StreamWriter streamWriter = new StreamWriter(ciliaClient))
                        {
                            streamWriter.AutoFlush = true;
                            streamWriter.WriteLine(aMessage);
                            streamWriter.Close();
                        }
                    }
                    catch (IOException e)
                    {
                    }
                }
                catch
                {

                }
            }
        }
    }
}
