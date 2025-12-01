using System;

namespace StewardessPlanning
{
    public static class Program
    {
        // Parameters
        static readonly string[] Months = { "Jan", "Feb", "Mrt", "Apr", "Mei", "Jun" };
        static readonly double[] Demand = { 8000, 9000, 7000, 10000, 9000, 8000 };

        const double experiencedWorkerHours = 150.0; // uren beschikbaar per ervaren werknemer per maand
        const double trainingHours = 100.0;          // uren verlies per trainee in de opleidingsmaand
        const double experiencedWorkerCost = 3000.0;
        const double traineeCost = 500.0;
        const double quitRate = 0.10;                // 10% uitstroom
        const double initialExperiencedWorker = 60.0;

        // Beslissingsvariabele
        static double[] trainee = new double[6]; // initieel 0

        // Dynamische variabelen
        static double[] newExperiencedWorker = new double[6]; // werknemers die niet mee doen aan quitrate in dezelfde maand
        static double[] oldExperiencedWorker = new double[6];
        static double[] experiencedWorker    = new double[6];
        static double[] availableHours       = new double[6];
        static double[] monthlyCost          = new double[6];

        public static void Main()
        {
            // C5 (init): oldExperiencedWorker[Jan] = initialExperiencedWorker
            oldExperiencedWorker[0] = initialExperiencedWorker;

            // C6 (init): newExperiencedWorker[Jan] = 0
            newExperiencedWorker[0] = 0.0;

            // C4 (definitie): experiencedWorker[t] = oldExperiencedWorker[t] + newExperiencedWorker[t]
            updateExperiencedWorkers();

            // C1–C3 (domein): variabelen niet-negatief (optioneel afdwingen, nu niet aangeroepen)
            // validateNonNegative(trainee);
            // validateNonNegative(newExperiencedWorker);
            // validateNonNegative(oldExperiencedWorker);
            // validateNonNegative(experiencedWorker);
        }

        // Houd experiencedWorker consistent met old/new (C4)
        static void updateExperiencedWorkers()
        {
            for (int t = 0; t < experiencedWorker.Length; t++)
            {
                experiencedWorker[t] = oldExperiencedWorker[t] + newExperiencedWorker[t];
            }
        }

        // Optioneel: dwing niet-negativiteit af zonder fouten te gooien (C1–C3)
        static void validateNonNegative(double[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] < 0) arr[i] = 0;
            }
        }
    }
}