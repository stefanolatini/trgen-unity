namespace Trgen
{
    /// <summary>
    /// Rappresenta la configurazione e le capacità del dispositivo TrGEN.
    /// </summary>
    public class TrgenImplementation
    {


        /// <summary>
        /// Crea una nuova istanza di TrgenImplementation a partire dal valore restituito dal dispositivo.
        /// </summary>
        /// <param name="packed">Valore packed ricevuto dal dispositivo.</param>
        public TrgenImplementation(int packed)
        {
            NsNum        = (packed >> 0)  & 0x1F;
            SaNum        = (packed >> 5)  & 0x1F;
            TmsoNum      = (packed >> 10) & 0x07;
            TmsiNum      = (packed >> 13) & 0x07;
            GpioNum      = (packed >> 16) & 0x1F;
            MemoryLength = (packed >> 26) & 0x3F;
        }

        public int NsNum { get; }
        public int SaNum { get; }
        public int TmsoNum { get; }
        public int TmsiNum { get; }
        public int GpioNum { get; }
        /// <summary>
        /// Lunghezza della memoria programmabile per ciascun trigger.
        /// </summary>
        public int MemoryLength { get; }
    }
}
