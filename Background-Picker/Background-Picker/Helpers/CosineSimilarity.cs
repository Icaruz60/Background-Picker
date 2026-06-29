namespace Helpers
{
    public class CosineSimilarity
    {
        public static double Calculate(float[] X, float[] Y)
        {
            double dot = 0;
            for (int i = 0; i < X.Length; i++)
            {
                dot += X[i] * Y[i];
            }
            double mag1 = Math.Sqrt(X.Sum(x => x * x));
            double mag2 = Math.Sqrt(Y.Sum(y => y * y));

            return (dot / (mag1 * mag2));
        }
    }
}
