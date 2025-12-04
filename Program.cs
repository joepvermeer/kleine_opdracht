using System;
using Gurobi; 

namespace StewardessPlanning
{
    public static class Program
    {
        // Parameters
        static readonly string[] Months = { "Jan", "Feb", "Mrt", "Apr", "Mei", "Jun" };
        static readonly double[] Demand = { 8000, 9000, 7000, 10000, 9000, 8000 };

        const double feasibilityEpsilon = 1e-6;

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
            try
                {
                    using var env = new GRBEnv(true);
                    env.Start();

                    using var model = new GRBModel(env);

                    int T = Months.Length;

                    // Vars
                    var traineeVar = new GRBVar[T];
                    var oldVar = new GRBVar[T];
                    var newVar = new GRBVar[T];
                    var expVar = new GRBVar[T];
                    var availVar = new GRBVar[T];

                    for (int t = 0; t < T; t++)
                    {
                        traineeVar[t] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"trainee[{t}]"); 
                        oldVar[t]     = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"old[{t}]");
                        newVar[t]     = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"new[{t}]");
                        expVar[t]     = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"exp[{t}]");
                        availVar[t]   = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"avail[{t}]");
                    }

                    // Initialisatie
                    model.AddConstr(oldVar[0] == initialExperiencedWorker, "init_old0");
                    model.AddConstr(newVar[0] == 0.0, "init_new0");
                    model.AddConstr(traineeVar[T - 1] == 0.0, "noTrainLast"); 
                    

                    // Dynamiek en definities
                    for (int t = 0; t < T; t++)
                    {
                        // experienced definitie
                        model.AddConstr(expVar[t] == oldVar[t] + newVar[t], $"def_exp[{t}]");

                        // capaciteit
                        model.AddConstr(availVar[t] == experiencedWorkerHours * expVar[t] - trainingHours * traineeVar[t], $"def_avail[{t}]");

                        // vraagconstraint
                        model.AddConstr(availVar[t] >= Demand[t], $"cap[{t}]");

                        // dynamiek (voor t+1)
                        if (t < T - 1)
                        {
                            model.AddConstr(oldVar[t + 1] == (1.0 - quitRate) * oldVar[t] + newVar[t], $"dyn_old[{t+1}]");
                            model.AddConstr(newVar[t + 1] == traineeVar[t], $"dyn_new[{t+1}]");
                        }
                    }

                    // Objective: minimize kosten
                    GRBLinExpr obj = 0.0;
                    for (int t = 0; t < T; t++)
                    {
                        obj += experiencedWorkerCost * expVar[t] + traineeCost * traineeVar[t];
                    }
                    model.SetObjective(obj, GRB.MINIMIZE);

                    // Solve
                    model.Optimize();

                    if (model.Status == (int)GRB.Status.OPTIMAL || model.Status == (int)GRB.Status.SUBOPTIMAL)
                    {
                        var plan = new double[T];
                        for (int t = 0; t < T; t++)
                        {
                            var x = traineeVar[t].X;
                            // Zet minuscule negatieve naar 0 en voorkom -0
                            plan[t] = ClampNonNegative(x);
                        }
                        // Eventueel rond heel dicht bij integer naar integer als je dat wil voor presentatiedoeleinden:
                        // plan[t] = Math.Abs(plan[t] - Math.Round(plan[t])) < 1e-9 ? Math.Round(plan[t]) : plan[t];

                        SetTraineePlan(plan);
                        EvaluatePlan();

                        Console.WriteLine("Optimal trainee plan: [" + string.Join(",", plan) + "]");
                        Console.WriteLine("Feasible: " + allFeasible());
                        Console.WriteLine("TotalCost (sim): " + totalCost().ToString("F2"));
                        Console.WriteLine("Per-month check:");
                        for (int t = 0; t < Months.Length; t++)
                        {
                            Console.WriteLine($"{Months[t]}: trainee={trainee[t]:0.###}, exp={experiencedWorker[t]:0.###}, avail={availableHours[t]:0.##}, demand={Demand[t]:0}, feasible={isFeasibleMonth(t)}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Solver did not find an optimal solution. Status: " + model.Status);
                    }
                }
                catch (GRBException e)
                {
                    Console.WriteLine("Gurobi error: " + e.Message);
                }


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
            return availableHours[t] + feasibilityEpsilon >= Demand[t];
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
        static double ClampNonNegative(double x)
        {
            return x < 0 && Math.Abs(x) < 1e-9 ? 0.0 : x; // kleine negatieve naar 0
        }
        

    }
}