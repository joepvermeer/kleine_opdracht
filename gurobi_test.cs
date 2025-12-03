using Gurobi;


// Define constants for small toy example
const int N = 10;
const int B = 30;
double[] a = [2, 3, 4, 5, 9, 7, 6, 4, 5, 3];
double[] c = [3, 4, 8, 8, 10, 13, 12, 7, 6, 5];


const string logFile = "gurobi.log";
// Remove existing logfile
File.Delete(logFile);
// Create new gurobi environment (this also reads the license information)
// The first argument tells the solver to write logs to the specified file.
var env = new GRBEnv(logFile);
env.Start();

var model = new GRBModel(env);

// Create array to store all the x variables
var x = new GRBVar[N];
for (var i = 0; i < N; i++) {
    // Then initialize each variable
    x[i] = model.AddVar(0, 1, 0, GRB.BINARY, $"x_{i}");
}

// Create a new linear expression that calculates the total weight
// NOTE: The use of a linear expression type means that the compiler
//       will return an error when we try something quadratic. Try it :)
var totalWeight = new GRBLinExpr();
for (var i = 0; i < N; i++) {
    totalWeight += a[i] * x[i];
}
// Add constraint to the model to limit the total weight
model.AddConstr(totalWeight <= B, "max weight restriction");

// Build linear expression to calculate total value
var totalValue = new GRBLinExpr();
for (var i = 0; i < N; i++) {
    totalValue += c[i] * x[i];
}
// Set the total value as objective and tell solver to maximize
model.SetObjective(totalValue, GRB.MAXIMIZE);

// Run the optimizer to find the optimal solution
model.Optimize();

// Printing of variables
// NOTE: This piece of code can be used for any Gurobi model (as long as it is
//       called `model`), so feel free to copy it to your own project.
Console.WriteLine();
Console.WriteLine($"Objective: {model.ObjVal}");
Console.WriteLine("Solution:");
foreach (var grbVar in model.GetVars()) {
    Console.WriteLine($"    {grbVar.VarName}: {grbVar.X}");
}


// Optional: Understanding the solution

// Collect indices of items in knapsack
var itemsInKnapsack = new List<int>();
for (var i = 0; i < N; i++) {
    if (x[i].X > 0.5) {
        itemsInKnapsack.Add(i);
    }
}
// Print the items in the knapsack
Console.WriteLine();
var itemsInKnapsackString = string.Join(", ", itemsInKnapsack.Select(x => x.ToString()));
Console.WriteLine($"Items in knapsack: {itemsInKnapsackString}");