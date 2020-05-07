using Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class Neural : Component, IComparable<Neural>
{
    //todo serialize and deserialize this network to copy/save to file


    //todo change to UnityEngine.Random.Range
    private static System.Random random;
    private readonly float LEARNING_RATE = 0.1f;
    private readonly bool AddStaticHiddenNeurons = true;

    protected int[] layers; //layers
    protected float[][] neurons; //neuron matrix
    protected float[][][] weights; //weight matrix
    public float fitness;//fitness of the network


    private bool created = false;
    public bool IsCreated => created;

    public Neural()
    {

    }

    /// <summary>
    /// deep copy
    /// </summary>
    /// <param name="copyNetwork"></param>
    public Neural(Neural copyNetwork)
    {
        this.layers = copyNetwork.layers.ToArray();

        initNeurons();
        initWeights();
        CopyWeights(copyNetwork.weights);
        created = true;
    }

    public Neural(int[] layers) : this()
    {
        Create(layers);
        created = true;
    }
    public Neural(NetworkParams serialized)
    {
        if (created) throw new Exception("Network allready created");
        this.layers = serialized.layers;
        this.neurons = serialized.neurons;
        this.weights = serialized.weights;
        created = true;
    }

    public NetworkParams Serialize()
    {
        return new NetworkParams(layers, neurons, weights);
    }
    private void CopyWeights(float[][][] copyWeights)
    {
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; j < weights[i][j].Length; k++)
                {
                    weights[i][j][k] = copyWeights[i][j][k];
                }
            }
        }
    }

    /// <summary>
    /// initializes an neural networks with random weights
    /// </summary>
    /// <param name="Layers">layers of the neural network</param>
    private void Create(int[] Layers)
    {
        //copy of layers of thix network
        layers = Layers.ToArray();
        random = new System.Random(DateTime.UtcNow.Millisecond);//random number seedtnew Random(DateTime.UtcNow.Millisecond)
                                                               //generate matrix
        initNeurons();
        initWeights();
    }

    private void initNeurons()
    {
        //neuron initilization
        List<float[]> neuronList = new List<float[]>();
        //foreach(int layer in layers)//run through all layers
        for (int i = 0; i < layers.Length; i++)
        {
            int layerSize = layers[i];

            //add static neurons to hidden layers with no weights TO it
            if (AddStaticHiddenNeurons && i> 0 && i < layers.Length - 1)
                layerSize++;
            neuronList.Add(new float[layerSize]);//add layer to neuron list
        }
        neurons = neuronList.ToArray();//convert list to array

    }

    /// <summary>
    /// create weight matrix
    /// </summary>
    private void initWeights()
    {
        random = new System.Random(DateTime.UtcNow.Millisecond);
        List<float[][]> weightsList = new List<float[][]>(); //weights list which will later be converted 

        //iterate through all neurons that have a weight connection
        for (int i = 1; i < layers.Length; i++)
        {
            List<float[]> layerWeightList = new List<float[]>();//layer weight list for this current layer
            int neuronsInPreviousLayers = neurons[i - 1].Length;

            //iterate over all neurons in this current layer
            for (int j = 0; j < neurons[i].Length; j++)
            {
                //add static neurons to hidden layers with no weights TO it and set its value to 1
                if (AddStaticHiddenNeurons && j == neurons[i].Length - 1 && i> 0 && i < layers.Length - 1)
                {
                    neurons[i][j] = 1;       //todo move it to layers creation             
                    continue;
                }

                float[] neuronWeights = new float[neuronsInPreviousLayers];//neuron weights

                //set the weight randomly between -1 and 1
                for (int k = 0; k < neuronsInPreviousLayers; k++)
                {
                    //give random weight to neuron weights
                    neuronWeights[k] = (float)random.NextDouble() - 0.5f;
                }

                layerWeightList.Add(neuronWeights);//add neuron weights of this current layer to layer weights
            }
            weightsList.Add(layerWeightList.ToArray());//add this layer weights converted into 2D array into weights list
        }
        weights = weightsList.ToArray();//convert to 3D array

    }


    /// <summary>
    /// feed forward this neural network with a given input array
    /// </summary>
    /// <param name="inputs">inputs to network</param>
    /// <returns></returns>
    public float[] FeedForward(float[] inputs)
    {
        //add inputs to neuron matrix
        for (int i = 0; i < inputs.Length; i++)
        {
            neurons[0][i] = inputs[i];
        }

        //start iterating layers from first HIDDEN layer [index=1]
        for (int i = 1; i < neurons.Length; i++)
        {
            //iterate neurons in current layer
            for (int j = 0; j < neurons[i].Length; j++)
            {
                //check if this neuron is not StaticHiddenNeurons
                bool isHiddenLayer = layers[i] != neurons[i].Length;
                bool lastInLayer = j == neurons[i].Length - 1;
                if (AddStaticHiddenNeurons && isHiddenLayer && lastInLayer)
                    continue;

                float value = 0;// = 0.25f; todo fix
                                //iterate all neurons in previous layer
                                //todo skip StaticNeuron
                for (int k = 0; k < neurons[i - 1].Length - 1; k++)
                {
                    //if (k > 35) { }
                    value += weights[i - 1][j][k] * neurons[i - 1][k];//sum of all weights connections of this neuron weight their values in previous layer
                }
                //setting neuron sum
                neurons[i][j] = (float)Math.Tanh(value);//hyperbolic tangent activation
            }
        }
        return neurons[neurons.Length - 1];//return output layer
    }

    public void BackPropagation(float[] expected)
    {
        if (expected.Length != neurons[neurons.Length - 1].Length) throw new Exception("Back propagation, expectedList.lengt != neuron output length");

        //iterate all layers with connections DESC
        for (int i = neurons.Length - 1; i > 0; i--)
        {
            //iterate each neuron
            for (int j = 0; j < neurons[i].Length; j++)
            {
                float error = 0;

                //check if this neuron is not StaticHiddenNeurons
                bool isHiddenLayer = layers[i] != neurons[i].Length;
                bool lastInLayer = j == neurons[i].Length - 1;
                if (AddStaticHiddenNeurons && isHiddenLayer && lastInLayer)
                    continue;

                if (i == weights.Length) //actual - expected for output layer
                    error = neurons[i][j] - expected[j];
                else //for hidden layer = SUM (weightDelta[i] * weight[nextLevel][i][this])
                {
                    for (int k = 0; k < neurons[i + 1].Length; k++)
                    {
                        //check if this neuron is not StaticHiddenNeurons
                        isHiddenLayer = layers[i + 1] != neurons[i + 1].Length;
                        lastInLayer = k == neurons[i + 1].Length - 1;
                        if (AddStaticHiddenNeurons && isHiddenLayer && lastInLayer)
                            continue;
                        error += neurons[i + 1][k] * weights[i][k][j];
                    }
                }
                //weightsDelta = error * sigmoid(x)dx
                //sigmoid(x)dx = sigmoid(x)*(1-sigmoid(x))
                float sigmoidDx = neurons[i][j] * (1 - neurons[i][j]);
                float weightsDelta = error * sigmoidDx;

                //iterate each weight
                for (int k = 0; k < weights[i - 1][j].Length; k++)
                {
                    //weight1 = weight1 - output1*weightsDelta*learningRate
                    weights[i - 1][j][k] = weights[i - 1][j][k] - neurons[i - 1][j] * weightsDelta * LEARNING_RATE;
                }


                neurons[i][j] = weightsDelta;
            }
        }
    }

    public void Mutate()
    {
        random = new System.Random(DateTime.UtcNow.Millisecond);
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    float weight = weights[i][j][k];

                    //mutate the value
                    float randomNumber = (float)random.NextDouble() * 1000f;
                    if (randomNumber <= 2)
                    {
                        //flip sigh of the weight
                        weight *= -1;
                    }
                    else if (randomNumber <= 4)
                    {
                        //pick random weight between -1 and 1
                        weight = (float)(random.NextDouble() * 2 - 1);
                    }
                    else if (randomNumber <= 6)
                    {
                        //randomly increase by 0% to 100%
                        weight *= (float)(random.NextDouble() + 1);
                    }
                    else if (randomNumber <= 8)
                    {
                        //randomly decrease by 0% to 100%
                        weight *= (float)(random.NextDouble());
                    }
                    weights[i][j][k] = weight;
                }
            }
        }
    }

    public int CompareTo(Neural other)
    {
        if (other == null) return 1;

        if (fitness > other.fitness) return 1;
        else if (fitness < other.fitness) return -1;
        else return 0;

    }
}

