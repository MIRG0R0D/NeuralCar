using Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Vehicles.Car;


public class CarBot : MonoBehaviour
{
    public bool learn = false, drivePlayer = false;
    [Header("objects")]
    private Vector3 startPosition, startRotation;
    public Text debugText;
    public bool Running  = false;
    public float Fitnes { get { return totalDistanceTravelled; } }


    [Range(-1f, 1f)]
    public float CarSpeed, SteerAngle;

    [Range(-1f, 1f)]
    public float Accel, Steering;

    public float timeSinceStart = 0f;

    [Header("Fitness")]
    public float overallFitness;
    public float distanceMultipler = 1.4f;
    public float avgSpeedMultiplier = 0.2f;
    public float sensorMultiplier = 0.1f;

    private CarController m_Car; // the car controller we want to use
    private Recorder recorder;
    private Vector3 lastPosition;
    private float totalDistanceTravelled;
    private float avgSpeed;

    private float[] sensors;

    public Neural neural = null;
    public bool printDebug=false;

    private void Awake()
    {
        startPosition = transform.position;
        lastPosition = transform.position;
        // get the car controller
        m_Car = GetComponent<CarController>();
        recorder = GetComponent<Recorder>();
        /*
        XmlSerializer xml = new XmlSerializer(typeof(Serialized));
        string filename = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"//neural1.xml";
        FileStream f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        Serialized serialized = xml.Deserialize(f) as Serialized;
        */
        
    }

    private Vector3 savedVelocity, savedAngularVelocity;
    /// <summary>
    /// freezing car method
    /// save and freeze its speed and velocity
    /// </summary>
    /// <param name="state"></param>
    public void CarIsActive(bool state) //true - freeze car; false - unfreeze
    {
        Running = state;
        enabled = state;
        if (state)
        {
            gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            gameObject.GetComponent<Rigidbody>().velocity = savedVelocity;
            gameObject.GetComponent<Rigidbody>().angularVelocity = savedAngularVelocity;
        }
        else
        {
            savedVelocity = gameObject.GetComponent<Rigidbody>().velocity;
            savedAngularVelocity = gameObject.GetComponent<Rigidbody>().angularVelocity;
            gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
            gameObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }

    public void InitNeural()
    {
        neural = new Neural(new int[] { 9, 7, 5, 4, 2 });
    }
    public void InitNeural (NetworkParams serialized)
    {
        neural = new Neural(serialized);
    }
    public void InitNeural(string xmlString)
    {
        NetworkParams serialized = NetworkParams.LoadFromXMLString(xmlString);
        InitNeural(serialized);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!Running) return; //if (!isCarInsideMap()) Reset();
        float steering;
        float accel;
        float handbrake;
        float speed = m_Car.CurrentSpeed / (m_Car.MaxSpeed + 10);
        float steer = m_Car.CurrentSteerAngle / 30;

        if (drivePlayer)
        {
            steering = CrossPlatformInputManager.GetAxis("Horizontal");
            accel = CrossPlatformInputManager.GetAxis("Vertical");
            handbrake = CrossPlatformInputManager.GetAxis("Jump");
            if (learn)
            {
                neural.BackPropagation(new float[] { steering, accel, handbrake });
            }
        }
        else
        {
            //updating sensors info and creating input layer
            float[] inputs = InputSensors().Concat(new float[] { speed, steer }).ToArray();
            float[] outputs = neural.FeedForward(inputs);
            steering = outputs[0];
            accel = outputs[1];
            handbrake = 0;// outputs[2];
        }
        
        CalculateFitness();

        if (printDebug)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("neural steering: " + steering);
            sb.AppendLine("neural accel + footbrake: " + accel);
            sb.AppendLine("neural handbrake: " + handbrake);
            sb.AppendLine("neural distance: " + totalDistanceTravelled);
            debugText.text = debugText.text + sb.ToString();
        }
        Accel = accel;
        Steering = steering;
        CarSpeed = speed;
        SteerAngle = steer;
        MoveCar(steering, accel, accel, handbrake);
    }

    public void Reset()
    {
        timeSinceStart = 0f;
        totalDistanceTravelled = 0f;
        avgSpeed = 0f;
        lastPosition = startPosition;
        overallFitness = 0f;
        transform.position = startPosition;
        transform.eulerAngles = startRotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //totalDistanceTravelled = totalDistanceTravelled/1.5f;
        //Running = false;
        CarIsActive(false);
        recorder.BreakeRecord();
    }


    private float[] InputSensors()
    {
        StringBuilder sb = new StringBuilder();
        float rayLength = 50;
        List<float> dist = new List<float>();

        Vector3 lift = new Vector3(0, 1.45f, 0);

        foreach (int i in Enumerable.Range(0, 7))
        {
            Vector3 direction = Quaternion.AngleAxis(i * 30 - 90, transform.up) * transform.forward * rayLength;
            Ray ray = new Ray(transform.position + lift, direction + lift);
            Physics.Raycast(ray, out RaycastHit hit);            

            bool isHit = hit.distance != 0 && hit.distance < rayLength;
            float distance = isHit ? (rayLength - hit.distance) / rayLength : 0;            
            dist.Add(distance);

            sb.AppendLine($"{i * 30}rad  hit:{isHit} dist:{distance}");
            Debug.DrawRay(transform.position + lift, (isHit)? hit.point - transform.position: direction, (isHit) ? Color.red : Color.green);
        }
        if (printDebug)
        {
            debugText.text = sb.ToString();
        }
        return dist.ToArray();
    }
    /// <summary>
    /// calculating fitness of the network
    /// </summary>
    private void CalculateFitness()
    {
        if (Accel > 0)
            totalDistanceTravelled += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
    }

    private void MoveCar(float steering, float accel, float footBrake, float handBrake)
    {
        //bot to move a car
        m_Car.Move(steering, accel, footBrake, handBrake);
    }

    private bool isCarInsideMap()
    {
        return transform.position.x > 0 &&
            transform.position.x < 100 &&
            transform.position.z > 0 &&
            transform.position.z < 100 &&
            transform.position.y > 0 &&
            transform.position.y < 30;
    }

}
