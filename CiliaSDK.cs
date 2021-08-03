using System;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Management;

namespace CiliaSDK
{
    class CiliaSDK
    {
        /*Member Variables*/
        /*Network Stream for sending TCP/IP messages*/
        private static NetworkStream mStream;
        /*Helper Classes*/
        private COMSender mComSender;
        private CiliaManager mCiliaManager;
        private ProfileUtil mProfileUtil;
        private bool mShutdown = false;
        static TcpClient mTcpClient;
        /**
         * Called when starting the Cilia Service.
         * Initializes many important variables.
         * Sets up a thread using method FakeCiliaEventHandler for handling when 
         * fake Cilias are attached.
         * Sets up callbacks with handler methods CiliaAttachedEvent and CiliaDettachedEvent
         * for when USB devices are plugged in or unplugged for adding Cilias.
         * Then sets up a TCP/IP server which accepts clients to get messages that it
         * does some initial parsing on and then sends on to the appropriate parsing 
         * functions such as SpecialProcess or ProcessMessage(as a new thread).
         * Finally does some cleanup when the service is stopped.
         */
        internal void StartSDK()
        {  
            /*Initialize some local function variable*/
            TcpListener server = null;//TCP_IP server
            Int32 port = 1995;//port we will be communicating on
            IPAddress localHost = IPAddress.Any;//Local host
            TcpClient client;//TCP_IP client the server will be accepting and gettign a stream from
            int i;//itterator
            /*Initialize helper classes*/
            mComSender = COMSender.GetComSender();
            mCiliaManager = CiliaManager.GetCiliaManager(mComSender);
            mProfileUtil = ProfileUtil.GetProfileUtil(mCiliaManager, mStream);
            mShutdown = false;

            Cilia.prepareGroups();
            /*Create the default game profile folder if it doesn't exist*/
            mProfileUtil.CreateDefaultGameProfile();
            /*Physical Cilias that were already attached before computer started*/
            //COMSender.RefreshPortsNonBlock();
            mCiliaManager.CiliasAttach(mComSender.GetListOfPorts());
            /*Setup and start a thread to receive when fake Cilias Attach or Detach*/
            CiliaPipeReceiver.SetCiliaManager(mCiliaManager);
            Thread attachFakeCilias = new Thread(CiliaPipeReceiver.FakeCiliaEventHandler);
            attachFakeCilias.Start();
            /*
             * Setup an interrupt to receive when physical Cilias are attached. 
             * Technically when any USB device is plugged in this will trigger
             * However we will filter for Cilias.
             */
            WqlEventQuery ciliaAttachedQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            ManagementEventWatcher ciliaAttachedWatcher = new ManagementEventWatcher(ciliaAttachedQuery);
            ciliaAttachedWatcher.EventArrived += new EventArrivedEventHandler(mCiliaManager.CiliaAttachedEvent);
            ciliaAttachedWatcher.Start();
            /* 
             * Setup an interrupt to receive when physical Cilias are detached.
             * Technically when any USB device is unplugged in this will trigger
             * However we will filter for Cilias.
             */
            WqlEventQuery ciliaDettachedQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            ManagementEventWatcher ciliaDettachedWatcher = new ManagementEventWatcher(ciliaDettachedQuery);
            ciliaDettachedWatcher.EventArrived += new EventArrivedEventHandler(mCiliaManager.CiliaDetachedEvent);
            ciliaDettachedWatcher.Start();
            /*String to hold messages received over network from games or control pannel*/
            String data = null;
            /*Used to hold tokenized versions of messages in data*/
            String[] dataSplit;
            /*Used to hold further tokenized versions of messages*/
            String[] dataSplit2;
            /*
             * Used to hold messages we construct that need to be processed
             * before sending them to a Cilia
             */
            String processString = "";
            /*
             * Main loop of the SDK for receiving messages and dispatching them
             * to threads for processing.
             */
            while (!mShutdown)
            {
                try
                {
                    /*Try to bind a TCP server to the local host at port 1995*/
                    server = new TcpListener(localHost, port);
                    
                    server.Start();
                    /*Buffer for receiving bytes from TCP client*/
                    Byte[] bytes = new Byte[10000];

                    while (!mShutdown)
                    {
                        /*Accept a client*/
                        //client = server.AcceptTcpClient();
                        bool first = true;
                        client = new TcpClient();
                        mTcpClient = null;
                        while (!mShutdown)
                        {
                            if (first)
                            {
                                CiliaSDK.GetaTCPClient(server);
                                first = false;
                            }
                            if (mTcpClient!= null)
                            {
                                client = mTcpClient;
                                break;
                            }
                            Thread.Sleep(1000);
                        }
                        /*Make sure data starts as null each loop*/
                        data = null;
                        /*Get the stream from the client*/
                        mStream = client.GetStream();
                        mProfileUtil.SetNetworkStream(mStream);
                        /*
                         * While we can still read messages of length greader than 0
                         * Keep reading messages from the client.
                         */
                        while ((i = mStream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            /*turn the bytes into a string and assigne them to data*/
                            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                            /*
                             * use data split to get the string tokenized by [ character
                             * which marks the beginning of a message
                             */
                            dataSplit = data.Split('[');
                            /*Go through substrings and see if we have any complete messages*/
                            foreach (String subString in dataSplit)
                            {
                                /*If we get a ] character that will signal we have a complete message*/
                                if (subString.Contains("]"))
                                {
                                    /*Go ahead and split the message and add put it in dataSplit2*/
                                    dataSplit2 = subString.Split(']');
                                    /*
                                     * Add on the first half of the message to processString to make
                                     * a complete message to be processed.
                                     */
                                    processString = processString + dataSplit2[0];
                                    /*
                                     * If the message has a !# at the beginning of it send it to the
                                     * SpecialProcess method for special processStrings
                                     */
                                    if (processString.Contains("!#"))
                                    {
                                        SpecialProcess(processString);
                                    }
                                    /*
                                     * Otherwise most of the time we will have a message we want to
                                     * process quickly nd we spin up a new ProcessMessage thread
                                     * and give it the processString to handle.
                                     */
                                    else
                                    {
                                        Thread processMessage = new Thread(ProcessMessage);
                                        processMessage.Start(processString);
                                    }
                                    /*
                                     * Finally before getting another message we put the
                                     * second half of that dataSplit2 as the start of a new message in processString
                                     */
                                    processString = dataSplit2[1];
                                }
                                /*If there is no ] character this must be a middle part of a message so we just append it to processString*/
                                else
                                {
                                    processString = processString + subString;
                                }
                            }
                        }
                        /*once the client closes we will attempt to close the stream and client objects*/
                        try
                        {
                            mStream.Close();
                            client.Close();
                        }
                        catch (ObjectDisposedException obe)
                        {
                            Console.WriteLine("ObjectDisposedException");
                        }
                        /*Then set the cilias to their default colors from ciliaContents and fan speeds of 0*/
                        for (int COMi = 0; COMi < 256; COMi++)
                        {
                            mCiliaManager.SetCiliaToDefaultState(COMi);
                        }
                    }
                }
                /*Handle various exceptions.*/
                catch (SocketException e)
                {
                    Console.WriteLine("socket exception");
                }
                catch (IOException e2)
                {
                    Console.WriteLine("IOException\n");
                }
                catch (ObjectDisposedException o)
                {
                    Console.WriteLine("ObjectDisposedException");
                }
                /*finally stop the server*/
                finally
                {
                    //client.Close();
                    try
                    {
                        server.Stop();
                    }
                    /*catch any disposal exception*/
                    catch (ObjectDisposedException o)
                    {
                        Console.WriteLine("ObjectDisposedException");
                    }
                }
            }
        }
        /**
         * asynchronously accepts a TCP Client connection and assigns it to mTcpClient.
         * @param aTCPListener to accept the client for.
         */
        static async void GetaTCPClient(TcpListener aTCPListener)
        {
            mTcpClient = await aTCPListener.AcceptTcpClientAsync();
        }

        /**
         * Sets mShutdown to true so that infinite loops can close.
         * Sets the shutdown on CiliaPipeReceiver too.
         */
        public void ShutDown()
        {
            mShutdown = true;
            CiliaPipeReceiver.ShutDown();
        }

        /**
         * Used to process messages which might take longer to execute.
         * We split these out into a separate function so that the core messages may be processed faster.
         * <pre>
         * Messages Handled:
         * SetSmell - Updates a specified cilia at a specified slot to a specified smell and then saves new cilia settings to file.
         * SetLights - Updates the default lighting values for a Cilia and saves the updated settings to file
         * SetGroup - Updates what group a Cilia is in and saves the updated settings to file.
         * Deluminate - Turns off the lights on a specified Cilia
         * GetSmells - returns all the smells in all the Cilias to the client
         * GetLibrary - returns the library of smells to the client.
         * UpdateLibrary - update the smell library and smell library file with smells received from client.
         * AddToLibrary - add any smells received from client that are not in the library to the library and save to file.
         * LoadProfile - loads up a specified game profile
         * GetProfiles - return a list of game profiles to client.
         * GetGroupNames - return a list of group names to client.
         * FactoryReset- delete all service appdata folders and then just recreate default game profile with default smells
         * DeleteProfile- delete a specified game profile. If it is the default profile it will be recreated with the default smells.
         * </pre>
         * @param processString to parse and handle.
         */
        void SpecialProcess(string processString)
        {
            string surroundGroups = "";
            String[] tSplit;
            if (processString.Contains("SetSmell"))
            {
                //processString = processString.Replace("SetSmell", "");

                tSplit = processString.Split('|');
                int tCiliaNumber = int.Parse(tSplit[1]);
                int tSlotNumber = int.Parse(tSplit[2])-1;
                String tValue = tSplit[3];
                mCiliaManager.SetSmellValue(tCiliaNumber,tSlotNumber,tValue);
                String tCOMNAME = "COMPORT" + tCiliaNumber.ToString();
                System.IO.File.WriteAllText(Path.Combine(mProfileUtil.GetGameDirectory(), tCOMNAME), mCiliaManager.GetCiliaToStringForWritingToFile(tCiliaNumber));
            }
            else if (processString.Contains("SetLights"))
            {
                //processString = processString.Replace("SetSmell", "");

                tSplit = processString.Split('|');
                int tCiliaNumber = int.Parse(tSplit[1]);
                String tLightValue1 = tSplit[2];
                String tLightValue2 = tSplit[3];
                String tLightValue3 = tSplit[4];
                String tLightValue4 = tSplit[5];
                String tLightValue5 = tSplit[6];
                String tLightValue6 = tSplit[7];
                mCiliaManager.SetLightValue(tCiliaNumber, 0, tLightValue1);
                mCiliaManager.SetLightValue(tCiliaNumber, 1, tLightValue2);
                mCiliaManager.SetLightValue(tCiliaNumber, 2, tLightValue3);
                mCiliaManager.SetLightValue(tCiliaNumber, 3, tLightValue4);
                mCiliaManager.SetLightValue(tCiliaNumber, 4, tLightValue5);
                mCiliaManager.SetLightValue(tCiliaNumber, 5, tLightValue6);
                String tCOMNAME = "COMPORT" + tCiliaNumber.ToString();
                System.IO.File.WriteAllText(Path.Combine(mProfileUtil.GetGameDirectory(), tCOMNAME), mCiliaManager.GetCiliaToStringForWritingToFile(tCiliaNumber));
            }
            else if (processString.Contains("SetGroup"))
            {
                //processString = processString.Replace("SetSmell", "");

                tSplit = processString.Split('|');
                byte tCiliaNumber = byte.Parse(tSplit[1]);
                byte surroundGroup = byte.Parse(tSplit[2]);
                //set contents to new group
                mCiliaManager.SetGroup(tCiliaNumber, surroundGroup);
                String tCOMNAME = "COMPORT" + tCiliaNumber.ToString();
                System.IO.File.WriteAllText(Path.Combine(mProfileUtil.GetGameDirectory(), tCOMNAME), mCiliaManager.GetCiliaToStringForWritingToFile(tCiliaNumber));
            }
            else if (processString.Contains("Deluminate"))
            {
                //processString = processString.Replace("SetSmell", "");
                try
                {
                    tSplit = processString.Split(',');
                    int tCiliaNumber = int.Parse(tSplit[1]);
                    mComSender.WriteString(tCiliaNumber, "N1000000000");
                    mComSender.WriteString(tCiliaNumber, "N2000000000");
                    mComSender.WriteString(tCiliaNumber, "N3000000000");
                    mComSender.WriteString(tCiliaNumber, "N4000000000");
                    mComSender.WriteString(tCiliaNumber, "N5000000000");
                    mComSender.WriteString(tCiliaNumber, "N6000000000");
                }
                catch
                {

                }
            }
            else if (processString.Contains("GetSmells"))
            {
                string smellsString = "[";
                for (uint ai = 0; ai < GVS.NUMBER_OF_SURROUND_POSITIONS; ai++)
                    for (byte ij = 0; ij < Cilia.GetCountOfGroup((byte)ai); ij++)
                    {
                        smellsString += mCiliaManager.GetCiliaToString(Cilia.GetCiliaInPositions((byte)ai, ij));
                        smellsString += "\n";
                    }
                smellsString = smellsString.TrimEnd('\n');
                smellsString += "]";
                byte[] smellBytes = System.Text.Encoding.UTF8.GetBytes(smellsString);
                mStream.Write(smellBytes, 0, smellBytes.Length);
            }
            else if (processString.Contains("GetLibrary"))
            {
                mProfileUtil.GetSmellLibrary();
            }
            else if (processString.Contains("UpdateLibrary"))
            {
                mProfileUtil.UpdateSmellLibrary(processString.Split('|')[1].Split(','));
            }
            else if (processString.Contains("AddToLibrary"))
            {
                mProfileUtil.AddToLibrary(processString.Split('|')[1].Split(','));
            }
            else if (processString.Contains("LoadProfile"))
            {
                mProfileUtil.LoadProfile(processString);
            }
            else if (processString.Contains("GetProfiles"))
            {
                mProfileUtil.GetProfiles();
            }
            else if (processString.Contains("GetGroupNames"))
            {
                surroundGroups = "";
                surroundGroups = System.IO.File.ReadAllText(Path.Combine(mProfileUtil.GetGameDirectory(), "SurroundGroups"));
                surroundGroups += "\n";
                byte[] surroundGroupsBytes = System.Text.Encoding.UTF8.GetBytes(surroundGroups);
                mStream.Write(surroundGroupsBytes, 0, surroundGroupsBytes.Length);
            }
            else if (processString.Contains("FactoryReset"))
            {
                mProfileUtil.FactoryReset();
            }
            else if (processString.Contains("DeleteProfile"))
            {
                mProfileUtil.DeleteProfile();
            }
        }
        /**
         * Handles core messages which need to be processed quickly and sent to the cilia
         * @param processStringObj message we need to parse.
         */
        void ProcessMessage(object processStringObj)
        {
            string processString = processStringObj.ToString();
            String[] processStringSplit;
            try
            {
                processStringSplit = processString.Split(',');

                if (processString.Contains("G<"))
                {
                    byte group = byte.Parse(processString.Split('<')[1].Split('>')[0]);
                    SubProcessMessage(group, processStringSplit, processString);
                }
                if (processString.Contains("All"))
                {
                    for (int ai = 0; ai < GVS.NUMBER_OF_SURROUND_POSITIONS; ai++)
                        SubProcessMessage(ai, processStringSplit, processString);
                }
                else if (processString.Contains("Specific"))
                {
                    string ciliaNumberSpecific = processStringSplit[0].Replace("Specific", "");
                    int tempCiliaNumber = int.Parse(ciliaNumberSpecific);
                    mComSender.WriteString(tempCiliaNumber, processStringSplit[1]);
                }
                else
                {
                    for (int ai = 0; ai < GVS.NUMBER_OF_SURROUND_POSITIONS; ai++)
                        RawSubProcessMessage(ai, processString);
                }

            }
            catch
            {

            }
        }
        /**
         * Handles some of the lower level portions of processing core messages
         * @param position surround position of cilia
         * @param processStringSplit information we need to parse
         * @param processString2 tells us if we are processing for a fan or a neopixel rgb LED
         */
        void SubProcessMessage(int position, string[] processStringSplit, string processString2)
        {
            int tempCiliaNumber = 0;
            for (int ij = 0; ij < Cilia.GetCountOfGroup((byte)position); ij++)
            {
                tempCiliaNumber = Cilia.GetCiliaInPositions((byte)position,(byte)ij);

                
                if (processString2.Contains(",N"))
                    mComSender.WriteString(tempCiliaNumber, processStringSplit[1]);
                else if (processString2.Contains("F,"))
                {
                    List<int> smellIndexes = mCiliaManager.ReturnSmellNumbers(tempCiliaNumber, processStringSplit[1]);
                    foreach(int smellIndex in smellIndexes)
                    {
                        mComSender.WriteString(tempCiliaNumber, "F" + smellIndex + processStringSplit[2]);
                    }
                }
            }
        }
        /**
         * Holdover for those who want to use some of the oldest messages from the earliest days of the SDK.
         * @param position we want to send the message to
         * @param processString2 we want to send in raw form to the Cilia
         */
        void RawSubProcessMessage(int position, string processString2)
        {
            int tempCiliaNumber = 0;
            for (int ij = 0; ij < Cilia.GetCountOfGroup((byte)position); ij++)
            {
                if (!mComSender.IsComOpen(tempCiliaNumber))
                    continue;
                tempCiliaNumber = Cilia.GetCiliaInPositions((byte)position,(byte)ij);
                mComSender.WriteString(tempCiliaNumber, processString2);
            }
        }
    }
}