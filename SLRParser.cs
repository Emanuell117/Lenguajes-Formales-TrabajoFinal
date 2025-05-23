using ProyectoFormales;
using System.Collections.Generic;
using System.Linq;

public class SLRParser : IParser
{
    private readonly Grammar _grammar;
    private readonly FirstFollowCalculator _firstFollow;
    private Dictionary<(int, char), string> _actionTable;
    private Dictionary<(int, char), int> _gotoTable;
    private List<HashSet<Item>> _states;
    private bool _tableBuilt;

    public SLRParser(Grammar grammar, FirstFollowCalculator firstFollow)
    {
        _grammar = grammar;
        _firstFollow = firstFollow;
        _actionTable = new Dictionary<(int, char), string>();
        _gotoTable = new Dictionary<(int, char), int>();
        _states = new List<HashSet<Item>>();
        _tableBuilt = false;
    }

    public bool Parse(string input)
    {
        // Asegurarse de que las tablas estén construidas
        if (!_tableBuilt)
        {
            BuildParsingTables();
        }

        // Añadir símbolo de fin de entrada si no está presente
        if (!input.EndsWith('$'))
        {
            input += '$';
        }

        var stack = new Stack<int>();
        stack.Push(0); // Estado inicial

        int position = 0;
        string errorMessage = string.Empty;

        while (position <= input.Length) // Incluimos el caso donde position == input.Length para procesar el $
        {
            int currentState = stack.Peek();
            char symbol = position < input.Length ? input[position] : '$';

            // Verificar si el símbolo es válido en la gramática
            if (position < input.Length && symbol != '$' && !_grammar.IsTerminal(symbol))
            {
                errorMessage = $"Símbolo '{symbol}' no reconocido en la posición {position}";
                break;
            }

            // Verificar si hay una acción definida para el estado actual y símbolo
            if (!_actionTable.TryGetValue((currentState, symbol), out string? action) || action == null)
            {
                errorMessage = $"Error sintáctico en la posición {position}: no hay acción definida para el estado {currentState} y símbolo '{symbol}'";
                break;
            }

            // Acciones shift, reduce o accept
            if (action.StartsWith("shift"))
            {
                // Acción shift: mover al siguiente símbolo y cambiar de estado
                int nextState = int.Parse(action.Split(' ')[1]);
                stack.Push(nextState);
                position++;
            }
            else if (action.StartsWith("reduce"))
            {
                // Extraer la producción a reducir
                string[] parts = action.Split(new[] { ' ' }, 3);
                char lhs = parts[1][0]; // No terminal izquierdo
                string rhsStr = parts[2]; // Parte derecha de la producción

                // Si la producción no es epsilon (representada por 'e')
                if (rhsStr != "e")
                {
                    // Desapilar estados correspondientes a cada símbolo en la parte derecha
                    for (int i = 0; i < rhsStr.Length; i++)
                    {
                        if (stack.Count > 1) // Siempre mantener al menos el estado 0
                        {
                            stack.Pop();
                        }
                        else
                        {
                            errorMessage = "Error en la pila de análisis: no hay suficientes estados para reducir";
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(errorMessage))
                        break;
                }

                // Estado actual después de desapilar
                int state = stack.Peek();

                // Consultar la tabla GOTO para determinar el nuevo estado
                if (!_gotoTable.TryGetValue((state, lhs), out int gotoState))
                {
                    errorMessage = $"Error en la tabla GOTO: no hay transición definida para el estado {state} y no terminal '{lhs}'";
                    break;
                }

                // Apilar el nuevo estado
                stack.Push(gotoState);

                // No avanzamos la posición en la entrada porque no consumimos símbolo
            }
            else if (action == "accept")
            {
                // Si estamos en la posición del $ final y hay una acción de aceptar
                return true; // Cadena aceptada
            }
            else
            {
                errorMessage = $"Acción no reconocida: {action}";
                break;
            }
        }

        // Si hay un mensaje de error, podemos mostrarlo para depuración
        if (!string.IsNullOrEmpty(errorMessage))
        {
            // Descomentar para mostrar el mensaje de error
            // Console.WriteLine(errorMessage);
        }

        // Si salimos del bucle sin aceptar, la cadena no pertenece al lenguaje
        return false;
    }

    public bool IsSLR1()
    {
        try
        {
            // Limpiar las tablas existentes
            _actionTable = new Dictionary<(int, char), string>();
            _gotoTable = new Dictionary<(int, char), int>();
            _states = new List<HashSet<Item>>();
            _tableBuilt = false;

            // Intentar construir las tablas de análisis
            BuildParsingTables();
            return true;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"La gramática no es SLR(1): {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al construir las tablas de análisis: {ex.Message}");
            return false;
        }
    }

