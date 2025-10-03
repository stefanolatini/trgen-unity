import re

with open('README.md', 'r') as f:
    content = f.read()

# Pattern per trovare i blocchi mermaid
mermaid_pattern = r'```mermaid\n(.*?)\n```'

def replace_mermaid(match):
    mermaid_code = match.group(1)
    # Converti in direttiva RST con indentazione corretta
    lines = mermaid_code.split('\n')
    indented_lines = ['   ' + line for line in lines if line.strip()]
    return '.. mermaid::\n\n' + '\n'.join(indented_lines)

# Sostituisci i blocchi mermaid
processed_content = re.sub(mermaid_pattern, replace_mermaid, content, flags=re.DOTALL)

# Salva il file processato
with open('README_processed.md', 'w') as f:
    f.write(processed_content)