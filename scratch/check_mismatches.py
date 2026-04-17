import os
import re

scripts_dir = r'c:\Users\Circu\Desktop\Sojartsa\Sojartsa\Assets\Scripts'

mismatches = []

for root, dirs, files in os.walk(scripts_dir):
    for file in files:
        if file.endswith('.cs'):
            file_path = os.path.join(root, file)
            expected_class = os.path.splitext(file)[0]
            
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
                # Simple regex to find class/interface/struct definitions
                # Matches "public class Name", "internal class Name", etc.
                match = re.search(r'(?:public|internal|private|protected)?\s+(?:class|interface|struct)\s+(\w+)', content)
                if match:
                    actual_class = match.group(1)
                    if actual_class != expected_class:
                        # Skip special cases if needed, but in Unity MonoBehaviours it MUST match
                        # Check if it inherits from MonoBehaviour
                        if 'MonoBehaviour' in content or 'ScriptableObject' in content:
                            mismatches.append((file, actual_class))

if mismatches:
    print("Class name mismatches found:")
    for file, actual in mismatches:
        print(f" - File: {file}, Class: {actual}")
else:
    print("No class name mismatches found.")