    // Método para validar la gramática antes de construir las tablas de análisis
    private bool ValidateGrammar()
    {
        // Verificar que no haya producciones vacías
        foreach (var productions in _grammar.Productions.Values)
        {
            if (productions.Count == 0)
            {
                throw new InvalidOperationException("La gramática tiene un no terminal sin producciones");
            }
        }

        // Verificar que todos los no terminales en la parte derecha tengan producciones
        foreach (var productions in _grammar.Productions.Values)
        {
            foreach (var production in productions)
            {
                foreach (var symbol in production)
                {
                    if (_grammar.IsNonterminal(symbol) && !_grammar.Productions.ContainsKey(symbol))
                    {
                        throw new InvalidOperationException($"El no terminal '{symbol}' es usado pero no tiene producciones");
                    }
                }
            }
        }

        return true;
    }

    private void BuildParsingTables()
    {
        // Validar la gramática antes de construir las tablas
        if (!ValidateGrammar())
        {
            throw new InvalidOperationException("La gramática no es válida para el análisis SLR(1)");
        }

        _states = ComputeLR0Items(); // Lista precomputada de estados

        _actionTable = new Dictionary<(int, char), string>(); // Almacena (estado, terminal) → accion
        _gotoTable = new Dictionary<(int, char), int>(); // Almacena (estado, no terminal) → nuevo estado

        // Para depuración
        // PrintStates();

        // itemSet : Conjunto de items LR(0) del estado actual
        foreach (var (stateIndex, itemSet) in _states.Select((set, i) => (i, set)))
        {
            foreach (var item in itemSet)
            {
                // Si el item es para la producción aumentada Z -> S$ y el punto está al final,
                // añadimos la acción de aceptación
                if (item.LHS == 'Z' && item.IsReduceItem && item.Production.Count > 0 &&
                    item.Production[0] == _grammar.StartSymbol)
                {
                    _actionTable[(stateIndex, '$')] = "accept";
                    continue;
                }

                if (item.IsReduceItem)
                {
                    foreach (var terminal in GetFollow(item.LHS))
                    {
                        // Detecta conflictos si dos items de reduccion comparten el mismo terminal en el mismo estado
                        if (_actionTable.TryGetValue((stateIndex, terminal), out var existingAction))
                        {
                            // Si ya hay una acción de aceptación para este terminal, permitimos que tenga prioridad
                            if (existingAction == "accept")
                            {
                                // Permitir que accept tenga prioridad, no hacer nada
                                continue;
                            }

                            // Si ya hay una acción shift, tenemos un conflicto shift/reduce
                            if (existingAction.StartsWith("shift"))
                            {
                                // Podríamos implementar una estrategia de resolución aquí, como preferir shift sobre reduce
                                // Por ahora, reportamos el conflicto
                                throw new InvalidOperationException(
                                    $"Conflicto shift/reduce en la tabla de acción para estado {stateIndex}, terminal {terminal}: " +
                                    $"Existente: {existingAction}, Nuevo: reduce {item.LHS} {new string(item.Production.ToArray())}");
                            }

                            // Si ya hay una acción reduce, tenemos un conflicto reduce/reduce
                            if (existingAction.StartsWith("reduce"))
                            {
                                // Podríamos implementar una estrategia de resolución aquí, como elegir la producción más corta
                                // Por ahora, reportamos el conflicto
                                throw new InvalidOperationException(
                                    $"Conflicto reduce/reduce en la tabla de acción para estado {stateIndex}, terminal {terminal}: " +
                                    $"Existente: {existingAction}, Nuevo: reduce {item.LHS} {new string(item.Production.ToArray())}");
                            }
                        }

                        // Asigna una acción de reducción en la tabla para el terminal actual
                        _actionTable[(stateIndex, terminal)] = $"reduce {item.LHS} {new string(item.Production.ToArray())}";
                    }
                }
                else
                {
                    var nextSymbol = item.NextSymbol;
                    if (_grammar.IsTerminal(nextSymbol) || nextSymbol == '$')
                    {
                        var nextItemSet = ComputeGoto(itemSet, nextSymbol);
                        if (nextItemSet.Count > 0)
                        {
                            var nextStateIndex = FindStateIndex(nextItemSet, _states);

                            if (nextStateIndex == -1)
                            {
                                // Si el estado no existe, agregarlo
                                nextStateIndex = _states.Count;
                                _states.Add(nextItemSet);
                            }

                            // Detecta conflictos si un estado tiene shift y reduce para el mismo terminal
                            if (_actionTable.TryGetValue((stateIndex, nextSymbol), out var existingAction))
                            {
                                if (existingAction == "accept")
                                {
                                    // Si ya hay una acción de aceptar, mantener la acción de aceptar
                                    continue;
                                }

                                // Si ya hay una acción de reducción para este terminal, tenemos un conflicto shift/reduce
                                if (existingAction.StartsWith("reduce"))
                                {
                                    throw new InvalidOperationException(
                                        $"Conflicto shift/reduce en la tabla de acción para estado {stateIndex}, terminal {nextSymbol}: " +
                                        $"Existente: {existingAction}, Nuevo: shift {nextStateIndex}");
                                }

                                // Si ya hay una acción shift para el mismo símbolo, verificar que sea al mismo estado
                                if (existingAction.StartsWith("shift"))
                                {
                                    int existingNextState = int.Parse(existingAction.Split(' ')[1]);
                                    if (existingNextState != nextStateIndex)
                                    {
                                        throw new InvalidOperationException(
                                            $"Conflicto shift/shift en la tabla de acción para estado {stateIndex}, terminal {nextSymbol}: " +
                                            $"Existente: shift {existingNextState}, Nuevo: shift {nextStateIndex}");
                                    }
                                }
                            }
                            else
                            {
                                // Añade una acción shift en la tabla actionTable solo si no hay acción previa
                                _actionTable[(stateIndex, nextSymbol)] = $"shift {nextStateIndex}";
                            }
                        }
                    }
                }
            }

            // Procesa transiciones goto para no terminales
            foreach (var nonterminal in _grammar.Nonterminals)
            {
                var gotoState = ComputeGoto(itemSet, nonterminal); // Calcula el estado siguiente al avanzar el punto
                if (gotoState.Count > 0)
                {
                    var stateIndexGoto = FindStateIndex(gotoState, _states);

                    if (stateIndexGoto == -1)
                    {
                        // Si el estado no existe, agregarlo
                        stateIndexGoto = _states.Count;
                        _states.Add(gotoState);
                    }

                    if (_gotoTable.ContainsKey((stateIndex, nonterminal))) // Detectar conflictos
                    {
                        throw new InvalidOperationException(
                            $"Conflicto en la tabla goto para estado {stateIndex}, no terminal {nonterminal}");
                    }

                    _gotoTable[(stateIndex, nonterminal)] = stateIndexGoto; // Asigna el nuevo estado en gotoTable
                }
            }
        }

        _tableBuilt = true;

        // Para depuración
        // PrintParsingTable();
    }

