using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CassandraQuickStartSample
{
    /**
     * Data table entity class
    */
    public class Data
    {
        public int station_id { get; set; }
        public int identity_id { get; set; }
        public int temp { get; set; }
        public String state { get; set; }

        public Data(int station_id, int identity_id, int temp, String state)
        {
            this.station_id = station_id;
            this.identity_id = identity_id;
            this.temp = temp;
            this.state = state;
        }

        public override String ToString()
        {
            return String.Format(" {0} | {1} | {2} | {3}", station_id, identity_id, temp, state);
        }
    }
}