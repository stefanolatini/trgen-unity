using System.Collections.Generic;

namespace Trgen
{
    /// <summary>
    /// Contiene le costanti per gli identificatori dei pin di trigger del dispositivo TriggerBox CoSANLab.
    /// 
    /// Questa classe statica definisce tutti gli ID numerici utilizzati per identificare
    /// i diversi tipi di pin hardware disponibili sul dispositivo. Include anche
    /// collezioni predefinite per operazioni su gruppi di pin dello stesso tipo.
    /// </summary>
    /// <remarks>
    /// <para>I pin sono organizzati in gruppi funzionali:</para>
    /// <list type="bullet">
    /// <item><description><strong>NeuroScan (NS0-NS7):</strong> Pin per amplificatori NeuroScan, IDs 0-7</description></item>
    /// <item><description><strong>Synamps (SA0-SA7):</strong> Pin per amplificatori Synamps, IDs 8-15</description></item>
    /// <item><description><strong>TMS (TMSO, TMSI):</strong> Pin per stimolazione magnetica transcranica, IDs 16-17</description></item>
    /// <item><description><strong>GPIO (GPIO0-GPIO7):</strong> Pin GPIO generici, IDs 18-25</description></item>
    /// </list>
    /// 
    /// <para>
    /// Le liste predefinite (<see cref="AllNs"/>, <see cref="AllSa"/>, etc.) permettono
    /// operazioni batch su tutti i pin di un tipo specifico.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Utilizzo di pin singoli
    /// client.StartTrigger(TriggerPin.NS5);
    /// client.StartTrigger(TriggerPin.GPIO3);
    /// 
    /// // Utilizzo di gruppi predefiniti
    /// client.ResetAll(TriggerPin.AllNs);    // Reset tutti i NeuroScan
    /// client.ResetAll(TriggerPin.AllGpio);  // Reset tutti i GPIO
    /// 
    /// // Creazione di gruppi personalizzati
    /// var customPins = new List&lt;int&gt; { 
    ///     TriggerPin.NS0, 
    ///     TriggerPin.SA2, 
    ///     TriggerPin.GPIO7 
    /// };
    /// client.StartTriggerList(customPins);
    /// </code>
    /// </example>
    public static class TriggerPin
    {
        // ===== Pin NeuroScan (NS0-NS7) =====
        /// <summary>Pin NeuroScan 0 (ID: 0)</summary>
        public const int NS0 = 0;
        /// <summary>Pin NeuroScan 1 (ID: 1)</summary>
        public const int NS1 = 1;
        /// <summary>Pin NeuroScan 2 (ID: 2)</summary>
        public const int NS2 = 2;
        /// <summary>Pin NeuroScan 3 (ID: 3)</summary>
        public const int NS3 = 3;
        /// <summary>Pin NeuroScan 4 (ID: 4)</summary>
        public const int NS4 = 4;
        /// <summary>Pin NeuroScan 5 (ID: 5)</summary>
        public const int NS5 = 5;
        /// <summary>Pin NeuroScan 6 (ID: 6)</summary>
        public const int NS6 = 6;
        /// <summary>Pin NeuroScan 7 (ID: 7)</summary>
        public const int NS7 = 7;

        // ===== Pin Synamps (SA0-SA7) =====
        /// <summary>Pin Synamps 0 (ID: 8)</summary>
        public const int SA0 = 8;
        /// <summary>Pin Synamps 1 (ID: 9)</summary>
        public const int SA1 = 9;
        /// <summary>Pin Synamps 2 (ID: 10)</summary>
        public const int SA2 = 10;
        /// <summary>Pin Synamps 3 (ID: 11)</summary>
        public const int SA3 = 11;
        /// <summary>Pin Synamps 4 (ID: 12)</summary>
        public const int SA4 = 12;
        /// <summary>Pin Synamps 5 (ID: 13)</summary>
        public const int SA5 = 13;
        /// <summary>Pin Synamps 6 (ID: 14)</summary>
        public const int SA6 = 14;
        /// <summary>Pin Synamps 7 (ID: 15)</summary>
        public const int SA7 = 15;

        // ===== Pin TMS =====
        /// <summary>Pin TMS Output (ID: 16)</summary>
        public const int TMSO = 16;
        /// <summary>Pin TMS Input (ID: 17)</summary>
        public const int TMSI = 17;

        // ===== Pin GPIO (GPIO0-GPIO7) =====
        /// <summary>Pin GPIO 0 (ID: 18)</summary>
        public const int GPIO0 = 18;
        /// <summary>Pin GPIO 1 (ID: 19)</summary>
        public const int GPIO1 = 19;
        /// <summary>Pin GPIO 2 (ID: 20)</summary>
        public const int GPIO2 = 20;
        /// <summary>Pin GPIO 3 (ID: 21)</summary>
        public const int GPIO3 = 21;
        /// <summary>Pin GPIO 4 (ID: 22)</summary>
        public const int GPIO4 = 22;
        /// <summary>Pin GPIO 5 (ID: 23)</summary>
        public const int GPIO5 = 23;
        /// <summary>Pin GPIO 6 (ID: 24)</summary>
        public const int GPIO6 = 24;
        /// <summary>Pin GPIO 7 (ID: 25)</summary>
        public const int GPIO7 = 25;

        /// <summary>
        /// Lista contenente tutti gli identificatori dei pin NeuroScan (NS0-NS7).
        /// Utile per operazioni batch su tutti i pin NeuroScan.
        /// </summary>
        /// <example>
        /// <code>
        /// // Reset di tutti i pin NeuroScan
        /// client.ResetAll(TriggerPin.AllNs);
        /// 
        /// // Programmazione di tutti i pin NeuroScan
        /// client.StartTriggerList(TriggerPin.AllNs);
        /// </code>
        /// </example>
        public static readonly List<int> AllNs = new() { NS0, NS1, NS2, NS3, NS4, NS5, NS6, NS7 };

        /// <summary>
        /// Lista contenente tutti gli identificatori dei pin Synamps (SA0-SA7).
        /// Utile per operazioni batch su tutti i pin Synamps.
        /// </summary>
        /// <example>
        /// <code>
        /// // Reset di tutti i pin Synamps
        /// client.ResetAll(TriggerPin.AllSa);
        /// </code>
        /// </example>
        public static readonly List<int> AllSa = new() { SA0, SA1, SA2, SA3, SA4, SA5, SA6, SA7 };

        /// <summary>
        /// Lista contenente tutti gli identificatori dei pin GPIO (GPIO0-GPIO7).
        /// Utile per operazioni batch su tutti i pin GPIO.
        /// </summary>
        /// <example>
        /// <code>
        /// // Reset di tutti i pin GPIO
        /// client.ResetAll(TriggerPin.AllGpio);
        /// 
        /// // Attivazione simultanea di tutti i GPIO
        /// client.StartTriggerList(TriggerPin.AllGpio);
        /// </code>
        /// </example>
        public static readonly List<int> AllGpio = new() { GPIO0, GPIO1, GPIO2, GPIO3, GPIO4, GPIO5, GPIO6, GPIO7 };

        /// <summary>
        /// Lista contenente tutti gli identificatori dei pin TMS (TMSO, TMSI).
        /// Utile per operazioni batch sui pin di stimolazione magnetica transcranica.
        /// </summary>
        /// <example>
        /// <code>
        /// // Reset di tutti i pin TMS
        /// client.ResetAll(TriggerPin.AllTMS);
        /// </code>
        /// </example>
        public static readonly List<int> AllTMS = new() { TMSO, TMSI };
    }
}
