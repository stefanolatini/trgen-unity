using System;

namespace Trgen
{
    /// <summary>
    /// Rappresenta un trigger programmabile, con memoria interna per le istruzioni.
    /// </summary>
    public class TrgenPort
    {
        /// <summary>
        /// Identificatore del trigger.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Tipologia di porta associata al trigger.
        /// Es. NS, SA, TMSI, TMSO, GPIO.
        /// </summary>
        public TriggerType Type { get; set; }

        /// <summary>
        /// Memoria delle istruzioni del trigger.
        /// </summary>
        public uint[] Memory { get; }

        /// <summary>
        /// Crea un nuovo trigger con un identificatore e una lunghezza di memoria specifica.
        /// </summary>
        /// <param name="id">Identificatore del trigger.</param>
        /// <param name="memoryLength">Numero di istruzioni programmabili.</param>
        public TrgenPort(int id, int memoryLength)
        {
            Id = id;
            Memory = new uint[memoryLength];
        }

        /// <summary>
        /// Imposta una istruzione nella memoria del trigger.
        /// </summary>
        /// <param name="index">Indice della memoria.</param>
        /// <param name="instruction">Valore dell'istruzione.</param>
        public void SetInstruction(int index, uint instruction)
        {
            if (index < 0 || index >= Memory.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            Memory[index] = instruction;
        }
    }

    public enum TriggerType
    {
        NS, SA, TMSI, TMSO, GPIO
    }
}
