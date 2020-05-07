using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets
{
    [Serializable]
    public class Record
    {
        public List<float[]> inputs, outputs;

        public Record()
        {
        }

        public Record(List<float[]> inputs, List<float[]> outputs)
        {
            this.inputs = inputs;
            this.outputs = outputs;
        }
    }
}
