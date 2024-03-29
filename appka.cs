/****** csharp ******/
//https://godbolt.org/z/4Wvbn7j6f
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Collections.Generic;

public class Relationship
{
    [JsonPropertyName("is")]
    public List<string>? Is { get; set; }
    [JsonPropertyName("can")]
    public List<string>? Can { get; set; }
    [JsonPropertyName("are")]
    public List<string>? Are { get; set; }
}

public class Universe
{
    [JsonPropertyName("entities")]
    public List<string> Entities { get; set; } = new List<string>();

    [JsonPropertyName("relation_types")]
    public List<string> RelationTypes { get; set; } = new List<string>();

    [JsonPropertyName("attributes")]
    public List<string> Attributes { get; set; } = new List<string>();

    [JsonPropertyName("operation")]
    public List<string> Operations { get; set; } = new List<string>();

    [JsonPropertyName("statements")]
    public Dictionary<string, Relationship> Statements { get; set; } = new Dictionary<string, Relationship>();

    public static Universe FromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        var result = JsonSerializer.Deserialize<Universe>(json, options);
        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize the universe JSON into the Universe object.");
        }
        return result;
    }

    public static Universe MergeUniverses(List<string> jsonUniverseStrings)
    {
        var mergedUniverse = new Universe();

        foreach (var json in jsonUniverseStrings)
        {
            Universe universe = FromJson(json);
            mergedUniverse.Entities.AddRange(universe.Entities);
            mergedUniverse.RelationTypes.AddRange(universe.RelationTypes);
            mergedUniverse.Attributes.AddRange(universe.Attributes);
            mergedUniverse.Operations.AddRange(universe.Operations);
            foreach (var statement in universe.Statements)
            {
                if (!mergedUniverse.Statements.ContainsKey(statement.Key))
                {
                    mergedUniverse.Statements[statement.Key] = statement.Value;
                }
                else
                {
                    // Merge the relationships if the key already exists
                    Relationship existingRelation = mergedUniverse.Statements[statement.Key];
                    existingRelation.Is = existingRelation.Is ?? new List<string>();
                    existingRelation.Can = existingRelation.Can ?? new List<string>();
                    existingRelation.Are = existingRelation.Are ?? new List<string>();

                    if (statement.Value.Is != null) existingRelation.Is.AddRange(statement.Value.Is);
                    if (statement.Value.Can != null) existingRelation.Can.AddRange(statement.Value.Can);
                    if (statement.Value.Are != null) existingRelation.Are.AddRange(statement.Value.Are);
                }
            }
        }

        // Removing duplicates
        mergedUniverse.Entities = mergedUniverse.Entities.Distinct().ToList();
        mergedUniverse.RelationTypes = mergedUniverse.RelationTypes.Distinct().ToList();
        mergedUniverse.Attributes = mergedUniverse.Attributes.Distinct().ToList();
        mergedUniverse.Operations = mergedUniverse.Operations.Distinct().ToList();

        return mergedUniverse;
    }

    // Assuming allStatements should be a combination of all entities and their attributes and operations
    public List<string> GetAllStatements()
    {
        List<string> allStatements = new List<string>();
        if (Statements == null)
        {
            throw new InvalidOperationException("Statements are empty");
        }
        foreach (var statement in Statements)
        {
            string entity = statement.Key;
            var relations = statement.Value;
            if (relations.Is != null)
            {
                allStatements.AddRange(relations.Is.Select(attr => $"{entity} is {attr}"));
            }
            if (relations.Can != null)
            {
                allStatements.AddRange(relations.Can.Select(action => $"{entity} can {action}"));
            }
            if (relations.Are != null)
            {
                allStatements.AddRange(relations.Are.Select(attr => $"{entity} are {attr}"));
            }
        }
        return allStatements;
    }
}

class Node
{
    private readonly List<string> statements;

    public Node(List<string> statements)
    {
        this.statements = statements ?? new List<string>();
    }

    public List<string> Share() => new List<string>(statements);
    public List<string> Share(int n) => statements.Take(n).ToList();
    public List<string> Share(int i, int j) => statements.Skip(i).Take(j - i + 1).ToList();