    // Método para depuración - muestra los estados y sus items
    public void PrintStates()
    {
        if (!_tableBuilt)
        {
            BuildParsingTables();
        }

        Console.WriteLine("\n=== ESTADOS LR(0) ===");
        string separator = new string('-', 50);

        for (int i = 0; i < _states.Count; i++)
        {
            Console.WriteLine(separator);
            Console.WriteLine($"Estado {i}:");
            Console.WriteLine(separator);

            // Ordenar los items para una visualización más consistente
            var orderedItems = _states[i].OrderBy(item => item.LHS).ThenBy(item => new string(item.Production.ToArray()));

            foreach (var item in orderedItems)
            {
                // Formatear y mostrar el item usando el método auxiliar
                Console.WriteLine($"  {FormatItem(item)}");
            }

            // Mostrar transiciones de este estado a otros estados
            Console.WriteLine("\n  Transiciones:");

            // Transiciones con terminales (ACTION)
            var transitions = _actionTable
                .Where(entry => entry.Key.Item1 == i && entry.Value.StartsWith("shift"))
                .Select(entry => $"    Con '{entry.Key.Item2}' -> Estado {entry.Value.Split(' ')[1]} (shift)")
                .OrderBy(s => s);

            foreach (var transition in transitions)
            {
                Console.WriteLine(transition);
            }

            // Transiciones con no terminales (GOTO)
            var gotoTransitions = _gotoTable
                .Where(entry => entry.Key.Item1 == i)
                .Select(entry => $"    Con '{entry.Key.Item2}' -> Estado {entry.Value} (goto)")
                .OrderBy(s => s);

            foreach (var transition in gotoTransitions)
            {
                Console.WriteLine(transition);
            }

            // Acciones de reducción
            var reductions = _actionTable
                .Where(entry => entry.Key.Item1 == i && entry.Value.StartsWith("reduce"))
                .GroupBy(entry => entry.Value)
                .Select(group => $"    Reducción: {group.Key} para símbolos: {string.Join(", ", group.Select(g => $"'{g.Key.Item2}'"))}")
                .OrderBy(s => s);

            foreach (var reduction in reductions)
            {
                Console.WriteLine(reduction);
            }

            // Acción de aceptación
            var accept = _actionTable
                .Where(entry => entry.Key.Item1 == i && entry.Value == "accept")
                .Select(entry => $"    Aceptar con símbolo: '{entry.Key.Item2}'");

            foreach (var a in accept)
            {
                Console.WriteLine(a);
            }

            Console.WriteLine();
        }
    }

