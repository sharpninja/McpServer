import os

# Read the MarkerFileService.cs file
with open('src/McpServer.Support.Mcp/Services/MarkerFileService.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Extract lines 28 through 593 (0-indexed: 27-592) - the template content
# Line 27 is opening """, line 594 is closing """;
template_lines = []
for i in range(27, len(lines)):
    line = lines[i].rstrip('\n').rstrip('\r')
    if line.strip() == '""";':
        break
    # Remove the 8-space prefix from each line
    if line.startswith('        '):
        template_lines.append(line[8:])
    elif line.strip() == '':
        template_lines.append('')
    else:
        template_lines.append(line)

template = '\n'.join(template_lines)

# Write as YAML with block scalar
yaml_lines = ['template: |']
for line in template.split('\n'):
    if line.strip() == '':
        yaml_lines.append('')
    else:
        yaml_lines.append('  ' + line)

yaml_content = '\n'.join(yaml_lines) + '\n'

os.makedirs('templates', exist_ok=True)
with open('templates/default-marker-prompt.hbs.yaml', 'w', encoding='utf-8', newline='\n') as f:
    f.write(yaml_content)

print(f'Wrote {len(yaml_lines)} lines to templates/default-marker-prompt.hbs.yaml')
