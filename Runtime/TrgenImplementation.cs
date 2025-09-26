namespace Trgen
{
    /// <summary>
    /// Rappresenta la configurazione hardware e le capacità specifiche di un dispositivo TriggerBox.
    /// 
    /// Questa classe incapsula le informazioni sulla configurazione hardware del dispositivo,
    /// inclusi il numero di pin disponibili per ogni tipo e la dimensione della memoria
    /// programmabile per i trigger. I dati vengono ottenuti direttamente dal dispositivo
    /// durante il processo di connessione.
    /// </summary>
    /// <remarks>
    /// <para>
    /// La configurazione hardware varia tra i diversi modelli di TriggerBox e può includere:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Pin NeuroScan:</strong> Connettori per amplificatori NeuroScan (tipicamente 0-8)</description></item>
    /// <item><description><strong>Pin Synamps:</strong> Connettori per amplificatori Synamps (tipicamente 0-8)</description></item>
    /// <item><description><strong>Pin TMS:</strong> Connettori per stimolazione magnetica transcranica (TMSO/TMSI)</description></item>
    /// <item><description><strong>Pin GPIO:</strong> Pin GPIO generici programmabili (tipicamente 0-8)</description></item>
    /// <item><description><strong>Memoria Trigger:</strong> Dimensione della memoria programmabile (tipicamente 32-64 istruzioni)</description></item>
    /// </list>
    /// 
    /// <para>
    /// Questa informazione è fondamentale per determinare quali operazioni sono supportate
    /// dal dispositivo specifico e per dimensionare correttamente le sequenze programmate.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Lettura della configurazione durante la connessione
    /// await client.ConnectAsync();
    /// int packed = await client.RequestImplementationAsync();
    /// var impl = new TrgenImplementation(packed);
    /// 
    /// Debug.Log($"Pin NeuroScan: {impl.NsNum}");
    /// Debug.Log($"Pin GPIO: {impl.GpioNum}"); 
    /// Debug.Log($"Memoria per trigger: {impl.MemoryLength}");
    /// 
    /// // Verifica supporto per un tipo di pin
    /// if (impl.GpioNum > 0)
    /// {
    ///     Debug.Log("Dispositivo supporta pin GPIO");
    ///     client.StartTrigger(TrgenPin.GPIO0);
    /// }
    /// </code>
    /// </example>
    public class TrgenImplementation
    {


        /// <summary>
        /// Crea una nuova istanza di TrgenImplementation decodificando la configurazione hardware
        /// dal valore packed restituito dal dispositivo TriggerBox.
        /// </summary>
        /// <param name="packed">
        /// Valore a 32 bit contenente la configurazione hardware codificata del dispositivo.
        /// Questo valore viene ottenuto tramite il comando di richiesta implementazione (0x04).
        /// </param>
        /// <remarks>
        /// <para>Il valore packed contiene la configurazione codificata nei seguenti bit:</para>
        /// <list type="bullet">
        /// <item><description>Bit 0-4 (5 bit): Numero di pin NeuroScan (0-31)</description></item>
        /// <item><description>Bit 5-9 (5 bit): Numero di pin Synamps (0-31)</description></item>
        /// <item><description>Bit 10-12 (3 bit): Numero di pin TMSO (0-7)</description></item>
        /// <item><description>Bit 13-15 (3 bit): Numero di pin TMSI (0-7)</description></item>
        /// <item><description>Bit 16-20 (5 bit): Numero di pin GPIO (0-31)</description></item>
        /// <item><description>Bit 26-31 (6 bit): Dimensione memoria programmabile (0-63)</description></item>
        /// </list>
        /// 
        /// <para>
        /// La decodifica viene eseguita automaticamente utilizzando operazioni bitwise
        /// per estrarre i singoli campi dal valore compresso.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Ottenimento e decodifica della configurazione
        /// int packed = client.RequestImplementation();
        /// var config = new TrgenImplementation(packed);
        /// 
        /// // Verifica delle capacità hardware
        /// Console.WriteLine($"Configurazione TriggerBox:");
        /// Console.WriteLine($"  Pin NeuroScan: {config.NsNum}");
        /// Console.WriteLine($"  Pin Synamps: {config.SaNum}");
        /// Console.WriteLine($"  Pin GPIO: {config.GpioNum}");
        /// Console.WriteLine($"  Memoria trigger: {config.MemoryLength} istruzioni");
        /// </code>
        /// </example>
        public TrgenImplementation(int packed)
        {
            NsNum        = (packed >> 0)  & 0x1F;
            SaNum        = (packed >> 5)  & 0x1F;
            TmsoNum      = (packed >> 10) & 0x07;
            TmsiNum      = (packed >> 13) & 0x07;
            GpioNum      = (packed >> 16) & 0x1F;
            MemoryLength = (packed >> 26) & 0x3F;
        }

        /// <summary>
        /// Numero di pin NeuroScan disponibili sul dispositivo (0-31).
        /// </summary>
        /// <value>
        /// Intero rappresentante il numero di connettori NeuroScan fisicamente presenti
        /// sul dispositivo. Tipicamente da 0 a 8 nei dispositivi standard.
        /// </value>
        public int NsNum { get; }
        
        /// <summary>
        /// Numero di pin Synamps disponibili sul dispositivo (0-31).
        /// </summary>
        /// <value>
        /// Intero rappresentante il numero di connettori Synamps fisicamente presenti
        /// sul dispositivo. Tipicamente da 0 a 8 nei dispositivi standard.
        /// </value>
        public int SaNum { get; }
        
        /// <summary>
        /// Numero di pin TMSO (TMS Output) disponibili sul dispositivo (0-7).
        /// </summary>
        /// <value>
        /// Intero rappresentante il numero di pin di output per stimolazione
        /// magnetica transcranica. Tipicamente 0 o 1 nei dispositivi standard.
        /// </value>
        public int TmsoNum { get; }
        
        /// <summary>
        /// Numero di pin TMSI (TMS Input) disponibili sul dispositivo (0-7).
        /// </summary>
        /// <value>
        /// Intero rappresentante il numero di pin di input per stimolazione
        /// magnetica transcranica. Tipicamente 0 o 1 nei dispositivi standard.
        /// </value>
        public int TmsiNum { get; }
        
        /// <summary>
        /// Numero di pin GPIO generici disponibili sul dispositivo (0-31).
        /// </summary>
        /// <value>
        /// Intero rappresentante il numero di pin GPIO programmabili fisicamente
        /// presenti sul dispositivo. Tipicamente da 0 a 8 nei dispositivi standard.
        /// </value>
        public int GpioNum { get; }
        
        /// <summary>
        /// Dimensione della memoria programmabile per ciascun trigger in numero di istruzioni.
        /// </summary>
        /// <value>
        /// Intero rappresentante il numero massimo di istruzioni che possono essere
        /// memorizzate per ogni trigger. Tipicamente 32 o 64 istruzioni nei dispositivi standard.
        /// Questa dimensione determina la complessità massima delle sequenze programmabili.
        /// </value>
        /// <remarks>
        /// Questo valore è fondamentale per:
        /// - Dimensionare correttamente gli array di memoria dei trigger
        /// - Verificare che le sequenze programmate non superino i limiti hardware
        /// - Ottimizzare l'utilizzo della memoria disponibile per sequenze complesse
        /// </remarks>
        public int MemoryLength { get; }
    }
}