    // Computar los estados (conjuntos de ítems LR(0))
    private List<HashSet<Item>> ComputeLR0Items()
    {
        // Crea una lista vacía de items para almacenar los estados del parser
        var states = new List<HashSet<Item>>();

        // Gramática aumentada con 'Z' como nuevo símbolo inicial: Z → S
        var startItem = new Item('Z', new List<char> { _grammar.StartSymbol }, 0); // Crea el item inicial
        var closure = ComputeClosure(new HashSet<Item> { startItem });
        states.Add(closure);

        int index = 0;
        // Recorre cada estado en la lista hasta que no haya más estados por procesar
        while (index < states.Count)
        {
            var currentState = states[index];

            // Obtener todos los símbolos (terminales y no terminales) que pueden seguir al punto
            var validSymbols = new HashSet<char>();
            foreach (var item in currentState)
            {
                if (!item.IsReduceItem)
                {
                    validSymbols.Add(item.NextSymbol);
                }
            }

            // Para cada símbolo, calcular el goto
            foreach (var symbol in validSymbols)
            {
                var gotoState = ComputeGoto(currentState, symbol);
                if (gotoState.Count > 0)
                {
                    // Verificar si este estado ya existe
                    bool stateExists = false;
                    foreach (var existingState in states)
                    {
                        if (existingState.SetEquals(gotoState))
                        {
                            stateExists = true;
                            break;
                        }
                    }

                    // Si es un nuevo estado, añadirlo a la lista
                    if (!stateExists)
                    {
                        states.Add(gotoState);
                    }
                }
            }

            index++;
        }

        return states;
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

        // Mientras haya items en la cola se sigue procesando
        while (queue.Count > 0)
        {
            var currentItem = queue.Dequeue(); // Saca un item de la cola para procesarlo

            // Si el ítem está completo (punto al final), no hay nada que hacer
            if (currentItem.IsReduceItem)
                continue;

            var nextSymbol = currentItem.NextSymbol;

            // Si el símbolo es un no terminal, se procesan todas sus producciones
            if (_grammar.IsNonterminal(nextSymbol) && _grammar.Productions.ContainsKey(nextSymbol))
            {
                foreach (var production in _grammar.Productions[nextSymbol])
                {
                    // Crea un nuevo item con el no terminal y el punto al inicio
                    var newItem = new Item(nextSymbol, production, 0);
                    if (!closure.Contains(newItem))
                    {
                        closure.Add(newItem); // Añade el nuevo ítem al cierre
                        queue.Enqueue(newItem); // Empuja el nuevo ítem para seguir procesando
                    }
                }
            }
        }

        return closure;
    }

    private HashSet<Item> ComputeGoto(HashSet<Item> items, char symbol)
    {
        // Conjunto para almacenar los ítems con el punto avanzado
        var result = new HashSet<Item>();

        foreach (var item in items)
        {
            // Verificar que el punto no está al final y que el siguiente símbolo coincide con symbol
            if (!item.IsReduceItem && item.NextSymbol == symbol)
            {
                var newItem = item.Advance(); // Avanza el punto
                result.Add(newItem); // Añade el ítem con el punto avanzado
            }
        }

        // Si no hay ítems que mover, devolver conjunto vacío
        if (result.Count == 0)
            return new HashSet<Item>();

        // Devuelve el cierre del conjunto resultante
        return ComputeClosure(result);
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
        // Recorrer todos los estados generados
        for (int i = 0; i < allStates.Count; i++)
        {
            if (allStates[i].SetEquals(state))
                return i;
        }
        // Devuelve -1 si el estado no está en la lista
        return -1;
    }

