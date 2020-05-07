using Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Recorder : MonoBehaviour
{
    public Button recordButton;
    public Button stopButton;
    public Text recorded;


    public bool Recording = false;
    private List<float[]> inputs, outputs;
    private int inputSize, outputSize;
    // Start is called before the first frame update
    void Start()
    {
        recordButton?.onClick.AddListener(StartRecord);
        stopButton?.onClick.AddListener(FinishRecord);

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void StartRecord()
    {
        inputs = new List<float[]>();
        outputs = new List<float[]>();
        Recording = true;
    }
    public void AddRecord(float[] inputs, float[] outputs)
    {
        if (!Recording) return;
        this.inputs?.Add(inputs);
        this.outputs?.Add(outputs);
        if (recorded != null)
            recorded.text = $"Records count: {this.inputs.Count}";

    }
    public void FinishRecord()
    {
        Recording = false;
        WriteXML();
        //return null;

    }

    public void WriteXML()
    {
        System.Xml.Serialization.XmlSerializer writer =
            new System.Xml.Serialization.XmlSerializer(typeof(Record));
        Record record = new Record(inputs, outputs);

        var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"//CarRecord__{DateTime.Now.ToString("yyyyMMdd_hh_mm_ss")}.xml";
        System.IO.FileStream file = System.IO.File.Create(path);

        writer.Serialize(file, record);
        file.Close();
    }

    public void ReadXML(string FilePath)
    {
        if (!System.IO.File.Exists(FilePath)) throw new Exception("File didnt exist");
        System.Xml.Serialization.XmlSerializer reader =
            new System.Xml.Serialization.XmlSerializer(typeof(Record));
        System.IO.FileStream file = System.IO.File.OpenRead(FilePath);
        Record record = reader.Deserialize(file) as Record;
        this.inputs = record.inputs;
        this.outputs = record.outputs;
    }

    public void BreakeRecord()
    {
        if (!Recording) return;
        if (recorded != null) recorded.text = $"BREAKED!!!!!";
        inputs = null;
        outputs = null;
    }
}
