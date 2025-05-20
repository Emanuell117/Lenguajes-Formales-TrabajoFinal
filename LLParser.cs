using ProyectoFormales;
using System;
using System.Collections.Generic;

public class LLParser : IParser
{
    private readonly Grammar _grammar;
    private readonly FirstFollowCalculator _firstFollow;
    //Tabla donde cada entrada define qué producción usar según el no terminal actual y el terminal de entrada.
    private readonly Dictionary<char, Dictionary<char, List<char>>> _table = new();

    public LLParser(Grammar grammar, FirstFollowCalculator firstFollow)
    {
        _grammar = grammar;
        _firstFollow = firstFollow;
        BuildTable();
    }

    private void BuildTable()
    {
        foreach (var (nt, productions) in _grammar.Productions)
        {
            _table[nt] = new();
            foreach (var production in productions)
            {
                var first = GetFirst(production);
                foreach (var terminal in first)
                {
                    if (terminal != 'e')
                    {
                        // Verificar si ya existe una entrada para este terminal
                        if (_table[nt].ContainsKey(terminal))
                            //Si llega a la excepcion entonces no es LL(1)
                            throw new InvalidOperationException($"Conflicto en {nt} con {terminal}");
                        // Añade el terminal actual con el no terminal y la produccion que corresponde
                        _table[nt][terminal] = production;
                    }
                }
                if (first.Contains('e'))
                {
                    foreach (var followTerminal in _firstFollow.Follow[nt])
                    {
                        // Verificar si ya existe una entrada para este terminal
                        if (_table[nt].ContainsKey(followTerminal))
                            throw new InvalidOperationException($"Conflicto en {nt} con {followTerminal}");
                        _table[nt][followTerminal] = new List<char> { 'e' };
                    }
                }
            }
        }
    }

    private HashSet<char> GetFirst(List<char> symbols)
    {
        var result = new HashSet<char>();

        // Si la lista esta vacia devuelve epsilon
        if (symbols == null || symbols.Count == 0)
        {
            result.Add('e');
            return result;
        }

        bool canEpsilon = true;
        foreach (var symbol in symbols)
        {
            if (!canEpsilon) break;
            //Verifica si es terminal
            if (_grammar.IsTerminal(symbol))
            {
                result.Add(symbol);
                canEpsilon = false;
            }
            else if (symbol == 'e') // Tratar 'e' como epsilon
            {
                result.Add('e');
                canEpsilon = true;
            }
            else
            {
                //Obtiene el conjunto firat del simbolo y los añade al resulatdo
                var firstSet = _firstFollow.First[symbol];
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

    public bool Parse(string input)
    {
        input += "$"; //// Añade $ al final de la entrada para marcar fin de cadena
        var stack = new Stack<char>();
        stack.Push('$'); // Pila inicial
        stack.Push(_grammar.StartSymbol);

        int index = 0;
        while (stack.Count > 0)
        {
            char top = stack.Pop(); // Saca el tope de la pila
            char currentInput = index < input.Length ? input[index] : '$'; // Obtiene el terminal actual

            if (top == '$')
                return currentInput == '$'; // Si el tope es $ y currentInput tambien, acepta la cadena

            if (_grammar.IsTerminal(top))
            {
                if (top == currentInput)
                    index++;  // Avanza en la entrada si hay coincidencia
                else
                    return false; // Rechaza si no coincide
            }
            else
            {
                // No hay producción definida, cadena inválida
                if (!_table.ContainsKey(top) || !_table[top].ContainsKey(currentInput))
                    return false;

                var production = _table[top][currentInput];// Obtiene la producción desde la tabla

                //Si la produccion es epsilon, no se añade nada a la pila
                if (production.Count == 1 && production[0] == 'e')
                    continue; 

                else
                {
                    // Empuja simbolos en orden inverso para mantener el orden original
                    for (int i = production.Count - 1; i >= 0; i--)
                    {
                        stack.Push(production[i]);
                    }
                }
            }
        }
        return false;
    }
}