    // Método para depuración - muestra las tablas ACTION y GOTO
    public void PrintParsingTable()
    {
        if (!_tableBuilt)
        {
            BuildParsingTables();
        }

        // Obtener todos los terminales y no terminales
        var terminals = new HashSet<char>();

        // Añadir todos los terminales de la gramática
        foreach (var terminal in _grammar.Terminals)
        {
            terminals.Add(terminal);
        }

        // Añadir el símbolo de fin de entrada
        terminals.Add('$');

        // Añadir todos los caracteres que aparecen en la tabla ACTION
        foreach (var key in _actionTable.Keys)
        {
            if (!_grammar.IsNonterminal(key.Item2))
            {
                terminals.Add(key.Item2);
            }
        }

        var nonTerminals = _grammar.Nonterminals;

        Console.WriteLine("\n=== TABLA ACTION ===");
        // Construir una línea de separación
        string separator = new string('-', 8 + terminals.Count * 12);

        // Encabezado de la tabla
        Console.WriteLine(separator);
        Console.Write("| Estado |");
        foreach (var terminal in terminals.OrderBy(t => t))
        {
            Console.Write($" {terminal,8} |");
        }
        Console.WriteLine("\n" + separator);

        // Filas de la tabla
        foreach (int state in Enumerable.Range(0, _states.Count))
        {
            Console.Write($"| {state,6} |");
            foreach (var terminal in terminals.OrderBy(t => t))
            {
                if (_actionTable.TryGetValue((state, terminal), out var action))
                {
                    // Formatear la acción para que se vea bien en la tabla
                    string displayAction = action;
                    if (action.StartsWith("shift"))
                    {
                        displayAction = "s" + action.Split(' ')[1];
                    }
                    else if (action.StartsWith("reduce"))
                    {
                        string[] parts = action.Split(new[] { ' ' }, 3);
                        displayAction = "r:" + parts[1] + "->" + parts[2];
                    }
                    Console.Write($" {displayAction,8} |");
                }
                else
                {
                    Console.Write($" {"",-8} |");
                }
            }
            Console.WriteLine("\n" + separator);
        }

        Console.WriteLine("\n=== TABLA GOTO ===");
        // Ajustar la línea de separación para la tabla GOTO
        separator = new string('-', 8 + nonTerminals.Count * 10);

        // Encabezado de la tabla
        Console.WriteLine(separator);
        Console.Write("| Estado |");
        foreach (var nonTerminal in nonTerminals.OrderBy(nt => nt))
        {
            Console.Write($" {nonTerminal,6} |");
        }
        Console.WriteLine("\n" + separator);

        // Filas de la tabla
        foreach (int state in Enumerable.Range(0, _states.Count))
        {
            Console.Write($"| {state,6} |");
            foreach (var nonTerminal in nonTerminals.OrderBy(nt => nt))
            {
                if (_gotoTable.TryGetValue((state, nonTerminal), out var nextState))
                {
                    Console.Write($" {nextState,6} |");
                }
                else
                {
                    Console.Write($" {"",-6} |");
                }
            }
            Console.WriteLine("\n" + separator);
        }
    }

