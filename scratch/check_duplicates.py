import os
import re
from collections import defaultdict

scripts_dir = r'c:\Users\Circu\Desktop\Sojartsa\Sojartsa\Assets'
classes = defaultdict(list)

pattern = re.compile(r'(?:public|internal|private|protected)?\s+(?:class|interface|struct|enum)\s+(\w+)')

for root, dirs, files in os.walk(scripts_dir):
    for file in files:
        if file.endswith('.cs'):
            path = os.path.join(root, file)
            try:
                with open(path, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                    matches = pattern.findall(content)
                    for m in matches:
                        classes[m].append(path)
            except:
                pass

duplicates = {name: paths for name, paths in classes.items() if len(paths) > 1}

if duplicates:
    print("Found duplicate definitions:")
    for name, paths in duplicates.items():
        # Filter out common names that might be false positives or legit duplicates (like nested classes, though simple regex picks them up as same)
        if len(set(paths)) > 1:
            print(f" - {name}:")
            for p in set(paths):
                print(f"   * {p}")
else:
    print("No obvious duplicate global-level definitions found.")
