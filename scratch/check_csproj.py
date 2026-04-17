import os
import xml.etree.ElementTree as ET

csproj_path = r'c:\Users\Circu\Desktop\Sojartsa\Sojartsa\Assembly-CSharp.csproj'
project_root = r'c:\Users\Circu\Desktop\Sojartsa\Sojartsa'

if not os.path.exists(csproj_path):
    print(f"File not found: {csproj_path}")
    exit(1)

tree = ET.parse(csproj_path)
root = tree.getroot()

# Namespaces in csproj
ns = {'ns': 'http://schemas.microsoft.com/developer/msbuild/2003'}

missing_files = []
for compile_item in root.findall('.//ns:Compile', ns):
    file_path = compile_item.get('Include')
    if file_path:
        full_path = os.path.join(project_root, file_path)
        if not os.path.exists(full_path):
            missing_files.append(file_path)

if missing_files:
    print("Missing files in csproj:")
    for f in missing_files:
        print(f" - {f}")
else:
    print("No missing files found in csproj.")
