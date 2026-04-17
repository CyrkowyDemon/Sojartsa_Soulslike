import os
import xml.etree.ElementTree as ET

csproj_path = r'c:\Users\Circu\Desktop\Sojartsa\Sojartsa\Assembly-CSharp.csproj'
project_root = r'c:\Users\Circu\Desktop\Sojartsa\Sojartsa'

if not os.path.exists(csproj_path):
    print(f"File not found: {csproj_path}")
    exit(1)

# Register namespaces to preserve them
ET.register_namespace('', 'http://schemas.microsoft.com/developer/msbuild/2003')
tree = ET.parse(csproj_path)
root = tree.getroot()
ns = {'ns': 'http://schemas.microsoft.com/developer/msbuild/2003'}

# Get current compiled files
current_files = set()
for item in root.findall('.//ns:Compile', ns):
    include = item.get('Include')
    if include:
        current_files.add(include.lower())

# Find all .cs files in Assets/Scripts
scripts_dir = os.path.join(project_root, 'Assets', 'Scripts')
added_count = 0

# Find a place to insert new Compile items
item_group = root.find('.//ns:ItemGroup', ns) # Find the first ItemGroup with Compiles
if item_group is None:
    item_group = ET.SubElement(root, 'ItemGroup')

for dirpath, dirnames, filenames in os.walk(scripts_dir):
    for filename in filenames:
        if filename.endswith('.cs'):
            rel_path = os.path.relpath(os.path.join(dirpath, filename), project_root)
            if rel_path.lower() not in current_files:
                compile_item = ET.SubElement(item_group, 'Compile')
                compile_item.set('Include', rel_path)
                current_files.add(rel_path.lower())
                added_count += 1
                print(f"Added to csproj: {rel_path}")

if added_count > 0:
    tree.write(csproj_path, encoding='utf-8', xml_declaration=True)
    print(f"Total files added to csproj: {added_count}")
else:
    print("No new files to add to csproj.")