    // Método para depuración - analiza paso a paso con todos los detalles
    public bool ParseWithTrace(string input)
    {
        // Asegurarse de que las tablas estén construidas
        if (!_tableBuilt)
        {
            BuildParsingTables();
        }

        // Añadir símbolo de fin de entrada si no está presente
        if (!input.EndsWith('$'))
        {
            input += '$';
        }

        Console.WriteLine("\nANÁLISIS PASO A PASO:");
        Console.WriteLine($"Entrada: {input}");

        // Crear una tabla más detallada para mostrar el análisis
        string header = $"{"PASO",5} | {"PILA",15} | {"SIMBOLOS",15} | {"ENTRADA",15} | {"ACCIÓN",20} | {"SIGUIENTE ESTADO",15}";
        Console.WriteLine(header);
        Console.WriteLine(new string('-', header.Length));

        var stateStack = new Stack<int>();     // Pila de estados
        var symbolStack = new Stack<char>();   // Pila de símbolos (opcional, para mejor visualización)

        stateStack.Push(0); // Estado inicial

        int position = 0;
        int step = 1;

        while (position <= input.Length)
        {
            int currentState = stateStack.Peek();
            char symbol = position < input.Length ? input[position] : '$';

            // Mostrar el estado actual
            string stateStackStr = string.Join(" ", stateStack.Reverse());
            string symbolStackStr = string.Join(" ", symbolStack.Count > 0 ? symbolStack.Reverse() : new List<char>());
            string remainingInput = position < input.Length ? input.Substring(position) : "$";

            // Verificar si hay una acción definida para el estado actual y símbolo
            if (!_actionTable.TryGetValue((currentState, symbol), out string? action) || action == null)
            {
                string errorMsg = $"ERROR: No hay acción definida para estado {currentState} y símbolo '{symbol}'";
                Console.WriteLine($"{step,5} | {stateStackStr,15} | {symbolStackStr,15} | {remainingInput,15} | {errorMsg,20} | {"",15}");

                // Mostrar posibles acciones esperadas para este estado
                var expectedSymbols = _actionTable.Keys
                    .Where(k => k.Item1 == currentState)
                    .Select(k => k.Item2.ToString())
                    .ToList();

                if (expectedSymbols.Count > 0)
                {
                    Console.WriteLine($"Símbolos esperados en el estado {currentState}: {string.Join(", ", expectedSymbols)}");
                }

                return false;
            }

            // Muestra la información del paso actual antes de ejecutar la acción
            Console.WriteLine($"{step,5} | {stateStackStr,15} | {symbolStackStr,15} | {remainingInput,15} | {action,20} | {"",15}");

            // Acciones shift, reduce o accept
            if (action.StartsWith("shift"))
            {
                // Acción shift: mover al siguiente símbolo y cambiar de estado
                int nextState = int.Parse(action.Split(' ')[1]);
                stateStack.Push(nextState);
                symbolStack.Push(symbol); // Añade el símbolo a la pila de símbolos
                position++; // Avanza la posición en la entrada
            }
            else if (action.StartsWith("reduce"))
            {
                // Extraer la producción a reducir
                string[] parts = action.Split(new[] { ' ' }, 3);
                char lhs = parts[1][0]; // No terminal izquierdo
                string rhsStr = parts[2]; // Parte derecha de la producción

                // Si la producción no es epsilon (representada por 'e')
                if (rhsStr != "e")
                {
                    // Desapilar estados y símbolos correspondientes a cada símbolo en la parte derecha
                    for (int i = 0; i < rhsStr.Length; i++)
                    {
                        if (stateStack.Count > 1) // Siempre mantener al menos el estado 0
                        {
                            stateStack.Pop();
                            if (symbolStack.Count > 0)
                                symbolStack.Pop();
                        }
                    }
                }

                // Estado actual después de desapilar
                int state = stateStack.Peek();

                // Consultar la tabla GOTO para determinar el nuevo estado
                if (!_gotoTable.TryGetValue((state, lhs), out int gotoState))
                {
                    string errorMsg = $"ERROR: No hay GOTO para ({state},{lhs})";
                    Console.WriteLine($"{step,5} | {stateStackStr,15} | {symbolStackStr,15} | {remainingInput,15} | {errorMsg,20} | {"",15}");
                    return false;
                }

                // Apilar el nuevo estado y el no terminal
                stateStack.Push(gotoState);
                symbolStack.Push(lhs);

                // Mostrar el resultado de la reducción con el nuevo estado
                stateStackStr = string.Join(" ", stateStack.Reverse());
                symbolStackStr = string.Join(" ", symbolStack.Reverse());
                Console.WriteLine($"{step,5} | {stateStackStr,15} | {symbolStackStr,15} | {remainingInput,15} | {"-> GOTO " + gotoState,20} | {gotoState,15}");
            }
            else if (action == "accept")
            {
                Console.WriteLine($"{step,5} | {stateStackStr,15} | {symbolStackStr,15} | {remainingInput,15} | {"ACCEPT",20} | {"",15}");
                return true; // Cadena aceptada
            }

            step++;
        }

        string finalStateStackStr = string.Join(" ", stateStack.Reverse());
        string finalSymbolStackStr = string.Join(" ", symbolStack.Reverse());
        Console.WriteLine($"{step,5} | {finalStateStackStr,15} | {finalSymbolStackStr,15} | {"$",15} | {"ERROR: Fin inesperado",20} | {"",15}");
        return false;
    }

