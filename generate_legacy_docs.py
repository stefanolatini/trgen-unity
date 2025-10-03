#!/usr/bin/env python3
"""
Legacy .NET Documentation Generator
Simula l'approccio classico di NDoc/Sandcastle per Unity projects
"""

import re
import os
import xml.etree.ElementTree as ET
from pathlib import Path
import argparse

class LegacyDocGenerator:
    """Generatore documentazione stile legacy .NET"""
    
    def __init__(self, source_dir="Runtime", output_dir="docs-site"):
        self.source_dir = source_dir
        self.output_dir = output_dir
        self.api_dir = os.path.join(output_dir, "api")
        self.guide_dir = os.path.join(output_dir, "guide")
        self.examples_dir = os.path.join(output_dir, "examples")
        
    def generate_docs(self):
        """Genera documentazione completa"""
        print("🏛️ Legacy .NET Documentation Generator")
        print("=" * 50)
        
        # Crea directory output per VuePress
        os.makedirs(self.api_dir, exist_ok=True)
        os.makedirs(self.guide_dir, exist_ok=True)
        os.makedirs(self.examples_dir, exist_ok=True)
        
        # Trova tutti i file C#
        cs_files = list(Path(self.source_dir).glob("**/*.cs"))
        
        print(f"📁 Found {len(cs_files)} C# files")
        print(f"📂 Generating VuePress documentation in: {self.output_dir}")
        
        # Genera documentazione per ogni file
        all_classes = []
        for cs_file in cs_files:
            class_info = self.process_cs_file(cs_file)
            if class_info:
                all_classes.append(class_info)
        
        # Genera index API per VuePress
        self.generate_api_index(all_classes)
        
        # Genera pagine aggiuntive per VuePress
        self.generate_guide_pages()
        self.generate_example_pages()
        
        print(f"✅ Generated VuePress documentation for {len(all_classes)} classes")
        print(f"📂 Output directory: {self.output_dir}")
        print("🚀 Run 'npm run dev' to preview the documentation")
        
    def process_cs_file(self, cs_file):
        """Processa un singolo file C#"""
        print(f"📝 Processing {cs_file}")
        
        with open(cs_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Estrai informazioni della classe
        class_info = self.extract_class_info(content)
        if not class_info:
            return None
            
        class_name = class_info['name']
        
        # Estrai membri della classe
        members = self.extract_members(content)
        
        # Genera file markdown
        output_file = os.path.join(self.api_dir, f"{class_name}.md")
        self.generate_class_doc(class_info, members, output_file)
        
        return {
            'name': class_name,
            'file': cs_file,
            'summary': class_info.get('summary', ''),
            'member_count': len(members)
        }
    
    def extract_class_info(self, content):
        """Estrai informazioni della classe principale"""
        # Pattern per trovare classe con documentazione
        class_pattern = r'///\s*<summary>(.*?)</summary>.*?public\s+(class|enum|interface)\s+(\w+)'
        
        match = re.search(class_pattern, content, re.DOTALL | re.MULTILINE)
        if match:
            summary = re.sub(r'\s+', ' ', match.group(1).strip())
            summary = re.sub(r'///\s*', '', summary)
            class_type = match.group(2)
            class_name = match.group(3)
            
            return {
                'name': class_name,
                'type': class_type,
                'summary': summary
            }
        
        # Fallback: cerca classe senza documentazione
        simple_class = re.search(r'public\s+(class|enum|interface)\s+(\w+)', content)
        if simple_class:
            return {
                'name': simple_class.group(2),
                'type': simple_class.group(1),
                'summary': 'No documentation available'
            }
        
        return None
    
    def extract_members(self, content):
        """Estrai membri della classe (metodi, proprietà, etc.)"""
        members = []
        
        # Pattern per trovare membri con documentazione
        member_pattern = r'///\s*<summary>(.*?)</summary>(.*?)(?=///|\Z)'
        
        for match in re.finditer(member_pattern, content, re.DOTALL):
            summary = re.sub(r'\s+', ' ', match.group(1).strip())
            summary = re.sub(r'///\s*', '', summary)
            
            code_block = match.group(2)
            
            # Cerca dichiarazione del membro
            member_decl = self.find_member_declaration(code_block)
            if member_decl:
                # Estrai parametri se presenti
                params = self.extract_parameters(match.group(0))
                returns = self.extract_returns(match.group(0))
                
                members.append({
                    'name': member_decl['name'],
                    'type': member_decl['type'],
                    'signature': member_decl['signature'],
                    'summary': summary,
                    'parameters': params,
                    'returns': returns
                })
        
        return members
    
    def find_member_declaration(self, code_block):
        """Trova la dichiarazione del membro nel blocco di codice"""
        lines = code_block.split('\n')
        
        for line in lines:
            line = line.strip()
            if not line or line.startswith('///') or line.startswith('//'):
                continue
                
            # Metodo
            method_match = re.search(r'public\s+.*?\s+(\w+)\s*\(', line)
            if method_match:
                return {
                    'name': method_match.group(1),
                    'type': 'Method',
                    'signature': line.rstrip('{').strip()
                }
            
            # Proprietà
            prop_match = re.search(r'public\s+.*?\s+(\w+)\s*{', line)
            if prop_match:
                return {
                    'name': prop_match.group(1),
                    'type': 'Property',
                    'signature': line.rstrip('{').strip()
                }
            
            # Costruttore
            ctor_match = re.search(r'public\s+(\w+)\s*\(', line)
            if ctor_match:
                return {
                    'name': ctor_match.group(1),
                    'type': 'Constructor',
                    'signature': line.rstrip('{').strip()
                }
        
        return None
    
    def extract_parameters(self, doc_block):
        """Estrai parametri dalla documentazione XML"""
        param_pattern = r'///\s*<param name="([^"]+)">(.*?)</param>'
        params = []
        
        for match in re.finditer(param_pattern, doc_block, re.DOTALL):
            param_name = match.group(1)
            param_desc = re.sub(r'\s+', ' ', match.group(2).strip())
            param_desc = re.sub(r'///\s*', '', param_desc)
            params.append({
                'name': param_name,
                'description': param_desc
            })
        
        return params
    
    def extract_returns(self, doc_block):
        """Estrai informazioni di ritorno dalla documentazione XML"""
        returns_pattern = r'///\s*<returns>(.*?)</returns>'
        match = re.search(returns_pattern, doc_block, re.DOTALL)
        
        if match:
            returns_desc = re.sub(r'\s+', ' ', match.group(1).strip())
            returns_desc = re.sub(r'///\s*', '', returns_desc)
            return returns_desc
        
        return None
    
    def generate_class_doc(self, class_info, members, output_file):
        """Genera documentazione per una singola classe"""
        with open(output_file, 'w', encoding='utf-8') as f:
            # Header
            f.write(f"# {class_info['name']} {class_info['type']}\n\n")
            f.write("**Generated using Legacy .NET Documentation Tools**\n\n")
            f.write("---\n\n")
            
            # Summary della classe
            f.write("## Overview\n\n")
            f.write(f"{class_info['summary']}\n\n")
            
            # Namespace info
            f.write("```csharp\n")
            f.write(f"namespace Trgen\n")
            f.write(f"{{\n")
            f.write(f"    public {class_info['type'].lower()} {class_info['name']}\n")
            f.write(f"}}\n")
            f.write("```\n\n")
            
            if not members:
                f.write("*No documented members found.*\n\n")
                return
            
            # Raggruppa membri per tipo
            constructors = [m for m in members if m['type'] == 'Constructor']
            methods = [m for m in members if m['type'] == 'Method']
            properties = [m for m in members if m['type'] == 'Property']
            
            # Constructors
            if constructors:
                f.write("## Constructors\n\n")
                for ctor in constructors:
                    self.write_member_doc(f, ctor)
            
            # Properties
            if properties:
                f.write("## Properties\n\n")
                for prop in properties:
                    self.write_member_doc(f, prop)
            
            # Methods
            if methods:
                f.write("## Methods\n\n")
                for method in methods:
                    self.write_member_doc(f, method)
            
            # Footer
            f.write("---\n\n")
            f.write("*This documentation was generated using legacy .NET documentation extraction techniques.*\n")
    
    def write_member_doc(self, f, member):
        """Scrivi documentazione per un singolo membro"""
        f.write(f"### {member['name']}\n\n")
        f.write(f"{member['summary']}\n\n")
        
        # Parametri
        if member['parameters']:
            f.write("**Parameters:**\n\n")
            for param in member['parameters']:
                f.write(f"- `{param['name']}`: {param['description']}\n")
            f.write("\n")
        
        # Returns
        if member['returns']:
            f.write(f"**Returns:** {member['returns']}\n\n")
        
        # Signature
        f.write("**Signature:**\n")
        f.write("```csharp\n")
        f.write(f"{member['signature']}\n")
        f.write("```\n\n")
    
    def generate_api_index(self, all_classes):
        """Genera index API per VuePress"""
        index_file = os.path.join(self.api_dir, "README.md")
        
        with open(index_file, 'w', encoding='utf-8') as f:
            f.write("# API Reference\n\n")
            f.write("Documentazione completa delle classi TrGEN Unity generate con tecniche legacy .NET.\n\n")
            
            f.write("## Classi Principali\n\n")
            for class_info in all_classes:
                f.write(f"### [{class_info['name']}](./{class_info['name']}.md)\n\n")
                f.write(f"{class_info['summary']}\n\n")
                f.write(f"**Membri documentati:** {class_info['member_count']}\n\n")
                f.write("---\n\n")
            
            f.write("## Informazioni Tecniche\n\n")
            f.write("Questa documentazione è stata generata utilizzando:\n\n")
            f.write("- **XML Documentation Comments**: Standard .NET (`/// <summary>`)\n")
            f.write("- **Pattern Matching**: Analisi testuale con regex\n")
            f.write("- **Legacy XML Processing**: Parsing ElementTree classico\n")
            f.write("- **VuePress Output**: Formato moderno per il web\n\n")
    
    def generate_guide_pages(self):
        """Genera pagine della guida"""
        # Index della guida
        guide_index = os.path.join(self.guide_dir, "README.md")
        with open(guide_index, 'w', encoding='utf-8') as f:
            f.write("# Guida TrGEN Unity\n\n")
            f.write("Benvenuto nella guida completa per l'utilizzo del package TrGEN Unity.\n\n")
            f.write("## Sezioni\n\n")
            f.write("- [Installazione](./installation.md)\n")
            f.write("- [Guida Rapida](./quickstart.md)\n")
            f.write("- [Configurazione](./configuration.md)\n")
            f.write("- [Risoluzione Problemi](./troubleshooting.md)\n\n")
        
        # Pagina installazione
        install_page = os.path.join(self.guide_dir, "installation.md")
        with open(install_page, 'w', encoding='utf-8') as f:
            f.write("# Installazione\n\n")
            f.write("## Requisiti\n\n")
            f.write("- Unity 2021.3 LTS o superiore\n")
            f.write("- .NET Standard 2.1\n")
            f.write("- Dispositivo TriggerBox CoSANLab\n\n")
            f.write("## Metodi di Installazione\n\n")
            f.write("### Via OpenUPM (Raccomandato)\n\n")
            f.write("```bash\n")
            f.write("openupm add com.cosanlab.trgen\n")
            f.write("```\n\n")
            f.write("### Via Package Manager\n\n")
            f.write("1. Apri Unity Package Manager\n")
            f.write("2. Clicca '+' → 'Add package from git URL'\n")
            f.write("3. Inserisci: `https://github.com/stefanolatini/trgen-unity.git`\n\n")
        
        # Pagina quickstart
        quickstart_page = os.path.join(self.guide_dir, "quickstart.md")
        with open(quickstart_page, 'w', encoding='utf-8') as f:
            f.write("# Guida Rapida\n\n")
            f.write("## Primo Utilizzo\n\n")
            f.write("```csharp\n")
            f.write("using Trgen;\n")
            f.write("using UnityEngine;\n\n")
            f.write("public class MyTriggerController : MonoBehaviour\n")
            f.write("{\n")
            f.write("    private TrgenClient client;\n\n")
            f.write("    async void Start()\n")
            f.write("    {\n")
            f.write("        client = new TrgenClient();\n")
            f.write("        await client.ConnectAsync('192.168.1.100', 4000);\n")
            f.write("        Debug.Log('Connesso!');\n")
            f.write("    }\n\n")
            f.write("    async void SendTrigger()\n")
            f.write("    {\n")
            f.write("        await client.StartTriggerAsync(TrgenPin.NS0);\n")
            f.write("    }\n")
            f.write("}\n")
            f.write("```\n\n")
        
        # Altre pagine guida
        config_page = os.path.join(self.guide_dir, "configuration.md")
        with open(config_page, 'w', encoding='utf-8') as f:
            f.write("# Configurazione\n\n")
            f.write("## File di Configurazione JSON\n\n")
            f.write("TrGEN Unity supporta configurazioni complete tramite file JSON.\n\n")
            f.write("```json\n")
            f.write('{\n')
            f.write('  "networkSettings": {\n')
            f.write('    "ipAddress": "192.168.1.100",\n')
            f.write('    "port": 4000\n')
            f.write('  },\n')
            f.write('  "triggerPorts": [\n')
            f.write('    {\n')
            f.write('      "portNumber": 0,\n')
            f.write('      "pinType": "NeuroScan",\n')
            f.write('      "isEnabled": true\n')
            f.write('    }\n')
            f.write('  ]\n')
            f.write('}\n')
            f.write("```\n\n")
        
        troubleshooting_page = os.path.join(self.guide_dir, "troubleshooting.md")
        with open(troubleshooting_page, 'w', encoding='utf-8') as f:
            f.write("# Risoluzione Problemi\n\n")
            f.write("## Problemi di Connessione\n\n")
            f.write("### Errore: Impossibile connettersi\n\n")
            f.write("1. Verificare l'indirizzo IP del dispositivo\n")
            f.write("2. Controllare la connessione di rete\n")
            f.write("3. Verificare che la porta 4000 sia aperta\n\n")
            f.write("### Timeout di Connessione\n\n")
            f.write("- Aumentare il valore di timeout\n")
            f.write("- Verificare la stabilità della rete\n\n")
    
    def generate_example_pages(self):
        """Genera pagine di esempi"""
        # Index esempi
        examples_index = os.path.join(self.examples_dir, "README.md")
        with open(examples_index, 'w', encoding='utf-8') as f:
            f.write("# Esempi di Codice\n\n")
            f.write("Raccolta di esempi pratici per l'utilizzo di TrGEN Unity.\n\n")
            f.write("## Disponibili\n\n")
            f.write("- [Connessione Base](./basic-connection.md)\n")
            f.write("- [Gestione Configurazioni](./configuration-management.md)\n")
            f.write("- [Sequenze di Trigger](./trigger-sequences.md)\n")
            f.write("- [Gestione Errori](./error-handling.md)\n\n")
        
        # Esempi specifici
        basic_conn = os.path.join(self.examples_dir, "basic-connection.md")
        with open(basic_conn, 'w', encoding='utf-8') as f:
            f.write("# Connessione Base\n\n")
            f.write("## Connessione Semplice\n\n")
            f.write("```csharp\n")
            f.write("using Trgen;\n")
            f.write("using UnityEngine;\n\n")
            f.write("public class BasicConnection : MonoBehaviour\n")
            f.write("{\n")
            f.write("    private TrgenClient client;\n\n")
            f.write("    async void Start()\n")
            f.write("    {\n")
            f.write("        client = new TrgenClient();\n")
            f.write("        \n")
            f.write("        try\n")
            f.write("        {\n")
            f.write("            await client.ConnectAsync('192.168.1.100', 4000);\n")
            f.write("            Debug.Log('Connessione stabilita!');\n")
            f.write("            \n")
            f.write("            var impl = await client.RequestImplementationAsync();\n")
            f.write("            Debug.Log($'Pin GPIO: {impl.GpioNum}');\n")
            f.write("        }\n")
            f.write("        catch (System.Exception ex)\n")
            f.write("        {\n")
            f.write("            Debug.LogError($'Errore: {ex.Message}');\n")
            f.write("        }\n")
            f.write("    }\n")
            f.write("}\n")
            f.write("```\n\n")
        
        # Gestione configurazioni
        config_mgmt = os.path.join(self.examples_dir, "configuration-management.md")
        with open(config_mgmt, 'w', encoding='utf-8') as f:
            f.write("# Gestione Configurazioni\n\n")
            f.write("## Import Configurazione JSON\n\n")
            f.write("```csharp\n")
            f.write("using Trgen;\n")
            f.write("using UnityEngine;\n")
            f.write("using System.IO;\n\n")
            f.write("public class ConfigManager : MonoBehaviour\n")
            f.write("{\n")
            f.write("    async void LoadConfiguration()\n")
            f.write("    {\n")
            f.write("        var client = new TrgenClient();\n")
            f.write("        await client.ConnectAsync('192.168.1.100', 4000);\n")
            f.write("        \n")
            f.write("        string configPath = Path.Combine(\n")
            f.write("            Application.streamingAssetsPath, \n")
            f.write("            'trigger_config.json'\n")
            f.write("        );\n")
            f.write("        \n")
            f.write("        bool success = await client.ImportConfiguration(\n")
            f.write("            configPath,\n")
            f.write("            applyNetworkSettings: false,\n")
            f.write("            programPorts: true\n")
            f.write("        );\n")
            f.write("        \n")
            f.write("        if (success)\n")
            f.write("        {\n")
            f.write("            Debug.Log('Configurazione caricata!');\n")
            f.write("        }\n")
            f.write("    }\n")
            f.write("}\n")
            f.write("```\n\n")
        
        # Sequenze trigger
        trigger_seq = os.path.join(self.examples_dir, "trigger-sequences.md")
        with open(trigger_seq, 'w', encoding='utf-8') as f:
            f.write("# Sequenze di Trigger\n\n")
            f.write("## Trigger Multipli\n\n")
            f.write("```csharp\n")
            f.write("using Trgen;\n")
            f.write("using UnityEngine;\n")
            f.write("using System.Threading.Tasks;\n\n")
            f.write("public class TriggerSequence : MonoBehaviour\n")
            f.write("{\n")
            f.write("    private TrgenClient client;\n\n")
            f.write("    async void Start()\n")
            f.write("    {\n")
            f.write("        client = new TrgenClient();\n")
            f.write("        await client.ConnectAsync('192.168.1.100', 4000);\n")
            f.write("        \n")
            f.write("        await SendSequence();\n")
            f.write("    }\n\n")
            f.write("    private async Task SendSequence()\n")
            f.write("    {\n")
            f.write("        // Trigger iniziale\n")
            f.write("        await client.StartTriggerAsync(TrgenPin.NS0);\n")
            f.write("        await Task.Delay(100);\n")
            f.write("        \n")
            f.write("        // Trigger intermedio\n")
            f.write("        await client.StartTriggerAsync(TrgenPin.GPIO0);\n")
            f.write("        await Task.Delay(200);\n")
            f.write("        \n")
            f.write("        // Trigger finale\n")
            f.write("        await client.StartTriggerAsync(TrgenPin.NS1);\n")
            f.write("        \n")
            f.write("        Debug.Log('Sequenza completata!');\n")
            f.write("    }\n")
            f.write("}\n")
            f.write("```\n\n")
        
        # Gestione errori
        error_handling = os.path.join(self.examples_dir, "error-handling.md")
        with open(error_handling, 'w', encoding='utf-8') as f:
            f.write("# Gestione Errori\n\n")
            f.write("## Try-Catch Avanzato\n\n")
            f.write("```csharp\n")
            f.write("using Trgen;\n")
            f.write("using UnityEngine;\n")
            f.write("using System;\n\n")
            f.write("public class ErrorHandling : MonoBehaviour\n")
            f.write("{\n")
            f.write("    async void SafeConnection()\n")
            f.write("    {\n")
            f.write("        var client = new TrgenClient();\n")
            f.write("        \n")
            f.write("        try\n")
            f.write("        {\n")
            f.write("            await client.ConnectAsync('192.168.1.100', 4000);\n")
            f.write("            Debug.Log('Connesso con successo!');\n")
            f.write("        }\n")
            f.write("        catch (TimeoutException ex)\n")
            f.write("        {\n")
            f.write("            Debug.LogError($'Timeout: {ex.Message}');\n")
            f.write("        }\n")
            f.write("        catch (Exception ex)\n")
            f.write("        {\n")
            f.write("            Debug.LogError($'Errore generico: {ex.Message}');\n")
            f.write("        }\n")
            f.write("        finally\n")
            f.write("        {\n")
            f.write("            client?.Dispose();\n")
            f.write("        }\n")
            f.write("    }\n")
            f.write("}\n")
            f.write("```\n\n")

def main():
    """Main entry point"""
    parser = argparse.ArgumentParser(description='Legacy .NET Documentation Generator')
    parser.add_argument('--source', default='Runtime', help='Source directory (default: Runtime)')
    parser.add_argument('--output', default='docs-site', help='Output directory (default: docs-site)')
    
    args = parser.parse_args()
    
    generator = LegacyDocGenerator(args.source, args.output)
    generator.generate_docs()

if __name__ == "__main__":
    main()