    public void Say() => Say(Share());
    public void Say(int n) => Say(Share(n));
    public void Say(int i, int j) => Say(Share(i, j));

    private void Say(List<string> sharedStatements)
    {
        foreach (var statement in sharedStatements)
        {
            Console.WriteLine(statement);
        }
    }

    protected int Compare(List<string> otherStatements)
    {
        // Using Intersect will give us only the elements that exist in both lists
        return statements.Intersect(otherStatements).Count();
    }

    public int Listen(List<string> otherStatements)
    {
        // The Listen method now returns the count of matches, as Compare does
        return Compare(otherStatements);
    }
}

public class Population
{
    private readonly List<Node> nodes;

    internal Population(List<Node> nodes)
    {
        this.nodes = nodes ?? new List<Node>();
    }

    public void Show()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Console.WriteLine($"Node {i + 1} statements:");
            nodes[i].Say();
            Console.WriteLine();
        }
    }
}

public class Friendship
{
    public int NodeA { get; }
    public int NodeB { get; }
    public int Grade { get; }
    public List<string> SharedStatements { get; }

    public Friendship(int nodeA, int nodeB, int grade, List<string> sharedStatements)
    {
        NodeA = nodeA;
        NodeB = nodeB;
        Grade = grade;
        SharedStatements = sharedStatements;
    }

    //public override string ToString()
    //{
    //    return $"(Node {NodeA + 1}, Node {NodeB + 1}): {Grade}\nShared Statements: {string.Join(", ", SharedStatements)}";
    //}
    private string ToPlainText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Node {NodeA + 1} and Node {NodeB + 1} has {Grade} connections");
        sb.AppendLine("their current topics are");
        foreach (var statement in SharedStatements)
        {
            sb.AppendLine($"\"{statement}\"");
        }
        return sb.ToString();
    }

    private string ToJson()
    {
        var netObj = new
        {
            who = new List<string> { $"Node {NodeA + 1}", $"Node {NodeB + 1}" },
            connections = Grade,
            topics = SharedStatements
        };
        return JsonSerializer.Serialize(netObj, new JsonSerializerOptions { WriteIndented = true });
    }

    private string ToYamlLike()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"- who:");
        sb.AppendLine($"    - Node {NodeA + 1}");
        sb.AppendLine($"    - Node {NodeB + 1}");
        sb.AppendLine($"  connections: {Grade}");
        sb.AppendLine($"  topics:");
        foreach (var statement in SharedStatements)
        {
            sb.AppendLine($"    - \"{statement}\"");
        }
        return sb.ToString().TrimEnd();
    }

    public string ToString(OutputFormat format)
    {
        return format switch
        {
            OutputFormat.PlainText => ToPlainText(),
            OutputFormat.Json => ToJson(),
            OutputFormat.YamlLike => ToYamlLike(),
            _ => throw new ArgumentException("Invalid output format")
        };
    }

    public override string ToString()
    {
        // Default to plain text if no format is specified.
        return ToPlainText();
    }
}

public class Graph
{
    public List<string> Nodes { get; set; }
    public List<Tuple<int, int>> Edges { get; set; }
    public int[,] AcceptanceMatrix { get; set; }

    public Graph()
    {
        Nodes = new List<string>();
        Edges = new List<Tuple<int, int>>();
        AcceptanceMatrix = new int[0, 0];
    }

    public Graph(List<string> nodes, int[,] acceptanceMatrix)
    {
        Nodes = nodes;
        AcceptanceMatrix = acceptanceMatrix;
        Edges = new List<Tuple<int, int>>();
    }

    // Helper method for DFS to find the longest path
    private void DFSForLongestPath(int node, bool[] visited, List<int> currentPath, ref List<int> longestPath)
    {
        visited[node] = true;
        currentPath.Add(node);

        foreach (var edge in Edges.Where(e => e.Item1 == node || e.Item2 == node))
        {
            int nextNode = edge.Item1 == node ? edge.Item2 : edge.Item1;
            if (!visited[nextNode])
            {
                DFSForLongestPath(nextNode, visited, currentPath, ref longestPath);
            }
        }

        if (currentPath.Count > longestPath.Count)
        {
            longestPath = new List<int>(currentPath);
        }

        visited[node] = false;
        currentPath.RemoveAt(currentPath.Count - 1);
    }

