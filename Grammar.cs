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
        Nonterminals.Add(lhs); // Asegurarse que LHS esta

        foreach (var production in rhs)
        {
            foreach (var symbol in production)
            {
                if (IsNonterminal(symbol))
                    Nonterminals.Add(symbol); // Añadir RHS a nonterminals
            }
        }
    }

    public bool IsNonterminal(char c) => Nonterminals.Contains(c);
    public bool IsTerminal(char c) => !char.IsUpper(c) && c != 'e';
}
