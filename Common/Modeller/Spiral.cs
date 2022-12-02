using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Modeller
{
    public class Spiral
    {
        public decimal length
        {
            get;
            set;
        }

        public decimal radiusEnd
        {
            get;
            set;
        }

        public decimal radiusStart
        {
            get;
            set;
        }
        public decimal rot
        {
            get;
            set;
        }
        public string spiType
        {
            get;
            set;
        }
        public decimal theta
        {
            get;
            set;
        }
        public decimal totalX
        {
            get;
            set;
        }
        public decimal tanLong
        {
            get;
            set;
        }
        public decimal tanShort
        {
            get;
            set;
        }
        public DXFLib.DXFPoint Start
        {
            get;
            set;
        }
        public DXFLib.DXFPoint PI
        {
            get;
            set;
        }
        public DXFLib.DXFPoint End
        {
            get;
            set;
        }
        
    }
}