    public void PrintLongestPath()
    {
        List<int> longestPath = new List<int>();
        bool[] visited = new bool[Nodes.Count];
        for (int i = 0; i < Nodes.Count; i++)
        {
            DFSForLongestPath(i, visited, new List<int>(), ref longestPath);
        }

        Console.WriteLine("Longest path (in connectivity order):");
        foreach (var node in longestPath)
        {
            Console.Write($"{Nodes[node]} ");
        }
        Console.WriteLine();
    }

    // Helper method for DFS to detect cycles
    private bool DFSForCycles(int node, bool[] visited, int parent, List<int> currentPath, List<List<int>> cycles, HashSet<Tuple<int, int>> visitedEdges)
    {
        visited[node] = true;
        currentPath.Add(node);

        foreach (var edge in Edges.Where(e => e.Item1 == node || e.Item2 == node))
        {
            int nextNode = edge.Item1 == node ? edge.Item2 : edge.Item1;
            Tuple<int, int> edgeTuple = new Tuple<int, int>(Math.Min(node, nextNode), Math.Max(node, nextNode));

            if (!visited[nextNode])
            {
                if (!visitedEdges.Contains(edgeTuple))
                {
                    visitedEdges.Add(edgeTuple);
                    if (DFSForCycles(nextNode, visited, node, currentPath, cycles, visitedEdges))
                    {
                        return true; // Only return true if you want to stop at the first cycle found
                    }
                    visitedEdges.Remove(edgeTuple);
                }
            }
            else if (nextNode != parent && currentPath.Count > 2 && !visitedEdges.Contains(edgeTuple))
            {
                // Found a back edge indicating a cycle
                int cycleStartIndex = currentPath.IndexOf(nextNode);
                if (cycleStartIndex != -1)
                {
                    var cycle = currentPath.GetRange(cycleStartIndex, currentPath.Count - cycleStartIndex);
                    cycle.Add(nextNode); // Complete the cycle
                    cycles.Add(cycle);
                    return true; // Only return true if you want to stop at the first cycle found
                }
            }
        }

        currentPath.RemoveAt(currentPath.Count - 1);
        visited[node] = false;
        return false;
    }

    public void PrintCycles()
    {
        List<List<int>> cycles = new List<List<int>>();
        bool[] visited = new bool[Nodes.Count];
        HashSet<Tuple<int, int>> visitedEdges = new HashSet<Tuple<int, int>>();

        for (int i = 0; i < Nodes.Count; i++)
        {
            if (!visited[i])
            {
                DFSForCycles(i, visited, -1, new List<int>(), cycles, visitedEdges);
            }
        }

        Console.WriteLine($"The number of cycles: {cycles.Count}");
        Console.WriteLine("The list of cycles:");
        foreach (var cycle in cycles)
        {
            Console.WriteLine("(" + string.Join("<>", cycle.Select(node => Nodes[node])) + ")");
        }
    }

    public void PrintPseudoGraphically()
    {
        // (Implement the pseudo-graphical printing logic here, similar to PrintGraph method above)
    }

    public void PrintStandardWay()
    {
        foreach (var edge in Edges)
        {
            Console.WriteLine($"{Nodes[edge.Item1]} <-> {Nodes[edge.Item2]}");
        }
    }

    public void PrintStandardWayWithGrades()
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            for (int j = i + 1; j < Nodes.Count; j++)
            {
                if (AcceptanceMatrix[i, j] > 0)
                {
                    Console.WriteLine($"{Nodes[i]} <{AcceptanceMatrix[i, j]}> {Nodes[j]}");
                }
            }
        }
    }


    public void PrintAsSets()
    {
        Console.WriteLine($"Nodes{{{string.Join(",", Nodes)}}},");
        Console.WriteLine($"Edges{{{string.Join(",", Edges.Select(edge => $"({Nodes[edge.Item1]},{Nodes[edge.Item2]})"))}}}");
    }
}

public class Relations
{
    private readonly List<Node> nodes;
    private int[,] acceptanceMatrix;
    private List<Friendship> friendships;

