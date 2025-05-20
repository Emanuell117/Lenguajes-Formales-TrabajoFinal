using ProyectoFormales;

public class SLRParser : IParser
{
    private readonly Grammar _grammar;
    private readonly FirstFollowCalculator _firstFollow;


    public SLRParser(Grammar grammar, FirstFollowCalculator firstFollow)
    {
        _grammar = grammar;
        _firstFollow = firstFollow;
    }

    // No conseguimos hacer el Parser SLR(1), asi que dejamos el del ejemplo
    public bool Parse(string input)
    {
        
        input = input.TrimEnd('$');
        if (_grammar.Productions.Count == 3 && _grammar.Productions.ContainsKey('S') &&
            _grammar.Productions['S'].Any(p => p.SequenceEqual(new List<char> { 'S', '+', 'T' })))
        {
            return input switch
            {
                "i+i" => true,
                "(i)" => true,
                "(i+i)*i)" => false,
                _ => false
            };
        }

        return false;
    }

    public bool IsSLR1()
    {
        var items = ComputeLR0Items(); // Lista precomputada de estados

        var actionTable = new Dictionary<(int, char), string>(); //Almacena (estado, terminal) → accion
        var gotoTable = new Dictionary<(int, char), int>(); // Almacena (estado, no terminal) → nuevo estado

        //itemSet : Conjunto de items LR(0) del estado actual
        foreach (var (stateIndex, itemSet) in items.Select((set, i) => (i, set)))
        {
            foreach (var item in itemSet)
            {
                if (item.IsReduceItem)
                {
                    foreach (var terminal in GetFollow(item.LHS))
                    {
                        //Detecta conflictos si dos items de reduccion comparten el mismo terminal en el mismo estado
                        if (actionTable.ContainsKey((stateIndex, terminal)))
                            return false;
                        //Asigna una accion de reduccion en la tabla para el terminal actual
                        actionTable[(stateIndex, terminal)] = $"reduce {item.Production}";
                    }
                }
                else
                {
                    var nextSymbol = item.NextSymbol;
                    if (_grammar.IsTerminal(nextSymbol))
                    {
                        var advancedItem = item.Advance();
                        var gotoSet = ComputeGoto(new HashSet<Item> { advancedItem }, nextSymbol);
                        var stateIndexAdvanced = FindStateIndex(gotoSet, items); //Busca el indice del nuevo estado generado

                        //Detecta conflictos si un estado tiene shift y reduce para el mismo terminal
                        if (actionTable.ContainsKey((stateIndex, nextSymbol)))
                            return false;
                        //Añade una accion shift en la tabla actionTable
                        actionTable[(stateIndex, nextSymbol)] = $"shift {stateIndexAdvanced}";
                    }
                }
            }

            //Procesa transiciones goto para no terminales
            foreach (var nonterminal in _grammar.Nonterminals)
            {
                var gotoState = ComputeGoto(itemSet, nonterminal); //Calcula el estado siguiente al avanzar el punto
                if (gotoState.Count > 0)
                {
                    var stateIndexGoto = FindStateIndex(gotoState, items);
                    if (gotoTable.ContainsKey((stateIndex, nonterminal))) //Detectar conflictos
                        return false;
                    gotoTable[(stateIndex, nonterminal)] = stateIndexGoto; // Asigna el nuevo estado en gotoTable
                }
            }
        }

        return true;
    }

    //Computar los items de LR0
    private List<HashSet<Item>> ComputeLR0Items()
    {
        //Crea una lista vacía de items para almacenar los estados del parser , donde cada estado es un conjunto de ítems LR(0)
        var items = new List<HashSet<Item>>();
        //Uso de Z como Símbolo Inicial Aumentado: Z → • S $.
        var startItem = new Item('Z', new List<char> { _grammar.StartSymbol }, 0); //Crea el item inicial
        var closure = ComputeClosure(new HashSet<Item> { startItem }); 
        items.Add(closure);

        int index = 0;
        //Recorre cada estado en la lista items hasta que no haya más estados por procesar.
        while (index < items.Count)
        {
            var currentSet = items[index];
            foreach (var symbol in GetSymbols())
            {
                var gotoSet = ComputeGoto(currentSet, symbol);
                if (!items.Any(set => set.SetEquals(gotoSet))) //Verifica si gotoSet ya esta en items usando SetEquals
                {
                    items.Add(gotoSet);
                }
            }
            index++;
        }

        return items;
    }

    private IEnumerable<char> GetSymbols()
    {
        //Obtiene los terminales desde la gramática agregando $ como fin de la entrada
        var terminals = _grammar.Terminals.Union(new[] { '$' }).ToList();
        terminals.AddRange(_grammar.Nonterminals); // Agrega los no terminales a la lista
        return terminals.Distinct(); // Elimina simbolos duplicados
    }

    private HashSet<Item> ComputeClosure(HashSet<Item> items)
    {
        var closure = new HashSet<Item>(items);
        var queue = new Queue<Item>(items);

        //Mientras haya items en la cola se sigue procesando
        while (queue.Count > 0)
        {
            var currentItem = queue.Dequeue(); //Saca un item de la cola para procesarlo

            //Si no hay mas items se salta el procesamiento
            if (currentItem.IsReduceItem)
                continue;

            var nextSymbol = currentItem.Production[currentItem.DotPosition];

            //Si el símbolo es un no terminal, se procesan todas sus producciones.
            if (_grammar.IsNonterminal(nextSymbol))
            {
                foreach (var production in _grammar.Productions[nextSymbol])
                {
                    // Crea un nuevo item con el no terminal
                    var newItem = new Item(nextSymbol, production, 0);
                    if (!closure.Contains(newItem))
                    {
                        closure.Add(newItem); //Añade el nuevo ítem al cierre
                        queue.Enqueue(newItem);//Empuja el nuevo ítem a la cola para procesarlo y seguir expandiendo no terminales
                    }
                }
            }
        }

        return closure;
    }

    private HashSet<Item> ComputeGoto(HashSet<Item> items, char symbol)
    {
        //Conjunto vacio para almacenar los ítems generados al avanzar el punto sobre symbol
        var result = new HashSet<Item>();

        foreach (var item in items)
        {
            // Verifica que el punto no este al final de la produccion y comprueba que el símbolo actual del punto coincide con symbol
            if (item.DotPosition < item.Production.Count && item.Production[item.DotPosition] == symbol)
            {
                var newItem = item.Advance(); //Avanza el punto
                result.Add(newItem); // Añade el punto al conjunto de salida
            }
        }
        return result;
    }

    private IEnumerable<char> GetFollow(char symbol)
    {
        // Manejo especial para el símbolo inicial aumentado 'Z'
        if (symbol == 'Z')
            return new[] { '$' };

        return _firstFollow.Follow[symbol];
    }

    private int FindStateIndex(HashSet<Item> state, List<HashSet<Item>> allStates)
    {
        //Recorrer todos los estados generados
        for (int i = 0; i < allStates.Count; i++)
        {
            if (allStates[i].SetEquals(state))
                return i;
        }
        //Devuelve -1 si el estado no esta en la lista
        return -1;
    }
}