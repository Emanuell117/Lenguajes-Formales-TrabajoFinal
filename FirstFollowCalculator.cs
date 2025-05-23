using System;
using System.Collections.Generic;

public class FirstFollowCalculator
{
    private readonly Grammar _grammar;
    public Dictionary<char, HashSet<char>> First { get; } = new();
    public Dictionary<char, HashSet<char>> Follow { get; } = new();

    public FirstFollowCalculator(Grammar grammar)
    {
        _grammar = grammar;
        foreach (var nt in _grammar.Nonterminals)
        {
            First[nt] = new();
            Follow[nt] = new();
        }
    }

    //Calcular el conjunto First de la gramática
    public void ComputeFirst()
    {
        bool changed;
        //Cuando se agrega un nuevo valor para first changes se vuelve verdadero y se repite desde el principio
        do
        {
            changed = false;
            foreach (var (nt, productions) in _grammar.Productions)
            {
                foreach (var production in productions)
                {
                    changed |= AddFirstFromSequence(production, nt);
                }
            }
        } while (changed);
    }

    private bool AddFirstFromSequence(List<char> symbols, char nt)
    {
        bool added = false;
        bool prevCanEpsilon = true;
        foreach (var symbol in symbols)
        {
            if (!prevCanEpsilon) break;
            if (_grammar.IsTerminal(symbol))
            {
                added |= AddToSet(First[nt], symbol);
                prevCanEpsilon = false;
            }
            else if (symbol == 'e') // Tratar 'e' como ε
            {
                added |= AddToSet(First[nt], 'e');
                prevCanEpsilon = true;
            }
            else
            {
                var firstSet = First[symbol];
                foreach (var s in firstSet)
                {
                    if (s != 'e')
                        added |= AddToSet(First[nt], s);
                }
                prevCanEpsilon = firstSet.Contains('e');
            }
        }
        if (prevCanEpsilon)
            added |= AddToSet(First[nt], 'e');
        return added;
    }

    //Agrega un elemento (value) a un conjunto (set) y devuelve true si el conjunto cambió, o false si ya existía.
    private bool AddToSet(HashSet<char> set, char value)
    {
        return set.Add(value);
    }

    //Calcular el conjunto Follow de la gramática
    public void ComputeFollow()
    {
        // Inicializar Follow del símbolo inicial aumentado
        if (!_grammar.Nonterminals.Contains('Z'))
        {
            _grammar.Nonterminals.Add('Z');
            Follow['Z'] = new HashSet<char>();
        }
        Follow['Z'].Add('$');

        // Asegurar que Follow(S) incluya '$'
        var startSymbol = _grammar.StartSymbol;
        if (!Follow[startSymbol].Contains('$'))
            Follow[startSymbol].Add('$');

        bool changed;
        do
        {
            changed = false;
            foreach (var (lhs, productions) in _grammar.Productions)
            {
                foreach (var production in productions)
                {
                    for (int i = 0; i < production.Count; i++)
                    {
                        char symbol = production[i];
                        if (_grammar.IsNonterminal(symbol))
                        {
                            List<char> beta = production.Skip(i + 1).ToList();
                            HashSet<char> firstBeta = GetFirst(beta);
                            bool canEpsilon = beta.Count == 0 || firstBeta.Contains('e');

                            // Agregar First(beta) - {e} a Follow[symbol]
                            foreach (var terminal in firstBeta.Where(t => t != 'e'))
                            {
                                if (AddToSet(Follow[symbol], terminal))
                                    changed = true;
                            }

                            // Agregar Follow(lhs) a Follow[symbol] solo si beta puede derivar ε
                            if (canEpsilon)
                            {
                                foreach (var followSymbol in Follow[lhs])
                                {
                                    if (AddToSet(Follow[symbol], followSymbol))
                                        changed = true;
                                }
                            }
                        }
                    }
                }
            }
        } while (changed);
    }

    //Obtiene el primer no terminal si no encuentra devuelve epsilon
    private HashSet<char> GetFirst(List<char> symbols)
    {
        var result = new HashSet<char>();
        if (symbols == null || symbols.Count == 0)
        {
            result.Add('e');
            return result;
        }

        bool canEpsilon = true;
        foreach (var symbol in symbols)
        {
            if (!canEpsilon) break;
            if (_grammar.IsTerminal(symbol))
            {
                result.Add(symbol);
                canEpsilon = false;
            }
            else if (symbol == 'e') // Tratar 'e' como ε
            {
                result.Add('e');
                canEpsilon = true;
            }
            else
            {
                var firstSet = First[symbol];
                foreach (var s in firstSet)
                {
                    if (s != 'e')
                        result.Add(s);
                }
                canEpsilon = firstSet.Contains('e');
            }
        }
        if (canEpsilon)
            result.Add('e');
        return result;
    }
}