    internal Relations(List<Node> nodes)
    {
        this.nodes = nodes;

        acceptanceMatrix = new int[nodes.Count, nodes.Count];
        BuildMatrix();

        friendships = new List<Friendship>();
        BuildFriendships();
    }

    private void BuildMatrix()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < nodes.Count; j++)
            {
                if (i != j) // A node does not compare to itself
                {
                    acceptanceMatrix[i, j] = nodes[j].Listen(nodes[i].Share());
                }
                else
                {
                    acceptanceMatrix[i, j] = 0; // No self-comparison
                }
            }
        }
    }

    private void BuildFriendships()
    {
        // Populate the list with friendships (i.e., non-zero grades)
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < nodes.Count; j++)
            {
                if (i < j && acceptanceMatrix[i, j] > 0)
                {
                    // Find shared statements between the two nodes
                    var sharedStatements = nodes[i].Share().Intersect(nodes[j].Share()).ToList();

                    // Add the friendship along with the shared statements
                    friendships.Add(new Friendship(i, j, acceptanceMatrix[i, j], sharedStatements));
                }
            }
        }

        // Sort the list by grade in descending order
        friendships.Sort((a, b) => b.Grade.CompareTo(a.Grade));
    }

    public void PrintMatrix(bool showZero = false)
    {
        Console.Write("   "); // Adjust spacing for row labels
        for (int j = 0; j < nodes.Count; j++)
        {
            Console.Write($"{j + 1,3}"); // Adjust the format for potentially larger numbers
        }
        Console.WriteLine();

        for (int i = 0; i < nodes.Count; i++)
        {
            Console.Write($"{i + 1,2}:");
            for (int j = 0; j < nodes.Count; j++)
            {
                if (acceptanceMatrix[i, j] != 0 || showZero)
                {
                    Console.Write($"{acceptanceMatrix[i, j],3}"); // Adjust the format for potentially larger numbers
                }
                else if (!showZero)
                {
                    Console.Write("   "); // Blank space for zero values
                }
            }
            Console.WriteLine();
        }
    }

    // Create a function to format as plain text
    private static string FormatFriendshipsAsPlainText(List<Friendship> friendships)
    {
        StringBuilder sb = new StringBuilder();

        foreach (var friendship in friendships)
        {
            sb.AppendLine($"Node {friendship.NodeA + 1} and Node {friendship.NodeB + 1} has {friendship.Grade} connections");
            sb.AppendLine("their current topics are");
            foreach (var statement in friendship.SharedStatements)
            {
                sb.AppendLine($"\"{statement}\"");
            }
            sb.AppendLine(); // Add an extra line for better readability
        }

        return sb.ToString();
    }

    // Create a function to serialize to JSON
    private static string SerializeFriendshipsToJson(List<Friendship> friendships)
    {
        var net = new Dictionary<string, object>();

        for (int i = 0; i < friendships.Count; i++)
        {
            var leg = new Dictionary<string, object>
            {
                ["who"] = new List<string> { $"Node {friendships[i].NodeA + 1}", $"Node {friendships[i].NodeB + 1}" },
                ["connections"] = friendships[i].Grade,
                ["topics"] = friendships[i].SharedStatements
            };
            net[$"leg {i + 1}"] = leg;
        }

        var root = new Dictionary<string, object> { ["net"] = net };
        return JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
    }

    //TODO: add native yaml
    // Assuming you have installed YamlDotNet and have the required using statements
    // Create a function to serialize to YAML
    /*
    Install-Package YamlDotNet
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;
    */
    //private static string SerializeFriendshipsToYaml(List<Friendship> friendships)
    //{
    //    var serializer = new YamlDotNet.Serialization.Serializer();
    //    var net = new List<Dictionary<string, object>>();
    //    for (int i = 0; i < friendships.Count; i++)
    //    {
    //        var leg = new Dictionary<string, object>
    //        {
    //            ["who"] = new List<string> { $"Node {friendships[i].NodeA + 1}", $"Node {friendships[i].NodeB + 1}" },
    //            ["connections"] = friendships[i].Grade,
    //            ["topics"] = friendships[i].SharedStatements
    //        };
    //        net.Add(leg);
    //    }
    //    var root = new Dictionary<string, object> { ["net"] = net };
    //    return serializer.Serialize(root);
    //}

    // Create a function to format as YAML-like text
    private static string FormatFriendshipsAsYamlLike(List<Friendship> friendships)
    {
        var sb = new StringBuilder();

        sb.AppendLine("net:");

        for (int i = 0; i < friendships.Count; i++)
        {
            sb.AppendLine($" - leg {i + 1}:");
            sb.AppendLine($"     who:");
            sb.AppendLine($"       - \"Node {friendships[i].NodeA + 1}\"");
            sb.AppendLine($"       - \"Node {friendships[i].NodeB + 1}\"");
            sb.AppendLine($"     connections: {friendships[i].Grade}");
            sb.AppendLine($"     topics:");
            foreach (var statement in friendships[i].SharedStatements)
            {
                sb.AppendLine($"        - \"{statement}\"");
            }
        }

        return sb.ToString();
    }

    public void Print()
    {
        // Report the friendships and their shared statements
        Console.WriteLine("Friendship grades and shared statements (sorted):");
        foreach (var friendship in friendships)
        {
            //Console.WriteLine(friendship.ToString());
            Console.WriteLine(friendship.ToString(OutputFormat.PlainText));
            //Console.WriteLine(friendship.ToString(OutputFormat.Json));
            //Console.WriteLine(friendship.ToString(OutputFormat.YamlLike));
            Console.WriteLine(); // Add an extra line for better readability
        }

        //Console.WriteLine(FormatFriendshipsAsPlainText(friendships));
        Console.WriteLine(SerializeFriendshipsToJson(friendships));
        Console.WriteLine(FormatFriendshipsAsYamlLike(friendships));
    }

    private static void Fill(char []arg, char filler = ' ')
    {
        var runtimeVersion = Environment.Version;
        if (runtimeVersion >= new Version(6, 0) || runtimeVersion <= new Version(7, 0))
        {
            Array.Fill(arg, ' ');
        }else{
            for (int i = 0; i < arg.Length; i++)
            {
                arg[i] = filler;
            }
        }
    }
    public void PrintGraph()
    {
        var nodes = Enumerable.Range(1, acceptanceMatrix.GetLength(0)).Select(n => n.ToString()).ToList();
        int nodeCount = nodes.Count;
        int canvasWidth = nodeCount * 4;
        char[] canvas = new char[canvasWidth];

        // Initialize the canvas with spaces
        Fill(canvas, ' ');

        // Print the connections based on the acceptanceMatrix
        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = i + 1; j < nodeCount; j++)
            {
                if (acceptanceMatrix[i, j] > 0)
                {
                    int x = i * 4;
                    int y = j * 4;

                    // Place the '+' character at the nodes
                    canvas[x] = '+';
                    canvas[y] = '+';

                    // Draw the horizontal line between nodes
                    for (int k = x + 1; k < y; k++)
                    {
                        canvas[k] = '-';
                    }

                    // Print the horizontal line
                    Console.WriteLine(new string(canvas));

                    // Reset the canvas except the vertical lines
                    for (int k = x + 1; k < y; k++)
                    {
                        canvas[k] = ' ';
                    }

                    // Draw the vertical lines
                    for (int k = 0; k < nodeCount; k++)
                    {
                        if (canvas[k * 4] == '+')
                        {
                            for (int l = 0; l < canvasWidth; l += 4)
                            {
                                if (canvas[l] == '+')
                                {
                                    canvas[l] = '|';
                                }
                            }
                        }
                    }
                }
            }
        }

        // Print the final nodes
        foreach (var node in nodes)
        {
            Console.Write(node.PadRight(4, ' '));
        }
        Console.WriteLine(); // Ensure there's a final newline at the end
    }

    public Graph GenerateGraph()
    {
        var graph = new Graph();
        for (int i = 0; i < nodes.Count; i++)
        {
            graph.Nodes.Add($"Node {i + 1}");
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                if (acceptanceMatrix[i, j] > 0)
                {
                    graph.Edges.Add(Tuple.Create(i, j));
                }
            }
        }

        return graph;
    }

    public Graph GenerateGraphWithGrades()
    {
        var nodes = Enumerable.Range(1, acceptanceMatrix.GetLength(0)).Select(n => $"Node {n}").ToList();
        return new Graph(nodes, acceptanceMatrix);
    }
}

