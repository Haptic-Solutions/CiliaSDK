using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CiliaSDK
{
    class GVS
    {
        //Constant Values
        public const uint MAX_NUMBER_OF_CILIAS = 256;
        public const uint NUMBER_OF_SURROUND_POSITIONS = 256;
        public const uint NUMBER_OF_SMELLS_PER_CILIA = 6;
        public const uint NUMBER_OF_LIGHTS_PER_CILIA = 6;
        public const uint SMELLS_OFFSET = 1;
        public const uint LIGHT_OFFSET = 7;
        public const uint SIZE_OF_CILIA_CONTENTS = 13;//surround position + smells + lights
    }
}
