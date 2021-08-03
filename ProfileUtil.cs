using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CiliaSDK
{
    class ProfileUtil
    {
        /*Member Variables*/
        private string mCiliaDirectory;
        private string mGameDirectory;
        private string mGameFolder = "Default";
        private string mSmellLibraryPath;
        private const string mAppDataDirectory = "C:/Windows/ServiceProfiles/LocalService/AppData/Roaming";//path to Windows Services appdata
        private const string mSmellLibraryName = "SmellLibrary.txt";
        private List<string> mSmellLibraryContentsConst = new List<string> { "Apple", "BahamaBreeze", "CleanCotton", "Leather", "Lemon", "Rose" };
        private List<string> mSmellLibraryContents = new List<string> { "Apple", "BahamaBreeze", "CleanCotton", "Leather", "Lemon", "Rose" };
        private CiliaManager mCiliaManager;
        private NetworkStream mStream;
        /*Static Class Variables*/
        private static ProfileUtil sInstance;

        /**
         * Returns singleton of ProfileUtil.
         * If the profile util instance doesn't exist create it and then return it.
         * @param aCiliaManager for initializing ProfileUtil
         * @param aNetworkStream for initializing ProfileUtil
         * @return ProfileUtil instance.
         */
        public static ProfileUtil GetProfileUtil(CiliaManager aCiliaManager, NetworkStream aNetworkStream)
        {
            if (sInstance == null)
            {
                sInstance = new ProfileUtil(aCiliaManager, aNetworkStream);
            }
            return sInstance;
        }
        /**
         * Constructor for ProfileUtil.
         * Stores CiliaManager and NetworkStream. Then
         */
        ProfileUtil(CiliaManager aCiliaManager, NetworkStream aNetworkStream)
        {
            mCiliaManager = aCiliaManager;
            mStream = aNetworkStream;
            /*Set the Cilia Directory Path from appdata path and cilia folder*/
            mCiliaDirectory = Path.Combine(mAppDataDirectory, "Cilia");
            /*Set Smell Library Path to a combination of the Cilia Appdata Directory and the Smell Library Name*/
            mSmellLibraryPath = Path.Combine(mCiliaDirectory, mSmellLibraryName);
            /*Set the current Game Folder name to the Default name*/
            mGameFolder = "Default";
            /*Set the current GameDirectory to a combination of the CiliaDirectory path and the game folder name*/
            mGameDirectory = Path.Combine(mCiliaDirectory, mGameFolder);
        }
        /**
         * Updates the network stream.
         * @param aNetworkStream to update the mStream with.
         */
        public void SetNetworkStream(NetworkStream aNetworkStream)
        {
            mStream = aNetworkStream;
        }
        /**
         * Loads a game profile.
         * <pre>
         * Loads a game profile and optionally creates a new one if the payload contains information about the profile.
         * Load Profile
         * !#LoadProfile|[GameProfile]
         * Create Profile
         * !#LoadProfile|[GameProfile],[surroundgroup],[Smell 1],[Smell 2],[Smell 3],[Smell4],[Smell5],[Smell6],[light1],[light2],[light3],[light4],[light5],[light6],[Any Number of Surround Positions]
         * </pre>
         * @param aProcessString for loading or creating profile.
         */
        public void LoadProfile(string aProcessString)
        {
            string ss = aProcessString.Split('|')[1];
            string[] sS = ss.Split(',');
            //sS[0] contains game folder sS may contain the rest of the info needed
            if (aProcessString.Split('|')[0].Contains("Force"))
                LoadGameProfile(sS[0], sS, true);
            else
                LoadGameProfile(sS[0], sS, false);
        }
        /**
         * Sends a list of the game profiles to the Control Pannel.
         */
        public void GetProfiles()
        {
            string[] directoryies = Directory.GetDirectories(mCiliaDirectory);

            string directoryListString = "[";
            for (int ai = 0; ai < directoryies.Length; ai++)
            {
                directoryListString += directoryies[ai].Substring(mCiliaDirectory.Length + 1, directoryies[ai].Length - (mCiliaDirectory.Length + 1)) + ",";
            }
            directoryListString += mGameFolder;
            directoryListString += "]\n";
            byte[] directoryBytes = System.Text.Encoding.UTF8.GetBytes(directoryListString);
            mStream.Write(directoryBytes, 0, directoryBytes.Length);
        }
        /**
         * Deletes all game profiles and recreates the default profile.
         */
        public void FactoryReset()
        {
            //System.IO.DirectoryInfo ciliaDirectoryInfo = new DirectoryInfo(CiliaDirectory);
            DeleteFolder(mCiliaDirectory);
            CreateDefaultGameProfile();
            string[] sS = { };//no parameters since loading an existing profile.
            LoadGameProfile("Default", sS, false);
            byte[] completeRefresh = { 99 };
            mStream.Write(completeRefresh, 0, 1);
            int i = 0;
        }
        /**
         * Deletes the currently selected game profile and if it is the default profile recreate it.
         */
        public void DeleteProfile()
        {
            DeleteFolder(mGameDirectory);
            if (mGameFolder.Equals("Default"))
            {
                CreateDefaultGameProfile();
            }
            string[] sS = { };//no parameters since loading an existing profile.
            LoadGameProfile("Default", sS, false);
            byte[] completeRefresh = { 99 };
            mStream.Write(completeRefresh, 0, 1);
        }
        /**
         * Creates the default game folder if it doesn't exist.
         */
        public void CreateDefaultGameProfile()
        {
            string comName = "";
            string ciliaContentsTemplate = "0\nLemon\nApple\nLeather\nCleanCotton\nBahamaBreeze\nRose\n000128064\n000128064\n000128064\n000128064\n000128064\n000128064";
            string surroundGroups = "FrontCenter,FrontLeft,SideLeft,RearLeft,RearCenter,RearRight,SideRight,FrontRight";

            if (!Directory.Exists(mCiliaDirectory))
            {
                Directory.CreateDirectory(mCiliaDirectory);
            }
            if (!Directory.Exists(mGameDirectory))
            {
                Directory.CreateDirectory(mGameDirectory);
            }

            System.IO.File.WriteAllText(Path.Combine(mGameDirectory, "SurroundGroups"), surroundGroups);

            if (!File.Exists(mSmellLibraryPath))
            {
                System.IO.File.WriteAllLines(mSmellLibraryPath, mSmellLibraryContentsConst.ToArray());
            }
            else
            {
                mSmellLibraryContents = new List<string>(System.IO.File.ReadAllLines(mSmellLibraryPath));
                if (mSmellLibraryContents.Count == 0)
                {
                    System.IO.File.WriteAllLines(mSmellLibraryPath, mSmellLibraryContentsConst.ToArray());
                    mSmellLibraryContents = new List<string>(System.IO.File.ReadAllLines(mSmellLibraryPath));
                }
            }
            for (int comI = 0; comI < GVS.MAX_NUMBER_OF_CILIAS; comI++)
            {
                comName = "COMPORT" + comI.ToString();
                if (!File.Exists(Path.Combine(mGameDirectory, comName)))
                {
                    System.IO.File.WriteAllText(Path.Combine(mGameDirectory, comName), ciliaContentsTemplate);
                }
                mCiliaManager.OverrideCilia((byte)comI, System.IO.File.ReadAllLines(Path.Combine(mGameDirectory, comName)));
            }
        }

        /**
        * Loades a Game Profile
        * Brings in all the set smells and surround groups etc for a particular game profile.
        * If it doesn't exist it gets created.
        * @param aGameFolder we want to load or create
        * @param aSS other parameters that will be used if we need to create the game profile.
        */
        public void LoadGameProfile(string aGameFolder, string[] aSS, bool forceUpdate)
        {
            mGameFolder = aGameFolder;
            string surroundGroups = "";
            //this function will check if a game profile exists profile is directory
            mGameDirectory = Path.Combine(mCiliaDirectory, mGameFolder);
            string ciliaContentsTemplate = "";
            if (!Directory.Exists(mGameDirectory))
            {
                Directory.CreateDirectory(mGameDirectory);
            }
            if (aSS.Length > 1)
            {
                //create template
                string sP = aSS[1];
                string s1 = aSS[2]; string s2 = aSS[3]; string s3 = aSS[4]; string s4 = aSS[5]; string s5 = aSS[6]; string s6 = aSS[7];
                string n1 = aSS[8]; string n2 = aSS[9]; string n3 = aSS[10]; string n4 = aSS[11]; string n5 = aSS[12]; string n6 = aSS[13];
                ciliaContentsTemplate = sP + "\n" + s1 + "\n" + s2 + "\n" + s3 + "\n" + s4 + "\n" + s5 + "\n" + s6 + "\n" +
                                                n1 + "\n" + n2 + "\n" + n3 + "\n" + n4 + "\n" + n5 + "\n" + n6;

                for (int i = 14; i < aSS.Length; i++)
                {
                    surroundGroups += aSS[i] + ",";
                }
                surroundGroups = surroundGroups.TrimEnd(',');
                //surroundGroups gets updated no matter what.
                System.IO.File.WriteAllText(Path.Combine(mGameDirectory, "SurroundGroups"), surroundGroups);
            }

            for (int comI = 0; comI < GVS.MAX_NUMBER_OF_CILIAS; comI++)
            {
                string comName = "COMPORT" + comI.ToString();
                //if contents dont exist write contents
                if ((!File.Exists(Path.Combine(mGameDirectory, comName))) || forceUpdate)
                {
                    System.IO.File.WriteAllText(Path.Combine(mGameDirectory, comName), ciliaContentsTemplate);
                }
                //load contents
                mCiliaManager.OverrideCilia((byte)comI, System.IO.File.ReadAllLines(Path.Combine(mGameDirectory, comName)));
                //ciliaManager.GetCiliaContents()[comI] = ;
            }
            //update what Cilias are in each group
            List<byte> activeCilias = Cilia.GetActiveCilias();
            for (int i = 0; i < activeCilias.Count(); i++)
            {
                mCiliaManager.SetGroup(activeCilias[i], mCiliaManager.GetCiliaPosition(activeCilias[i]));
            }
        }

        /**
         * Deletes a folder
         * @param folder name of folder we want to delete.
         */
        public void DeleteFolder(string aFolder)
        {
            for (int i = 0; i < 1000; i++)
            {
                try
                {
                    if (Directory.Exists(aFolder))
                    {
                        Directory.Delete(aFolder, true);
                    }
                    else
                        break;
                }
                catch (IOException e)
                {
                    string exception = e.Message;
                };
            }
        }
        /**
         * Returns path to current game profile.
         * @return string of game profile directory path
         */
        public string GetGameDirectory()
        {
            return mGameDirectory;
        }
        /**
         * Returns a list of the smells to the control pannel over TCP/IP.
         */
        public void GetSmellLibrary()
        {
            string smellsLibraryString = "[";
            for (int ai = 0; ai < mSmellLibraryContents.Count; ai++)
            {
                smellsLibraryString += mSmellLibraryContents[ai].ToString() + ",";
            }
            smellsLibraryString = smellsLibraryString.TrimEnd(',');
            smellsLibraryString += "]\n";
            byte[] smellBytes = System.Text.Encoding.UTF8.GetBytes(smellsLibraryString);
            mStream.Write(smellBytes, 0, smellBytes.Length);
        }
        /**
         * Updates the smell library to a new list of smells.
         * Saves list to file.
         * @param aSmells list of smells.
         */
        public void UpdateSmellLibrary(string[] aSmells)
        {
            mSmellLibraryContents = new List<string>(aSmells);
            System.IO.File.WriteAllLines(mSmellLibraryPath, mSmellLibraryContents.ToArray());
        }
        /**
         * Add list of smells to existing list of smells and then sorts the list.
         * Saves list to file.
         * @param aSmellsToAdd list of smells to add to existing list of smells.
         */
        public void AddToLibrary(string[] aSmellsToAdd)
        {
            //get a list of new smells to add to the old list of smells.
            List<string> SmellLibraryAditionalContents = new List<string>(aSmellsToAdd);
            //check to see if each smell is already in old list
            foreach (string newSmell in SmellLibraryAditionalContents)
            {
                if (!mSmellLibraryContents.Contains(newSmell))
                    mSmellLibraryContents.Add(newSmell);
            }
            mSmellLibraryContents.Sort();
            System.IO.File.WriteAllLines(mSmellLibraryPath, mSmellLibraryContents.ToArray());
        }
    }
}
