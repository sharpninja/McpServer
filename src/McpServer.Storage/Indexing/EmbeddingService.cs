using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace McpServer.Support.Mcp.Indexing;

/// <summary>
/// TR-PLANNED-CORE-013: ONNX embedding service using all-MiniLM-L6-v2 for vector search.
/// FR-SUPPORT-010: Generates 384-dimensional embeddings with mean pooling and L2 normalization.
/// Gracefully degrades to stub mode when model is unavailable (CI/CD, first run before download).
/// </summary>
public sealed class EmbeddingService : IEmbeddingService, IDisposable
{
    private const string ModelUrl = "https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/onnx/model.onnx";
    private const string VocabUrl = "https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/vocab.txt";

    private readonly ILogger<EmbeddingService> _logger;
    private readonly EmbeddingOptions _options;
    private readonly object _lock = new();
    private InferenceSession? _session;
    private WordPieceTokenizer? _tokenizer;
    private bool _disposed;

    /// <summary>TR-PLANNED-CORE-013: Constructor with configuration and optional model loading.</summary>
    public EmbeddingService(IOptions<EmbeddingOptions> options, ILogger<EmbeddingService> logger)
    {
        _options = options?.Value ?? new EmbeddingOptions();
        _logger = logger;
        TryLoadModel();
    }

    /// <summary>TR-PLANNED-CORE-013: Constructor for testing without DI options.</summary>
    internal EmbeddingService(EmbeddingOptions options, ILogger<EmbeddingService> logger)
    {
        _options = options ?? new EmbeddingOptions();
        _logger = logger;
        TryLoadModel();
    }

    /// <inheritdoc />
    public int Dimensions => _options.Dimensions;

    /// <inheritdoc />
    public bool IsAvailable => _session is not null && _tokenizer is not null;

    /// <inheritdoc />
    public float[] GenerateEmbedding(string text)
    {
        if (!IsAvailable)
            return new float[Dimensions];

        lock (_lock)
        {
            return GenerateEmbeddingCore(text);
        }
    }

    /// <inheritdoc />
    public ReadOnlyMemory<float>[] GenerateEmbeddings(IReadOnlyList<string> texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        var results = new ReadOnlyMemory<float>[texts.Count];
        if (!IsAvailable)
        {
            for (var i = 0; i < texts.Count; i++)
                results[i] = new float[Dimensions];
            return results;
        }

        lock (_lock)
        {
            for (var i = 0; i < texts.Count; i++)
                results[i] = GenerateEmbeddingCore(texts[i]);
        }
        return results;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session?.Dispose();
        _session = null;
    }

    /// <summary>TR-PLANNED-CORE-013: Attempt to download the model if AutoDownload is enabled.</summary>
    internal async Task TryDownloadModelAsync(CancellationToken ct = default)
    {
        if (IsAvailable) return;
        if (!_options.AutoDownload) return;

        var modelDir = GetModelDirectory();
        var modelPath = Path.Combine(modelDir, "model.onnx");
        var vocabPath = Path.Combine(modelDir, "vocab.txt");

        if (File.Exists(modelPath) && File.Exists(vocabPath))
        {
            TryLoadModel();
            return;
        }

        try
        {
            Directory.CreateDirectory(modelDir);
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

            if (!File.Exists(modelPath))
            {
                _logger.LogInformation("Downloading ONNX model from {Url}...", ModelUrl);
                var modelBytes = await http.GetByteArrayAsync(new Uri(ModelUrl), ct).ConfigureAwait(false);
                await File.WriteAllBytesAsync(modelPath, modelBytes, ct).ConfigureAwait(false);
                _logger.LogInformation("ONNX model downloaded ({Size:N0} bytes)", modelBytes.Length);
            }

            if (!File.Exists(vocabPath))
            {
                _logger.LogInformation("Downloading vocab.txt from {Url}...", VocabUrl);
                var vocabText = await http.GetStringAsync(new Uri(VocabUrl), ct).ConfigureAwait(false);
                await File.WriteAllTextAsync(vocabPath, vocabText, ct).ConfigureAwait(false);
                _logger.LogInformation("vocab.txt downloaded");
            }

            TryLoadModel();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to download ONNX model; embedding service will remain in stub mode");
        }
    }

