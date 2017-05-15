using Enterprise.Models;
using System.Collections.Generic;

namespace Enterprise.Infrastructure
{
    public class AlgorithmAnalytic
    {
        public Analytic Analytic { get; }

        public List<double[]> GraphPoints { get; }

        public AlgorithmAnalytic(Analytic analytic, List<double[]> graphPoints)
        {
            Analytic = analytic;
            GraphPoints = graphPoints;
        }
    }
}