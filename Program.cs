using System;
using Gurobi;

namespace StewardessPlanning
{
    public static class Program
    {
        // Parameters
        static readonly string[] Months = { "Jan", "Feb", "Mrt", "Apr", "Mei", "Jun" };
        static readonly double[] Demand = { 8000, 9000, 7000, 10000, 9000, 8000 };

        const double experiencedWorkerHours = 150.0;
        const double trainingHours = 100.0;
        const double experiencedWorkerCost = 3000.0;
        const double traineeCost = 500.0;
        const double quitRate = 0.10;
        const double initialExperiencedWorker = 60.0;
        const double feasibilityEpsilon = 1e-6;

        // State arrays
        static double[] trainee = new double[6];
        static double[] newExperiencedWorker = new double[6];
        static double[] oldExperiencedWorker = new double[6];
        static double[] experiencedWorker = new double[6];
        static double[] availableHours = new double[6];
        static double[] monthlyCost = new double[6];

        public static void Main()
        {
            try
            {
                // Gurobi setup
                using var env = new GRBEnv(true);
                env.Start();
                using var model = new GRBModel(env);

                int T = Months.Length;

                // Vars
                var traineeVar = new GRBVar[T];
                var oldVar     = new GRBVar[T];
                var newVar     = new GRBVar[T];
                var expVar     = new GRBVar[T];
                var availVar   = new GRBVar[T];

                for (int t = 0; t < T; t++)
                {
                    traineeVar[t] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"trainee[{t}]");
                    oldVar[t]     = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"old[{t}]");
                    newVar[t]     = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"new[{t}]");
                    expVar[t]     = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"exp[{t}]");
                    availVar[t]   = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, $"avail[{t}]");
                }

                // Initial conditions
                model.AddConstr(oldVar[0] == initialExperiencedWorker, "init_old0");
                model.AddConstr(newVar[0] == 0.0, "init_new0");

                // Dynamics, definitions, capacity and demand
                for (int t = 0; t < T; t++)
                {
                    model.AddConstr(expVar[t] == oldVar[t] + newVar[t], $"def_exp[{t}]");
                    model.AddConstr(availVar[t] == experiencedWorkerHours * expVar[t] - trainingHours * traineeVar[t], $"def_avail[{t}]");
                    model.AddConstr(availVar[t] >= Demand[t], $"cap[{t}]");

                    if (t < T - 1)
                    {
                        model.AddConstr(oldVar[t + 1] == (1.0 - quitRate) * oldVar[t] + newVar[t], $"dyn_old[{t + 1}]");
                        model.AddConstr(newVar[t + 1] == traineeVar[t], $"dyn_new[{t + 1}]");
                    }
                }

                // No trainees in last month (no benefit within horizon)
                model.AddConstr(traineeVar[T - 1] == 0.0, "noTrainLast");

                // Objective: minimize total cost
                GRBLinExpr obj = 0.0;
                for (int t = 0; t < T; t++)
                    obj += experiencedWorkerCost * expVar[t] + traineeCost * traineeVar[t];

                model.SetObjective(obj, GRB.MINIMIZE);
                model.Optimize();

                // Read solution, validate with simulation pipeline
                if (model.Status == GRB.Status.OPTIMAL || model.Status == GRB.Status.SUBOPTIMAL)
                {
                    var plan = new double[T];
                    for (int t = 0; t < T; t++)
                        plan[t] = ClampNonNegative(traineeVar[t].X);

                    SetTraineePlan(plan);
                    EvaluatePlan();

                    Console.WriteLine("Optimal trainee plan: [" + string.Join(",", plan) + "]");
                    Console.WriteLine("Feasible: " + allFeasible());
                    Console.WriteLine("TotalCost: " + totalCost().ToString("F2"));

                    Console.WriteLine("Per-month check:");
                    for (int t = 0; t < T; t++)
                        Console.WriteLine($"{Months[t]}: trainee={trainee[t]:0.###}, exp={experiencedWorker[t]:0.###}, avail={availableHours[t]:0.##}, demand={Demand[t]:0}, feasible={isFeasibleMonth(t)}");
                }
                else
                {
                    Console.WriteLine("Solver status: " + model.Status);
                }
            }
            catch (GRBException e)
            {
                Console.WriteLine("Gurobi error: " + e.Message);
            }
        }

        // Apply plan into state
        static void SetTraineePlan(double[] plan)
        {
            if (plan == null || plan.Length != trainee.Length)
                throw new ArgumentException("Lengte van trainees-plan ongeldig.");
            for (int i = 0; i < trainee.Length; i++)
                trainee[i] = plan[i] < 0 ? 0 : plan[i];
        }

        // Full recompute for current plan
        static void EvaluatePlan()
        {
            oldExperiencedWorker[0] = initialExperiencedWorker;
            newExperiencedWorker[0] = 0.0;

            updateDynamics();
            updateExperiencedWorkers();
            updateAvailableHours();
            updateMonthlyCost();
        }

        // exp = old + new
        static void updateExperiencedWorkers()
        {
            for (int t = 0; t < experiencedWorker.Length; t++)
                experiencedWorker[t] = oldExperiencedWorker[t] + newExperiencedWorker[t];
        }

        // Old/New dynamics
        static void updateDynamics()
        {
            for (int t = 0; t < Months.Length - 1; t++)
            {
                oldExperiencedWorker[t + 1] = (1.0 - quitRate) * oldExperiencedWorker[t] + newExperiencedWorker[t];
                newExperiencedWorker[t + 1] = trainee[t];
            }
        }

        // avail = 150*exp - 100*trainee
        static void updateAvailableHours()
        {
            for (int t = 0; t < availableHours.Length; t++)
                availableHours[t] = experiencedWorkerHours * experiencedWorker[t] - trainingHours * trainee[t];
        }

        // demand constraint check with epsilon
        static bool isFeasibleMonth(int t)
        {
            return availableHours[t] + feasibilityEpsilon >= Demand[t];
        }

        static bool allFeasible()
        {
            for (int t = 0; t < Months.Length; t++)
                if (!isFeasibleMonth(t)) return false;
            return true;
        }

        // cost per month
        static void updateMonthlyCost()
        {
            for (int t = 0; t < monthlyCost.Length; t++)
                monthlyCost[t] = experiencedWorkerCost * experiencedWorker[t] + traineeCost * trainee[t];
        }

        static double totalCost()
        {
            double sum = 0.0;
            for (int t = 0; t < monthlyCost.Length; t++)
                sum += monthlyCost[t];
            return sum;
        }

        // remove tiny negative noise from solver
        static double ClampNonNegative(double x)
        {
            return x < 0 && Math.Abs(x) < 1e-9 ? 0.0 : x;
        }
    }
}