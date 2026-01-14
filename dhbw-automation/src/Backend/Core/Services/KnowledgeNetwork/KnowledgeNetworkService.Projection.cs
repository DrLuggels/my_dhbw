using DHBWAutomation.Backend.Core.Models;
using DHBWAutomation.Backend.Infrastructure.VectorDb;

namespace DHBWAutomation.Backend.Core.Services.KnowledgeNetwork;

public partial class KnowledgeNetworkService
{
    /// <summary>
    /// Project high-dimensional vectors to 2D using simple PCA approximation
    /// </summary>
    private List<ClusterPoint> ProjectTo2D(List<PointWithVector> points)
    {
        if (points.Count == 0) return new List<ClusterPoint>();

        var vectorDim = points[0].Vector.Length;
        var n = points.Count;

        var mean = CalculateMeanVector(points, vectorDim, n);
        var centered = CenterVectors(points, mean, vectorDim);
        var (projMatrix1, projMatrix2) = CreateProjectionMatrices(vectorDim);

        var result = ProjectPoints(points, centered, projMatrix1, projMatrix2, vectorDim, n);
        NormalizeCoordinates(result);

        return result;
    }

    private float[] CalculateMeanVector(List<PointWithVector> points, int vectorDim, int n)
    {
        var mean = new float[vectorDim];
        foreach (var point in points)
        {
            for (int i = 0; i < vectorDim; i++)
            {
                mean[i] += point.Vector[i] / n;
            }
        }
        return mean;
    }

    private float[][] CenterVectors(List<PointWithVector> points, float[] mean, int vectorDim)
    {
        return points.Select(p =>
        {
            var c = new float[vectorDim];
            for (int i = 0; i < vectorDim; i++)
            {
                c[i] = p.Vector[i] - mean[i];
            }
            return c;
        }).ToArray();
    }

    private (float[], float[]) CreateProjectionMatrices(int vectorDim)
    {
        var random = new Random(42); // Fixed seed for reproducibility
        var projMatrix1 = Enumerable.Range(0, vectorDim)
            .Select(_ => (float)(random.NextDouble() * 2 - 1)).ToArray();
        var projMatrix2 = Enumerable.Range(0, vectorDim)
            .Select(_ => (float)(random.NextDouble() * 2 - 1)).ToArray();

        // Normalize projection vectors
        var norm1 = (float)Math.Sqrt(projMatrix1.Sum(x => x * x));
        var norm2 = (float)Math.Sqrt(projMatrix2.Sum(x => x * x));
        for (int i = 0; i < vectorDim; i++)
        {
            projMatrix1[i] /= norm1;
            projMatrix2[i] /= norm2;
        }

        return (projMatrix1, projMatrix2);
    }

    private List<ClusterPoint> ProjectPoints(
        List<PointWithVector> points,
        float[][] centered,
        float[] projMatrix1,
        float[] projMatrix2,
        int vectorDim,
        int n)
    {
        var result = new List<ClusterPoint>();
        for (int i = 0; i < n; i++)
        {
            float x = 0, y = 0;
            for (int j = 0; j < vectorDim; j++)
            {
                x += centered[i][j] * projMatrix1[j];
                y += centered[i][j] * projMatrix2[j];
            }

            result.Add(new ClusterPoint
            {
                X = x,
                Y = y,
                EntityType = MapEntityTypeForFrontend(points[i].EntityType),
                EntityId = points[i].EntityId,
                Label = points[i].Topic ?? points[i].Filename ?? $"{points[i].EntityType}:{points[i].EntityId}",
                Category = DetermineCategory(points[i].EntityType)
            });
        }
        return result;
    }

    private void NormalizeCoordinates(List<ClusterPoint> result)
    {
        if (result.Count == 0) return;

        var minX = result.Min(p => p.X);
        var maxX = result.Max(p => p.X);
        var minY = result.Min(p => p.Y);
        var maxY = result.Max(p => p.Y);
        var rangeX = maxX - minX;
        var rangeY = maxY - minY;

        foreach (var point in result)
        {
            point.X = rangeX > 0 ? 2 * (point.X - minX) / rangeX - 1 : 0;
            point.Y = rangeY > 0 ? 2 * (point.Y - minY) / rangeY - 1 : 0;
        }
    }

    private string DetermineCategory(string entityType)
    {
        return entityType switch
        {
            KnowledgeEntityTypes.Document => "Documents",
            KnowledgeEntityTypes.DocumentChunk => "Chunks",
            KnowledgeEntityTypes.JavaDocsExercise => "Exercises",
            KnowledgeEntityTypes.KnowledgeItem => "Knowledge",
            KnowledgeEntityTypes.Image => "Images",
            _ => "Other"
        };
    }

    private string MapEntityTypeForFrontend(string entityType)
    {
        return entityType switch
        {
            KnowledgeEntityTypes.Document => "Document",
            KnowledgeEntityTypes.DocumentChunk => "Document", // Map chunks to Document for frontend
            KnowledgeEntityTypes.JavaDocsExercise => "JavaDocsExercise",
            KnowledgeEntityTypes.KnowledgeItem => "KnowledgeItem",
            KnowledgeEntityTypes.Image => "Image",
            KnowledgeEntityTypes.MoodleResource => "MoodleResource",
            _ => entityType
        };
    }
}