public enum OutputFormat
{
    PlainText,
    Json,
    YamlLike
}

class Program
{
    private static Random random = new Random();

    public static List<Node> GenerateNodes(int numberOfNodes = 10, List<string>? statements = null, int minKnowledgeLimit = 2, int maxKnowledgeLimit = 6)
    {
        if(statements == null)
        {
            statements = new List<string>();
        }

        List<Node> nodes = new List<Node>();
        for (int i = 0; i < numberOfNodes; i++)
        {
            var randomStatements = statements.OrderBy(x => random.Next()).Take(random.Next(minKnowledgeLimit, maxKnowledgeLimit)).ToList();
            nodes.Add(new Node(randomStatements));
        }
        return nodes;
    }

    static void Main()
    {

        Console.WriteLine(Environment.Version);

        string jsonBasics = @"{
            ""entities"": [
                ""Grass"", ""Sun"", ""Love"", ""Water"", ""Sky"", ""Birds"", ""Snow"", ""Fire"", ""Trees"", ""Mountains"", ""Star"", ""Everything""
            ],
            ""relation_types"": [""is"", ""can"", ""are""],
            ""attributes"": [""bright"", ""green"", ""big"", ""organic"", ""wet"", ""blue"", ""cold"", ""hot"", ""tall"", ""high""],
            ""operation"": [""shine"", ""grow"", ""leak"", ""live"", ""sing""],
            ""statements"": {
                ""Grass"": { ""is"": [""green"", ""organic"", ""tall""], ""can"": [""grow""] },
                ""Sun"": { ""is"": [""bright"", ""hot"", ""big""], ""can"": [""shine""] },
                ""Love"": { ""is"": [""everything""] },
                ""Water"": { ""is"": [""wet""] },
                ""Sky"": { ""is"": [""blue""] },
                ""Birds"": { ""can"": [""fly"", ""sing""] },
                ""Snow"": { ""is"": [""cold""] },
                ""Fire"": { ""is"": [""hot""] },
                ""Trees"": { ""are"": [""tall""] },
                ""Mountains"": { ""are"": [""high""] },
                ""Star"": { ""is"": [""bright""] },
                ""Everything"": { ""is"": [""connected""] }
            }
        }";

