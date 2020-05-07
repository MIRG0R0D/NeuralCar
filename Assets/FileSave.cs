using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Assets
{
    [Serializable]
    public class FileSave
    {
        public List<NetworkParams> NetworkParamsList;
        public int Generations;

        public FileSave(List<NetworkParams> neurals, int generation)
        {
            this.NetworkParamsList = neurals;
            this.Generations = generation;
        }

        public FileSave()
        {
        }

        public void SaveToFile(string fileName)
        {
            string str = ToXML();
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.WriteLine(str);
            }
        }
        public static FileSave DeserializeFromXmlFile(string fileName)
        {
            string fromFile;
            using (StreamReader sr = new StreamReader(fileName))
            {
                fromFile = sr.ReadToEnd();
            }
            return LoadFromXMLString(fromFile);
        }


        private string ToXML()
        {
            using (var stringwriter = new System.IO.StringWriter())
            {
                var serializer = new XmlSerializer(this.GetType());
                serializer.Serialize(stringwriter, this);
                return stringwriter.ToString();
            }
        }

        private static FileSave LoadFromXMLString(string xmlText)
        {
            using (var stringReader = new System.IO.StringReader(xmlText))
            {
                var serializer = new XmlSerializer(typeof(FileSave));
                return serializer.Deserialize(stringReader) as FileSave;
            }
        }
    }
}
