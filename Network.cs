using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeuralNetwork
{
    public class Network
    {
        #region Properties
        private List<Layer> Layers { get; set; }
        public double Alpha { get; set; } // Learning rate
        #endregion

        #region Configuration
        public Network(double learningRate, int[] layers, string[] names, string[] activationFunctions)
        {
            this.Layers = new List<Layer>();
            this.Alpha = learningRate;
            if(layers.Length < 2)
            {
                throw new Exception("Layers are less than 2!");
            }
            else if(layers.Length != names.Length)
            {
                throw new Exception("Arrays given do not match lengths. Check layers and names.");
            }
            else if(activationFunctions.Length == 1)
            {
                var af = activationFunctions[0];
                var temp = new string[layers.Length];
                for (int k = 0; k < temp.Length; k++)
                {
                    temp[k] = activationFunctions[0];
                }
                activationFunctions = temp;
            }
            else if (activationFunctions.Length != layers.Length)
            {
                throw new Exception("Number of layers and their activation functions don't match!" + 
                " Try matching the numbers, or leaving only one activation function for all hidden layers");
            }
            
            this.Layers.Add(new InputLayer(new LayerConfiguration(names[0], names[1], layers[0], layers[1], activationFunctions[0])));
            int i;
            for (i = 1; i < layers.Length - 1; i++)
            {
                this.Layers.Add(new HiddenLayer(new LayerConfiguration(names[i], names[i + 1], layers[i], layers[i + 1], activationFunctions[i])));
            }
            this.Layers.Add(new OutputLayer(new LayerConfiguration(names[i], "", layers[i], -1, activationFunctions[i])));

            this.Layers.ForEach(layer => layer.Network = this);
        }
        #endregion

        #region Methods
        public static bool IsLayerLast(Layer layer)
        {
            return layer.LayerConfiguration?.NextLayerNeuronCount == -1;
        }
        
        public static Layer GetLayerByName(string layerName, Network Network)
        {
            return Network.Layers.Where(l => l.Name == layerName).FirstOrDefault();
        }

        public static int GetIndexOfLayer(Layer layer)
        {
            return layer.Network.Layers.IndexOf(layer);
        }

        public static Layer GetPreviousLayer(Layer layer)
        {
            if(GetIndexOfLayer(layer) == 0)
            {
                return null;
            }

            return layer.Network.Layers[GetIndexOfLayer(layer) - 1];
        }

        public static void UpdateLayer(Layer layer)
        {
            layer.Network.Layers[GetIndexOfLayer(layer)] = layer;
        }

        public static double[] Multiply(Neuron[] neurons, double[][] weights)
        {
            // Activate neurons
            // Each neuron output * weight to each neuron in next layer
            // For loop in next layer and for loop in those neurons
            var result = new double[weights[0].Length];
        
            for (int i = 0; i < neurons.Length; i++)
            {
                neurons[i].Activate();
            }

            for (int j = 0; j < result.Length; j++)
            {
                for (int i = 0; i < neurons.Length; i++)
                {
                    result[j] += neurons[i].Output * weights[i][j];
                }
            }

            return result;
        }

        private void BackPropagate(Layer l, double[] expected = null)
        {
            // This is a recursive function
            // l is the current working layer
            // p is previous layer in net (next working layer)
            var p = GetPreviousLayer(l);
            if(p == null)
            {
                // GetPreviousLayerByName returns null only when there is no previous layer
                // Then we are all done!
                // This means we can simply return
                return;
            }
            int ln = l.Neurons.Length; // Number of neurons of current layer
            int pn = p.Neurons.Length; // Number of neurons of previous layer (next working layer)


            if(expected != null)
            {
                // Note: expected is not recursed; this parameter is not null only at last layer
                // Calculate error for the last layer (first to backpropagate)
                // We can assure this is the last layer, because its error is set to null
                for (int i = 0; i < ln - 1; i++)
                {
                    // We have -1 because the expected data does not cover the unused bias
                    l.Neurons[i].Error = expected[i] - l.Neurons[i].Output;
                }
            }

            // Calculate error at each neuron for next working layer
            // Error at each neuron i at layer l-1 = 
            // Sum of (Error at each neuron j at layer l *times* weight between i and j)
            // *times* derivative of the activation function of layer l-1 with input, the neuron output
            // at layer l-1
            // Where current working layer is l, and l-1 is layer p
            for (int i = 0; i < pn; i++)
            {
                double sum = 0;
                // Since this is not last layer, the last neuron is bias, and it is not responsible
                // for the error of the previous layer
                for (int j = 0; j < ln - 1; j++)
                {
                    sum += l.Neurons[j].Error * p.Weights[i][j] * l.Neurons[j].DeActivate();
                }
                
                p.Neurons[i].Error = sum;
            }


            // Calculate the deltas for the weights between current and next working layer
            for (int i = 0; i < pn; i++)
            {
                for (int j = 0; j < ln - 1; j++)
                {
                    // The delta in weight between neurons i and j in layers l-1 and l
                    // is output at i *times* error at j *times* alpha (learning rate)
                    double deltaWeight = p.Neurons[i].Output * l.Neurons[j].Error * l.Neurons[j].DeActivate() * this.Alpha;
                    // Now change weight
                    p.Weights[i][j] += deltaWeight;
                }
            }

            BackPropagate(p);
        }

        public double[] ForwardPropagate(double[] input)
        {
            // Give input to first layer
            for (int i = 0; i < Layers[0].Neurons.Length - 1; i++)
            {
                Layers[0].Neurons[i].Input = input[i];
            }

            var result = new double[this.Layers[this.Layers.Count - 1].Neurons.Length];

            for (int z = 0; z < this.Layers.Count; z++)
            {
                var layer = this.Layers[z];

                if (!IsLayerLast(layer))
                {
                    layer.Forward();
                }
                else
                {
                    result = layer.Output();
                }
            }

            return result;
        }

        public void Train(int epochs, bool randomizeData, Dictionary<double[], double[]> trainingSet, Action<int> actionAfterEachEpoch = null)
        {
            for (int i = 0; i < epochs; i++)
            {
                if (!randomizeData)
                {
                    foreach (var data in trainingSet)
                    {
                        // Forward propagate, beginning with the first layer
                        var result = ForwardPropagate(data.Key);
                        // Check for NaN
                        if (result.ToList().Contains(double.NaN))
                        {
                            Console.WriteLine($"Error: This results are now NaN. Epoch: {i}. Wait till next version for fix.");
                            return;
                        }
                        // Backporpagate, beginning with the last layer
                        BackPropagate(Layers[Layers.Count - 1], data.Value);
                    }
                }
                else
                {
                    var rnd = new Random();
                    foreach (var data in trainingSet)
                    {
                        var r = rnd.Next(0, trainingSet.Count);
                        // Forward propagate, beginning with the first layer
                        var result = ForwardPropagate(trainingSet.ElementAt(r).Key);
                        // Check for NaN
                        if (result.ToList().Contains(double.NaN))
                        {
                            Console.WriteLine($"Error: This results are now NaN. Epoch: {i}. Wait till next version for fix.");
                            return;
                        }
                        // Backporpagate, beginning with the last layer
                        BackPropagate(Layers[Layers.Count - 1], trainingSet.ElementAt(r).Value);
                    }
                }

                // Just in case
                if(actionAfterEachEpoch != null)
                {
                    actionAfterEachEpoch(i);
                }
            }
        }

        public static Network Import(string filePath)
        {
            if(!filePath.Contains(".bnn"))
            {
                throw new Exception("File format must be .bnn! Short for Bob the Neural Network");
            }
            else if(filePath.Contains("ilovecats"))
            {
                throw new Exception("I love cats too!");
            }

            var text = File.ReadAllLines(filePath);
            if(text.Length < 4)
            {
                throw new Exception("Meta data was not provided");
            }

            var learningRate = double.Parse(text[0]);
            var l = text[1].Split(';');
            var layers = new int[l.Length];
            for (int i = 0; i < l.Length; i++)
            {
                bool conversionSuccessful = int.TryParse(l[i], out layers[i]);

                if(!conversionSuccessful)
                {
                    throw new Exception("Meta data was wrongly formatted");
                }
            }
            var names = text[2].Split(';');
            var hlaf = text[3].Split(';');
            var n = new Network(learningRate, layers, names, hlaf);

            for (int p = 0; p < n.Layers.Count - 1; p++)
            {
                // The last layer doesn't have weights
                for (int i = 0; i < n.Layers[p].Neurons.Length; i++)
                {
                    // There is no weight to the bias
                    for (int j = 0; j < n.Layers[p + 1].Neurons.Length - 1; j++)
                    {
                        string weight = text[4 + p].Split(' ')[i * (n.Layers[p + 1].Neurons.Length - 1) + j];
                        n.Layers[p].Weights[i][j] = double.Parse(weight);
                    }
                }
            }

            return n;
        }

        public void Export(string filePath)
        {
            if(!filePath.Contains(".bnn"))
            {
                throw new Exception("File format must be .bnn! Short for Bob the Neural Network");
            }
            else if(filePath.Contains("ilovecats"))
            {
                throw new Exception("I love cats too!");
            }

            var learningRate = this.Alpha.ToString();
            var layers = Layers.Select(l => (l.Neurons.Length - 1).ToString()).ToArray();
            var names = Layers.Select(l => l.Name).ToArray();
            var hlaf = Layers.Select(l => l.Neurons[0].ActivationFunction).ToArray();
            var text = new string[3 + Layers.Count];

            text[0] = learningRate;
            text[1] = string.Join(";", layers);
            text[2] = string.Join(";", names);
            text[3] = string.Join(";", hlaf);

            for (int p = 0; p < Layers.Count - 1; p++)
            {
                string t = "";
                // The last layer doesn't have weights
                for (int i = 0; i < Layers[p].Neurons.Length; i++)
                {
                    t += string.Join(" ", Layers[p].Weights[i]);
                    t += " ";
                }
                text[4 + p] = t;
            }

            File.WriteAllLines(filePath, text);
        }
        #endregion
    }
}
