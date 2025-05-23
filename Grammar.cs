using System;
using System.Collections.Generic;

public class Grammar
{
    public HashSet<char> Nonterminals { get; set; } = new();
    public HashSet<char> Terminals { get; set; } = new();

    //Diccionario donde cada clave es un no terminal y el valor es una lista de producciones.
    public Dictionary<char, List<List<char>>> Productions { get; set; } = new();
    public char StartSymbol { get; set; } = 'S';

    public void AddProduction(char lhs, List<List<char>> rhs)
    {
        Productions[lhs] = rhs;
        Nonterminals.Add(lhs); // Asegurarse que LHS está

        foreach (var production in rhs)
        {
            foreach (var symbol in production)
            {
                if (char.IsUpper(symbol))
                    Nonterminals.Add(symbol); // Añadir símbolos mayúsculas a no terminales
            }
        }

        // Actualizar el conjunto de terminales
        UpdateTerminals();
    }

    public bool IsNonterminal(char c) => Nonterminals.Contains(c);
    public bool IsTerminal(char c) => !char.IsUpper(c) && c != 'e' && Terminals.Contains(c);

    // Método para actualizar automáticamente el conjunto de terminales basado en las producciones
    public void UpdateTerminals()
    {
        // Reinicia el conjunto de terminales para evitar símbolos obsoletos
        Terminals.Clear();

        // Recorre todas las producciones
        foreach (var production in Productions.Values)
        {
            foreach (var rhs in production)
            {
                foreach (var symbol in rhs)
                {
                    // Si el símbolo no es un no terminal y no es epsilon (e), es un terminal
                    if (!IsNonterminal(symbol) && symbol != 'e')
                    {
                        Terminals.Add(symbol);
                    }
                }
            }
        }
    }
}
