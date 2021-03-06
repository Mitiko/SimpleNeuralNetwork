using System;
using System.Linq;

namespace SimpleNeuralNetwork
{
    public class Layer
    {
        public int NeuronCount { get; private set; }
        public string Id { get; private set; }
        public double LearningRate { get; set; }

        public Layer NextLayer { get; internal protected set; }
        public Layer PreviousLayer { get; internal protected set; }

        public double[] Input { get; internal protected set; }
        public double[] Output { get; internal protected set; }
        public double[] Error { get; internal protected set; }
        public double[][] Weights { get; internal protected set; }

        public Func<double[], double[]> ActivationFunction { get; set; }
        public Func<double[], double[][]> ActivationFunctionDerivative { get; set; }

        public Layer(int neurons, string id)
        {
            // TODO: Add activation function
            this.NeuronCount = neurons;
            this.Input = new double[neurons + 1];
            this.Error = new double[neurons];
            this.Id = id;
            this.ActivationFunction = ActivationFunctions.Tanh;
            this.ActivationFunctionDerivative = ActivationFunctions.DTanh;
        }

        public Layer(int neurons, string id, Func<double[], double[]> activationFunction, Func<double[], double[][]> activationFunctionDerivative)
        {
            // TODO: Add activation function
            this.NeuronCount = neurons;
            this.Input = new double[neurons + 1];
            this.Error = new double[neurons];
            this.Id = id;
            this.ActivationFunction = activationFunction;
            this.ActivationFunctionDerivative = activationFunctionDerivative;
        }

        public virtual void Setup()
        {
            if (this.NextLayer != null)
            {
                this.Output = new double[this.NextLayer.NeuronCount];
                this.Weights = new double[this.NeuronCount + 1][];
                var rnd = new Random();
                for (int i = 0; i < this.Weights.Length; i++)
                {
                    this.Weights[i] = new double[this.NextLayer.NeuronCount];
                    for (int j = 0; j < this.Weights[i].Length; j++)
                    {
                        this.Weights[i][j] = rnd.NextDouble() * 2 - 1;
                    }
                }
            }
        }

        public virtual double[] Forward()
        {
            // Multitply input vector with matrix
            this.Output = this.Input.Multiply(this.Weights);
            // Apply activation function
            return this.ActivationFunction.Invoke(this.Output);
        }

        public virtual void Backward()
        {
            var afd = this.ActivationFunctionDerivative.Invoke(this.Output);

            // Calculate error at previous layer
            this.Error = new double[this.Error.Length];

            if (afd.Length == 1)
            {
                this.Error = this.NextLayer.Error.HadamardMultiply(afd[0])
                    .TransposeMultiply(this.Weights).Take(this.NeuronCount).ToArray();
                this.Weights = (MatrixOperations) this.Weights +
                    this.LearningRate * (MatrixOperations)
                    this.Input.Multiply(this.NextLayer.Error.HadamardMultiply(afd[0]));
            }
            else
            {
                this.Error = this.NextLayer.Error.Multiply(afd)
                    .TransposeMultiply(this.Weights).Take(this.NeuronCount).ToArray();
                this.Weights = (MatrixOperations) this.Weights +
                    this.LearningRate * (MatrixOperations)
                    this.Input.Multiply(this.NextLayer.Error).Multiply(afd);
            }
        }
    }
}