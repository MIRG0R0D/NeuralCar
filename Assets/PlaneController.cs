using Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class PlaneController : MonoBehaviour
{
    public List<GameObject> carList;
    public Button btnCreate, btnDrive, btnStop, btnSaveBest, btnDeleteCars, btnLoadCar;
    public InputField txtBestCarCount;
    public Button NextGenerationButton;
    public Text debugText;
    public Text statusText;

    public int CarCount = 200;
    public int BestCarPercent = 20;
    public int SecondsForGeneration = 120;

    public bool Evolving = false;
    

    public int Generation = 0;
    private Vector3 spawnPosition ;
    private GameObject defaultCar ;
    private int carsToTake;
    private bool printDebug = false;
    private Stopwatch stopwatch;
    

    // Start is called before the first frame update
    void Start()
    {
        btnCreate?.onClick.AddListener(CreateCar);
        btnDeleteCars?.onClick.AddListener(DeleteCars);
        btnDrive?.onClick.AddListener(CarsDrive);
        btnStop?.onClick.AddListener(CarsStop);
        btnSaveBest?.onClick.AddListener(SaveBest);
        btnLoadCar?.onClick.AddListener(LoadCars);
        NextGenerationButton?.onClick.AddListener(NextGeneration);
        spawnPosition = new Vector3( 0f, 1f, -140f);
        defaultCar = GameObject.Find("Car");
        defaultCar.GetComponent<CarBot>().CarIsActive(false);
        carList = null;
        carsToTake = (int)(CarCount / 100 * BestCarPercent);
        stopwatch = new Stopwatch();
    }


    #region BUTTON_FUNC
    private void CarsDrive()
    {
        AllCarsActive(true);
    }
    private void CarsStop()
    {
        AllCarsActive(false);
    }

    private void LoadCars()
    {
        FileSave fileSave = FileSave.DeserializeFromXmlFile(@"bestNeuronCars.txt");
        carList = fileSave.NetworkParamsList.Select(x =>
        {
            GameObject car = Instantiate(defaultCar, spawnPosition, new Quaternion(0f, 0f, 0f, 0f)) as GameObject;
            car.GetComponent<CarBot>().InitNeural(x);
            return car;
        }).ToList();
        Generation = fileSave.Generations;
        AllCarsActive(true);
    }

    private void DeleteCars()
    {
        if (carList == null || carList.Count == 0) return;
        Evolving = false;
        stopwatch.Reset();
        AllCarsActive(false);
        carList.ForEach(x => GameObject.Destroy(x));
        carList = new List<GameObject>();
    }

    private void SaveBest()
    {
        string bestCarCountString = txtBestCarCount.text;
        if (!int.TryParse(bestCarCountString, out int bestCarCount))
        {
            txtBestCarCount.text = "type correct car count";
            return; 
        }
        List<NetworkParams> neurals = carList.Select(x => x.GetComponent<CarBot>()).OrderByDescending(x => x.Fitnes).Take(bestCarCount).Select(x => x.neural.Serialize()).ToList();
        FileSave fileSave = new FileSave(neurals, Generation);
        fileSave.SaveToFile(@"bestNeuronCars.txt");
    }
    #endregion

    private void AllCarsActive(bool state)
    {
        if (carList == null || carList.Count == 0) return;
        carList.ForEach(x => x.GetComponent<CarBot>().CarIsActive(state));
        Evolving = state;

        if (state) stopwatch.Start();
        else stopwatch.Stop();

    }
    public void NextGeneration()
    {
        if (carList == null || carList.Count == 0) { UnityEngine.Debug.Log("NextGeneration empty" ); return; }
        
        //filling half of winner positions with survived
        List<CarBot> winnerList = carList.Select(x => x.GetComponent<CarBot>()).Where(x=>x.Running && x.Fitnes>0).OrderByDescending(x=>x.Fitnes).Take((int)(carsToTake*0.2)).ToList();
        //adding rest of the with max distance winners
        List<CarBot> winnerDistance = carList.Select(x => x.GetComponent<CarBot>()).OrderByDescending(x => x.Fitnes).Take(carsToTake - winnerList.Count ).ToList();
        winnerList.AddRange(winnerDistance);
        if (winnerList.Sum(x=>x.Fitnes) == 0)
        {
            UnityEngine.Debug.Log($"Evolving failure, restarting evolution. Generation: {Generation}");
            DeleteCars();
            CreateCar();
            return;
        }
        

        List <Neural> winnerNeural = winnerList.Select(x => x.neural).ToList();
        DeleteCars();
        
        foreach (var net in winnerNeural)
        {
            string neuralXmlString = net.Serialize().ToXML();
            List<GameObject> list = Enumerable.Range(0, (int)(100 / BestCarPercent)).Select(x => Instantiate(defaultCar, spawnPosition, new Quaternion(0f, 0f, 0f, 0f)) as GameObject).ToList();
            list.ForEach(x => {
                x.GetComponent<CarBot>().InitNeural(neuralXmlString);
                x.GetComponent<CarBot>().neural.Mutate(); 
            });
            carList.AddRange(list);
        }
        stopwatch.Restart();
        Generation++;
        AllCarsActive(true);
    }

    public void CreateCar()
    {
        carList = Enumerable.Range(0,CarCount).Select(x => Instantiate(defaultCar, spawnPosition, new Quaternion(0f, 0f, 0f, 0f)) as GameObject).ToList();
        carList.ForEach(x => x.GetComponent<CarBot>().InitNeural());
        stopwatch.Reset();
        AllCarsActive(true);
    }
    
    private void showStatus()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Generation: {Generation}");
        sb.AppendLine($"Elapsed time: {stopwatch.Elapsed.TotalSeconds} s.");
        sb.AppendLine($"Cars total/active: {carList.Count} / {carList.Select(x => x.GetComponent<CarBot>()).Where(x => x.Running).Count()}");
        
        statusText.text = sb.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (carList != null)
        {
            if (printDebug)
            {
                StringBuilder sb = new StringBuilder();
                carList.OrderByDescending(x => x.GetComponent<CarBot>().Fitnes).Take(20).ToList().ForEach(x =>
                {
                    CarBot carBot = x.GetComponent<CarBot>();
                    sb.AppendLine("dist: " + carBot.Fitnes.ToString() + " steering: " + carBot.Steering + " accel: " + carBot.Accel);
                });
                debugText.text = sb.ToString();
            }

            showStatus();
            if (Evolving)
            {
                if ((carList.Select(x => x.GetComponent<CarBot>()).Count(x => x.Running) == 0) && stopwatch.Elapsed.Seconds>5)
                    NextGeneration();
                if ((stopwatch.Elapsed.TotalSeconds >= SecondsForGeneration))
                    NextGeneration();
            }
        }
    }
}
