using System;
using ProyectoFormales;

public class Program
{
    static void Main()
    {
        var grammar = new Grammar();

        //Numero de lineas a leer
        string input = Console.ReadLine() ?? string.Empty;
        if (!int.TryParse(input, out int n))
        {
            Console.WriteLine("Invalid input. Please enter a valid integer.");
            return;
        }

        for (int i = 0; i < n; i++)
        {
            string line = Console.ReadLine() ?? string.Empty;
            // Separa la parte izquierda y derecha de la producción
            var parts = line.Split("->");
            char lhs = parts[0][0]; // Extrae el no terminal

            // Separa las alternativas de la parte derecha
            var rhsAlternatives = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(alt => new List<char>(alt)).ToList();
            grammar.AddProduction(lhs, rhsAlternatives);
        }

        //Calcular los conjuntos First y Follow 
        var firstFollow = new FirstFollowCalculator(grammar);
        firstFollow.ComputeFirst();
        firstFollow.ComputeFollow();

        bool isLL1 = false;

        //Parsers sin definir
        IParser? llParser = null;
        IParser? slrParser = null;

        // Intenta crear un parser LL(1). Se lanza una excepción si no es LL(1)
        try
        {
            var testLLParser = new LLParser(grammar, firstFollow);
            isLL1 = true;
        }
        catch { }

        //Crea un parser SLR 
        var slrParserInstance = new SLRParser(grammar, firstFollow);
        bool isSlrGrammar = slrParserInstance.IsSLR1();

        //Si es LL1 y SLR1
        if (isLL1 && isSlrGrammar)
        {
            Console.WriteLine("Select a parser(T: for LL(1), B: for SLR(1), Q: quit):");
            llParser = new LLParser(grammar, firstFollow);
            slrParser = slrParserInstance;
            string choice;
            while (!string.IsNullOrEmpty((choice = Console.ReadLine() ?? string.Empty)) && choice != "Q")
            {
                if (choice == "T" || choice == "B")
                {
                    var parser = choice == "T" ? llParser : slrParser;
                    while (true)
                    {
                        string userInput = Console.ReadLine() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(userInput)) break;
                        Console.WriteLine(parser.Parse(userInput) ? "yes" : "no");
                    }
                }
            }
        }
        //Si es solo LL1
        else if (isLL1)
        {
            Console.WriteLine("Grammar is LL(1).");
            llParser = new LLParser(grammar, firstFollow);
            while (true)
            {
                string userInput = Console.ReadLine() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userInput)) break;
                //Verifica si una cadena dada es aceptada por LL(1)
                Console.WriteLine(llParser.Parse(userInput) ? "yes" : "no");
            }
        }
        //Solo es SLR1
        else if (isSlrGrammar)
        {
            Console.WriteLine("Grammar is SLR(1).");
            slrParser = new SLRParser(grammar, firstFollow);

            Console.WriteLine("Options:");
            Console.WriteLine("  F: full SLR info       - Mostrar toda la información del análisis SLR");
            Console.WriteLine("  P: parsing table       - Mostrar las tablas ACTION y GOTO");
            Console.WriteLine("  S: states              - Mostrar los estados LR(0)");
            Console.WriteLine("  D: debug mode          - Analizar cada cadena paso a paso");
            Console.WriteLine("  V: verify SLR(1)       - Verificar propiedades de la gramática SLR(1)");
            Console.WriteLine("  T: test grammar        - Probar la gramática con cadenas de ejemplo");
            Console.WriteLine("  Enter: normal mode     - Modo normal de análisis");

            string option = Console.ReadLine() ?? string.Empty;

            if (option == "F" || option == "f")
            {
                // Mostrar información completa del análisis SLR
                ((SLRParser)slrParser).PrintFullSLRInfo();
            }
            else if (option == "P" || option == "p")
            {
                // Mostrar la tabla de análisis
                ((SLRParser)slrParser).PrintParsingTable();
            }
            else if (option == "S" || option == "s")
            {
                // Mostrar los estados LR(0)
                ((SLRParser)slrParser).PrintStates();
            }
            else if (option == "V" || option == "v")
            {
                // Verificar propiedades de la gramática SLR(1)
                bool isValidSLR = ((SLRParser)slrParser).VerifySLR1Properties();
                Console.WriteLine($"\\nResultado: La gramática {(isValidSLR ? "cumple" : "NO cumple")} con las propiedades SLR(1).");
            }
            else if (option == "T" || option == "t")
            {
                // Probar la gramática con cadenas de ejemplo
                Console.WriteLine("Ingrese las cadenas de prueba (una por línea, línea vacía para terminar):");
                var testStrings = new List<string>();
                while (true)
                {
                    string testString = Console.ReadLine() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(testString)) break;
                    testStrings.Add(testString);
                }

                if (testStrings.Count > 0)
                {
                    ((SLRParser)slrParser).TestGrammar(testStrings);
                }
                else
                {
                    Console.WriteLine("No se ingresaron cadenas de prueba.");
                }
            }

            bool debugMode = option == "D" || option == "d";

            while (true)
            {
                string userInput = Console.ReadLine() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(userInput)) break;

                // Verificar si una cadena dada es aceptada por SLR(1)
                if (debugMode)
                {
                    // Mostrar análisis paso a paso
                    bool result = ((SLRParser)slrParser).ParseWithTrace(userInput);
                    Console.WriteLine(result ? "yes" : "no");
                }
                else
                {
                    // Análisis normal
                    Console.WriteLine(slrParser.Parse(userInput) ? "yes" : "no");
                }
            }
        }
        //No es ninguna
        else
        {
            Console.WriteLine("Grammar is neither LL(1) nor SLR(1).");
        }
    }
}
