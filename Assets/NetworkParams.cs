using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace Assets
{

    /// <summary>
    /// Class for Neural serialization
    /// </summary>
    
        [Serializable]
    public class NetworkParams
    {
        public int[] layers; //layers
        public float[][] neurons; //neuron matrix
        public float[][][] weights; //weight matrix
        public NetworkParams() { }

        public NetworkParams(int[] layers, float[][] neurons, float[][][] weights)
        {
            this.layers = layers;
            this.neurons = neurons;
            this.weights = weights;
        }
        public string ToXML()
        {
            using (var stringwriter = new System.IO.StringWriter())
            {
                var serializer = new XmlSerializer(this.GetType());
                serializer.Serialize(stringwriter, this);
                return stringwriter.ToString();
            }
        }
        public static NetworkParams LoadFromXMLString(string xmlText)
        {
            using (var stringReader = new System.IO.StringReader(xmlText))
            {
                var serializer = new XmlSerializer(typeof(NetworkParams));
                return serializer.Deserialize(stringReader) as NetworkParams;
            }
        }
    }
}

