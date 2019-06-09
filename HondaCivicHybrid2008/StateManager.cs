using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HondaCivicHybrid2008
{

    /// <summary>
    /// Class to control the state of the Arduino
    /// </summary>
    class StateManager
    {
        /// <summary>
        /// The red led state 
        /// </summary>
        public bool RedLightOn { get; set; }

        /// <summary>
        /// The green led state 
        /// </summary>
        public bool GreenLightOn { get; set; }

        /// <summary>
        /// The yellow led state 
        /// </summary>
        public bool YellowLightOn { get; set; }

        /// <summary>
        /// The proximity sensor state.
        /// </summary>
        public bool BodyDetected { get; set; }

        /// <summary>
        /// Initialize the state manager.
        /// </summary>
        public void Initialize()
        {
            RedLightOn = false;
            GreenLightOn = false;
            YellowLightOn = false;
            BodyDetected = false;
        }
    }
}