    private void TryLoadModel()
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("EmbeddingService: disabled via configuration, running in stub mode.");
            return;
        }

        var modelPath = ResolveModelPath();
        var vocabPath = ResolveVocabPath();

        if (!File.Exists(modelPath) || !File.Exists(vocabPath))
        {
            _logger.LogInformation("EmbeddingService: model or vocab not found, running in stub mode. Model={Model}, Vocab={Vocab}", modelPath, vocabPath);
            return;
        }

        try
        {
            using var sessionOptions = new Microsoft.ML.OnnxRuntime.SessionOptions
            {
                IntraOpNumThreads = 2,
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };
            _session = new InferenceSession(modelPath, sessionOptions);
            _tokenizer = new WordPieceTokenizer(vocabPath, _options.MaxSequenceLength);
            _logger.LogInformation("EmbeddingService: ONNX model loaded from {Path}, IsAvailable=true", modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load ONNX model; embedding service will remain in stub mode");
            _session?.Dispose();
            _session = null;
            _tokenizer = null;
        }
    }

    private float[] GenerateEmbeddingCore(string text)
    {
        var (inputIds, attentionMask, tokenTypeIds) = _tokenizer!.Tokenize(text ?? string.Empty);
        var seqLen = inputIds.Length;

        var inputIdsTensor = new DenseTensor<long>(inputIds.Select(x => (long)x).ToArray(), [1, seqLen]);
        var attentionMaskTensor = new DenseTensor<long>(attentionMask.Select(x => (long)x).ToArray(), [1, seqLen]);
        var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds.Select(x => (long)x).ToArray(), [1, seqLen]);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
        };

        using var results = _session!.Run(inputs);
        var output = results[0];
        var lastHiddenState = output.AsTensor<float>();

        // Mean pooling weighted by attention mask
        var embedding = new float[Dimensions];
        var maskSum = 0f;
        for (var t = 0; t < seqLen; t++)
        {
            var mask = attentionMask[t];
            maskSum += mask;
            for (var d = 0; d < Dimensions; d++)
                embedding[d] += lastHiddenState[0, t, d] * mask;
        }

        if (maskSum > 0)
        {
            for (var d = 0; d < Dimensions; d++)
                embedding[d] /= maskSum;
        }

        // L2 normalize
        var norm = 0f;
        for (var d = 0; d < Dimensions; d++)
            norm += embedding[d] * embedding[d];
        norm = MathF.Sqrt(norm);
        if (norm > 1e-10f)
        {
            for (var d = 0; d < Dimensions; d++)
                embedding[d] /= norm;
        }

        return embedding;
    }

    private static string GetModelDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "McpServer.Support.Mcp", "models");

    private string ResolveModelPath() =>
        _options.ModelPath ?? Path.Combine(GetModelDirectory(), "model.onnx");

    private string ResolveVocabPath()
    {
        var dir = _options.ModelPath is not null ? Path.GetDirectoryName(_options.ModelPath)! : GetModelDirectory();
        return Path.Combine(dir, "vocab.txt");
    }
}

/// <summary>
/// TR-PLANNED-CORE-013: Simple WordPiece tokenizer for BERT-compatible models.
/// Loads vocab.txt and tokenizes input into input_ids, attention_mask, token_type_ids.
/// </summary>
internal sealed class WordPieceTokenizer
{
    private readonly Dictionary<string, int> _vocab;
    private readonly int _maxSeqLength;
    private readonly int _clsId;
    private readonly int _sepId;
    private readonly int _unkId;
    private readonly int _padId;

    public WordPieceTokenizer(string vocabPath, int maxSeqLength)
    {
        _maxSeqLength = maxSeqLength;
        _vocab = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lines = File.ReadAllLines(vocabPath);
        for (var i = 0; i < lines.Length; i++)
            _vocab[lines[i]] = i;

        _clsId = _vocab.GetValueOrDefault("[CLS]", 101);
        _sepId = _vocab.GetValueOrDefault("[SEP]", 102);
        _unkId = _vocab.GetValueOrDefault("[UNK]", 100);
        _padId = _vocab.GetValueOrDefault("[PAD]", 0);
    }

    public (int[] InputIds, int[] AttentionMask, int[] TokenTypeIds) Tokenize(string text)
    {
        var tokens = new List<int> { _clsId };
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (tokens.Count >= _maxSeqLength - 1)
                break;

            var subTokens = WordPieceTokenize(word);
            foreach (var subToken in subTokens)
            {
                if (tokens.Count >= _maxSeqLength - 1)
                    break;
                tokens.Add(subToken);
            }
        }

        tokens.Add(_sepId);

        var seqLen = tokens.Count;
        var inputIds = tokens.ToArray();
        var attentionMask = Enumerable.Repeat(1, seqLen).ToArray();
        var tokenTypeIds = new int[seqLen];

        return (inputIds, attentionMask, tokenTypeIds);
    }

    private List<int> WordPieceTokenize(string word)
    {
        var result = new List<int>();
        var start = 0;

        while (start < word.Length)
        {
            var found = false;
            for (var end = word.Length; end > start; end--)
            {
                var substr = start == 0
                    ? word[start..end]
                    : "##" + word[start..end];

                if (_vocab.TryGetValue(substr, out var id))
                {
                    result.Add(id);
                    start = end;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                result.Add(_unkId);
                start++;
            }
        }

        return result;
    }
}
