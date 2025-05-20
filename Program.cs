using System;
using ProyectoFormales;

public class Program
{
    static void Main()
    {
        var grammar = new Grammar();

        //Numero de lineas a leer
        int n = int.Parse(Console.ReadLine());

        for (int i = 0; i < n; i++)
        {
            string line = Console.ReadLine();
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
        IParser llParser = null;
        IParser slrParser = null;

        // Intenta crear un parser LL(1). Se lanza una excepción si no es LL(1)
        try
        {
            var testLLParser = new LLParser(grammar, firstFollow);
            isLL1 = true;
        }
        catch { }

        //Crea un parser SLR 
        var slrParserInstance = new SLRParser(grammar, firstFollow);
        bool isSLR1 = slrParserInstance.IsSLR1();

        //Si es LL1 y SLR1
        if (isLL1 && isSLR1)
        {
            Console.WriteLine("Select a parser(T: for LL(1), B: for SLR(1), Q: quit):");
            llParser = new LLParser(grammar, firstFollow);
            slrParser = slrParserInstance;
            string choice;
            while ((choice = Console.ReadLine()) != "Q")
            {
                if (choice == "T" || choice == "B")
                {
                    var parser = choice == "T" ? llParser : slrParser;
                    while (true)
                    {
                        string input = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(input)) break;
                        Console.WriteLine(parser.Parse(input) ? "yes" : "no");
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
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) break;
                //Verifica si una cadena dada es aceptada por LL(1)
                Console.WriteLine(llParser.Parse(input) ? "yes" : "no");
            }
        }
        //Solo es SLR1
        else if (isSLR1)
        {
            Console.WriteLine("Grammar is SLR(1).");
            slrParser = new SLRParser(grammar, firstFollow);
            while (true)
            {
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) break;
                //Verifica si una cadena dada es aceptada por SLR(1)
                Console.WriteLine(slrParser.Parse(input) ? "yes" : "no");
            }
        }
        //No es ninguna
        else
        {
            Console.WriteLine("Grammar is neither LL(1) nor SLR(1).");
        }
    }
}