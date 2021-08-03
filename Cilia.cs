using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace CiliaSDK
{
    public struct RGB
    {
        byte red;
        byte green;
        byte blue;
        /**
         * Initializes red green blue values from a string containing the values.
         * Each three characters are a value.
         * @param aRGBString containing the RGB values.
         */
        public RGB(string aRGBString)
        {
            red = byte.Parse(aRGBString.Substring(0,3));
            green = byte.Parse(aRGBString.Substring(3,3));
            blue = byte.Parse(aRGBString.Substring(6,3));
        }
        /**
         * Initilizes red green and blue with byte values.
         * @param aRed byte value.
         * @param aBlue byte value.
         * @param aGreen byte value.
         */
        public RGB(byte aRed, byte aBlue, byte aGreen)
        {
            red = aRed;
            green = aGreen;
            blue = aBlue;
        }
        /**
         * Overrides default ToString function for RGB.
         * @return string with the red green and blue ToString values formatted such that each value has 3 decimal places.
         */
        public override string ToString()
        {
            string iString = red.ToString("D3") + green.ToString("D3") + blue.ToString("D3");
            return iString;
        }
    }
    class Cilia
    {
        /*Member Variables*/
        //Serial port is used to index Cilia.
        private byte mSerialPort;
        private byte mSurroundPosition = 0;
        private string[] mSmells = new string[6];
        private RGB[] mLights = new RGB[6];
        /*Static Variables*/
        private static List<byte>[] sCiliaGroups = new List<byte>[GVS.NUMBER_OF_SURROUND_POSITIONS];
        private static List<byte> sActiveCilias = new List<byte>();
        /**
         * Default Constructor
         */
        Cilia()
        {
        }
        /**
         * Constructor sets up values including: mSerialPort, mSurroundPosition, mSmells, mLights.
         * @param aCilia that this cilia is attached to.
         * @param aInitializationValues string array containing initialization values.
         */
        public Cilia(byte aCilia, string[] aInitializationValues)
        {
            //store serial port
            mSerialPort = aCilia;
            //store surround position
            mSurroundPosition = byte.Parse(aInitializationValues[0]);
            //store smells and lights
            for(int i = 0; i < mSmells.Length; i++)
            {
                mSmells[i] = aInitializationValues[i + GVS.SMELLS_OFFSET];
                mLights[i] = new RGB(aInitializationValues[i + GVS.LIGHT_OFFSET]);
            }
        }
        /**
         * Returns light value for a light at a specific index as a string
         * @return string with formatted light value RRRGGGBBB
         */
        public string GetLightValue(int aLightIndex)
        {
            return mLights[aLightIndex].ToString();
        }
        /**
         * Sets a light value at a specified index to a specified value.
         * @param aLightIndex index of which light is being set.
         * @param aValue string value the light is being set to.
         */
        public void SetLightValue(int aLightIndex, string aLightValue)
        {
            mLights[aLightIndex] = new RGB(aLightValue);
        }
        /**
         * Sets the lists in mCiliaGroups to new byte lists.
         */
        public static void prepareGroups()
        {
            for (int cPI = 0; cPI < GVS.NUMBER_OF_SURROUND_POSITIONS; cPI++)
                sCiliaGroups[cPI] = new List<byte>();
        }
        /**
         * Sets what group a Cilia is in.
         * Removes the Cilia from any former groups it was in, stores its new group, places it in its new group, and sorts the group.
         * Also, makes sure the cilia is in the list of active cilias
         */
        public void SetCiliaPosition(byte aGroup)
        {
            //if possible remove from previous group.
            if (sCiliaGroups[mSurroundPosition].Contains(mSerialPort))
            {
                sCiliaGroups[mSurroundPosition].Remove(mSerialPort);
            }
            //assigne new group
            mSurroundPosition = aGroup;
            //add to group list and sortS
            sCiliaGroups[aGroup].Add(mSerialPort);
            sCiliaGroups[aGroup].Sort();
            //add as an active Cilia
            if (!sActiveCilias.Contains(mSerialPort))
            {
                sActiveCilias.Add(mSerialPort);
            }
        }
        /**
         * Remove Cilia from group and active cilias.
         * @param aGroup to remove the Cilia from.
         * @param aCilia to be removed.
         */
        public static void RemoveCiliaFromGroup(byte aGroup, byte aCilia)
        {
            if (sCiliaGroups[aGroup].Contains(aCilia))
            {
                sCiliaGroups[aGroup].Remove(aCilia);
            }
            if(sActiveCilias.Contains(aCilia))
            {
                sActiveCilias.Remove(aCilia);
            }
        }
        /**
         * Returns how many cilias are in a group.
         * @param aGroup to search.
         * @return int value of how many cilias are in a group.
         */
        public static int GetCountOfGroup(byte aGroup)
        {
            if (sCiliaGroups[aGroup] != null)
                return sCiliaGroups[aGroup].Count();
            else
                return 0;
        }
        /**
         * Returns which cilia is in a group at a specified index
         * @param aGroup to search.
         * @param aSmellIndex to return the value of.
         * @return byte value of what cilia was found.
         */
        public static byte GetCiliaInPositions(byte aGroup, byte aIndex)
        {
            return (byte)sCiliaGroups[aGroup][aIndex];
        }
        /**
         * Returns the surround position of a Cilia
         * @return byte value of what surround position a Cilia is in.
         */
        public byte GetCiliaPosition()
        {
            return mSurroundPosition;
        }
        
        /**
         * Returns a list of active Cilias.
         * @return byte list of active Cilias
         */
        public static List<byte> GetActiveCilias()
        {
            return sActiveCilias;
        }
        /**
         * Overrides the ToString method for Cilia Class.
         * @returns a comma separated version of the contents of the Cilia including serial port, surround position, smells, and light settings.
         */
        public override string ToString()
        {
            string toReturn = mSerialPort + "," + mSurroundPosition.ToString();
            for (int i = 0; i < mSmells.Length; i++)
            {
                toReturn += "," + mSmells[i];
            }
            for (int i = 0; i < mLights.Length; i++)
            {
                toReturn += "," + mLights[i].ToString();
            }
            return toReturn;
        }

        /**
         * An alternate ToString method meant for writing to file.
         * @returns a new line separated version of the contents of the Cilia including surround position, smells, and light settings.
         */
        public string ToStringForWritingToFile()
        {
            string toReturn = mSurroundPosition.ToString();
            for (int i = 0; i < mSmells.Length; i++)
            {
                toReturn += "\n" + mSmells[i];
            }
            for (int i = 0; i < mLights.Length; i++)
            {
                toReturn += "\n" + mLights[i].ToString();
            }
            return toReturn;
        }
        /**
         * Returns a list of the indexes a smell is found in.
         * Searches mSmells for what indexes contain a smell and returns the indexes as an int list.
         * @param aSmellValue we want the inexes of.
         * @return int list of what indexes a smell is stored in.
         **/
        public List<int> GetSmellIndexes(string aSmellValue)
        {
            List<int> smellIndexes = new List<int>();
            for (int i = 0; i < mSmells.Length; i++)
            {
                if (mSmells[i].Equals(aSmellValue))
                    smellIndexes.Add(i+1);
            }
            return smellIndexes;
        }
        /**
         * Sets a smell at a specified index to a smell
         * @param aSmellIndex we want to store the smell at.
         * @param aSmellValue string we want to store.
         */
        public void SetSmell(int aSmellIndex, string aSmellValue)
        {
            mSmells[aSmellIndex] = aSmellValue;
        }
    }
}
