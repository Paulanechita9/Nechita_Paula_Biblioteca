
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Intalnirea1.Services;

public class GenreInput
{
    [LoadColumn(0)]
    public string Description { get; set; } = "";

    [LoadColumn(1)]
    public string Genre { get; set; } = "";
}

public class GenrePrediction
{
    [ColumnName("PredictedGenre")]
    public string Genre { get; set; } = "";
}

public class GenreClassifier
{
    private readonly IWebHostEnvironment _env;

    public GenreClassifier(IWebHostEnvironment env)
    {
        _env = env;
    }

    public (double Accuracy, double MicroAccuracy) Train()
    {
        var ml = new MLContext(seed: 1);

        var csvPath = Path.Combine(
            _env.ContentRootPath,
            "DataML",
            "book-descriptions.csv");

        var data = ml.Data.LoadFromTextFile<GenreInput>(
            csvPath,
            hasHeader: true,
            separatorChar: ',',
            allowQuoting: true);

        var split = ml.Data.TrainTestSplit(
            data, testFraction: 0.2);

        var pipeline = ml.Transforms.Conversion
            .MapValueToKey("Label", nameof(GenreInput.Genre))
            .Append(ml.Transforms.Text.FeaturizeText(
                "Features", nameof(GenreInput.Description)))
            .Append(ml.MulticlassClassification.Trainers
                .SdcaMaximumEntropy("Label", "Features"))
            .Append(ml.Transforms.Conversion.MapKeyToValue(
                "PredictedGenre", "PredictedLabel"));

        var model = pipeline.Fit(split.TrainSet);

        var predictions = model.Transform(split.TestSet);
        var metrics = ml.MulticlassClassification.Evaluate(
            predictions, labelColumnName: "Label");

        var folder = Path.Combine(
            _env.ContentRootPath, "MLModels");

        Directory.CreateDirectory(folder);

        var modelPath = Path.Combine(
            folder, "GenreClassifier.zip");

        ml.Model.Save(model, data.Schema, modelPath);

        return (metrics.MacroAccuracy, metrics.MicroAccuracy);
    }

    public string Predict(string description)
    {
        var ml = new MLContext();

        var modelPath = Path.Combine(
            _env.ContentRootPath,
            "MLModels",
            "GenreClassifier.zip");

        if (!File.Exists(modelPath))
            return "Modelul nu este antrenat.";

        var model = ml.Model.Load(modelPath, out _);

        using var engine = ml.Model.CreatePredictionEngine<
            GenreInput, GenrePrediction>(model);

        var result = engine.Predict(new GenreInput
        {
            Description = description
        });

        return result.Genre;
    }
}
