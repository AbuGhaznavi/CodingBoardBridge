using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoardWriteServer
{
    /// <summary>
    /// A class for giving meaning to GPIO status signals.
    /// </summary>
    /// <remarks>
    /// Different boards have different way of returning their GPIO status signals when supported.
    /// This class unifies the meaning of these signals across all board types so that extra work
    /// does not need to be done outside of the IProgrammer.
    /// </remarks>
    public class GPIOStatus
    {
        /// <summary>
        /// Gets or sets whether an SFP module is absent.
        /// </summary>
        public bool MOD_ABS_SFP { get; set; }
        /// <summary>
        /// Gets or sets whether an XFP module is absent.
        /// </summary>
        public bool MOD_ABS_XFP { get; set; }
        /// <summary>
        /// Gets or sets whether a QSFP module is absent.
        /// </summary>
        public bool MOD_ABS_QSFP { get; set; }
        /// <summary>
        /// Gets or sets whether a CFP module is absent.
        /// </summary>
        public bool MOD_ABS_CFP { get; set; }
        /// <summary>
        /// Gets or sets whether a CFP2 module is absent.
        /// </summary>
        public bool MOD_ABS_CFP2 { get; set; }
        /// <summary>
        /// Gets or sets whether a CFP4 module is absent.
        /// </summary>
        public bool MOD_ABS_CFP4 { get; set; }

        /// <summary>
        /// Creates a new GPIOStatus object.
        /// </summary>
        /// <param name="sfp_abs">Whether an SFP module is absent.</param>
        /// <param name="xfp_abs">Whether an XFP module is absent.</param>
        /// <param name="qsfp_abs">Whether a QSFP module is absent.</param>
        /// <param name="cfp_abs">Whether a CFP module is absent.</param>
        /// <param name="cfp2_abs">Whether a CFP2 module is absent.</param>
        /// <param name="cfp4_abs">Whether a CFP4 module is absent.</param>
        public GPIOStatus(bool sfp_abs = false, bool xfp_abs = false, bool qsfp_abs = false,
                          bool cfp_abs = false, bool cfp2_abs = false, bool cfp4_abs = false)
        {
            MOD_ABS_SFP = sfp_abs;
            MOD_ABS_XFP = xfp_abs;
            MOD_ABS_QSFP = qsfp_abs;
            MOD_ABS_CFP = cfp_abs;
            MOD_ABS_CFP2 = cfp2_abs;
            MOD_ABS_CFP4 = cfp4_abs;
        }
    }
}
