using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GAB.BatchServer.API.Models
{
    /// <summary>
    /// Represents an output entity
    /// </summary>
    public class Output
    {
        /// <summary>
        /// Id of the output
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OutputId { get; set; }

        /// <summary>
        /// Associated input for the output
        /// </summary>
        public Input Input { get; set; }

        /// <summary>
        /// Content of the output (currently the filename)
        /// </summary>
        [MaxLength(1024)]        
        public string Result { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Last modification date
        /// </summary>
        public DateTime ModifiedOn { get; set; }

        /// <summary>
        /// Container ID
        /// </summary>
        [MaxLength(256)]
        public string ContainerId { get; set; }

        /// <summary>
        /// Client version
        /// </summary>
        [MaxLength(25)]
        public string ClientVersion { get; set; }

        /// <summary>
        /// TIC Id
        /// </summary>
        [MaxLength(20)]
        public string TICId { get; set; }

        /// <summary>
        /// Sector
        /// </summary>
        public int Sector { get; set; }        

        /// <summary>
        /// Camera
        /// </summary>
        public int Camera { get; set; }

        /// <summary>
        /// CCD
        /// </summary>
        public int CCD { get; set; }

        /// <summary>
        /// RA
        /// </summary>
        public double RA { get; set; }

        /// <summary>
        /// Dec
        /// </summary>
        public double Dec { get; set; }

        /// <summary>
        /// TESS Mag
        /// </summary>
        public double TMag { get; set; }

        /// <summary>
        /// Is planet
        /// </summary>
        public double IsPlanet { get; set; }

        /// <summary>
        /// Is planet
        /// </summary>
        public double IsNotPlanet { get; set; }
        
        /// <summary>
        /// Probability
        /// </summary>
        public string Frequencies { get; set; }
    }
}
