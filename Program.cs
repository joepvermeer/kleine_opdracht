using System;

namespace StewardessPlanning
{
    public static class Program
    {
        // aanmaken parameters
        static readonly string[] Months = { "Jan", "Feb", "Mrt", "Apr", "Mei", "Jun" };
        static readonly double[] Demand = { 8000, 9000, 7000, 10000, 9000, 8000 };

        const double experiencedWorkerHours = 150.0; // uren beschibaar per ervaren werknemer per maand
        const double trainingHours = 100.0;          // uren verlies per trainee in de opleidingsmaand
        const double experiencedWorkerCost = 3000.0; 
        const double traineeCost = 500.0;           
        const double quitRate = 0.10;                // 10% uitstroom
        const double initialExperiencedWorker = 60.0;

        // aanmaken beslissings variabelen 
        static double[] trainee = new double[6]; // initieel allemaal 0

        // aanmaken variabelen nodig voor dynamische berekeningen
        static double[] newExperiencedWorker = new double[6]; // werknermers die niet mee doen aan quitrate berekening 
        static double[] oldExperiencedWorker = new double[6]; 
        static double[] experiencedWorker    = new double[6]; 
        static double[] availableHours = new double[6];       
        static double[] monthlyCost    = new double[6]; 

        public static void Main()
        {
            Console.WriteLine("Stewardess Planning – Basisstructuur (zonder berekeningen)");
            Console.WriteLine("Maanden en vraag (uren):");
            for (int t = 0; t < Months.Length; t++)
            {
                Console.WriteLine($"  {Months[t]}: vraag = {Demand[t]} uur");
            }

            Console.WriteLine("\nParameters:");
            Console.WriteLine($"  experiencedWorkerHours = {experiencedWorkerHours}");
            Console.WriteLine($"  trainingHours          = {trainingHours}");
            Console.WriteLine($"  experiencedWorkerCost  = {experiencedWorkerCost}");
            Console.WriteLine($"  traineeCost            = {traineeCost}");
            Console.WriteLine($"  quitRate               = {quitRate:P0}");
            Console.WriteLine($"  initialExperienced     = {initialExperiencedWorker}");

            Console.WriteLine("\nVariabele placeholders (nog niet gevuld/berekend):");
            Console.WriteLine("  trainee[t]              (beslissingsvariabele)");
            Console.WriteLine("  newExperiencedWorker[t] (= trainee[t-1])");
            Console.WriteLine("  oldExperiencedWorker[t] (volgt uit dynamiek en uitstroom)");
            Console.WriteLine("  experiencedWorker[t]    (= old + new)");
            Console.WriteLine("  availableHours[t]       (= 150 * experienced - 100 * trainee)");
            Console.WriteLine("  monthlyCost[t]          (= 3000 * experienced + 500 * trainee)");

        }
    }
}