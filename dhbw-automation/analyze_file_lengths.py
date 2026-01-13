import os
from pathlib import Path
import json

def count_lines(file_path):
    """Count lines in a file."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            return len(f.readlines())
    except Exception as e:
        print(f"Error reading {file_path}: {e}")
        return 0

def analyze_files():
    """Analyze all source files in Frontend and Backend."""
    base_path = Path(__file__).parent / "src"
    
    extensions = {'.cs', '.ts', '.tsx', '.js', '.jsx', '.vue', '.py'}
    exclude_patterns = ['node_modules', 'bin', 'obj', 'dist', 'build', 'Migrations', '.Designer.cs']
    
    file_stats = []
    
    for folder in ['Backend', 'Frontend']:
        folder_path = base_path / folder
        if not folder_path.exists():
            continue
            
        for root, dirs, files in os.walk(folder_path):
            # Skip excluded directories
            dirs[:] = [d for d in dirs if not any(pattern in d for pattern in exclude_patterns)]
            
            for file in files:
                file_path = Path(root) / file
                
                # Check if file has relevant extension
                if file_path.suffix in extensions:
                    # Skip Designer and Migration files
                    if any(pattern in str(file_path) for pattern in exclude_patterns):
                        continue
                    
                    line_count = count_lines(file_path)
                    if line_count > 0:
                        rel_path = file_path.relative_to(base_path)
                        file_stats.append({
                            'path': str(rel_path),
                            'full_path': str(file_path),
                            'lines': line_count,
                            'type': folder
                        })
    
    # Sort by line count descending
    file_stats.sort(key=lambda x: x['lines'], reverse=True)
    
    # Print top 20
    print("\n=== TOP 20 LONGEST FILES ===\n")
    for i, stat in enumerate(file_stats[:20], 1):
        print(f"{i:2d}. {stat['lines']:5d} lines - {stat['path']}")
    
    # Save to JSON
    output_file = Path(__file__).parent / "file_analysis.json"
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(file_stats[:20], f, indent=2)
    
    print(f"\n✓ Analysis saved to {output_file}")
    print(f"\nTotal files analyzed: {len(file_stats)}")
    print(f"Backend files: {len([f for f in file_stats if f['type'] == 'Backend'])}")
    print(f"Frontend files: {len([f for f in file_stats if f['type'] == 'Frontend'])}")

if __name__ == "__main__":
    analyze_files()
