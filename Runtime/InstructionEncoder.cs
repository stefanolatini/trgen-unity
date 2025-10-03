namespace Trgen
{
    /// <summary>
    /// Fornisce metodi statici per la codifica delle istruzioni programmate sui trigger del TriggerBox.
    /// 
    /// Questa classe offre un'interfaccia di alto livello per creare le istruzioni che vengono
    /// caricate nella memoria dei trigger. Ogni trigger può eseguire una sequenza di istruzioni
    /// che controllano quando il pin è attivo, inattivo, o attende eventi esterni.
    /// </summary>
    /// <remarks>
    /// <para>Le istruzioni supportate includono:</para>
    /// <list type="bullet">
    /// <item><description><strong>Attivo/Inattivo:</strong> Controllano lo stato logico del pin per un tempo determinato</description></item>
    /// <item><description><strong>Wait PE/NE:</strong> Attendono eventi di positive/negative edge su altri trigger</description></item>
    /// <item><description><strong>Repeat:</strong> Ripetono parti di sequenza un numero specificato di volte</description></item>
    /// <item><description><strong>End:</strong> Terminano l'esecuzione della sequenza</description></item>
    /// <item><description><strong>Not Admissible:</strong> Istruzioni di riempimento per prevenire esecuzioni indesiderate</description></item>
    /// </list>
    /// 
    /// <para>
    /// Le istruzioni vengono codificate come valori a 32 bit che combinano il tipo di operazione
    /// con i parametri specifici (durata, indirizzo, numero di ripetizioni).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var trigger = client.CreateTrgenPort(TrgenPin.NS5);
    /// 
    /// // Sequenza: attivo 50μs, inattivo 10μs, ripeti 3 volte, fine
    /// trigger.SetInstruction(0, InstructionEncoder.ActiveForUs(50));
    /// trigger.SetInstruction(1, InstructionEncoder.UnactiveForUs(10));
    /// trigger.SetInstruction(2, InstructionEncoder.Repeat(0, 3));
    /// trigger.SetInstruction(3, InstructionEncoder.End());
    /// 
    /// client.SetTrgenMemory(trigger);
    /// </code>
    /// </example>
    public static class InstructionEncoder
    {
        /// <summary>Codice di istruzione per stato inattivo (pin LOW)</summary>
        private const uint INST_UNACTIVE = 0;
        /// <summary>Codice di istruzione per stato attivo (pin HIGH)</summary>
        private const uint INST_ACTIVE = 1;
        /// <summary>Codice di istruzione per attesa di positive edge</summary>
        private const uint INST_WAITPE = 2;
        /// <summary>Codice di istruzione per attesa di negative edge</summary>
        private const uint INST_WAITNE = 3;
        /// <summary>Codice di istruzione per loop/ripetizione</summary>
        private const uint INST_REPEAT = 7;
        /// <summary>Codice di istruzione per terminazione sequenza</summary>
        private const uint INST_END = 4;
        /// <summary>Codice di istruzione non ammissibile (riempimento memoria)</summary>
        private const uint INST_NOT_ADMISSIBLE = 6;

        /// <summary>
        /// Crea un'istruzione per mantenere il pin attivo (HIGH) per un tempo specificato.
        /// </summary>
        /// <param name="us">Durata in microsecondi per cui mantenere il pin attivo.</param>
        /// <returns>Istruzione codificata che attiva il pin per la durata specificata.</returns>
        /// <remarks>
        /// Durante l'esecuzione di questa istruzione, il pin del trigger sarà nello stato
        /// logico alto per il tempo specificato, permettendo la generazione di impulsi
        /// di durata precisa per applicazioni di sincronizzazione e stimolazione.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Pin attivo per 20 microsecondi
        /// uint instruction = InstructionEncoder.ActiveForUs(20);
        /// trigger.SetInstruction(0, instruction);
        /// </code>
        /// </example>
        public static uint ActiveForUs(uint us) => (us << 3) | INST_ACTIVE;
        
        /// <summary>
        /// Crea un'istruzione per mantenere il pin inattivo (LOW) per un tempo specificato.
        /// </summary>
        /// <param name="us">Durata in microsecondi per cui mantenere il pin inattivo.</param>
        /// <returns>Istruzione codificata che mantiene il pin inattivo per la durata specificata.</returns>
        /// <remarks>
        /// Questa istruzione mantiene il pin nello stato logico basso, utile per creare
        /// pause tra impulsi attivi o per garantire tempi di ripristino appropriati
        /// tra segnali di trigger successivi.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Pin inattivo per 5 microsecondi (tempo di ripristino)
        /// uint instruction = InstructionEncoder.UnactiveForUs(5);
        /// trigger.SetInstruction(1, instruction);
        /// </code>
        /// </example>
        public static uint UnactiveForUs(uint us) => (us << 3) | INST_UNACTIVE;
        
        /// <summary>
        /// Crea un'istruzione per attendere un positive edge su un altro trigger.
        /// </summary>
        /// <param name="tr">ID del trigger da monitorare per il positive edge.</param>
        /// <returns>Istruzione codificata per l'attesa del positive edge.</returns>
        /// <remarks>
        /// L'esecuzione si fermerà su questa istruzione fino a quando il trigger specificato
        /// non passerà da stato LOW a HIGH. Utile per sincronizzare l'esecuzione di trigger
        /// multipli o per creare sequenze condizionali basate su eventi esterni.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Attende che il trigger NS0 vada da LOW a HIGH
        /// uint instruction = InstructionEncoder.WaitPE(TrgenPin.NS0);
        /// trigger.SetInstruction(2, instruction);
        /// </code>
        /// </example>
        public static uint WaitPE(uint tr) => (tr << 3) | INST_WAITPE;
        
        /// <summary>
        /// Crea un'istruzione per attendere un negative edge su un altro trigger.
        /// </summary>
        /// <param name="tr">ID del trigger da monitorare per il negative edge.</param>
        /// <returns>Istruzione codificata per l'attesa del negative edge.</returns>
        /// <remarks>
        /// L'esecuzione si fermerà su questa istruzione fino a quando il trigger specificato
        /// non passerà da stato HIGH a LOW. Complementare a <see cref="WaitPE"/>, permette
        /// di sincronizzare l'esecuzione su transizioni di discesa dei segnali.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Attende che il trigger GPIO3 vada da HIGH a LOW
        /// uint instruction = InstructionEncoder.WaitNE(TrgenPin.GPIO3);
        /// trigger.SetInstruction(3, instruction);
        /// </code>
        /// </example>
        public static uint WaitNE(uint tr) => (tr << 3) | INST_WAITNE;
        
        /// <summary>
        /// Crea un'istruzione per ripetere una porzione di sequenza un numero specificato di volte.
        /// </summary>
        /// <param name="addr">Indirizzo di memoria (numero di istruzione) da cui ripartire per la ripetizione.</param>
        /// <param name="times">Numero di volte da ripetere la sequenza dall'indirizzo specificato.</param>
        /// <returns>Istruzione codificata per il loop di ripetizione.</returns>
        /// <remarks>
        /// Questa istruzione crea un loop che riporta l'esecuzione all'indirizzo specificato
        /// per il numero di volte richiesto. Permette di creare pattern complessi senza
        /// duplicare istruzioni nella memoria del trigger.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Ripeti dalla istruzione 0 per 5 volte
        /// trigger.SetInstruction(0, InstructionEncoder.ActiveForUs(10));
        /// trigger.SetInstruction(1, InstructionEncoder.UnactiveForUs(10));
        /// trigger.SetInstruction(2, InstructionEncoder.Repeat(0, 5)); // Ripeti 5 volte
        /// trigger.SetInstruction(3, InstructionEncoder.End());
        /// </code>
        /// </example>
        public static uint Repeat(uint addr, uint times) => (times << 8) | (addr << 3) | INST_REPEAT;
        
        /// <summary>
        /// Crea un'istruzione di terminazione per concludere la sequenza di un trigger.
        /// </summary>
        /// <returns>Istruzione codificata per terminare l'esecuzione della sequenza.</returns>
        /// <remarks>
        /// Questa istruzione deve essere presente alla fine di ogni sequenza valida per
        /// indicare al dispositivo che l'esecuzione del trigger è completata. Senza
        /// questa istruzione, il trigger potrebbe continuare a leggere istruzioni 
        /// casuali dalla memoria con comportamenti imprevedibili.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Sequenza completa con terminazione
        /// trigger.SetInstruction(0, InstructionEncoder.ActiveForUs(20));
        /// trigger.SetInstruction(1, InstructionEncoder.UnactiveForUs(5));
        /// trigger.SetInstruction(2, InstructionEncoder.End()); // Terminazione obbligatoria
        /// </code>
        /// </example>
        public static uint End() => INST_END;
        
        /// <summary>
        /// Crea un'istruzione "non ammissibile" utilizzata per riempire la memoria inutilizzata.
        /// </summary>
        /// <returns>Istruzione codificata non ammissibile per il riempimento della memoria.</returns>
        /// <remarks>
        /// Questa istruzione viene utilizzata per riempire le posizioni di memoria non utilizzate
        /// di un trigger, garantendo che eventuali errori nell'esecuzione della sequenza non
        /// causino comportamenti indesiderati. Se il trigger dovesse eseguire accidentalmente
        /// questa istruzione, genererà un errore identificabile invece di un comportamento casuale.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Riempimento sicuro della memoria dopo la sequenza principale
        /// trigger.SetInstruction(0, InstructionEncoder.ActiveForUs(20));
        /// trigger.SetInstruction(1, InstructionEncoder.End());
        /// 
        /// // Riempi il resto della memoria con istruzioni non ammissibili
        /// for (int i = 2; i &lt; memorySize; i++)
        /// {
        ///     trigger.SetInstruction(i, InstructionEncoder.NotAdmissible());
        /// }
        /// </code>
        /// </example>
        public static uint NotAdmissible() => INST_NOT_ADMISSIBLE;
    }
}