    // Método para mostrar toda la información del análisis SLR
    public void PrintFullSLRInfo()
    {
        if (!_tableBuilt)
        {
            try
            {
                BuildParsingTables();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== ERROR AL CONSTRUIR LA TABLA SLR ===");
                Console.WriteLine($"Error: {ex.Message}");
                return;
            }
        }

        string separator = new string('=', 60);

        // Imprimir la información de la gramática
        Console.WriteLine("\n" + separator);
        Console.WriteLine("                   ANÁLISIS SLR(1) COMPLETO                   ");
        Console.WriteLine(separator);

        // Imprimir la gramática
        Console.WriteLine("\n=== GRAMÁTICA ===");
        Console.WriteLine($"Símbolo inicial: {_grammar.StartSymbol}");
        Console.WriteLine("Producciones:");

        int productionCount = 1;
        foreach (var nonTerminal in _grammar.Nonterminals.OrderBy(nt => nt))
        {
            if (_grammar.Productions.TryGetValue(nonTerminal, out var productions))
            {
                foreach (var production in productions)
                {
                    Console.WriteLine($"  [{productionCount}] {FormatProduction(nonTerminal, production)}");
                    productionCount++;
                }
            }
        }

        // Imprimir información sobre terminales y no terminales
        Console.WriteLine($"\nNo terminales ({_grammar.Nonterminals.Count}): {string.Join(", ", _grammar.Nonterminals.OrderBy(nt => nt))}");
        Console.WriteLine($"Terminales ({_grammar.Terminals.Count}): {string.Join(", ", _grammar.Terminals.OrderBy(t => t))}");

        // Imprimir los conjuntos FIRST y FOLLOW
        Console.WriteLine("\n=== CONJUNTOS FIRST Y FOLLOW ===");
        Console.WriteLine($"{"No Terminal",10} | {"FIRST",20} | {"FOLLOW",20}");
        Console.WriteLine(new string('-', 55));

        foreach (var nonTerminal in _grammar.Nonterminals.OrderBy(nt => nt))
        {
            string firstSet = string.Join(", ", _firstFollow.First[nonTerminal].OrderBy(c => c).Select(c => c.ToString()));
            string followSet = string.Join(", ", _firstFollow.Follow[nonTerminal].OrderBy(c => c).Select(c => c.ToString()));

            if (string.IsNullOrEmpty(firstSet)) firstSet = "∅"; // Conjunto vacío
            if (string.IsNullOrEmpty(followSet)) followSet = "∅"; // Conjunto vacío

            Console.WriteLine($"{nonTerminal,10} | {{{firstSet},20}} | {{{followSet},20}}");
        }

        // Mostrar estadísticas del análisis SLR
        Console.WriteLine($"\n=== ESTADÍSTICAS DEL ANALIZADOR SLR ===");
        Console.WriteLine($"Número de estados: {_states.Count}");
        Console.WriteLine($"Tamaño de la tabla ACTION: {_actionTable.Count} entradas");
        Console.WriteLine($"Tamaño de la tabla GOTO: {_gotoTable.Count} entradas");

        // Contar acciones shift, reduce y accept
        int shiftCount = _actionTable.Count(a => a.Value.StartsWith("shift"));
        int reduceCount = _actionTable.Count(a => a.Value.StartsWith("reduce"));
        int acceptCount = _actionTable.Count(a => a.Value == "accept");

        Console.WriteLine($"Acciones shift: {shiftCount}");
        Console.WriteLine($"Acciones reduce: {reduceCount}");
        Console.WriteLine($"Acciones accept: {acceptCount}");

        // Imprimir los estados LR(0)
        PrintStates();

        // Imprimir las tablas ACTION y GOTO
        PrintParsingTable();
    }

    // Método auxiliar para formatear una producción
    private string FormatProduction(char lhs, List<char> rhs)
    {
        string rhsStr = rhs.Count == 0 ? "ε" : new string(rhs.ToArray());
        return $"{lhs} -> {rhsStr}";
    }