        string jsonProgrammingSafetyByGoogle = @"{
          ""entities"": [
            ""Memory safety vulnerabilities"",
            ""Secure-by-Design approach"",
            ""Memory-unsafe languages"",
            ""Memory corruption vulnerabilities"",
            ""Safe Coding"",
            ""Memory-safe languages"",
            ""Rust"",
            ""Java"",
            ""Go"",
            ""C++"",
            ""Incremental transition"",
            ""Hardware security features"",
            ""Memory-safe ecosystem"",
            ""Cross-language attacks"",
            ""XSS attacks"",
            ""Software security"",
            ""Developer ecosystems""
          ],
          ""relation_types"": [
            ""is"",
            ""are"",
            ""can""
          ],
          ""attributes"": [
            ""widespread"",
            ""leading causes"",
            ""high assurance"",
            ""rigorous"",
            ""temporal safety"",
            ""gradual transition"",
            ""existing codebase"",
            ""safety improvements"",
            ""spatial safety"",
            ""memory usage"",
            ""inter-procedural calls"",
            ""better performance"",
            ""security bug"",
            ""tooling"",
            ""consumers of software"",
            ""security vulnerabilities"",
            ""responsibility"",
            ""secure-coding guidelines"",
            ""security threat"",
            ""coding error"",
            ""configuration change"",
            ""Secure-by-Design approach"",
            ""developer ecosystems"",
            ""safety and security"",
            ""security invariants"",
            ""vulnerabilities"",
            ""assurance levels"",
            ""memory safe languages"",
            ""external memory-safe ecosystem"",
            ""Linux Kernel"",
            ""safe coding"",
            ""deployment"",
            ""guidance"",
            ""frameworks"",
            ""best practices"",
            ""digital domain""
          ],
          ""operation"": [
            ""addressing"",
            ""improving"",
            ""transitioning"",
            ""investing"",
            ""building out"",
            ""engaging"",
            ""sharing"",
            ""partnering"",
            ""advancing""
          ],
          ""statements"": {
            ""Memory safety vulnerabilities"": {
              ""is"": [
                ""the standard for attacking software"",
                ""how attackers are having success""
              ],
              ""are"": [
                ""widespread"",
                ""one of the leading causes of vulnerabilities""
              ]
            },
            ""Secure-by-Design approach"": {
              ""is"": [
                ""centered around comprehensive adoption of languages with rigorous memory safety guarantees""
              ]
            },
            ""Memory-unsafe languages"": {
              ""are"": [
                ""still commonly exploited""
              ]
            },
            ""Memory corruption vulnerabilities"": {
              ""are"": [
                ""used in two thirds of 0-day exploits detected in the wild""
              ]
            },
            ""Safe Coding"": {
              ""is"": [
                ""Google's approach to addressing vulnerabilities""
              ]
            },
            ""Memory-safe languages"": {
              ""are"": [
                ""Java"",
                ""Go"",
                ""Rust"",
                ""C"",
                ""C++"",
                ""Python"",
                ""C51 ASM"",
              ]
            },
            ""Rust"": {
              ""is"": [
                ""a language that Google is investing in"",
                ""enhancing interoperability with C++ code"",
                ""being considered for adoption in existing codebases""
              ]
            },
            ""Java"": {
              ""is"": [
                ""considered for adoption in existing codebases""
              ]
            },
            ""Go"": {
              ""is"": [
                ""considered for adoption in existing codebases""
              ]
            },
            ""C++"": {
              ""is"": [
                ""a language with significant challenges for transitioning to memory safety"",
                ""being considered for a safer subset with hardware security features""
              ]
            },
            ""Incremental transition"": {
              ""is"": [
                ""considered for existing C++ codebases""
              ]
            },
            ""Hardware security features"": {
              ""are"": [
                ""considered for augmenting memory safety in C++""
              ]
            },
            ""Memory-safe ecosystem"": {
              ""is"": [
                ""being advanced through investments and grants""
              ]
            },
            ""Cross-language attacks"": {
              ""are"": [
                ""being addressed when mixing Rust and C++""
              ]
            },
            ""XSS attacks"": {
              ""were"": [
                ""eliminated through tooling""
              ]
            },
            ""Software security"": {
              ""is"": [
                ""the responsibility of the ecosystem, not just the developer""
              ]
            },
            ""Developer ecosystems"": {
              ""are"": [
                ""the focus of a Secure-by-Design approach"",
                ""designed for safety and security"",
                ""responsible for ensuring security invariants"",
                ""preventing entire classes of vulnerabilities""
              ]
            }
          }
        }";

        string jsonRosesAreRedVialetsAreBlueMemes = @"{
            ""entities"": [
                ""Roses"", ""Violets"", ""Good morning text"", ""Screenshots"", ""Toxic text"", ""Expectation"", ""Reality"", ""Drama"", ""Drama queen"", ""Silent treatment"", ""Evidence"", ""Manipulation"", ""Control"", ""Trust issues"", ""Therapist"", ""Red flags"", ""Toxic love"", ""Sanctuary"", ""Referee"", ""Space"", ""Battleground"", ""Emotional warfare"", ""Unicorn"", ""Lecture"", ""Landmine"", ""Soap opera"", ""Compromise"", ""Band-Aid"", ""Fire""
            ],
            ""relation_types"": [""comes with"", ""is a"", ""feels like"", ""means"", ""turns into"", ""requires"", ""loses""],
            ""attributes"": [""demanding"", ""full-time"", ""explosive"", ""dramatic"", ""manipulative"", ""controlling"", ""distrustful"", ""negotiating"", ""red-flagged"", ""surviving"", ""crying"", ""volume-speaking"", ""battle-like"", ""prison-like"", ""foreign"", ""band-aided"", ""gasoline-like""],
            ""operation"": [""run"", ""find peace"", ""save"", ""talk"", ""argue"", ""love"", ""retreat""],
            ""statements"": {
                ""Good morning text"": { ""comes with"": [""demanding""] },
                ""Screenshots"": { ""is a"": [""full-time""] },
                ""Toxic text"": { ""feels like"": [""explosive""] },
                ""Expectation"": { ""is"": [""flowers""] },
                ""Reality"": { ""is"": [""drama""] },
                ""Drama"": { ""means"": [""more drama""] },
                ""Drama queen"": { ""requires"": [""find peace""] },
                ""Silent treatment"": { ""is"": [""volume-speaking""] },
                ""Evidence"": { ""is"": [""screenshots""] },
                ""Manipulation"": { ""turns into"": [""trust issues""] },
                ""Control"": { ""is a"": [""love_language""] },
                ""Trust issues"": { ""are"": [""norm""] },
                ""Therapist"": { ""feels like"": [""hostage_negotiator""] },
                ""Red flags"": { ""are"": [""red-flagged""] },
                ""Toxic love"": { ""is"": [""surviving""] },
                ""Sanctuary"": { ""loses"": [""battle-like""] },
                ""Referee"": { ""is"": [""relationship status""] },
                ""Space"": { ""means"": [""break_from_you""] },
                ""Battleground"": { ""is"": [""heart condition""] },
                ""Emotional warfare"": { ""requires"": [""retreat""] },
                ""Unicorn"": { ""is"": [""unreal""] },
                ""Lecture"": { ""turns into"": [""talk""] },
                ""Landmine"": { ""is"": [""Toxic love""] },
                ""Soap opera"": { ""feels like"": [""relationship""] },
                ""Compromise"": { ""is"": [""foreign word""] },
                ""Band-Aid"": { ""is"": [""sorry saying""] },
                ""Fire"": { ""is"": [""gasoline-like""] }
            }
        }";

        string jsonElectronicsRepearMemes = @"{
            ""entities"": [
                ""Electronics Repair""
            ],
            ""relation_types"": [""is"", ""provides"", ""has""],
            ""attributes"": [
                ""right way"",
                ""enjoyment"",
                ""persistence"",
                ""ultimate experience"",
                ""long-lasting"",
                ""trustworthy"",
                ""passion"",
                ""seriousness"",
                ""care"",
                ""lifetime quality"",
                ""improvement"",
                ""ease"",
                ""reliability"",
                ""life restoration"",
                ""versatility"",
                ""determination"",
                ""restoration""
            ],
            ""operation"": [
                ""fixing"",
                ""repairing"",
                ""upgrading"",
                ""troubleshooting"",
                ""restoring""
            ],
            ""statements"": {
                ""Electronics Repair"": {
                    ""is"": [
                        ""right way"",
                        ""ultimate experience"",
                        ""long-lasting"",
                        ""trustworthy"",
                        ""seriousness"",
                        ""care"",
                        ""lifetime quality"",
                        ""improvement"",
                        ""ease"",
                        ""reliability"",
                        ""versatility"",
                        ""determination"",
                        ""restoration""
                    ],
                    ""provides"": [
                        ""enjoyment"",
                        ""passion"",
                        ""life restoration""
                    ],
                    ""has"": [
                        ""persistence"",
                        ""care""
                    ]
                }
            }
        }";

        //Universe topics = Universe.FromJson(jsonBasics);
        Universe topics = Universe.MergeUniverses(new List<string> {
            jsonBasics,
            jsonProgrammingSafetyByGoogle,
            jsonElectronicsRepearMemes,
            jsonRosesAreRedVialetsAreBlueMemes
        });

        List<Node> nodes = GenerateNodes(10, topics.GetAllStatements(), 4, 6);

        Population society = new Population(nodes);
        society.Show();

        Relations relations = new Relations(nodes);
        relations.PrintMatrix(); //default = false, show 0 = true
        Console.WriteLine();

        relations.PrintGraph();
        Console.WriteLine();

        Graph graph = relations.GenerateGraph();

        Graph graphWithGrades = relations.GenerateGraphWithGrades();
        graphWithGrades.PrintStandardWayWithGrades();

        graph.PrintPseudoGraphically();
        Console.WriteLine();

        //graph.PrintStandardWay();
        //Console.WriteLine();

        graph.PrintAsSets();
        Console.WriteLine();

        graph.PrintLongestPath();
        Console.WriteLine();

        graph.PrintCycles();
        Console.WriteLine();

        relations.Print();
        Console.WriteLine();
    }
}


