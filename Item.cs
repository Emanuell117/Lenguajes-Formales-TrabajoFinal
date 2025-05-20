using System.Numerics;

namespace ProyectoFormales
{
    //La clase Item representa un ítem LR(0) en el contexto del análisis sintáctico SLR(1)
    public class Item
    {
        public char LHS { get; } // Lado izquierdo de la producción
        public List<char> Production { get; } // Lado derecho de la producción 
        public int DotPosition { get; } // Posición del punto en la producción 

        //Si el punto no está al final, devuelve Production[DotPosition].
        //Si el punto está al final, devuelve epsilon, indicando que la producción está completa.
        public char NextSymbol => DotPosition < Production.Count ? Production[DotPosition] : 'ε';

        public Item(char lhs, List<char> production, int dotPosition)
        {
            LHS = lhs;
            Production = production;
            DotPosition = dotPosition;
        }
        //Devuelve true si el punto está al final de la producción
        public bool IsReduceItem => DotPosition == Production.Count;

        //Mueve el punto una posición adelante, creando un nuevo ítem.
        public Item Advance()
        {
            if (DotPosition >= Production.Count)
                throw new InvalidOperationException("No se puede avanzar más allá de la producción.");
            return new Item(LHS, Production, DotPosition + 1);
        }

        //Verifica si dos items son iguales para evitar duplicados
        public override bool Equals(object obj)
        {
            if (obj is Item other)
                return LHS == other.LHS && Production.SequenceEqual(other.Production) && DotPosition == other.DotPosition;
            return false;
        }

        //Genera un código hash único para cada ítem
        public override int GetHashCode()
        {
            return HashCode.Combine(LHS, string.Join("", Production), DotPosition);
        }
    }
}