    // Método auxiliar para formatear un item LR(0) con el punto
    private string FormatItem(Item item)
    {
        string production = item.Production.Count == 0 ? "ε" : new string(item.Production.ToArray());
        string itemStr = $"{item.LHS} -> ";

        // Insertar el punto en la posición correcta
        if (item.DotPosition == 0)
        {
            itemStr += "• " + production;
        }
        else if (item.DotPosition == item.Production.Count)
        {
            itemStr += production + " •";
        }
        else
        {
            // Insertar el punto entre los caracteres
            string before = production.Substring(0, item.DotPosition);
            string after = production.Substring(item.DotPosition);
            itemStr += before + " • " + after;
        }

        return itemStr;
    }

    // Método para probar si la gramática es compatible con el análisis SLR(1)
    public bool TestGrammar(List<string> testStrings)
    {
        if (!_tableBuilt)
        {
            try
            {
                BuildParsingTables();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al construir las tablas SLR: {ex.Message}");
                return false;
            }
        }

        Console.WriteLine("\n=== PRUEBA DE LA GRAMÁTICA SLR(1) ===");
        Console.WriteLine($"{"Cadena",20} | {"Resultado",10}");
        Console.WriteLine(new string('-', 35));

        bool allSuccess = true;

        foreach (var testString in testStrings)
        {
            bool result = Parse(testString);
            Console.WriteLine($"{testString,20} | {(result ? "ACEPTA" : "RECHAZA"),10}");

            if (!result)
            {
                allSuccess = false;
            }
        }

        Console.WriteLine(new string('-', 35));
        Console.WriteLine($"Resultado general: {(allSuccess ? "TODAS LAS CADENAS ACEPTADAS" : "ALGUNAS CADENAS RECHAZADAS")}");

        return allSuccess;
    }

    // Método para verificar automáticamente las propiedades de una gramática SLR(1)
    public bool VerifySLR1Properties()
    {
        try
        {
            // 1. Verificar que la gramática esté bien formada
            if (!ValidateGrammar())
            {
                Console.WriteLine("La gramática no está bien formada.");
                return false;
            }

            // 2. Verificar que los conjuntos First y Follow estén calculados correctamente
            if (_firstFollow.First == null || _firstFollow.Follow == null)
            {
                Console.WriteLine("Los conjuntos First y Follow no han sido calculados.");
                return false;
            }

            // 3. Verificar que la construcción de la tabla SLR no genere conflictos
            _actionTable = new Dictionary<(int, char), string>();
            _gotoTable = new Dictionary<(int, char), int>();
            _states = new List<HashSet<Item>>();
            _tableBuilt = false;

            try
            {
                BuildParsingTables();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"La gramática no es SLR(1): {ex.Message}");
                return false;
            }

            // 4. Verificar que cada estado tenga acciones definidas para todos los terminales relevantes
            foreach (var state in Enumerable.Range(0, _states.Count))
            {
                var terminalSymbols = _grammar.Terminals.Union(new[] { '$' });

                // Obtener todos los símbolos terminales que siguen al punto en los ítems de este estado
                var terminalsInItems = _states[state]
                    .Where(item => !item.IsReduceItem && (_grammar.IsTerminal(item.NextSymbol) || item.NextSymbol == '$'))
                    .Select(item => item.NextSymbol);

                // Para cada terminal que sigue a un punto, debe haber una acción definida
                foreach (var terminal in terminalsInItems)
                {
                    if (!_actionTable.ContainsKey((state, terminal)))
                    {
                        Console.WriteLine($"El estado {state} no tiene acción definida para el símbolo '{terminal}'.");
                        return false;
                    }
                }

                // Para cada ítem de reducción en el estado, debe haber acciones de reducción
                // para todos los terminales en el conjunto Follow del no terminal izquierdo
                var reduceItems = _states[state].Where(item => item.IsReduceItem);
                foreach (var item in reduceItems)
                {
                    // Saltar el ítem de aceptación
                    if (item.LHS == 'Z' && item.Production.Count > 0 && item.Production[0] == _grammar.StartSymbol)
                    {
                        continue;
                    }

                    foreach (var terminal in GetFollow(item.LHS))
                    {
                        if (!_actionTable.ContainsKey((state, terminal)))
                        {
                            Console.WriteLine($"El estado {state} no tiene acción de reducción para el símbolo '{terminal}' en Follow({item.LHS}).");
                            return false;
                        }
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al verificar las propiedades SLR(1): {ex.Message}");
            return false;
        }
    }
}