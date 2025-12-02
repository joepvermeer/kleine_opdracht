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
            // init: oldExperiencedWorker[Jan] = initialExperiencedWorker
            oldExperiencedWorker[0] = initialExperiencedWorker;

            // init: newExperiencedWorker[Jan] = 0
            newExperiencedWorker[0] = 0.0;

            // Defenitie: experiencedWorker[t] = oldExperiencedWorker[t] + newExperiencedWorker[t]
            updateExperiencedWorkers();

            updateDynamics();

            updateAvailableHours();

        }

        // Houd experiencedWorker consistent met old/new 
        static void updateExperiencedWorkers()
        {
            for (int t = 0; t < experiencedWorker.Length; t++)
            {
                experiencedWorker[t] = oldExperiencedWorker[t] + newExperiencedWorker[t];
            }
        }


        // trainees worden new ervaren, new ervaren worden old ervaren workers in t + 1  
        static void updateDynamics()
        {
            for (int t = 0; t < Months.Length - 1; t++)
            {
                oldExperiencedWorker[t + 1] = (1.0 - quitRate) * oldExperiencedWorker[t] + newExperiencedWorker[t];
                newExperiencedWorker[t + 1] = trainee[t];
            }
        }

        // beschikbare uren berekenen per maand 
        static void updateAvailableHours()
        {
            for (int t = 0; t < availableHours.Length; t++)
            {
                availableHours[t] = experiencedWorkerHours * experiencedWorker[t] - trainingHours * trainee[t];
            }
        }

        // kan waarschijnlijk weg, maar dit is om af te dwingen dat arrays niet-negatief zijn
        static void validateNonNegative(double[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] < 0) arr[i] = 0;
            }
        }
    }
}