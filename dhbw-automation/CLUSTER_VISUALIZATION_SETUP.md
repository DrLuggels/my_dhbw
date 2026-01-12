# Cluster-Visualisierung Setup

Die 2D Cluster-Visualisierung wurde erfolgreich zum Wissensnetzwerk hinzugefügt!

## Was wurde implementiert:

### Backend (C#):
1. **API Endpoint**: `/api/knowledgenetwork/clusters`
   - Unterstützt UMAP, t-SNE und PCA
   - Parameter: `method` (umap/tsne/pca), `maxNodes` (default: 200)

2. **Python Service**: `src/Backend/Python/dimensionality_reduction.py`
   - Reduziert 1536D Embeddings auf 2D
   - Verwendet sklearn, umap-learn

### Frontend (Vue):
1. **ClusterVisualization Komponente**: 
   - Interaktive 2D Scatter-Plots mit Plotly.js
   - Farbcodierung nach Entity-Typ
   - Zoom, Pan, Hover-Tooltips
   
2. **Integration in KnowledgeNetworkView**:
   - Neuer Tab "Cluster" neben "Graph" und "List"
   - Auto-Load beim Tab-Wechsel
   - Method-Switcher (UMAP/t-SNE/PCA)

## Installation:

### 1. Python Dependencies:
```bash
cd dhbw-automation/src/Backend/Python
pip install -r requirements.txt
```

Oder mit conda:
```bash
conda install numpy scikit-learn
pip install umap-learn
```

### 2. Frontend Dependencies:
```bash
cd dhbw-automation/src/Frontend
npm install plotly.js-dist-min
```

Oder:
```bash
npm install
```

## Verwendung:

1. **Öffne Wissensnetzwerk**: `/knowledge-network`
2. **Wechsel zum Cluster-Tab**: Klicke auf das Scatter-Plot Icon
3. **Wähle Methode**: UMAP (empfohlen), t-SNE oder PCA
4. **Interaktion**:
   - Klicke auf Punkte für Details
   - Zoom/Pan mit Maus
   - Hover für Tooltips

## Unterschiede der Methoden:

- **UMAP** (empfohlen): 
  - Schnell (~2-5 Sek)
  - Erhält globale + lokale Struktur
  - Beste Cluster-Separation

- **t-SNE**: 
  - Langsamer (~5-15 Sek)
  - Sehr gute lokale Cluster
  - Kann globale Struktur verfälschen

- **PCA**: 
  - Sehr schnell (~1 Sek)
  - Linear, einfach
  - Schlechtere Cluster-Separation

## Nächste Schritte (Optional):

- [ ] Python-Service als API-Endpoint (statt Stdin/Stdout)
- [ ] Caching der reduzierten Vektoren
- [ ] 3D-Modus (optional toggle)
- [ ] Cluster-Labels (automatische Kategorisierung)
- [ ] Export als Bild (PNG/SVG)

## Fehlerbehebung:

**"Python-Module nicht gefunden"**:
```bash
pip install numpy scikit-learn umap-learn
```

**"Cluster-Daten nicht verfügbar"**:
- Stelle sicher, dass Embeddings existieren (Index erstellen)
- Minimum 3 Dokumente mit Embeddings erforderlich

**"plotly.js Error"**:
```bash
npm install --save plotly.js-dist-min
```
