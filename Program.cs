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
            // Voor testdoeleinden: evalueer een paar plannen (zonder output).
            // Inspecteer r0/r1/r2 in de debugger.
            var r0 = EvaluateForPlan(new double[] { 0, 0, 0, 0, 0, 0 });      // verwacht infeasible
            var r1 = EvaluateForPlan(new double[] { 6, 0, 0, 0, 0, 0 });      // check dynamiek en uren
            var r2 = EvaluateForPlan(new double[] { 8, 4, 0, 0, 0, 0 });      // vaak haalbaarder, hogere kosten

        }

        static void SetTraineePlan(double[] plan)
        {
            if (plan == null || plan.Length != trainee.Length)
                throw new ArgumentException("Lengte van trainees-plan ongeldig.");
            for (int i = 0; i < trainee.Length; i++)
            {
                trainee[i] = plan[i] < 0 ? 0 : plan[i];
            }
        }
        
        // Voert een volledige evaluatie uit voor het huidige trainees-plan:
        // C5/C6 init, C7 dynamiek, C4 experienced, C8 uren, en berekent kosten.
        static void EvaluatePlan()
        {
            // Init (C5, C6)
            oldExperiencedWorker[0] = initialExperiencedWorker;
            newExperiencedWorker[0] = 0.0;

            // Dynamiek (C7)
            updateDynamics();

            // Definitie experienced (C4)
            updateExperiencedWorkers();

            // Capaciteit (C8)
            updateAvailableHours();

            // Kosten (Iteratie 5)
            updateMonthlyCost();
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

        // Controleer of maand t haalbaar is: beschikbare uren >= vraag
        static bool isFeasibleMonth(int t)
        {
            return availableHours[t] >= Demand[t];
        }


        // Controleer of alle maanden haalbaar zijn voor het huidige trainee-plan
        static bool allFeasible()
        {
            for (int t = 0; t < Months.Length; t++)
            {
                if (!isFeasibleMonth(t)) return false;
            }
            return true;
        }

        static void updateMonthlyCost()
        {
            for (int t = 0; t < monthlyCost.Length; t++)
            {
                monthlyCost[t] = experiencedWorkerCost * experiencedWorker[t] + traineeCost * trainee[t];
            }
        }

        static double totalCost()
        {
            double sum = 0.0;
            for (int t = 0; t < monthlyCost.Length; t++)
            {
                sum += monthlyCost[t];
            }
            return sum;
        }


        // kan waarschijnlijk weg, maar dit is om af te dwingen dat arrays niet-negatief zijn
        static void validateNonNegative(double[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] < 0) arr[i] = 0;
            }
        }

        // Resultaatcontainer voor snelle tests
        public record PlanResult(bool AllFeasible, double TotalCost, double[] AvailableHours, double[] ExperiencedWorker);

        // Evalueer een extern meegegeven trainees-plan en geef kernresultaten terug (geen prints).
        static PlanResult EvaluateForPlan(double[] plan)
        {
            SetTraineePlan(plan);
            EvaluatePlan(); // init + dynamiek + experienced + uren + kosten
            return new PlanResult(
                AllFeasible: allFeasible(),
                TotalCost: totalCost(),
                AvailableHours: (double[])availableHours.Clone(),
                ExperiencedWorker: (double[])experiencedWorker.Clone()
            );
        }
    }
}