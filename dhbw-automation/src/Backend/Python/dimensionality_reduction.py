"""
Dimensionality Reduction Service for Vector Embeddings
Reduces high-dimensional vectors (1536D) to 2D/3D for visualization
"""

import numpy as np
from sklearn.manifold import TSNE
from sklearn.decomposition import PCA
import umap
import json
import sys


def reduce_dimensions(vectors, method='umap', n_components=2, random_state=42):
    """
    Reduce dimensionality of vectors
    
    Args:
        vectors: numpy array of shape (n_samples, n_features)
        method: 'umap', 'tsne', or 'pca'
        n_components: 2 or 3
        random_state: random seed for reproducibility
    
    Returns:
        numpy array of shape (n_samples, n_components)
    """
    vectors = np.array(vectors)
    
    if method == 'umap':
        reducer = umap.UMAP(
            n_components=n_components,
            random_state=random_state,
            n_neighbors=15,
            min_dist=0.1,
            metric='cosine'
        )
    elif method == 'tsne':
        reducer = TSNE(
            n_components=n_components,
            random_state=random_state,
            perplexity=30,
            n_iter=1000,
            metric='cosine'
        )
    elif method == 'pca':
        reducer = PCA(
            n_components=n_components,
            random_state=random_state
        )
    else:
        raise ValueError(f"Unknown method: {method}")
    
    reduced = reducer.fit_transform(vectors)
    return reduced


def main():
    """
    Read JSON from stdin, perform dimensionality reduction, write JSON to stdout
    
    Expected input format:
    {
        "vectors": [[...], [...], ...],
        "method": "umap",
        "n_components": 2
    }
    
    Output format:
    {
        "reduced_vectors": [[x, y], [x, y], ...],
        "method": "umap"
    }
    """
    try:
        # Read input from stdin
        input_data = json.load(sys.stdin)
        
        vectors = input_data.get('vectors', [])
        method = input_data.get('method', 'umap')
        n_components = input_data.get('n_components', 2)
        
        if not vectors:
            raise ValueError("No vectors provided")
        
        # Perform dimensionality reduction
        reduced = reduce_dimensions(vectors, method, n_components)
        
        # Convert to list for JSON serialization
        reduced_list = reduced.tolist()
        
        # Write output to stdout
        output = {
            'success': True,
            'reduced_vectors': reduced_list,
            'method': method,
            'n_components': n_components,
            'n_samples': len(reduced_list)
        }
        
        json.dump(output, sys.stdout)
        
    except Exception as e:
        # Write error to stdout as JSON
        error_output = {
            'success': False,
            'error': str(e)
        }
        json.dump(error_output, sys.stdout)
        sys.exit(1)


if __name__ == '__main__':
    main()
